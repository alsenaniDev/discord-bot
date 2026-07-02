# PB-001 — Product Blueprint (Final Report)

**Date:** 2026-07-02  
**Task:** PB-001 — Create the Official Product Blueprint  
**Type:** Documentation only — no code changes

---

## Summary

Created the **official Product Blueprint** for the Discord Bot Platform — the highest-level product authority document for the project. It defines vision, mission, principles, non-goals, personas, domains, boundaries, competitive position, five-phase roadmap, success metrics, risks, five-year destination, and product philosophy.

The blueprint is written specifically for **this** codebase and business model: multi-tenant Discord bot SaaS (.NET API + Bot + Angular dashboard + PostgreSQL), six subscription-gated modules, manual billing today, EN/AR dashboard, unified permission roles, and ~75% closed-beta / ~58% commercial readiness per the architecture audit.

All future architecture, domain specs, features, and backlog items should align with `/docs/blueprint/product-blueprint.md`.

---

## Documents Created

| Path | Description |
|------|-------------|
| [docs/blueprint/product-blueprint.md](../blueprint/product-blueprint.md) | Official Product Blueprint (PB-001) — 13 required sections + appendices |

**Progress report:** this file.

---

## Major Product Decisions Documented

### Strategic positioning

1. **Product category:** Integrated **community operations control plane** — not a no-code bot builder, not an entertainment bot.
2. **Core wedge:** Dashboard-first operations + modular SaaS + bilingual (EN/AR) admin — not feature parity with MEE6 or Ticket Tool on day one.
3. **Tenant model:** One bot, one dashboard, one API per deployment; per-guild subscription (not per-seat).

### Scope boundaries

4. **Explicit non-goals:** Music, memes, games, no-code scripting, full Discord forensic logging, multi-platform chat, mobile-native admin, leveling as core product.
5. **Logging definition:** Platform "Logs" = **bot activity audit trail**, not replacement for Discord server audit log.
6. **Inside vs outside:** Bot/dashboard/API/PostgreSQL business logic inside; Stripe/SSO/Discord client as integrations or external.

### Domain architecture

7. **Sixteen product domains** defined with purpose, responsibilities, dependencies, future expansion, and maturity scores grounded in step-30 audit and CM-001 ticket review.
8. **Module system** remains the product packaging unit — new capabilities must map to modules and plans.

### Roadmap

9. **Five phases** codified: Beta Foundation → Hardening → Team Operations → Growth/Extensibility → Enterprise.
10. **Ticket System v1** explicitly placed in Phase 2 (aligned with CM-001), blocking credible Pro-plan marketing.

### Philosophy (binding)

11. **Control plane, not command bag** — dashboard visibility required for complete features.
12. **Honesty over demo magic** — directly addresses current ticket archive misleading copy.
13. **Single permission model convergence** — no third auth system.
14. **International by default** — EN + AR required for new dashboard strings.

---

## Questions That Remain Unanswered

These require product/business decisions outside PB-001 scope:

| # | Question | Why it matters |
|---|----------|----------------|
| 1 | **Stripe timeline and pricing changes?** | Manual billing cannot scale; exact launch date not set |
| 2 | **Pro plan marketing before Ticket v1?** | Risk selling incomplete support desk |
| 3 | **Auto-mod in scope for Phase 2 or 3?** | Competitive gap vs Dyno; effort vs ticket priority |
| 4 | **Bot-side i18n priority?** | Arabic dashboard but English bot responses — acceptable how long? |
| 5 | **Log retention tiers?** | Enterprise need; storage cost model undefined |
| 6 | **White-label / reseller?** | Mentioned Phase 5; no business model decision |
| 7 | **Self-host vs SaaS-only positioning?** | Stack is self-hostable; commercial strategy unclear |
| 8 | **Leveling module ever?** | Listed as deferred in non-goals; demand unknown |
| 9 | **Free tier module mix final?** | Free = welcome + logs today; is that permanently sufficient? |
| 10 | **Discord App Directory launch?** | Growth channel; timing and requirements not planned |

Recommend resolving **#1, #2, #4** before public launch marketing.

---

## Recommendations

### Documentation hierarchy

1. Add link to Product Blueprint at top of `/docs/architecture/README.md` and `/docs/product/vision.md` pointing to blueprint as canonical.
2. When CM/ticket or permission tasks complete, update **domain maturity** in blueprint appendix (lightweight version bumps).

### Product sequencing

3. **Do not** market Pro tier as "full support platform" until CM-002–CM-006 (ticket v1 foundation) ship.
4. **Prioritize Phase 2** items that unblock revenue: Stripe + permission scale + ticket v1 + ban/timeout.
5. Resolve **moderation vs dashboard permission split** in product messaging until technical convergence completes.

### Metrics

6. Instrument **activated guilds** (onboarding checklist complete) and **30-day retention** before scaling beta invites.
7. Track **dashboard MAU** separately from bot installs — key indicator for control-plane thesis.

### Risk mitigation

8. Fix ticket archive copy in next ticket sprint — violates documented principle #9 (Honesty).
9. File **ADR** when Stripe integration approach is chosen — billing is architectural.

---

## Suggested Next Task

**CM-002 — Ticket message persistence & Discord ingestion**

Rationale:
- Highest-severity product honesty gap identified in CM-001 and now elevated in Product Blueprint principles.
- Ticket domain maturity (~52% toward v1) is the largest drag on Pro-plan credibility.
- Unblocks transcript API, detail UX, and accurate archive — all Phase 2 exit criteria in blueprint.

Alternative if product priority shifts to revenue infrastructure first:

**PB-002 or backlog item — Stripe integration specification** (documentation + ADR, then implementation).

---

## Constraints Observed

- No code modified
- No migrations created
- No features implemented

---

## Document relationship

```
docs/blueprint/product-blueprint.md   ← NEW: canonical product authority
docs/architecture/                    ← How to build
docs/tickets/                         ← Ticket domain depth (CM-001)
docs/product/                         ← Detail docs (reference blueprint)
docs/progress/                        ← Task reports including this file
```

---

## Approval note

This blueprint synthesizes existing shipped behavior, architecture audit findings, CM-001 ticket review, and product docs. It should be reviewed by the product owner and revised via version increment + changelog when strategic decisions (#1–#10 above) are resolved.
