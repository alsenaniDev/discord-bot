# SUB-UX-001 — Subscription Page Product Review

**Date:** 2026-07-03  
**Status:** Complete — review only, no implementation  
**Task:** Subscription Page Product Review  
**Deliverable:** [Subscription Page Review](../reviews/subscription-page-review.md)

---

## Summary

Completed a design and UX review of the guild owner **Subscription** page (`/guilds/:id/subscription`) against PX-001, PX-002, UX-001, SB-003, SB-004, and PP-001.

**Verdict:** SB-003/SB-004 delivered a working manual billing loop and a strong admin queue. The owner page is beta-functional but **not yet premium SaaS quality**. Primary gaps are payment instruction concreteness, mission-focused layout, terminal state UX (rejected/expired/cancelled), and mobile history presentation.

**Overview Mission Control (UI-005) remains frozen** — no further Overview visual work unless bugfix.

**No code was written in this sprint.**

---

## Scope reviewed

| Artifact | Reviewed |
|----------|----------|
| `subscription.component.html` | ✅ |
| `subscription.component.ts` | ✅ |
| `subscription.component.css` | ✅ |
| `en.json` / `ar.json` `subscription.*` | ✅ |
| `admin-upgrade-requests.component.*` | ✅ Cross-reference |
| `modules.component.html` locked state | ✅ Cross-reference |
| UX / product authority docs | ✅ |

---

## Authority mapping

| Authority | Key findings |
|-----------|--------------|
| **PX-001** | Page lacks single mission; duplicate plan info; trust gaps on rejection |
| **PX-002** | Billing missions on Overview should deep-link to aligned Subscription mission zone |
| **UX-001** | Stepper, payment ref, cancel dialog implemented; instructions panel, confirm modal, terminal states missing |
| **SB-003** | Core change flow delivered as specified |
| **SB-004** | Admin polished; owner rejection/expiry banners remain backlog |
| **PP-001** | Uses `page-medium`, shared cards/dialogs; should adopt beta banner + status badges consistently |

---

## What works (headline)

1. End-to-end subscription change workflow with stepper and payment reference.  
2. EN/AR core journey strings in parity.  
3. Platform cancel dialog and load/error patterns.  
4. Admin review queue ready for beta operators.  
5. Beta manual billing notice sets expectations.

---

## Critical gaps (headline)

1. **No concrete payment instructions** (bank details, copy, reference format).  
2. **No pre-submit confirmation** for money-adjacent actions.  
3. **No owner UX for rejected / expired / cancelled** requests.  
4. **Stacked cards + duplicated plan content** — unprofessional density.  
5. **History table on mobile** — below UX-001 spec.

---

## Recommended next sprint (not started)

**Proposed ID:** FE-SUB-001 — Subscription Page Experience v2

**P0 scope (suggested):**

1. Payment instructions panel (static config)  
2. Confirmation modal before submit  
3. Rejected / expired / cancelled result cards  
4. Hide request form during active change  
5. SUB-COPY-001 i18n cleanup  

**Estimated effort:** ~4–5 days (1 engineer + design review), aligned with UX-001 FE-001 estimate.

---

## Deliverables

| File | Purpose |
|------|---------|
| [docs/reviews/subscription-page-review.md](../reviews/subscription-page-review.md) | Full design/UX review |
| `docs/progress/2026-07-03-SUB-UX-001-subscription-review.md` | This report |

---

## Validation

| Criterion | Met |
|-----------|-----|
| Reviewed against all six authority references | ✅ |
| What works / unprofessional / payment / trust / copy / states / mobile / RTL | ✅ |
| Recommended redesign documented | ✅ |
| Top implementation tasks prioritized | ✅ |
| No code changes | ✅ |
| Overview frozen acknowledged | ✅ |

---

## Related documents

- [Subscription Page Review](../reviews/subscription-page-review.md)
- [Subscription Experience (UX-001)](../ux/subscription-experience.md)
- [SB-003 Progress](./2026-07-03-SB-003-subscription-change-flow.md)
- [SB-004 Progress](./2026-07-03-SB-004-admin-subscription-review.md)
- [PR-002 v2 Overview](../reviews/overview-redesign-v2.md) — frozen at UI-005
