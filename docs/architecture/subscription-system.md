# Subscription System

## Purpose

Monetize the platform by limiting which **modules** each guild may enable based on their **subscription plan**.

Subscriptions are **per guild**, not per user.

## Entities

### SubscriptionPlan (catalog)

**File:** `src/DiscordBot.Domain/Entities/SubscriptionPlan.cs`  
**Table:** `SubscriptionPlans`

| Field | Purpose |
|-------|---------|
| `Key` | Stable id: free, basic, pro, premium |
| `Name`, `Description` | Display |
| `AllowedModulesJson` | JSON array of module keys or `"*"` for all |
| `MonthlyPrice` | Decimal price (display/billing reference) |
| `IsActive` | Available for new subscriptions |

### GuildSubscription (per guild)

**File:** `src/DiscordBot.Domain/Entities/GuildSubscription.cs`  
**Table:** `GuildSubscriptions`

| Field | Purpose |
|-------|---------|
| `GuildId` | Unique — one subscription per guild |
| `PlanId` | FK to SubscriptionPlan |
| `Status` | Active, Expired, Cancelled |
| `StartedAt`, `ExpiresAt` | Subscription window |
| `ApprovedRequestId` | Optional FK to PlanUpgradeRequest |

### PlanUpgradeRequest

**File:** `src/DiscordBot.Domain/Entities/PlanUpgradeRequest.cs`  
**Table:** `PlanUpgradeRequests`  
**Workflow rules:** `src/DiscordBot.Domain/SubscriptionBilling/PlanUpgradeRequestWorkflow.cs`

Manual subscription change workflow (SB-002 foundation + SB-003 payment reference):

1. Owner creates **subscription change** request (upgrade or renewal + duration months) → `Requested` → `PendingPayment`
2. `ChangeType`: `Upgrade` (different plan) or `Renewal` (same paid plan)
3. Price and plan snapshots stored on the request (`CurrentPlanId`, `RequestedPlanId`, `RequestedPlanMonthlyPrice`, `EstimatedTotalAmount`)
4. `RequestExpiresAt` set on create (default 14 days); lazy expiry on read (no worker yet)
5. Owner submits **payment reference** (text only, no file upload) from `PendingPayment` → `PaymentSubmitted` → `UnderReview`
6. Admin approves from `UnderReview` or `PaymentSubmitted` → `Approved` → `Activated` (subscription updated)
7. Admin may approve from `PendingPayment` only with `AdminOverrideReason` (bypass payment proof)
8. Admin rejects from reviewable states → `Rejected`
9. Owner or admin can cancel in-flight requests → `Cancelled`

**Status enum** (`PlanUpgradeRequestStatus`):

| Value | Meaning |
|-------|---------|
| `Requested` | Created (transient — moves to PendingPayment immediately) |
| `PendingPayment` | Awaiting off-platform payment (legacy `Pending` migrates here) |
| `PaymentSubmitted` | Payment reference submitted by owner |
| `UnderReview` | Admin reviewing |
| `Approved` | Admin approved (transient — moves to Activated) |
| `Activated` | Subscription activated (terminal; legacy `Approved` migrates here) |
| `Rejected` | Admin rejected |
| `Cancelled` | Owner or admin cancelled |
| `Expired` | Request expired without payment |

| Field | Purpose |
|-------|---------|
| `ChangeType` | `Upgrade` or `Renewal` |
| `GuildId`, `RequestedPlanId`, `CurrentPlanId` | Plan snapshots |
| `DurationMonths` | Activation period |
| `RequestedPlanMonthlyPrice`, `EstimatedTotalAmount` | Price snapshots at request time |
| `PaymentReference`, `PaymentSubmittedAt` | Owner-submitted payment reference (SB-003) |
| `RequestExpiresAt` | Optional request expiry |
| `AdminOverrideReason` | Logged when admin bypasses validation |
| `CancelledAt`, `CancelledByUserId` | Cancel audit |
| `AdminNote`, `ReviewedAt`, `ReviewedByAdminId` | Review audit |

## Plan keys and defaults

**File:** `src/DiscordBot.Domain/Constants/PlanKeys.cs`

| Key | Seeded price | Modules |
|-----|--------------|---------|
| `free` | $0 | welcome, logs |
| `basic` | $9.99 | + reaction-roles |
| `pro` | $19.99 | + tickets, moderation |
| `premium` | $29.99 | `*` (all) |

