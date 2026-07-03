# PR-002 — Guild Overview Redesign (Design Proposal)

**Date:** 2026-07-03  
**Status:** Complete — **design only, awaiting approval**  
**Review ID:** PR-002  
**Priority:** P0  
**Deliverable:** [Overview Redesign Review](../reviews/overview-redesign-review.md)

---

## Summary

Produced a **complete product experience redesign proposal** for the Guild Overview page (`/guilds/:id/overview`). No Angular, CSS, or HTML was written. This document becomes the **implementation specification** once approved.

**Current Overview score: 5.5 / 10**  
**Target after implementation: 8.5 / 10**

---

## Scope completed

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 1 — Audit | 29 issues with severity, reference, recommendation | ✅ |
| 2 — Information architecture | 9-section hierarchy with rationale | ✅ |
| 3 — Desktop layout | 12-column grid + ASCII wireframe | ✅ |
| 4 — Mobile layout | Intentional order + wireframe (not desktop stack) | ✅ |
| 5 — Visual language | PP-001 compliance review + gaps | ✅ |
| 6 — Card review | All 7 current cards + 4 new sections | ✅ |
| 7 — Copy review | Issues + EN rewrites + new keys | ✅ |
| 8 — Competitive review | Linear, Stripe, Vercel, GitHub, Discord, Notion, Slack | ✅ |
| 9 — Final blueprint | Full spec: a11y, RTL, empty/loading/error, roadmap | ✅ |

---

## Overall score

| Dimension | Current | Target |
|-----------|---------|--------|
| 10-second comprehension | 4/10 | 9/10 |
| Information hierarchy | 5/10 | 9/10 |
| Visual polish | 6/10 | 8/10 |
| Mobile experience | 5/10 | 8/10 |
| Copy & i18n | 6/10 | 9/10 |
| Accessibility | 5/10 | 8/10 |
| **Overall** | **5.5/10** | **8.5/10** |

---

## Top identified issues (P0)

| ID | Issue |
|----|-------|
| OV-001 | No single primary action — seven competing sections |
| OV-002 | “Activated” at 85% without first value (O-001 violation) |
| OV-003 | Activity feed English-only in AR locale |
| OV-004 | Subscription invisible except header badge |
| OV-005 | No modules snapshot on overview |

---

## Redesign proposal (headline changes)

1. **Status strip** replaces community header card — compact, no duplicate guild name  
2. **Critical alerts strip** — Stripe-style blockers (bot offline, sync stale, billing)  
3. **Primary action hero** — one dominant CTA from top recommendation or activation step  
4. **Setup progress** — renamed, collapsible; separate from “community live” state  
5. **Health ring + metric chips** — merges health card + stats card  
6. **Next steps + shortcuts** — splits recommendations from quick actions  
7. **Activity feed** — typed icons, day groups, i18n structured events  
8. **Modules + subscription snapshots** — new bottom row  
9. **Resources footer** — help links  

---

## Wireframes

Included in main review as ASCII diagrams:

- Desktop 12-column layout  
- Mobile intentional single-column order  
- Tablet 8-column simplification  

---

## Priorities for implementation

| Priority | Work |
|----------|------|
| **P0** | Hero CTA, alerts, activation truth fix, activity i18n, subscription/modules snapshots |
| **P1** | Status strip, health ring, next steps/shortcuts split, mobile layout |
| **P2** | Copy pass, skeleton update, icon unification, dismiss recommendations |
| **P3** | Sticky mobile strip, analytics pipeline, Welcome Wizard link (O-003) |

---

## Implementation roadmap

Nine implementation phases (IM-1 through IM-9), ~23 dev-days estimated.

See full breakdown in [overview-redesign-review.md § Implementation roadmap](../reviews/overview-redesign-review.md#implementation-roadmap).

| Phase | Focus | Est. |
|-------|-------|------|
| IM-1 | Status strip + alerts | 3d |
| IM-2 | Hero CTA | 2d |
| IM-3 | Setup progress + activation fix | 3d |
| IM-4 | Health ring + metrics | 3d |
| IM-5 | Next steps + shortcuts | 2d |
| IM-6 | Activity i18n | 3d |
| IM-7 | Modules + subscription snapshots | 2d |
| IM-8 | Responsive + skeleton | 3d |
| IM-9 | Copy, a11y, QA | 2d |

---

## Validation method

- Reviewed O-002 implementation (HTML, TS, CSS, backend service)  
- Cross-referenced PR-001 audit, O-001 blueprint, PP-001 design system  
- Compared patterns against Linear, Stripe, Vercel, GitHub, Discord Developer Portal  
- **No code modified**  
- **No screenshots captured** — element references documented for QA follow-up  

---

## Files created

| File | Purpose |
|------|---------|
| `docs/reviews/overview-redesign-review.md` | Full design specification |
| `docs/progress/2026-07-03-PR-002-overview-redesign.md` | This report |

---

## Approval required before implementation

- [ ] Product — IA and hero-first approach  
- [ ] Design — wireframes and visual language  
- [ ] Engineering — DTO extensions and 23-day roadmap  
- [ ] Localization — copy keys and activity i18n strategy  
- [ ] CTO — scope boundary (Welcome Wizard deferred to O-003)  

**No implementation begins until this document is reviewed and approved.**

---

## Suggested next step

Schedule **PR-002 approval review** → upon sign-off, begin **IM-1 (Status strip + alerts)** as first implementation sprint.

---

## Related docs

- [Overview Redesign Review](../reviews/overview-redesign-review.md)
- [Product Review PR-001](../reviews/product-review-001.md)
- [Design System PP-001](../design/design-system.md)
- [O-001 Activation Blueprint](../ux/first-time-user-activation.md)
- [O-002 Dashboard Overview](./2026-07-03-O-002-dashboard-overview.md)
