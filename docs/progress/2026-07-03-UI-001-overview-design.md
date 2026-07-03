# UI-001 — Mission Control Overview (Design First)

**Date:** 2026-07-03  
**Status:** Complete — design only, no implementation  
**Sprint ID:** UI-001  
**Deliverable:** [Overview Mission Control UI Design](../design/overview-mission-control-ui.md)

---

## Summary

Completed a **full product design specification** for the Guild Overview page replacement. The current O-002 widget dashboard is **obsolete** — UI-001 defines Mission Control from scratch per PX-001, PX-002, PR-002 v2, PP-001, and UX-002 findings.

**No code was written.** This sprint is implementation-ready for a future engineering sprint.

---

## Authority documents read

| Document | Applied to design |
|----------|-------------------|
| [PX-001](../ux/product-experience-architecture.md) | One mission, one CTA, above-the-fold, trust, empty/loading/error |
| [PX-002](../ux/product-decision-architecture.md) | Full MissionId matrix, dismiss rules, persona filtering |
| [PR-002 v2](../reviews/overview-redesign-v2.md) | 5-zone IA, wireframes, backend contract |
| [PP-001](../design/design-system.md) | Typography classes, tokens, spacing, card/button patterns |
| [UX-002](../reviews/global-experience-audit.md) | Overview P0 debt, i18n blockers, widget anti-patterns |

---

## Deliverables

| File | Contents |
|------|----------|
| `docs/design/overview-mission-control-ui.md` | Full UI spec — wireframes, all mission states, pulse, activity, drawer, a11y, RTL, top 20 tasks |
| `docs/progress/2026-07-03-UI-001-overview-design.md` | This report |

---

## Design decisions

### Page structure

- **5 zones only** — Status extension · Mission Card · Community Pulse · Activity Timeline · Context Drawer  
- **1440×900 above-the-fold contract** — mission + pulse + activity header visible without scroll  
- **One primary CTA** per viewport — enforced in Mission Card only  

### Mission Card

- **24 MissionId states** fully specified with title, body, CTA, severity, icon, border color, dismiss policy  
- Categories: critical (bot, billing) · warning (sync, backlog, expiry) · info (setup, recommendations) · neutral (all clear)  
- Beginner 3-phase progress footer inside card — not separate setup widget  

### Community Pulse

- Borderless 72px strip — **not cards**  
- Veteran 6 metrics · Beginner 4 metrics  
- Semantic color on values only when state is bad  

### Activity Timeline

- GitHub-style grouped timeline — Today / Yesterday / Earlier  
- i18n structured params — **no raw API English** (UX-002 P0)  

### Context Drawer

- Collapsed by default  
- Setup · Modules · Billing · Suggestions · Help tabs with visibility rules  

### Visual system

- Spacing rhythm documented with PP-001 tokens  
- Typography hierarchy per zone  
- Motion: subtle crossfade only; reduced-motion safe  
- Added design note: `.card-status.is-danger` needed in PP-001 for critical missions  

---

## Wireframes delivered

| Viewport | Format |
|----------|--------|
| Desktop 1440×900 | Full ASCII — backlog, all-clear, beginner, critical states |
| Tablet 768×1024 | 2×3 pulse grid, independent layout |
| Mobile 375×812 | Mission → scroll-snap pulse → activity → drawer |

---

## States documented

Every widget includes **Loading · Error · Empty** (where applicable):

| Widget | Empty | Loading | Error |
|--------|-------|---------|-------|
| Page | — | Skeleton geometry | Full empty-state + retry |
| Mission Card | N/A (always has mission) | Card skeleton | Inline retry |
| Pulse | Zeros as `0` | Cell shimmer | `—` + optional banner |
| Activity | Inline copy + link | 3 row skeletons | Inline retry |
| Drawer | Per-tab empty copy | Panel skeleton | Inline message |

---

## Accessibility & RTL

- Tab order, landmarks, aria-live on critical mission change  
- Pulse list semantics, drawer aria-expanded  
- Full RTL table for layout mirroring  
- Arabic parity gate before ship  

---

## Implementation backlog (Top 20)

Priority order in main design doc — highlights:

1. Mission Engine backend (PX-002)  
2. Activity i18n API shape  
3. PP-001 critical border token  
4. Zone 1 topbar extension  
5. Mission Card component (full matrix)  
6. … through O-002 deletion and above-fold QA  

**Estimate:** ~28 dev-days total

---

## Relationship to other sprints

| Sprint | Relationship |
|--------|--------------|
| PR-002 v2 | IA authority — UI-001 adds visual/interaction depth |
| PP-001 | Visual tokens — one gap identified (`.is-danger` on card-status) |
| UX-002 | Overview P0 issues addressed in design |
| MC-1..MC-11 (PR-002 v2 roadmap) | Engineering IDs map to UI-001-01..20 |

---

## Next step (not in scope)

Engineering sprint implementing UI-001-01 through UI-001-20. Recommended gate: 5-user 10-second comprehension test before production merge.

---

## Related documents

- [Overview Mission Control UI Design](../design/overview-mission-control-ui.md)
- [PR-002 v2 Mission Control](../reviews/overview-redesign-v2.md)
- [PX-002 Product Decision Architecture](../ux/product-decision-architecture.md)
- [Global Experience Audit](../reviews/global-experience-audit.md)
