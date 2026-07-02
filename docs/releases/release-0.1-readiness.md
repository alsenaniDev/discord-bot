# Release 0.1 — Readiness Review

**Review ID:** R-001  
**Date:** 2026-07-02  
**Reviewer role:** CTO / Engineering Leadership  
**Scope:** First real beta customers (closed beta, not commercial launch)  
**Question answered:** *Can Release 0.1 be safely given to the first real customers?*

---

## Executive Summary

Release 0.1 maps to **Phase 1 — Closed Beta Foundation** in the Product Blueprint. The platform delivers a credible multi-tenant loop: invite bot → `/setup` → configure modules in dashboard → operate tickets, moderation, logs, welcome, and reaction roles with EN/AR UI and manual subscription administration.

**Verdict: APPROVED WITH REQUIRED FIXES**

The codebase is substantially ready for a **small, coached beta cohort** (5–15 guilds). It is **not** ready for open signup, self-serve paid conversion, or positioning against Ticket Tool / Dyno at parity.

**Overall Release Readiness Score: 6.8 / 10** (closed beta bar)

### Why not full approval today

1. **Critical ticket work (CM-002–004) is uncommitted** — timeline, read models, transcript, and archive honesty exist locally but are not merged/deployed as a unit.
2. **Database migrations are manual and easy to miss** — production and new dev environments fail at runtime without `TicketTimelineEvents` and related migrations (observed in local testing).
3. **No automated tests or CI/CD** — regressions ship silently; every deploy is a manual judgment call.
4. **Ticket staff cannot access Discord channels** without native Admin/Manage Server — support teams must work dashboard-only unless they hold Discord admin roles.

### Why not “NOT READY”

Core flows work when migrations and env are correct: OAuth, guild registration, module gating, ticket create/close/reply, logs, moderation warn/kick, platform admin, and Railway/Vercel deployment artifacts exist with production config validation.

---

## Methodology

This review cross-checked:

- Product Blueprint (PB-001), Ubiquitous Language (UL-001), Ticket Domain Blueprint (D-001)
- Architecture Handbook, Read Model Architecture (AR-001), progress reports CM-001–004
- Source code: API controllers, Infrastructure services, Bot workers, Dashboard guards/routes
- Backlog, technical debt, known issues, beta readiness runbooks

Documentation alone was **not** trusted where stale reviews (CM-001, step-30 audit ticket sections, product blueprint transcript row) contradicted implemented code.

---

## Domain Evaluation

For each domain: **Completion %**, **Stability**, **Technical Risk**, **Commercial Readiness**, **Recommendation**.

| Domain | Completion | Stability | Tech Risk | Commercial | Recommendation |
|--------|------------|-----------|-----------|------------|----------------|
| **Platform** | 82% | Medium | Medium | Low | 🟡 Ship closed beta; document manual ops |
| **Authentication** | 85% | Medium-High | Medium | Medium | 🟡 Solid OAuth+JWT; scale cache later |
| **Authorization** | 78% | Medium | High | Low | 🟡 Unified permissions merged; cross-grants confuse |
| **Guild Management** | 80% | Medium-High | Low | Medium | ✅ Beta-ready with `/setup` coaching |
| **Dashboard** | 72% | Medium | Medium | Low | 🟡 Usable EN/AR; coarse guards, stub notifications |
| **Subscription** | 70% | High | Low | **None** | 🟡 Manual billing only — set expectations |
| **Tickets** | 68% | Medium | Medium | Low | 🟡 Usable after CM-002–004 deploy + migrations |
| **Moderation** | 45% | Medium | Low | Low | 🟡 Warn/kick MVP; no ban/timeout |
| **Logging** | 85% | High | Low | Medium | ✅ MVP complete for beta |
| **Modules** | 88% | High | Low | Medium | ✅ Plan gating end-to-end |
| **Bot** | 70% | Medium | Medium | Low | 🟡 Single worker, 30s polling, no cache |
| **API** | 80% | Medium-High | Medium | Medium | 🟡 Good patterns; monolithic controller debt |
| **Database** | 75% | Medium | **High** | Medium | 🔴 Manual migrations are launch risk |
| **Deployment** | 72% | Medium | High | Low | 🟡 Railway/Vercel path exists; no CI |
| **Monitoring** | 35% | Low | **High** | Low | 🔴 Health check only — ops blind spots |
| **Documentation** | 85% breadth / 70% freshness | Medium | Low | Medium | 🟡 Handbook strong; several stale pages |

