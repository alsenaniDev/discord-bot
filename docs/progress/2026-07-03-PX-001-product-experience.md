# PX-001 — Product Experience Architecture

**Date:** 2026-07-03  
**Status:** Complete — mandatory authority document  
**Document ID:** PX-001  
**Deliverable:** [Product Experience Architecture](../ux/product-experience-architecture.md)

---

## Summary

Created the **highest-level UX architecture document** for the Discord Bot Platform — above page designs, design system, and implementation.

This document defines **how the product should feel** for the next 5+ years: emotions, principles, page hierarchy, trust, notifications, loading, copy, accessibility, responsive mindsets, benchmarks, governance, and a **30-question PR checklist**.

**No code. No CSS. No Angular.**

---

## Deliverable

| File | Purpose |
|------|---------|
| `docs/ux/product-experience-architecture.md` | Single mandatory experience authority |
| `docs/progress/2026-07-03-PX-001-product-experience.md` | This report |

---

## Document contents

| Section | Coverage |
|---------|------------|
| Product philosophy | 7 target emotions · 7 anti-emotions · daily-use test |
| Core UX principles | **20 principles** (P-01–P-20) |
| Product IA | Universal hierarchy Mission → Status → Work → History → Advanced |
| Above-the-fold contract | Desktop 1440×900 · Tablet · Mobile mindsets |
| CTA rules | Primary / secondary / ghost / danger — max one primary |
| Hero rules | When exists · forbidden content · 200px max |
| Status communication | Banner · toast · dialog · inline · badge · activity matrix |
| Empty states | Illustration + explanation + CTA structure |
| Success moments | Proportionate celebration — no gamification |
| Error philosophy | 6 severity levels + copy structure |
| Notification architecture | Channel budget · what deserves nothing |
| Loading philosophy | Skeleton · spinner · optimistic · polling rules |
| Trust architecture | Full chapter — Healthy / Activated / Synced truth commitments |
| Copywriting architecture | Voice · verbs · EN/AR · forbidden patterns |
| Accessibility | Global keyboard · focus · RTL · contrast rules |
| Responsive philosophy | Desktop / tablet / mobile mindsets — not breakpoints only |
| SaaS benchmarks | Adopt vs reject matrix (Linear, Stripe, GitHub, etc.) |
| Product consistency | Five questions every page must answer |
| UX debt | P0–P3 · release blockers |
| Design review checklist | **30 questions** per dashboard PR |
| Governance | PX-001 wins conflicts · amendment process |

---

## Document hierarchy (established)

```
PX-001  Product Experience Architecture  ← NEW authority
  ↓
PB-001  Product Blueprint
UL-001  Ubiquitous Language
  ↓
Page specs (O-001, UX-001, PR-002 v2, …)
  ↓
PP-001  Design System (visual)
  ↓
Code
```

---

## Alignment with recent work

| Prior doc | Relationship to PX-001 |
|-----------|------------------------|
| PR-002 v2 Mission Control | Reference implementation of PX-001 principles on Overview |
| PR-003 Critique | Reinforced restraint — codified in P-02, P-17, Hero rules |
| PP-001 Design System | Visual layer — PX-001 governs mission and trust above it |
| O-001 Activation | P-11 No fake activation — first value required |
| PR-001 Product Audit | UX debt severity model extended in §19 |

---

## Key principles (headline)

1. One page → one mission  
2. One primary CTA  
3. Truth over optimism  
4. Scrolling is for history  
5. No duplicate information  
6. No fake metrics / activation / health  
7. Trust architecture is non-optional  
8. Mobile is a mindset — not stacked desktop  
9. PX-001 wins conflicts until revised  

---

## Governance

- **Mandatory** for all dashboard UX work  
- PR review checklist (30 items) required  
- P0 UX debt blocks release  
- Amendments require version bump + changelog  

---

## Validation

- Cross-reviewed against PB-001, O-001, UX-001, PR-002 v2, PP-001, PR-001  
- Benchmarked against Linear, Stripe, GitHub, Vercel, Discord, Notion, Slack patterns  
- **No implementation performed**

---

## Suggested next steps

1. Link PX-001 from `docs/architecture/README.md` and PR template  
2. Run existing page specs (Overview, Subscription, Tickets) against §20 checklist  
3. Add `UX-` backlog items from PR-001 P0/P1 into project backlog with PX-001 severity  
4. Require “PX-001 checklist” section in dashboard PR descriptions  

---

## Related docs

- [Product Experience Architecture](../ux/product-experience-architecture.md)
- [Design System PP-001](../design/design-system.md)
- [Mission Control PR-002 v2](../reviews/overview-redesign-v2.md)
- [Product Blueprint PB-001](../blueprint/product-blueprint.md)
