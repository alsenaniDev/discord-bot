# SB-004 — Admin Subscription Change Review Polish

**Date:** 2026-07-03  
**Status:** Complete  
**Sprint:** SB-004  
**Alignment:** SB-001 · SB-002 · SB-003 · D-002 · UX-001

---

## Summary

Polished the platform admin review queue for manual billing so operators can process **Subscription Changes** confidently during Closed Beta. The page now surfaces payment references, change type, expiry, and owner context; supports status/type/search filters; and uses platform dialogs for approve/reject with override and rejection-reason validation.

Backend routes remain `/api/admin/upgrade-requests` for compatibility.

---

## Files changed

### Backend

- `src/DiscordBot.Infrastructure/Services/PlanUpgradeRequestService.cs` — reject requires non-empty `adminNote`

### Dashboard

- `dashboard/.../admin/admin-upgrade-requests/admin-upgrade-requests.component.ts`
- `dashboard/.../admin/admin-upgrade-requests/admin-upgrade-requests.component.html`
- `dashboard/.../admin/admin-upgrade-requests/admin-upgrade-requests.component.css`
- `dashboard/.../assets/i18n/en.json`
- `dashboard/.../assets/i18n/ar.json`

### Documentation

- `docs/architecture/subscription-system.md`
- `docs/ux/subscription-experience.md`
- `docs/project-management/release-notes.md`

---

## Admin UX changes

| Feature | Detail |
|---------|--------|
| Queue columns | Change type, current/requested plan, duration, estimated total, payment reference, request expiry, payment submitted at, status, guild, owner, created |
| Filters | Status, change type (Upgrade/Renewal), guild/owner search |
| Approve dialog | Warning when no payment reference; **AdminOverrideReason** required for `PendingPayment` or missing reference |
| Reject dialog | Required rejection reason (shown to owner via `adminNote`) |
| Labels | Nav/page copy renamed to **Subscription Changes** / **تغييرات الاشتراك** |
| Reviewable rows | Highlighted when approve/reject actions are available |

Route unchanged: `/admin/upgrade-requests`

---

## API changes

| Change | Detail |
|--------|--------|
| Reject validation | `POST .../reject` returns 400 if `adminNote` is empty |
| DTOs | No new fields — SB-003 `AdminPlanUpgradeRequestDto` already includes `ChangeType`, `PaymentReference`, `PaymentSubmittedAt`, `RequestExpiresAt` |

Existing approve body unchanged: `{ adminNote?, adminOverrideReason? }`

---

## Validation

| Check | Result |
|-------|--------|
| `dotnet build DiscordBot.sln` | Pass |
| `npm run build` | Pass |

### Manual smoke (recommended)

1. Owner creates subscription change → `PendingPayment`
2. Owner submits payment reference → `UnderReview`
3. Admin opens **Subscription Changes** → filter **Under review**
4. Approve from dialog → subscription activates
5. Create another change → reject with reason → owner history shows rejected status

---

## Risks

| Risk | Mitigation |
|------|------------|
| Wide table on small screens | Horizontal scroll via `.table-wrap`; filters stack on mobile |
| Legacy API clients rejecting without note | Documented breaking validation; admin UI always sends reason |
| Admin approves without checking bank | Warning banner + override reason when reference missing |

---

## Remaining work

- Owner-facing rejection/expiry banners (UX-001 alternate journeys)
- Server-side filter/query params on admin list (client-side filters only in SB-004)
- Payment instructions from platform config
- Route alias `/admin/subscription-changes` (D-002 follow-up)

---

## Suggested next sprint (SB-005)

1. Owner rejection/expiry state cards + “start new change” CTA  
2. Renewal reminder banners (7/3/1 days before expiry)  
3. Admin list API filters (`status`, `changeType`, `search`)  
4. Optional admin route alias + OpenAPI description updates  

---

## Related docs

- [SB-003 Subscription Change Flow](./2026-07-03-SB-003-subscription-change-flow.md)
- [Subscription System](../architecture/subscription-system.md)
- [Subscription Experience (UX-001)](../ux/subscription-experience.md)