---

## Readiness by Category

| Area | Status | Notes |
|------|--------|-------|
| OAuth + JWT login | ✅ Ready | One-time code exchange; 60 min JWT |
| Guild registration + sync | ✅ Ready | `/setup`, resource sync worker |
| Module catalog + plan gating | ✅ Ready | Six modules, bot guard |
| Manual subscription admin | ✅ Ready | Upgrade requests, admin assign |
| Dashboard EN/AR | ✅ Ready | i18n on core pages |
| Tickets create/close/reply | 🟡 Needs improvement | Works; deploy CM stack + migrations |
| Ticket transcript (Timeline) | 🟡 Needs improvement | Implemented locally, uncommitted |
| Archive honesty | 🟡 Needs improvement | CM-004 fixes misleading copy |
| Staff Discord ticket access | 🔴 Release blocker | Permission roles not in channel overwrites |
| Moderation warn/kick | ✅ Ready | For beta scope |
| Ban / timeout | 🟡 Needs improvement | Flags exist; commands absent (document) |
| Logs module | ✅ Ready | Dashboard + Discord delivery |
| Platform admin | ✅ Ready | Guilds, plans, upgrades |
| Production migrations | 🔴 Release blocker | Must apply all 18 before/with deploy |
| CI/CD + tests | 🔴 Release blocker | Zero test projects; no GitHub Actions |
| Rate limiting | 🟡 Needs improvement | Not required for tiny closed beta if URL private |
| Observability | 🔴 Release blocker | No APM/uptime alerts for unattended ops |
| Self-serve billing | 🔴 Out of scope | Manual only — not a 0.1 blocker if disclosed |

---

## Release Blockers

Fix before inviting **first real customers**.

### B-01 — Deploy ticket stack + apply all migrations

| | |
|---|---|
| **Impact** | API 500s (`TicketTimelineEvents` missing), broken tickets/transcript, permission schema mismatch |
| **Severity** | Critical |
| **Effort** | 0.5–1 day |
| **Solution** | Merge CM-002–004 + unified permissions; run `dotnet ef database update` on production via `deploy/railway/migrate.sh`; verify `__EFMigrationsHistory` includes `20260702151245_UnifyGuildPermissions` and `20260702195029_AddTicketTimelineEvents` |

**Evidence:** User hit `42P01: relation "TicketTimelineEvents" does not exist`; `known-issues.md` lists pending prod migration.

---

### B-02 — End-to-end production smoke test

| | |
|---|---|
| **Impact** | CORS/auth misconfig blocks entire product silently |
| **Severity** | Critical |
| **Effort** | 0.5 day |
| **Solution** | Execute `docs/step-24-beta-readiness.md` checklist including OAuth, `/setup`, ticket open/close/reply, transcript route, logs, AR locale switch |

---

### B-03 — Environment alignment (API URL, CORS, Discord OAuth)

| | |
|---|---|
| **Impact** | Login failures, API unreachable from dashboard |
| **Severity** | Critical |
| **Effort** | 2–4 hours |
| **Solution** | `Discord__DashboardUrl` must match Vercel origin exactly; `environment.production.ts` `apiUrl` must match Railway API; Discord redirect URI updated; bot `Platform__DashboardUrl` for archive transcript links |

**Evidence:** Hardcoded `apiUrl` in `dashboard/.../environment.production.ts`; backlog C-02.

---

### B-04 — Staff cannot see ticket Discord channels

