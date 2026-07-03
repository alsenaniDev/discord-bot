# UX-001 — Subscription Experience Blueprint (Final Report)

**Date:** 2026-07-03  
**Task:** UX-001 — Subscription Experience Blueprint  
**Depends on:** SB-001, SB-002  
**Type:** UX / product design documentation only — no code

---

## Summary

Created the **official Subscription Experience Blueprint** — a complete UX specification for manual billing during Closed Beta. The design treats manual bank-transfer billing as a **professional SaaS journey** with guided states, clear CTAs, admin review flows, and a path to Stripe without redesign.

The document is intended to be **implementation-ready**: a UI/UX designer and frontend engineer can build the full owner and admin experience from this spec alone.

**Deliverable:** [docs/ux/subscription-experience.md](../ux/subscription-experience.md)

---

## Journey Designed

### Owner primary path

Free/paid plan → Compare plans → Select plan + duration → Confirm summary → Submit request → **Pending payment** (instructions) → Pay off-platform → Submit payment proof → **Under review** → **Activated** → Use modules → Renew before expiry.

### Alternate paths documented

| Path | UX outcome |
|------|--------------|
| **Rejected** | Reason visible + "Submit new request" |
| **Cancelled** | Who cancelled + upgrade again |
| **Expired (request)** | Deadline missed + new request |
| **Expired (subscription)** | Downgrade messaging + renew CTA |
| **Renewal** | Same upgrade loop + 7/3/1-day nudges |
| **Downgrade** | Auto on expiry; admin path documented |

**4 Mermaid diagrams:** user journey flow, owner journey map, navigation IA, admin review flow, request lifecycle stepper.

---

## Screens Defined

### Guild Owner (10 screens / sections)

Current Subscription · Plan Comparison · Upgrade Confirmation · Payment Instructions · Submit Payment Proof · Request Status (stepper) · Request Detail (optional) · Subscription History · Expired Subscription · Renew flow

### Platform Admin (10 screens)

Admin Overview · Upgrade Review Queue · Request Detail · Approve / Reject / More Info / Cancel dialogs · Guild Subscription Panel · Billing Settings (future) · Plan Catalog (live)

Each screen includes **purpose**, **entry/exit**, and **primary/secondary CTAs** — no component code.

---

## Status UX

Full specification for all **9 request statuses** + **subscription expired** state:

- Headline, description, primary/secondary buttons  
- Badge color (mapped to dashboard tokens)  
- Icon and optional illustration  
- Explicit **next step** for owner and admin  

Includes unified **status stepper** pattern replacing today's generic "pending" banner.

---

## UX Principles (10)

1. One clear primary action per screen  
2. No dead-end screens  
3. Waiting states explain what happens next  
4. Every status has owner action or explicit wait  
5. Money never ambiguous  
6. Deadlines always visible  
7. Rejection respectful + actionable  
8. Manual beta disclosed professionally  
9. Admin and owner see aligned truth  
10. Mobile and Arabic first-class  

Plus **11 trust & transparency** commitments (always show plan, expiry, rejection reason, etc.).

---

## Additional Sections

| Section | Coverage |
|---------|----------|
| **Empty states** | 7 contexts (no history, no plans, admin empty queue, etc.) |
| **Error states** | 10 errors with recovery CTAs |
| **Notifications** | 10 owner events + admin event template |
| **Mobile** | Grid, stepper, sticky CTAs, table→card patterns |
| **Accessibility** | Contrast, keyboard, SR, RTL/LTR, reduced motion |
| **Future ready** | Stripe, invoices, auto-renew without journey redesign |

---

## Open Questions

| # | Question | Recommendation |
|---|----------|----------------|
| OQ-1 | **Owner cancel during Under review** — allowed? | Yes for beta; reduces support load |
| OQ-2 | **Rejection reason required** in admin dialog? | UX: required; API can stay optional with validation in UI |
| OQ-3 | **Separate request detail page** vs inline stepper? | Inline for v1; detail page at 10+ fields |
| OQ-4 | **Review SLA copy** — 1–2 business days fixed? | Configurable in admin billing settings |
| OQ-5 | **Renewal pre-fill** previous plan? | Yes — reduce friction |
| OQ-6 | **Discord DM notifications** for beta? | After in-dashboard banners proven |
| OQ-7 | **Payment instructions** platform-wide vs per-plan? | Platform default + optional plan override |
| OQ-8 | **History table fix** — separate FE ticket? | Include in first UX implementation sprint |

---

## Recommendations

1. **Replace single "pending" banner** with status stepper immediately — highest impact for "what do I do now?"  
2. **Add Payment Instructions panel** in same sprint as SB-003 payment reference API.  
3. **Fix history table columns** (status under wrong header — live bug).  
4. **Admin: add request detail drawer** before scaling past ~10 paid guilds.  
5. **Copy pass EN + AR** for all new status strings before beta onboarding.  
6. **Beta disclaimer** on confirmation modal — sets manual billing expectations.  
7. **Module locked state** should link directly to Subscription with plan name in CTA.

---

## Suggested Next Task

**FE-001 — Subscription Experience Implementation (Owner v1)**

Scope (aligned with UX-001 + SB-003):

1. Status stepper component on `/guilds/:id/subscription`  
2. Payment instructions panel (static config)  
3. Payment reference form + submit  
4. Upgrade confirmation modal  
5. Fix history table columns  
6. Empty/error states per UX-001  
7. i18n EN/AR for all new copy  

**Follow-up:** **FE-002 — Admin Review UX** (queue filters, detail drawer, dialogs).

**Estimated effort:** FE-001 ~4–5 days · FE-002 ~3–4 days (1 engineer + design review)

---

## Success Criteria

| Criterion | Met |
|-----------|-----|
| Complete user journey documented | ✅ |
| All screens inventoried with purpose | ✅ |
| Every status has headline, CTA, badge, next step | ✅ |
| Admin experience specified | ✅ |
| Mobile, a11y, RTL addressed | ✅ |
| Future Stripe path without redesign | ✅ |
| 10 UX principles defined | ✅ |
| No implementation code | ✅ |

---

## Related Documents

- [Subscription Experience Blueprint](../ux/subscription-experience.md)
- [Manual Billing Domain Blueprint (SB-001)](../domains/subscription-billing/manual-billing-domain-blueprint.md)
- [SB-002 Progress Report](2026-07-03-SB-002-manual-billing-foundation.md)
- [Subscription System](../architecture/subscription-system.md)
