# Mission

## Mission statement

Deliver a **production-grade Discord bot platform** that lets community operators run their server professionally — with configurable modules, fair subscription tiers, and a dashboard that non-developers can use.

## What we optimize for

1. **Correctness** — permissions, subscriptions, and module gates must not leak across guilds.
2. **Operability** — deployable on Railway/Vercel with documented configuration.
3. **Clarity** — one permission model, one module catalog, one API surface for the bot.
4. **Incremental delivery** — ship closed beta features before enterprise polish.

## What we do not optimize for (yet)

- Sub-millisecond bot latency (HTTP round-trip to API is acceptable at current scale)
- Full CQRS / event sourcing
- 100% test coverage
- Real-time dashboard push (polling and refresh are used today)

## Current phase mission (Beta → Early Commercial)

From the architecture audit (`docs/step-30-architecture-audit.md`):

- **Closed beta readiness:** ~75% — core flows work (invite, setup, modules, tickets, partial moderation, admin)
- **Full commercial readiness:** ~58% — gaps in analytics, advanced moderation, audit, caching, plugin extensibility

**Near-term mission:** stabilize beta, unify permissions (done), document architecture (this handbook), then Phase 2 permission catalog + operational hardening.

## Engineering mission

Every change should:

- Respect **dependency direction** (Domain has no dependencies; Bot does not access DB directly)
- Keep **bot behavior** aligned with **dashboard configuration**
- Document non-obvious decisions in ADRs or progress reports
- Avoid introducing a fourth permission or staff model
