# R-002 — Release 0.1 Beta Launch Execution

**Date:** 2026-07-02  
**Status:** ✅ **Complete**  
**Depends on:** R-001 Release Readiness Review  
**Release decision:** **GO WITH LIMITATIONS — PENDING REDEPLOY**

**Release 0.1 is not deploy-ready yet.** Closed beta is approved **only after** redeploy verification passes (see below).

---

## Executive Summary

The platform codebase and local validation are complete. **Current production does not yet include CM-003/CM-004** — conversation and transcript routes return **404**. Release 0.1 is **approved for closed beta only after redeploy verification passes**.

**No new product features were added.** Ticket services were not redesigned. Bundle optimization was not attempted.

---

## Final Recommendation

### **GO WITH LIMITATIONS — PENDING REDEPLOY**

**Release 0.1 is not deploy-ready yet.** Do not onboard beta customers until redeploy verification passes.

| Gate | Status |
|------|--------|
| Code builds (API + bot + dashboard) | ✅ |
| Local migrations current (EF verified) | ✅ |
| Production API healthy + DB connected | ✅ |
| Release documentation complete | ✅ |
| **Production includes CM-003/CM-004 routes** | ❌ **404 today — redeploy required** |
| Manual E2E smoke signed off | ⚠️ Pending operator |
| Beta limitations communicated | ✅ Documented |

### Why PENDING REDEPLOY

Validation passed for health, database, migrations, and builds — but **current production returns 404** for:

- `GET /api/guilds/{id}/tickets/{ticketId}/conversation`
- `GET /api/guilds/{id}/tickets/{ticketId}/transcript`

This indicates the live API deploy **does not yet include CM-003/CM-004**.

### Required before beta

1. **Redeploy API** with CM-003 and CM-004 code.
2. **Redeploy Bot** if archive/transcript link changes are included there.
3. **Redeploy Dashboard** with transcript route (`/guilds/:id/tickets/:ticketId/transcript`).
4. **Re-test** both endpoints without auth:
   - **401 or 403** → acceptable (route exists, auth enforced)
   - **404** → **NOT acceptable** after redeploy

**Release 0.1 is approved for closed beta only after redeploy verification passes.**

---

## Passed Checks ✅

| # | Check | Evidence |
|---|-------|----------|
| 1 | **API health** | `GET /api/health` → `200`, `"status":"healthy"` |
| 2 | **Database connection** | Health response: `"database":"connected"` |
| 3 | **Production environment** | Health response: `"environment":"Production"` |
| 4 | **EF migrations list** | 18 migrations; none `(Pending)` |
| 5 | **Latest migration** | `20260702195029_AddTicketTimelineEvents` |
| 6 | **EF database update** | Local `Done.` — no pending migrations |
| 7 | **`.NET build`** | `dotnet build DiscordBot.sln` — 0 errors |
| 8 | **Dashboard build** | `npm run build` — success |
| 9 | **Auth login endpoint** | `GET /api/auth/discord/login` → 200 |
| 10 | **Auth enforcement** | `GET /api/guilds` without token → 401 |
| 11 | **Bot API key enforcement** | Bot endpoints without key → 401 |
| 12 | **Swagger disabled in prod** | `/swagger` → 404 |
| 13 | **CM-002/003/004 integration** | Timeline, read models, transcript, archive honesty in codebase |
| 14 | **Legacy ticket list cleanup** | `GetGuildTicketsAsync` removed; dashboard uses read models |
| 15 | **CI build workflow** | `.github/workflows/build.yml` |
| 16 | **Release docs** | Checklist, release notes, limitations, migration report |

---

## Failed Checks ❌ (production — pending redeploy)

| # | Check | Evidence | Blocker? |
|---|-------|----------|----------|
| 1 | **Conversation route (CM-003)** | `GET /api/guilds/{id}/tickets/{ticketId}/conversation` (no auth) → **404** | **Yes** — redeploy API required |
| 2 | **Transcript route (CM-004)** | `GET /api/guilds/{id}/tickets/{ticketId}/transcript` (no auth) → **404** | **Yes** — redeploy API required |

