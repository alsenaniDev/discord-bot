# PR-003 — Overview Redesign Critique (Pre-Implementation)

**Date:** 2026-07-03  
**Status:** Complete — review only, no implementation  
**Critique ID:** PR-003  
**Subject:** [PR-002 Overview Redesign Proposal](../reviews/overview-redesign-review.md)  
**Deliverable:** [Overview Redesign Critique](../reviews/overview-redesign-critique.md)

---

## Summary

Independent **aggressive design critique** of PR-002, written from the perspective of a Principal Product Designer (not the PR-002 author).

**Verdict:** **Do not implement PR-002 as written.** It rearranges O-002 and **adds sections** while claiming simplification. Realistic outcome: **7/10**, not the stated **8.5/10**.

**Core finding:** PR-002 imports Stripe/Vercel/Linear **pattern names** without **product discipline** — premium dashboards remove decisions; PR-002 multiplies them (hero + alerts + setup + next steps + shortcuts + snapshots + resources).

---

## Recommendation

Revise to **PR-002 v2** with:

- **≤5 visible zones** (not 9)  
- **Merged alert + hero** (one action panel)  
- **Deleted shortcuts and resources footer**  
- **Conditional billing/modules** (alerts only, not bottom cards)  
- **Veteran-user mode** (activity-first, no setup/health widgets)  
- **Above-the-fold contract** on 1440×900  
- **5-user comprehension test** before engineering IM-1  

---

## Scores (PR-002 as proposed)

| Area | PR-002 target | Critique score |
|------|---------------|----------------|
| Visual Design | 8 | **6.5** |
| Navigation | — | **5** |
| Hierarchy | 9 | **6** |
| Discoverability | — | **7** |
| Activation | — | **7** |
| Trust | — | **6** |
| Density | — | **5** |
| Readability | 9 | **7** |
| Accessibility | 8 | **7** |
| Mobile | 8 | **5.5** |
| RTL | 9 | **8** |
| **Overall polish** | **8.5** | **6.5–7** |

---

## Section verdicts (headline)

| Section | Verdict |
|---------|---------|
| Status strip | **Delete** — merge into topbar |
| Critical alerts | **Keep** — max 1, merge with hero |
| Primary hero | **Keep** — enlarge, sole CTA |
| Setup progress | **Merge into hero** — delete standalone |
| Health + metrics | **Shrink** — text pulse row, no ring v1 |
| Next steps | **Delete** — drawer link from hero |
| Shortcuts | **Delete** — sidebar duplication |
| Activity | **Keep** — full width, no sidebar |
| Modules snapshot | **Demote** — one line or alert-only |
| Subscription snapshot | **Demote** — alert-only |
| Resources | **Delete** |

---

## Top 20 improvements (8.5 → 10/10)

Documented in full in [overview-redesign-critique.md § Top 20 improvements](../reviews/overview-redesign-critique.md#top-20-improvements-85--1010).

Highlights:

1. Collapse to 5 zones  
2. Merge alert + hero  
3. Merge setup into hero  
4. Delete shortcuts  
5. Delete resources footer  
6. Billing/modules alert-only  
7. Replace health ring with issues drawer  
8. Veteran mode layout  
9. Staff persona wireframes  
10. Linked activity rows  
11–20. Copy, pulse row, above-the-fold contract, mobile reduction, 3-phase setup, hero precedence rules, comprehension testing  

---

## What to preserve from PR-002

- Activation truth fix (`firstValueAchieved`)  
- Activity i18n (structured events)  
- Hero concept (single resolver in backend)  
- Remove duplicate guild name  
- Persona-aware data (expand spec)  

---

## Files created

| File | Purpose |
|------|---------|
| `docs/reviews/overview-redesign-critique.md` | Full critique + revised IA + top 20 |
| `docs/progress/2026-07-03-PR-003-overview-critique.md` | This report |

---

## Next step

Schedule **PR-002 revision workshop** using PR-003 critique. Produce **PR-002 v2** wireframes before any IM-1 implementation.

---

## Related docs

- [PR-002 Overview Redesign](./overview-redesign-review.md)
- [PR-002 Progress](../progress/2026-07-03-PR-002-overview-redesign.md)
- [PR-001 Product Audit](./product-review-001.md)
