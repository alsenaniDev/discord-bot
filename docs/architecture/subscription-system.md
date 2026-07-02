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

Manual upgrade workflow:

1. Owner creates request (target plan + duration months)
2. Status: Pending → Approved / Rejected by platform admin
3. On approve: subscription activated/extended, `ApprovedRequestId` linked

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

Owner: create/list requests for their guild.  
Admin: list all pending, approve/reject.

## API endpoints

### Dashboard (JWT)

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/api/plans` | All active plans |
| GET | `/api/guilds/{id}/subscription` | Guild's current subscription |
| PUT | `/api/guilds/{id}/subscription` | **403** — use upgrade requests |
| GET/POST | `/api/guilds/{id}/subscription/upgrade-requests` | Owner workflow |

### Admin (JWT + PlatformAdmin)

| Method | Route |
|--------|-------|
| GET | `/api/admin/upgrade-requests` |
| POST | `/api/admin/upgrade-requests/{id}/approve` |
| POST | `/api/admin/upgrade-requests/{id}/reject` |
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

**Route:** `/guilds/:id/subscription` — shows plan, expiry, upgrade request form  
**Admin:** `/admin/plans`, `/admin/upgrade-requests`

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