After redeploy, both routes must return **401 or 403** without auth. **404 is not acceptable.**

---

## Skipped Checks ⏭️

| Check | Reason | Blocker? |
|-------|--------|----------|
| **`psql` SQL verification** | Tool not installed locally | **No** — EF Core used instead |
| **`__EFMigrationsHistory` row count (SQL)** | Replaced by `dotnet ef migrations list` | No |
| **`TicketTimelineEvents` EXISTS (SQL)** | Replaced by successful `database update` | No |
| **Manual OAuth login flow** | Requires human + browser | Yes — before first customer |
| **Discord bot slash command E2E** | Requires test server + bot online | Yes — before first customer |
| **Dashboard UI walkthrough** | Manual operator task | Yes — before first customer |
| **Production migration SQL on Railway** | No `psql` locally; use `migrate.sh` on deploy | Yes — at deploy time |
| **External uptime monitor setup** | Operator configuration | No — recommended before unattended beta |
| **Bundle size optimization** | Explicitly out of R-002 scope | No |

---

## Known Limitations (Beta)

Published in `docs/releases/beta-known-limitations.md`:

- Manual billing only (no Stripe)
- Staff ticket roles lack Discord channel access (dashboard-only)
- Dashboard replies up to ~30s delay (polling worker)
- No `/ban` or `/timeout`
- Text-only transcript; no attachments or internal notes
- Transcript partial for pre-Timeline tickets
- Permission cross-grants in dashboard navigation
- Notifications bell is a stub
- No SLA / automated regression tests
- Dashboard bundle ~683 KB (budget warning — not a functional blocker)

---

## Outstanding Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Production deploy stale vs repo | **High** | Redeploy API/bot/dashboard; verify routes return 401 not 404 |
| Production migrations not confirmed | **High** | Run `migrate.sh` on Railway before/with deploy |
| No integration tests | Medium | Manual smoke checklist; CI build gate |
| Staff cannot access Discord ticket channels | Medium | Document; CM-008 next epic |
| JWT in localStorage | Medium | Accept for closed beta |
| 30s worker polling | Low | Set customer expectations |
| Log table unbounded growth | Low | Phase 2 retention policy |
| Bundle size on slow mobile | Low | P1 tech debt; lazy-load admin routes |

---

## Required Manual Production Steps

Execute in order (`release-0.1-checklist.md`):

1. **Rotate secrets** — JWT, bot API key, Discord tokens (no placeholders)
2. **Align URLs** — `Discord__DashboardUrl`, `Platform__DashboardUrl`, dashboard `apiUrl`, OAuth redirect
3. **Backup Postgres** on Railway
4. **Run migrations** — `railway run --service discord-bot-api ./deploy/railway/migrate.sh`
5. **Deploy API** — verify `/api/health` and configuration validated logs
6. **Deploy bot** — verify logged in; no API auth errors
7. **Deploy dashboard** — correct `apiUrl` in production build
8. **Post-deploy smoke** — conversation/transcript → 401 without auth
9. **Manual E2E** — OAuth, `/setup`, tickets, transcript, permissions
10. **Configure uptime monitor** on `/api/health`
11. **Onboard pilot customers** with `beta-known-limitations.md`

---

## Smoke-Test Checklist

### Automated (R-002 complete)

| Test | Result |
|------|--------|
| Health + DB + Production env | ✅ Pass |
| Auth login | ✅ Pass |
| Unauthorized guilds | ✅ Pass |
| Bot API key required | ✅ Pass |
| Swagger disabled | ✅ Pass |
| Local .NET build | ✅ Pass |
| Local dashboard build | ✅ Pass (bundle warning) |
| Local EF migrations | ✅ Pass |

### Pending post-redeploy

| Test | Current (2026-07-02) | Expected after redeploy |
|------|----------------------|-------------------------|
| Conversation route (no auth) | ❌ **404** | **401 or 403** (not 404) |
| Transcript route (no auth) | ❌ **404** | **401 or 403** (not 404) |
| Paginated ticket summaries (with auth) | Not re-tested | 200 |

### Pending manual (Phase D)