| | |
|---|---|
| **Impact** | Support staff with `ViewTickets` must use dashboard only; Discord-native workflow broken — primary beta friction |
| **Severity** | High (blocker for teams expecting Discord-side support) |
| **Effort** | 2–3 days (CM-008) |
| **Solution** | Add channel overwrites for roles mapped to ticket permissions in `TicketCommandHandlers.BuildTicketOverwrites`, **or** document “dashboard-only support” in beta agreement until CM-008 ships |

**Evidence:** `TicketCommandHandlers.cs` — overwrites only owner + Admin/ManageGuild.

---

### B-05 — No CI/CD or automated tests

| | |
|---|---|
| **Impact** | Regressions reach production; no gate on migrations compiling |
| **Severity** | High for sustained beta; acceptable for day-one if team manually verifies |
| **Effort** | 3–5 days minimum viable CI (build + migrate check) |
| **Solution** | GitHub Actions: `dotnet build`, `npm run build`, optional `dotnet ef migrations list` against ephemeral Postgres; add 5–10 API integration tests on auth + guild access |

**Evidence:** No test projects in solution; `technical-debt.md` P0.

---

### B-06 — Operational monitoring gap

| | |
|---|---|
| **Impact** | Outages discovered by customers, not ops |
| **Severity** | High for unattended beta |
| **Effort** | 0.5–1 day |
| **Solution** | External uptime on `GET /api/health`; Railway log alerts; document incident runbook in `configuration-runbook.md` |

---

## Technical Debt

### Critical (blocks Release 0.1 if ignored)

| Item | Risk |
|------|------|
| Manual migration process | Production schema drift |
| Uncommitted CM-002–004 work | Deploy/code mismatch |
| No automated test gate | Silent regressions |

### High (should not block day-one closed beta if documented)

| Item | Risk |
|------|------|
| Permission cross-grants (`ViewTickets` → moderation page access) | Broader nav/access than intended |
| Coarse dashboard guards (`owner` vs `moderation`) | UX confusion |
| JWT in localStorage | XSS token theft |
| Bot HTTP permission call per command | Latency at scale |
| 30s polling workers | Reply/cleanup delay up to 30s |
| Dual ticket close paths (Discord immediate vs dashboard worker) | Inconsistent timing |

### Medium

| Item | Risk |
|------|------|
| GuildsController monolith (~1100 lines) | Maintenance cost |
| Log 200-row cap, no retention policy | DB growth over months |
| Stale documentation (CM-001 review, api-design pagination) | Operator confusion |
| Hardcoded production API URL in Angular env | Multi-env deploy friction |
| OAuth state in memory cache | Breaks if API scaled horizontally |

### Low

| Item | Risk |
|------|------|
| Bundle size budget exceeded (~683 KB) | Perf on slow devices |
| Notifications bell stub | Cosmetic confusion |
| EF tools version warning | Developer friction |
| No ADRs filed | Decision archaeology gap |

---

## Security Review

| Area | Status | Findings |
|------|--------|----------|
| **Authentication** | 🟡 | Discord OAuth with CSRF state; one-time auth code; JWT HMAC ≥32 chars enforced at startup |
| **Authorization** | 🟡 | Layered owner/admin/staff; bot API key on `/api/bot/*`; 404 on denied guild access (intentional) |
| **Secrets** | ✅ | Placeholders in repo; production validation rejects `YOUR_` values |
| **API** | 🟡 | Input validation on settings; ProblemDetails errors; no rate limiting |
| **Bot communication** | ✅ | Bot never touches DB; API key required; bot should have no public HTTP on Railway |
| **Dashboard** | 🟡 | JWT in localStorage; AuthInterceptor handles 401 |
| **Input validation** | ✅ | Guild settings validator; ticket message length limits |
| **Rate limiting** | 🔴 | None on `/api/auth/*` or bot endpoints — acceptable only for private closed beta |
| **Production safety** | ✅ | HTTPS enforced in Production config validation; Swagger disabled in Production |

**Pre-launch security actions:** Rotate all secrets for production; confirm bot networking disabled; restrict beta invite link; do not expose admin Discord user ID in docs.

