# SB-003 — Subscription Change Flow v1

**Date:** 2026-07-03  
**Status:** Complete  
**Sprint:** SB-003  
**Alignment:** SB-001 · SB-002 · D-002 · UX-001

---

## Implementation plan (Phase 1)

### Files to change

| Area | Files |
|------|-------|
| Domain | `SubscriptionChangeType.cs`, `PlanUpgradeRequest.cs`, `PlanUpgradeRequestWorkflow.cs` |
| Infrastructure | `PlanUpgradeRequestConfiguration.cs`, `PlanUpgradeRequestService.cs`, `UpgradeRequestDtos.cs`, migration |
| API | `GuildsController.cs` |
| Dashboard | `upgrade-request.models.ts`, `guild.service.ts`, `subscription.component.*`, `en.json`, `ar.json` |
| Docs | `subscription-system.md`, `api-design.md`, `release-notes.md` |

### Migration

`20260703011044_SubscriptionChangeFlowV1` — adds `ChangeType`, `PaymentReference`, `PaymentSubmittedAt` to `PlanUpgradeRequests`.

### API changes

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/guilds/{id}/subscription/status` | Current subscription + active change |
| GET | `/api/guilds/{id}/subscription/change-requests/current` | Active change or 204 |
| PUT | `/api/guilds/{id}/subscription/change-requests/{requestId}/payment` | Submit payment reference |
| POST | `/api/guilds/{id}/subscription/upgrade-requests` | Extended body: optional `changeType` |

Existing create/list/cancel/approve routes retained (`PlanUpgradeRequest` entity name unchanged).

### Dashboard changes

Owner `/guilds/:id/subscription` per UX-001:

- Current plan card + renew button
- 5-step subscription change stepper
- Payment reference form (`PendingPayment`)
- Waiting review card (`PaymentSubmitted`, `UnderReview`)
- Activated summary card
- History table: Type, Status, Requested plan, Created
- Platform cancel dialog (no browser confirm)

### Risks

| Risk | Mitigation |
|------|------------|
| Admin used to approving from `PendingPayment` without payment | `AdminOverrideReason` required; workflow enforces |
| Legacy rows missing `ChangeType` | Migration default `Upgrade = 0` |
| Route naming split (`upgrade-requests` vs `change-requests`) | Documented; aliases per D-002 Option C |

### Compatibility

- DB table and C# entity remain `PlanUpgradeRequest`
- Product language: **Subscription Change**
- No breaking changes to existing upgrade-request routes
- Admin UI unchanged (still `/admin/upgrade-requests`)

---

## Implementation summary

First production-ready **Subscription Change** workflow for manual billing:

1. Owners create upgrade or renewal requests (same paid plan allowed for renewal).
2. Owners submit a **payment reference** (text only) from `PendingPayment`.
3. Request auto-advances to `UnderReview` for admin approval.
4. Dashboard guides owners through the full journey with stepper, cards, and EN/AR copy.

---

## Files changed

### Backend

- `src/DiscordBot.Domain/Enums/SubscriptionChangeType.cs` (new)
- `src/DiscordBot.Domain/Entities/PlanUpgradeRequest.cs`
- `src/DiscordBot.Domain/SubscriptionBilling/PlanUpgradeRequestWorkflow.cs`
- `src/DiscordBot.Infrastructure/Data/Configurations/PlanUpgradeRequestConfiguration.cs`
- `src/DiscordBot.Infrastructure/Models/UpgradeRequestDtos.cs`
- `src/DiscordBot.Infrastructure/Services/PlanUpgradeRequestService.cs`
- `src/DiscordBot.Api/Controllers/GuildsController.cs`
- `src/DiscordBot.Infrastructure/Migrations/20260703011044_SubscriptionChangeFlowV1.cs`

### Dashboard

- `dashboard/.../core/models/upgrade-request.models.ts`
- `dashboard/.../core/services/guild.service.ts`
- `dashboard/.../features/subscription/subscription.component.ts`
- `dashboard/.../features/subscription/subscription.component.html`
- `dashboard/.../features/subscription/subscription.component.css`
- `dashboard/.../assets/i18n/en.json`
- `dashboard/.../assets/i18n/ar.json`

### Documentation

- `docs/architecture/subscription-system.md`
- `docs/architecture/api-design.md`
- `docs/project-management/release-notes.md`

---

## Migration

**Name:** `20260703011044_SubscriptionChangeFlowV1`

| Column | Type | Notes |
|--------|------|-------|
| `ChangeType` | int, required, default 0 | `Upgrade` / `Renewal` |
| `PaymentReference` | varchar(500), nullable | Owner-submitted reference |
| `PaymentSubmittedAt` | timestamptz, nullable | Set on submit |

Applied locally via `dotnet ef database update`.

---

## API changes (detail)

### Submit payment reference

```
PUT /api/guilds/{guildId}/subscription/change-requests/{requestId}/payment
Body: { "paymentReference": "..." }
```

Validation errors (400):

- Empty reference
- Already submitted
- Expired / cancelled / rejected / terminal status
- Not in `PendingPayment`

### Get subscription status

```
GET /api/guilds/{guildId}/subscription/status
→ { subscription, currentChange }
```

### Get current change

```
GET /api/guilds/{guildId}/subscription/change-requests/current
→ 200 + change | 204 if none
```

---

## Dashboard changes (detail)

| UX-001 requirement | Implementation |
|--------------------|----------------|
| Current subscription card | Plan, expiry, status, modules |
| Renew button | Visible on active paid plan with no in-flight change |
| Stepper | 5 steps; highlights current workflow stage |
| Payment reference form | Shown only in `PendingPayment` |
| Waiting review card | `PaymentSubmitted` / `UnderReview` |
| Activated card | Paid active subscription + last activated change date |
| History fix | Columns aligned; status no longer under Created |
| No alerts/confirms | Toast + overlay dialog for cancel |

---

## Validation

| Check | Result |
|-------|--------|
| `dotnet build DiscordBot.sln` | Pass |
| `dotnet ef migrations add` | `SubscriptionChangeFlowV1` generated |
| `dotnet ef database update` | Applied |
| `npm run build` (dashboard) | Pass (bundle size budget warning pre-existing) |

### Smoke test (manual)

Recommended flow on a test guild:

1. Create subscription change (upgrade) → status `PendingPayment`, stepper step 2
2. Submit payment reference → `UnderReview`, waiting card visible
3. Admin approve from `/admin/upgrade-requests` → `Activated`, subscription updated
4. Renew: click **Renew plan** → create renewal for same plan → repeat payment + approve

---

## Known limitations

- No receipt upload or object storage
- No Stripe / payment gateway
- No downgrade or scheduled changes
- No email notifications
- No payment instructions API (static i18n copy only)
- Request expiry is lazy (on read), not a background worker
- Admin UI still labeled “Upgrade Requests” (out of scope)

---

## Technical debt

- Unify route naming (`upgrade-requests` → `change-requests` aliases per D-002)
- Admin queue UX: show `PaymentReference`, `ChangeType`, require override UX for `PendingPayment`
- Rejection / expiry owner banners (UX-001 alternate journeys)
- Background job for request expiry
- OpenAPI/Swagger annotations for new endpoints

---

## Suggested next sprint (SB-004)

1. Owner rejection/expiry state cards + “start new change” CTA  
2. Admin subscription change review polish (payment reference column, override dialog)  
3. Renewal reminder banners (7/3/1 days before expiry per UX-001)  
4. Route aliases `/subscription/change-requests` for create/list  
5. Optional: payment instructions from platform config

---

## Related docs

- [Manual Billing Domain Blueprint (SB-001)](../domains/subscription-billing/manual-billing-domain-blueprint.md)
- [SB-002 Manual Billing Foundation](./2026-07-03-SB-002-manual-billing-foundation.md)
- [D-002 Subscription Domain Refactoring](./2026-07-03-D-002-subscription-domain-refactoring.md)
- [UX-001 Subscription Experience](../ux/subscription-experience.md)
- [Subscription System](../architecture/subscription-system.md)