**Seeder:** `src/DiscordBot.Infrastructure/Data/SubscriptionPlanSeeder.cs`

**All modules token:** `PlanKeys.AllModulesToken = "*"`

## Duration options

**File:** `src/DiscordBot.Domain/Constants/SubscriptionDurations.cs`

Allowed months: **1, 3, 6, 12**

## Services

### SubscriptionService

**File:** `src/DiscordBot.Infrastructure/Services/SubscriptionService.cs`

| Capability | Detail |
|------------|--------|
| List plans | Public `GET /api/plans` |
| Get guild subscription | Includes plan name, expiry, allowed modules |
| Owner plan change | **Blocked** — `PUT /api/guilds/{id}/subscription` returns 403 |
| Module gating | `IsModuleAllowedForGuildAsync`, `GetAllowedModuleKeysForGuildAsync` |
| Admin assign/extend/cancel | Platform admin endpoints |

### PlanUpgradeRequestService

**File:** `src/DiscordBot.Infrastructure/Services/PlanUpgradeRequestService.cs`

Owner: create/list/cancel requests; submit payment reference; get current change and combined subscription status.  
Admin: list all, approve/reject/cancel.

Business rules enforced:

- One active (non-terminal) request per guild
- Upgrade requires a different plan; renewal requires the same paid plan (inferred when plan matches current)
- Plan and price snapshots at creation
- Payment reference submit only from `PendingPayment`; blocked if expired, cancelled, rejected, or already submitted
- Validated state transitions via `PlanUpgradeRequestWorkflow`
- Lazy request expiry when `RequestExpiresAt` is passed

## API endpoints

### Dashboard (JWT)

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/plans` | All active plans |
| GET | `/api/guilds/{id}/subscription` | Guild's current subscription |
| GET | `/api/guilds/{id}/subscription/status` | Subscription + current in-flight change (SB-003) |
| PUT | `/api/guilds/{id}/subscription` | **403** — use subscription change requests |
| GET/POST | `/api/guilds/{id}/subscription/upgrade-requests` | Owner create/list (legacy route name) |
| GET | `/api/guilds/{id}/subscription/change-requests/current` | Active subscription change or 204 |
| PUT | `/api/guilds/{id}/subscription/change-requests/{requestId}/payment` | Submit payment reference |
| POST | `/api/guilds/{id}/subscription/upgrade-requests/{requestId}/cancel` | Owner cancel in-flight request |

### Admin (JWT + PlatformAdmin)

| Method | Route |
|--------|-------|
| GET | `/api/admin/upgrade-requests` |
| POST | `/api/admin/upgrade-requests/{id}/approve` | Body: `{ adminNote?, adminOverrideReason? }` |
| POST | `/api/admin/upgrade-requests/{id}/reject` | Body: `{ adminNote }` — **reason required** (SB-004) |
| POST | `/api/admin/upgrade-requests/{id}/cancel` |
| PUT | `/api/admin/guilds/{id}/subscription` |
| POST | `/api/admin/guilds/{id}/subscription/extend` |
| POST | `/api/admin/guilds/{id}/subscription/cancel` |
| CRUD | `/api/admin/plans` |

## Integration with modules

When owner toggles module in dashboard:

```
ModuleService.UpdateGuildModuleAsync
  → SubscriptionService.IsModuleAllowedForGuildAsync
  → throws if module not in plan
```

Bot checks module enabled separately via `ModuleGuard` (does not re-check subscription on every call — relies on `GuildModule.IsEnabled` which can only be set if plan allows).

## Dashboard

**Route:** `/guilds/:id/subscription` — current plan card, subscription change stepper, payment reference form, waiting review card, renew CTA, change history  
**Admin:** `/admin/plans`, `/admin/upgrade-requests` (UI: **Subscription Changes** — review queue with payment reference, filters, approve/reject dialogs)

## Assumptions

- **No payment gateway** (Stripe/PayPal) — billing is manual/operational
- **No automatic renewal** — expiry handled administratively
- **Single currency** — prices stored as decimal without currency code field
- New guilds receive default plan on registration (via `GuildService.RegisterGuildAsync` / subscription creation — verify in service when implementing billing)

## Future expansion

- Stripe integration webhooks
- Trial periods
- Usage-based billing (message volume)
- Plan feature flags beyond modules (seat count, log retention)
- Self-serve downgrade with grace period

## Related docs

- `module-system.md`, `authorization.md`
- `/docs/product/pricing.md`
