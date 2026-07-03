# PX-002 — Product Decision Architecture

**Date:** 2026-07-03  
**Status:** Complete — documentation only  
**Document ID:** PX-002  
**Deliverable:** [Product Decision Architecture](../ux/product-decision-architecture.md)

---

## Summary

Created the **official Product Decision Architecture** for the Discord Bot Platform — the deterministic authority that decides **what to show, when, why, and what wins** when multiple situations occur simultaneously.

Sits between **Product Blueprint / PX-001** and **UI implementation**. Engineers building the Mission Engine should not make product decisions — they implement PX-002 rules.

**No code. No APIs. No Angular.**

---

## Problem solved

The platform previously scattered product judgment across:

- `GuildOverviewExperienceService` recommendation scoring  
- PR-002 v2 hero precedence (page-level)  
- Per-page widgets and badges  

PX-002 centralizes this into one **Mission Resolver** pipeline producing a canonical **Mission object**.

---

## Document structure delivered

| § | Topic | Status |
|---|-------|--------|
| 1 | Purpose — Product vs UX vs Decision vs UI vs Mission Engine | ✅ |
| 2 | Decision philosophy — 10 principles (DP-01–DP-10) | ✅ |
| 3 | Mission object — 22 fields specified | ✅ |
| 4 | Priority levels — 7-level total order | ✅ |
| 5 | Mission resolver — pipeline architecture | ✅ |
| 6 | Conflict resolution — tie-breakers + precedence table | ✅ |
| 7 | Mission lifetime — create → resolve → supersede | ✅ |
| 8 | Dismiss rules — 5 categories (Never / Snooze / Session / Auto) | ✅ |
| 9 | Personas — Owner, Staff, Support, Platform Admin, Enterprise | ✅ |
| 10 | Permission integration — module before permission | ✅ |
| 11 | Mission catalog — 30+ permanent MissionIds | ✅ |
| 12 | Future AI — rules decide; AI recommends only | ✅ |
| 13 | Analytics — 6 mission lifecycle events | ✅ |
| 14 | Governance — MissionId permanence, change process | ✅ |
| 15 | Examples — 7 realistic scenarios | ✅ |

---

## Key architectural decisions

| Decision | Rationale |
|----------|-----------|
| **One winning Mission** | PX-001 P-02 — one primary CTA |
| **7 priority levels** | Critical → History — only 1–5 win Mission Card |
| **Frozen precedence table** | Deterministic conflicts — no engineering judgment |
| **MissionId permanence** | Analytics + i18n stability |
| **Separate admin catalog** | Platform admin missions ≠ guild Overview |
| **Beginner vs Veteran** | Setup missions suppressed after `firstValueAchieved` |
| **AI cannot select winner** | Trust + auditability |

---

## Mission catalog highlights

| Category | Example MissionIds |
|----------|-------------------|
| Platform health | BotOffline, SynchronizationStale |
| Billing | PaymentRejected, SubscriptionExpiringSoon, SubscriptionExpired |
| Setup | CompleteSetupConnect, CompleteSetupConfigure, CompleteSetupFirstValue |
| Operations | TicketBacklogCritical, TicketBacklogElevated |
| Growth | InviteStaff, EnableModule, CreateWelcome |
| Calm | EverythingOperational |

---

## Document hierarchy (updated)

```
PB-001  Product Blueprint
PX-001  Product Experience Architecture
PX-002  Product Decision Architecture  ← NEW
UL-001  Ubiquitous Language
Page specs (PR-002 v2, UX-001, O-001, …)
PP-001  Design System
Code    Mission Engine (future)
```

**PX-002 wins** over page-level precedence docs. **PX-001 wins** over PX-002 on experience principles (e.g. one CTA).

---

## Alignment with prior work

| Document | Integration |
|----------|-------------|
| PR-002 v2 | Mission Card = resolver output; stateKey → MissionId |
| PR-003 | One hero, no competing missions — codified in §6 |
| O-001 | First value gates Veteran mode; 3 setup phases |
| UX-001 | PaymentRejected, expiring, pending review missions |
| UL-001 | Subscription Change terminology in billing missions |
| PB-001 | Module before permission; dashboard-first |
| PR-001 | Fixes fake activation — `firstValueAchieved` in resolver inputs |

---

## Files created

| File | Purpose |
|------|---------|
| `docs/ux/product-decision-architecture.md` | Single decision authority |
| `docs/progress/2026-07-03-PX-002-product-decision-architecture.md` | This report |

---

## Suggested next steps

1. **MC-1 implementation** — align `GuildOverviewExperienceService` with Mission Resolver (rename to Mission Engine)  
2. Map PR-002 v2 `stateKey` → **MissionId** registry  
3. Add PX-002 checklist to dashboard PR template (extends PX-001 §20)  
4. Implement dismiss/snooze store (guildId + userId + MissionId)  
5. Wire analytics events §13  

---

## Success criteria met

- [x] Any engineer can build Mission Engine without product decisions  
- [x] Deterministic conflict resolution documented  
- [x] Full mission catalog with dismiss/expiry rules  
- [x] Persona + permission integration  
- [x] No code, no pseudo-code  
- [x] Respects PB-001, UL-001, PX-001, O-001, PR-002 v2  

---

## Related docs

- [Product Decision Architecture](../ux/product-decision-architecture.md)
- [Product Experience Architecture (PX-001)](../ux/product-experience-architecture.md)
- [Mission Control PR-002 v2](../reviews/overview-redesign-v2.md)
