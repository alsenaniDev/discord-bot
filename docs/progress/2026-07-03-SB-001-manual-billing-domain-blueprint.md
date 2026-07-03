# SB-001 — Manual Subscription Billing Domain Blueprint (Final Report)

**Date:** 2026-07-03  
**Task:** SB-001 — Manual Subscription Billing Domain Blueprint  
**Type:** Documentation only — no code changes

---

## Summary

Created the **official Manual Subscription Billing Domain Blueprint** — the business architecture foundation for all subscription billing work (payment proof, admin review, activation, expiry, notifications, and future Stripe migration).

The document models manual billing as an intentional **Phase 1 operating model** for closed beta, not a temporary hack. It extends the Live **Upgrade Request → Admin Approve → Guild Subscription** loop with payment reference, receipt upload, expanded request states, and operator UX requirements.

**Deliverable:** [docs/domains/subscription-billing/manual-billing-domain-blueprint.md](../domains/subscription-billing/manual-billing-domain-blueprint.md)

**Live baseline:** ~70% of core workflow exists (`PlanUpgradeRequest`, `GuildSubscription`, `SubscriptionPlan`, owner subscription page, admin upgrade queue). **Missing:** payment proof, expanded states, payment instructions, admin extend/cancel UI, expiry notifications, structured rejection UX.

---

## Business Rules

**30+ rules** codified with IDs (BR-R, BR-P, BR-A, BR-S, BR-C):

| Category | Highlights |
|----------|------------|
| **Request creation** | Owner-only; one in-flight request per guild; plan/duration snapshots; no direct PUT subscription |
| **Payment submission** | Reference required (configurable); receipt optional/required; instructions before pay |
| **Admin review** | Approve activates subscription; reject unchanged; request-more-info returns to pending payment |
| **Subscription lifecycle** | Lazy expiry → Free + module disable; extend/cancel admin APIs; grace period optional |
| **Catalog** | Free plan undeletable; inactive plans hidden from new requests |

**Key modeling decision:** **Upgrade Request** (workflow) remains separate from **Guild Subscription** (entitlement). **Approval** on the request triggers **Activation** on the subscription — same as Live, preserved for Stripe migration.

---

## Workflow

### Target Upgrade Request states

`Requested` → `PendingPayment` → `PaymentSubmitted` → `UnderReview` → `Approved` → `Activated` (subscription side effect)

Terminal: `Rejected`, `Cancelled`, `Expired`

**Live today:** `Pending` → `Approved` / `Rejected` (payment steps implicit/off-platform).

### Mermaid diagrams included

- Upgrade Request state machine (§2)
- Guild Subscription lifecycle (§2)
- Manual → Stripe migration flow (§12)

### Operator paths documented

| Path | Status |
|------|--------|
| Owner submit request | Live |
| Admin approve/reject queue | Live |
| Admin direct plan dropdown (guilds) | Live — documented inconsistency (clears expiry) |
| Admin extend/cancel | API Live, no dashboard UI |
| Owner payment proof upload | v1 |

---

## Data Model Recommendations

### PlanUpgradeRequests — extend (v1)

| New / changed | Purpose |
|---------------|---------|
| Expanded `Status` enum (8 values) | Full workflow |
| `PaymentReference`, `PaymentReceiptStorageKey` | Manual payment proof |
| `PaymentSubmittedAt`, `PaymentMethodKey` | Audit |
| `EstimatedTotalAmount`, `CurrencyCode` | Snapshot pricing |
| `RejectionReason`, `RequestExpiresAt` | Owner UX + timeout |
| Cancel audit fields | Owner/admin cancel |

### GuildSubscriptions — extend (v1 optional)

| Field | Purpose |
|-------|---------|
| `GracePeriodEndsAt` | Delay downgrade |
| `ActivationSource` | Manual vs admin vs future Stripe |
| `ActivatedTotalAmount`, `CurrencyCode` | Billing snapshot |
| `LastNotifiedExpiryAt` | Expiry reminders |

### SubscriptionPlans — extend (v1)

| Field | Purpose |
|-------|---------|
| `CurrencyCode`, `SortOrder`, `IsListed` | Catalog display |
| `PaymentInstructionsMarkdown` | Per-plan override (optional) |

### Platform config (new, no migration in SB-001)

`ManualBilling:*` settings for payment instructions markdown, require reference/receipt, request expiry days, grace period, reminder days.

### Future entity

**SubscriptionAuditLog** — append-only plan change history (not v1).