---

## Operational Readiness

| Area | Status | Detail |
|------|--------|--------|
| **Deployment** | 🟡 | Dockerfiles + Railway toml + Vercel dashboard; manual migrate script |
| **Configuration** | ✅ | Runbook + startup validation; env example files |
| **Logging** | 🟡 | Console ILogger API/bot; product LogEntry table; no centralized APM |
| **Monitoring** | 🔴 | `/api/health` only; no Sentry/Datadog; no bot heartbeat |
| **Backup** | 🔴 | No documented Postgres backup/restore procedure for Railway |
| **Migrations** | 🔴 | 18 migrations; manual apply; no CI check |
| **Incident recovery** | 🟡 | Documented in runbook partially; no on-call playbook |

**18 migrations (must be applied in order):** from `InitialCreate` through `AddTicketTimelineEvents` — see `docs/architecture/database.md` and `Infrastructure/Migrations/`.

---

## Customer Experience Walkthrough

### First-time customer journey

| Step | Experience | Rating |
|------|------------|--------|
| 1. Discover product / invite bot | Clear if given beta guide | 🟡 |
| 2. Run `/setup` in Discord | Required; easy to skip | 🟡 Confusing if not coached |
| 3. Login dashboard (Discord OAuth) | Smooth when CORS correct | ✅ |
| 4. See server in list | Works for owner + permission roles | ✅ |
| 5. Onboarding checklist | Helpful but not a wizard | 🟡 |
| 6. Enable modules | Clear; plan limits surfaced | ✅ |
| 7. Configure welcome/tickets in Settings | Many fields; templates need explanation | 🟡 |
| 8. Run `/ticket setup` or set category | Two paths — confusing | 🟡 |
| 9. Open ticket (button/panel) | Works | ✅ |
| 10. Staff replies from dashboard | Up to ~30s delay | 🟡 |
| 11. Close ticket | Works; archive digest + transcript in dashboard | ✅ (after CM-004 deploy) |
| 12. View transcript after channel deleted | Works (Timeline-based) | ✅ |
| 13. Upgrade plan | Manual request → admin approval | 🟡 Set expectations |
| 14. Moderation from dashboard | Works for owner/staff with access | 🟡 |
| 15. Arabic locale | Works on core pages | ✅ |

### Confusing steps to address in beta onboarding

1. **Must run `/setup` after bot invite** — server won’t appear otherwise.
2. **Tickets need category + `/ticket setup` or settings** — dual enable path.
3. **Staff with ticket roles don’t get Discord channel access** — must use dashboard.
4. **404 on guild pages** — means no access, not missing data (explain in beta guide).
5. **Old tickets have empty transcript** — pre-Timeline history not backfilled.
6. **No `/ban` or `/timeout`** — competitors expect these; flags exist but commands don’t.
7. **Notifications bell is non-functional** — remove or label “coming soon”.

---

## Release Checklist

### Pre-launch (required)

- [ ] Merge and tag CM-002, CM-003, CM-004 + unified permissions work
- [ ] `dotnet build DiscordBot.sln` — zero errors
- [ ] `npm run build` (dashboard) — zero errors
- [ ] Apply all EF migrations on production PostgreSQL
- [ ] Rotate production secrets (Discord, JWT, Bot API key)
- [ ] Set Railway API env vars per `deploy/railway/railway.env.example`
- [ ] Set Railway Bot env vars; **disable public networking**
- [ ] Set Vercel dashboard env; verify `apiUrl` matches Railway
- [ ] Set `Discord__DashboardUrl`, `Discord__RedirectUri`, `Platform__DashboardUrl` consistently
- [ ] Discord Developer Portal: OAuth redirect URIs, bot intents, invite URL
- [ ] Seed platform admin via `Admin__DiscordUserId`
- [ ] Run full smoke test (`step-24-beta-readiness.md` + transcript route)
- [ ] Publish beta limitations addendum (staff channel access, reply delay, manual billing)
- [ ] Configure external uptime monitor on `/api/health`

