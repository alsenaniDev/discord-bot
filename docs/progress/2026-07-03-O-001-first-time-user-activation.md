# O-001 — First-Time User Activation Blueprint

**Date:** 2026-07-03  
**Status:** Complete (design only)  
**Sprint:** O-001  
**Deliverable:** [First-Time User Activation Blueprint (O-001)](../ux/first-time-user-activation.md)

---

## Summary

Created the official **First-Time User Activation** blueprint for the Discord Bot Platform. The document shifts the north-star metric from **guild linked** to **first module value achieved** with a **TTFV target under 5 minutes**.

This is a **product + UX + domain design** deliverable only. No code, APIs, migrations, or Angular components were created.

The blueprint supersedes setup-only onboarding semantics (Step 19 checklist) while proposing evolution of existing `OnboardingService` infrastructure in future sprints.

---

## Journey

Designed end-to-end path:

**Landing → Login → Add Bot → Link Guild → Permissions Check → Module Selection → Configure First Module → First Success → Congratulations → Recommended Next Steps**

Includes Mermaid user journey diagram, bot readiness gate, module-specific first-value detection table, and Discord ↔ dashboard loop guidance aligned with `/setup` and live `/servers` onboarding hero.

---

## Checklist

Redesigned activation checklist in three phases:

| Phase | Focus |
|-------|-------|
| **A — Connect** | Login, add bot, link guild (35%) |
| **B — Activate** | Goal selection, enable module, configure, **first value** (65%) |
| **C — Expand** | Staff, logs, subscription review (optional to 100%) |

**Activation milestone** at ≥85% (Phase A + B). Progress messaging and reward copy specified for EN/AR.

---

## Wizard

Six-step **Welcome Wizard** (W0–W6) with per-step:

- Purpose, duration, CTA, error recovery, skip rules

Module-specific minimum configuration for Welcome, Tickets, Logs, Reaction Roles, Moderation, Auto Role.

Wizard flow diagram included; auto-resume and refresh-after-Discord behavior specified.

---

## Health Score

**Community Health** score **0–100** with nine weighted factors:

- Guild linked, bot online, modules enabled, activation completed, tickets/logs/permissions configured, subscription bonus, recent activity

Score bands (Needs attention → Thriving), breakdown UI concept, and tie-in to recommendations documented.

---

## Recommendation Engine

Priority-scored cards (`REC_WELCOME`, `REC_LOGS`, `REC_STAFF`, `REC_TICKET_PANEL`, `REC_UPGRADE`, `REC_RENEW`, etc.) with:

```
score = basePriority × urgency × relevance
```

Top 3 shown on Overview; progressive disclosure suppresses upsell before activation complete. Recommendation flow diagram included.

---

## Analytics

**35+ recommended events** (no implementation):

- Funnel: `GuildLinked`, `WizardStarted`, `WizardCompleted`, `ActivationGoalSelected`, `FirstValueAchieved`, `ActivationCompleted`
- Module: `TicketConfigured`, `PanelCreated`
- Engagement: `RecommendationShown`, `HealthScoreViewed`, `SuccessMomentShown`

Property conventions: `guildId`, `userId`, `moduleKey`, `ttfvMinutes`.

---

## Empty states

Designed **10 platform empty states** with illustration, headline, description, primary/secondary CTA:

No Guild, No Modules, No Tickets, No Staff, No Logs, No Subscription, No Activity, No Panels, No Permissions — all EN/AR ready.

---

## Admin perspective

Platform admin metrics defined:

- Guild activation %, median TTFV, abandoned step, setup failures, skipped modules, activation funnel with conversion stages

---

## Product principles

Ten activation principles documented (single primary action, no dead ends, celebrate progress, honest plan gates, i18n-first, etc.).

---

## Files changed

| File | Action |
|------|--------|
| `docs/ux/first-time-user-activation.md` | **Created** — official O-001 blueprint |
| `docs/progress/2026-07-03-O-001-first-time-user-activation.md` | **Created** — this report |

No application source files modified.

---

## Open questions

| # | Question | Recommendation |
|---|----------|----------------|
| 1 | Should **Tickets** or **Welcome** be the default activation goal on Free tier? | Default **Welcome** (fastest proof); offer Tickets as second card if plan allows |
| 2 | Auto-detect first value vs owner self-report? | **Auto-detect** primary; manual confirm fallback after 2 min polling |
| 3 | Replace Step 19 checklist API fields or extend? | **Extend** `OnboardingChecklistDto` with `activationGoal`, `firstValueAchieved`, `activationCompletedAt` in O-002 |
| 4 | Store activation state per guild or per owner? | **Per guild** (multi-tenant); owner sees aggregate on `/servers` |
| 5 | Health score visible before activation? | Show **"Setup in progress"** placeholder, not numeric score |
| 6 | Wizard modal vs dedicated route `/guilds/:id/activate`? | Dedicated route for shareable resume links; modal for first interrupt |
| 7 | Bot "online" signal — heartbeat API or infer from sync? | Short-term: `resourcesSyncedAt` recency; long-term: bot heartbeat endpoint |

---

## Recommendations

1. **O-002:** Extend `OnboardingService` + DTOs with activation fields; add `FirstValueAchieved` detectors per module in bot/API.
2. **O-003:** Welcome Wizard UI on `/guilds/:id/overview` with EN/AR strings.
3. **O-004:** Health score + recommendation cards on Overview (client-side rules first).
4. **O-005:** Platform admin activation funnel dashboard widgets.
5. **Align copy:** Migrate remaining "upgrade request" onboarding strings to **Subscription Change** where owners see them (UX-001/SB-003 already done for billing).
6. **Update Step 19 doc** with pointer to O-001 as superseding activation authority (keep as historical implementation reference).

---

## Suggested next sprint (O-002)

**Activation foundation — backend + analytics hooks**

1. Extend `OnboardingChecklistDto` with activation phase fields  
2. Implement `FirstValueAchieved` detection for Welcome + Tickets (highest beta demand)  
3. Emit analytics events to structured logs (no third-party yet)  
4. Update overview checklist UI to Phase A/B labels (no full wizard yet)  
5. Document API additions in architecture handbook  

**Success criteria:** TTFV measurable in logs for pilot guilds; checklist reflects first value not just config.

---

## Validation

| Check | Result |
|-------|--------|
| Code written | None (by design) |
| Blueprint sections 1–13 | Complete |
| Mermaid diagrams | 5 included |
| Alignment with PB-001, UL-001, UX-001, D-001, R-001 | Reviewed |
| Integration requirements for future modules | § end of blueprint |

---

## Related docs

- [First-Time User Activation Blueprint (O-001)](../ux/first-time-user-activation.md)
- [Product Blueprint (PB-001)](../blueprint/product-blueprint.md)
- [Subscription Experience (UX-001)](../ux/subscription-experience.md)
- [Step 19 — Customer Onboarding](../step-19-customer-onboarding.md)
- [Release 0.1 Readiness (R-001)](../releases/release-0.1-readiness.md)
