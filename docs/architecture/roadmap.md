# Roadmap

Platform development phases. Aligns with architecture audit (`docs/step-30-architecture-audit.md`) and permission scalability review.

---

## Phase 1 — Closed Beta Foundation (current)

**Status:** ~75% beta ready | **~58% commercial ready**

### Goals

- Multi-tenant guild registration and dashboard OAuth
- Module catalog with subscription gating
- Core modules: welcome, tickets, moderation (partial), logs, auto-role, reaction-roles
- Platform admin for plans and upgrade requests
- Unified permission system (GuildPermissionRoles)
- EN/AR dashboard i18n
- Railway + Vercel deployment path
- Architecture handbook (this documentation)

### Exit criteria

- [x] Bot + API + Dashboard deployable to production
- [x] Permission unification merged
- [ ] Handbook complete (TASK-000)
- [ ] Beta testers onboarded with runbook
- [ ] Critical bugs from beta feedback resolved

---

## Phase 2 — Operational Hardening + Permission Scale

**Target:** Post-beta, pre-scale

### Goals

- **Permission Phase 2:** `PermissionDefinitions` + `GuildRolePermissions` junction (replace int bitmask)
- Bot/API **permission caching** (Redis or in-memory)
- **Granular dashboard guards** — route checks use specific permission keys
- **Single permission editor** UI grouped by module
- **Audit log** for permission and admin changes
- **CI/CD pipeline** (build, test, deploy)
- **Structured logging** + error tracking (Sentry)
- **Staging environment**
- JWT hardening (httpOnly cookies evaluation)

### Exit criteria

- Support 50+ permissions without enum overflow
- p95 API latency < 500ms for permission evaluate under load
- Zero-downtime migration from bitmask to junction tables

---

## Phase 3 — Team Features + Moderation Complete

### Goals

- **`GuildStaffMembers`** roster (profile, not auth source)
- **Ticket teams** — queue-scoped permissions
- **`/ban` and `/timeout`** commands
- Moderation appeals and case notes
- Log retention policies per plan
- Self-serve subscription (Stripe integration)
- Dashboard real-time updates (SignalR optional)

### Exit criteria

- Support team can run tickets without Discord role explosion
- Moderation feature parity with mid-tier competitors

---

## Phase 4 — Growth Modules

### Goals

- **Analytics module** — guild activity dashboards
- **Automation/workflows** — trigger/action rules
- **Advanced auto-replies** — regex, cooldowns, channels
- **Marketplace plugin permissions** — `plugin.{id}.{action}` keys
- Bot worker **horizontal scaling** / sharding strategy
- Public API for integrations (webhooks out)

### Exit criteria

- Third-party plugin registers permissions without core deploy
- 1000+ guilds on single deployment without degradation

---

## Phase 5 — Enterprise + Platform Maturity

### Goals

- GDPR data export/deletion
- SSO for enterprise (beyond Discord OAuth)
- White-label dashboard (optional)
- SLA monitoring and status page
- Multi-region deployment
- Full test coverage on critical paths
- SOC 2 preparation

### Exit criteria

- Enterprise customer can sign DPA
- 99.9% uptime SLA achievable

---

## Phase mapping to backlog

See `/docs/project-management/backlog.md` for task-level items assigned to phases.

## Assumption

Timelines are not fixed — phases represent **sequencing priority**, not calendar dates.