### Post-launch (first week)

- [ ] Monitor Railway logs daily
- [ ] Track migration history on prod after each deploy
- [ ] Collect beta feedback in structured form
- [ ] Update stale docs (CM-001 review banner, beta-tester-guide ticket section)
- [ ] File ADR-0001 for unified permissions

---

## Scoring (0–10)

| Dimension | Score | Rationale |
|-----------|-------|-----------|
| Architecture | 7.5 | Clean API/bot split, read models emerging, permission debt acknowledged |
| Code Quality | 6.5 | Consistent patterns; large controllers; zero tests |
| Security | 6.0 | Solid baseline; no rate limits; localStorage JWT |
| Documentation | 7.5 | Excellent handbook; stale ticket/audit sections |
| User Experience | 6.5 | EN/AR dashboard good; permission/nav confusion; polling delays |
| Deployment | 6.0 | Railway/Vercel ready; manual migrations; no CI |
| Maintainability | 6.0 | Unified permissions helped; monoliths and no tests hurt |
| Commercial Readiness | 4.0 | Manual billing; incomplete moderation; not self-serve |
| **Overall (closed beta)** | **6.8** | Coached cohort viable after required fixes |

---

## Recommendation

### **APPROVED WITH REQUIRED FIXES**

Release 0.1 may go to **first real customers** as a **closed, coached beta** once blockers B-01 through B-03 are complete and B-04 is either fixed or explicitly accepted in writing with customers.

Do **not** approve for:

- Open public signup
- Paid self-serve conversion
- “Ticket Tool replacement” marketing
- Unattended production operation without health monitoring (B-06)

---

## Suggested Next Epic

**Epic: Beta Launch Hardening (Release 0.1.1)**

1. CM-008 — Staff Discord channel access for ticket roles  
2. CI pipeline (build + migration verify)  
3. Minimal API integration test suite  
4. Uptime monitoring + backup runbook  
5. Beta guide refresh (transcript, conversation, limitations)

**Estimated time to beta-ready:** **5–8 engineering days** (assuming 1 engineer, includes deploy + smoke test + doc updates).

---

## R-002 Follow-up (2026-07-02)

Launch execution sprint **R-002** completed local validation and release documentation. It **does not** supersede the R-001 readiness assessment — it adds a deploy gate.

### **GO WITH LIMITATIONS — PENDING REDEPLOY**

**Release 0.1 is not deploy-ready yet.** Closed beta is approved **only after** redeploy verification passes.

| R-002 validation | Result |
|------------------|--------|
| API health | ✅ Pass |
| Database connected | ✅ Pass |
| EF migrations (local) | ✅ Verified — none pending |
| Builds (API + dashboard) | ✅ Pass |
| Production conversation route | ❌ **404** — CM-003 not deployed |
| Production transcript route | ❌ **404** — CM-004 not deployed |

**Required before beta:**

1. Redeploy **API** with CM-003 and CM-004 code.
2. Redeploy **Bot** if archive/transcript link changes are included.
3. Redeploy **Dashboard** with transcript route.
4. Re-test without auth — **401 or 403 acceptable; 404 not acceptable.**

See `docs/progress/2026-07-02-R-002-beta-launch-execution.md` and `docs/releases/release-0.1-checklist.md`.

---

## Related Documents

- `docs/blueprint/product-blueprint.md` — Phase 1 scope
- `docs/step-24-beta-readiness.md` — Deploy smoke checklist
- `docs/beta-tester-guide.md` — Customer-facing guide (needs ticket section update)
- `docs/project-management/known-issues.md` — Active issues
- `docs/project-management/technical-debt.md` — Prioritized debt
- `docs/progress/2026-07-02-R-001-release-readiness.md` — Progress report summary
- `docs/progress/2026-07-02-R-002-beta-launch-execution.md` — Launch execution; **PENDING REDEPLOY** gate
- `docs/releases/release-0.1-checklist.md` — Deploy and smoke checklist

---

*This review did not modify code, migrations, or features.*