---

## API Recommendations

### User (new v1)

- `GET .../billing-config` — payment instructions
- `PUT .../upgrade-requests/{id}/payment` — submit reference
- `POST .../upgrade-requests/{id}/receipt` — upload
- `POST .../upgrade-requests/{id}/cancel` — owner cancel

### Admin (new v1)

- `POST .../request-info` — return to pending payment
- `POST .../cancel` — admin cancel request
- `GET/PUT .../billing-config` — platform payment settings
- Queue filters on `GET /admin/upgrade-requests`

### Live endpoints retained

All existing plan, subscription, upgrade-request, approve/reject, extend, cancel, and admin plan CRUD routes documented in §9.

---

## Dashboard UX Recommendations

| Area | v1 target |
|------|-----------|
| Owner subscription page | Payment instructions panel, upload form, status stepper, rejection reason |
| Admin upgrade queue | Filters, receipt preview, payment reference column |
| Admin guilds | Expiry display, extend/cancel buttons |
| Admin billing settings | Payment instructions editor, require reference/receipt toggles |
| Empty/error states | Defined per §10 |

**Live gaps:** request history table column mismatch; extend/cancel unused in admin service; no payment UI.

---

## Open Questions

| # | Question | Recommendation |
|---|----------|----------------|
| OQ-1 | **Default payment method for beta** — single bank account vs multiple methods? | Start with one platform-wide instructions block; add `PaymentMethodKey` later |
| OQ-2 | **Request auto-expiry** — how many days without payment? | 14 days default; configurable |
| OQ-3 | **Grace period on subscription expiry** | 0 days for v1; 3 days optional for beta guilds |
| OQ-4 | **Receipt storage** — local disk vs S3/Railway volume? | S3-compatible object storage before production scale |
| OQ-5 | **Direct admin plan change** — disable or require expiry? | Require expiry input + confirmation modal in v1 |
| OQ-6 | **Currency** — USD only or SAR for regional beta? | Add `CurrencyCode` on plan; display only until Stripe |
| OQ-7 | **Approve without payment proof** — allow for trusted beta customers? | Admin override flag on approve with mandatory note |
| OQ-8 | **Lazy vs scheduled expiry** | Keep lazy for v1; add worker in v1.1 if operators report surprise lockouts |
| OQ-9 | **Notifications** — email required for v1? | Dashboard-only blocking; email recommended before >10 paid guilds |
| OQ-10 | **Map Live `Pending` to which v1 state?** | `PendingPayment` if no reference; `UnderReview` if reference present after migration |

---

## Suggested Next Task

**SB-002 — Manual Billing v1 Implementation (Backend + Dashboard)**

Suggested scope (single epic, no Stripe):

1. **CM/SB-002a — Data model:** Expand `PlanUpgradeRequestStatus`, payment fields, platform billing config; EF migration.
2. **SB-002b — APIs:** Payment submit, receipt upload, cancel, request-info, billing-config endpoints.
3. **SB-002c — Owner dashboard:** Payment instructions, upload UI, status stepper, fix history table columns.
4. **SB-002d — Admin dashboard:** Queue filters, receipt view, extend/cancel on guilds, billing settings page.
5. **SB-002e — Docs sync:** Update `subscription-system.md`, `pricing.md`, `beta-known-limitations.md`.

**Estimated effort:** 5–8 engineering days (1 engineer), aligned with R-001 beta hardening estimate.

**Dependency:** Can run in parallel with Release 0.1 redeploy (CM-003/CM-004); does not block closed beta if manual payment is handled entirely off-dashboard today, but **payment proof in-app is required before scaling past ~5 paid guilds**.

---

## Artifacts

| File | Action |
|------|--------|
| `docs/domains/subscription-billing/manual-billing-domain-blueprint.md` | **Created** |
| `docs/progress/2026-07-03-SB-001-manual-billing-domain-blueprint.md` | **Created** (this report) |

**No code, migrations, or UI were modified.**

---

## Related Documents

- [Manual Billing Domain Blueprint](../domains/subscription-billing/manual-billing-domain-blueprint.md)
- [Subscription System](../architecture/subscription-system.md)
- [Pricing](../product/pricing.md)
- [Release 0.1 Readiness](../releases/release-0.1-readiness.md)
- [Beta Known Limitations](../releases/beta-known-limitations.md)
- [Product Blueprint](../blueprint/product-blueprint.md)
- [Ubiquitous Language](../blueprint/ubiquitous-language.md)