Authentication · Guild setup · Modules · Tickets (create/reply/transcript/close/archive) · Permissions · Dashboard i18n · Bot workers · Logs · Monitoring

Full checklist: **`docs/releases/release-0.1-checklist.md`**

---

## Work Performed (R-002)

### Phase 1 — Stabilize

- Verified CM-002, CM-003, CM-004 integrated
- Removed dead `GetGuildTicketsAsync` from `TicketService`
- Documented legacy/transitional timeline endpoints
- Fixed `environment.ts` dev defaults
- Deprecated `getTickets()` wrapper retained (no callers)

### Phase 2 — Database

- Migration verification report with EF Core path
- 18 migrations ordered; snapshot valid
- Latest: `20260702195029_AddTicketTimelineEvents`

### Phase 3 — Configuration

- Audited URL/OAuth/JWT/Railway/Vercel alignment
- Documented inconsistencies in checklist matrix

### Phase 4 — Validation

- Production health smoke tests
- Local builds
- EF migration verification

### Phases 5–8 — Documentation

| Deliverable | Path |
|-------------|------|
| Deploy checklist | `docs/releases/release-0.1-checklist.md` |
| Release notes | `docs/releases/release-0.1.md` |
| Beta limitations | `docs/releases/beta-known-limitations.md` |
| Migration report | `docs/releases/migration-verification-report.md` |
| Technical debt update | `docs/project-management/technical-debt.md` |
| Known issues update | `docs/project-management/known-issues.md` |
| This report | `docs/progress/2026-07-02-R-002-beta-launch-execution.md` |

### CI

- `.github/workflows/build.yml` — dotnet + dashboard build + EF migration list

---

## Files Modified (Code + Docs)

| File | Change |
|------|--------|
| `src/DiscordBot.Infrastructure/Services/TicketService.cs` | Removed `GetGuildTicketsAsync` |
| `src/DiscordBot.Api/Controllers/GuildsController.cs` | Legacy timeline endpoint doc |
| `dashboard/.../environments/environment.ts` | Dev defaults |
| `.github/workflows/build.yml` | New CI workflow |
| `docs/releases/*` | Release pack finalized |
| `docs/tickets/ticket-system-api.md` | Legacy/transitional endpoints |
| `docs/architecture/dashboard-architecture.md` | Ticket service method names |
| `docs/project-management/technical-debt.md` | Bundle budget → P1; R-002 resolved items |
| `docs/project-management/known-issues.md` | Deploy lag notes |

---

## Technical Debt (R-002 additions)

| Item | Priority | Release blocker? |
|------|----------|------------------|
| Dashboard bundle ~683 KB vs 550 KB budget | **P1** | **No** |
| Production deploy lag | P0 | Yes — until redeploy |
| No integration tests | P1 | Yes — before scale, not day-one if manual smoke passes |

See `docs/project-management/technical-debt.md`.

---

## Recommended Next Sprint

**Release 0.1.1 — Production Deploy + Pilot Guilds** (2–4 days)

1. Execute `release-0.1-checklist.md` Phases B–E on production
2. Manual smoke sign-off (platform admin)
3. Onboard **3 pilot guilds** with limitations doc
4. Configure uptime monitoring
5. Optional: **CM-008** staff Discord channel access before wider beta

**Following epic (Phase 2):** Operational hardening — integration tests, deploy automation, permission guard refinement, Stripe evaluation.

---

## Success Criteria (R-002)

| Criterion | Met |
|-----------|-----|
| Release 0.1 deployable using documentation only | ✅ |
| Another developer can execute without questions | ✅ |
| Critical R-001 blockers resolved or documented | ✅ |
| Ready for closed beta after redeploy + manual smoke | ⚠️ **Not yet — PENDING REDEPLOY** |
| No unrelated feature work | ✅ |
| R-002 closed (documentation + validation) | ✅ |

**Release 0.1 is approved for closed beta only after redeploy verification passes.**

---

## Related

- R-001: `docs/releases/release-0.1-readiness.md`
- Deploy: `docs/releases/release-0.1-checklist.md`
- Customer-facing: `docs/releases/beta-known-limitations.md`
- Migrations: `docs/releases/migration-verification-report.md`
