# Release 0.1 — Production Deployment Checklist

**Release:** 0.1 (Closed Beta Foundation)  
**Execution:** R-002 (complete)  
**Recommendation:** **GO WITH LIMITATIONS — PENDING REDEPLOY**

**Release 0.1 is not deploy-ready yet.** Code and local validation are complete; current production lacks CM-003/CM-004 routes (404). Closed beta is approved **only after** redeploy verification passes.

Use this checklist to deploy Release 0.1. Another engineer should complete deployment using only this document plus Railway, Vercel, and Discord Developer Portal access.

**Estimated time:** 2–4 hours (first deploy) · 30–60 minutes (subsequent deploys)

---

## R-002 Validation Summary (pre-deploy)

Automated checks completed **2026-07-02**:

| Check | Status |
|-------|--------|
| API health (`GET /api/health`) | ✅ PASSED — healthy, database connected, Production |
| Database connection (via health) | ✅ PASSED |
| EF migrations list (local) | ✅ PASSED — 18 migrations, none pending |
| Latest migration | ✅ `20260702195029_AddTicketTimelineEvents` |
| `dotnet build DiscordBot.sln` | ✅ PASSED |
| Dashboard `npm run build` | ✅ PASSED (bundle budget warning — **not a blocker**) |
| `psql` SQL verification | ⏭️ SKIPPED — tool unavailable; EF Core used instead |

**Not yet complete (required before beta customers):**

1. Redeploy API with CM-003/CM-004 code
2. Redeploy Bot (if archive/transcript link changes included)
3. Redeploy Dashboard with transcript route
4. Re-test conversation + transcript routes — expect **401/403**, not **404**
5. Complete manual smoke Phases D–E

---

## Prerequisites

- [ ] Git access to repository; `main` contains CM-002, CM-003, CM-004, and unified permissions
- [ ] Railway account with API, Bot, and Postgres services
- [ ] Vercel account (or Railway dashboard service) for Angular dashboard
- [ ] Discord Developer Portal access (Application → OAuth2, Bot)
- [ ] Platform admin Discord user ID for seeding

---

## Phase A — Secrets & Configuration

### A1. Generate / rotate secrets (Production)

Generate new values; **never commit secrets to git**.

| Secret | Min length | Used by |
|--------|------------|---------|
| `Jwt__Secret` | 32 chars | API |
| `Bot__ApiKey` / `Api__ApiKey` | Strong random | API + Bot (must match) |
| `Discord__ClientSecret` | From Discord Portal | API |
| `Discord__BotToken` / `Discord__Token` | From Discord Portal | API + Bot |

- [ ] Secrets rotated from any dev/placeholder values
- [ ] `Bot__ApiKey` on API **equals** `Api__ApiKey` on Bot

### A2. Railway — API service variables

Set in Railway → **discord-bot-api** → Variables:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
Discord__ClientId=<YOUR_CLIENT_ID>
Discord__ClientSecret=<SECRET>
Discord__BotToken=<BOT_TOKEN>
Discord__RedirectUri=https://<YOUR_API_DOMAIN>/api/auth/discord/callback
Discord__DashboardUrl=https://<YOUR_DASHBOARD_DOMAIN>
Discord__AllowVercelOrigins=true
Bot__ApiKey=<STRONG_KEY>
Jwt__Secret=<32+_CHAR_SECRET>
Jwt__Issuer=DiscordBot
Jwt__Audience=DiscordBot.Dashboard
Admin__DiscordUserId=<YOUR_DISCORD_USER_ID>
```

- [ ] All variables set (no `YOUR_` or `CHANGE_ME` placeholders)
- [ ] `Discord__RedirectUri` **exactly** matches Discord Portal OAuth redirect
- [ ] `Discord__DashboardUrl` **exactly** matches dashboard origin (HTTPS, no trailing slash mismatch)

Reference: `deploy/railway/railway.env.example`

### A3. Railway — Bot service variables

```
ASPNETCORE_ENVIRONMENT=Production
Discord__Token=<BOT_TOKEN>
Api__BaseUrl=https://<YOUR_API_DOMAIN>
Api__ApiKey=<SAME_AS_Bot__ApiKey>
Platform__DashboardUrl=https://<YOUR_DASHBOARD_DOMAIN>
```

- [ ] Bot service **public networking disabled** (worker only)
- [ ] `Api__BaseUrl` matches API Railway public URL

### A4. Dashboard build configuration

**Option A — Vercel:**

- [ ] Set `apiUrl` in `dashboard/DiscordBot.Dashboard/src/environments/environment.production.ts` to `https://<YOUR_API_DOMAIN>`
- [ ] Or inject at build time via your Vercel build command

**Option B — Railway Docker dashboard:**

- [ ] Set build arg `API_URL=https://<YOUR_API_DOMAIN>` (see `deploy/railway/Dockerfile.dashboard`)

- [ ] Dashboard `apiUrl` matches API public URL (HTTPS)

### A5. Discord Developer Portal

- [ ] OAuth2 redirect: `https://<YOUR_API_DOMAIN>/api/auth/discord/callback`
- [ ] Bot intents: **Server Members Intent** enabled (welcome, member sync)
- [ ] Bot permissions for invite URL documented in beta guide
- [ ] Application ownership verified

---

## Phase B — Database Migrations

**Do this before or immediately with API deploy that includes ticket Timeline code.**

- [ ] Backup production Postgres (Railway backup or manual dump)
- [ ] Run migrations:

```bash
railway run --service discord-bot-api ./deploy/railway/migrate.sh
```

- [ ] Verify 18 migrations applied (see `migration-verification-report.md`)

**Option A — EF Core (no `psql` required):**

```bash
dotnet ef migrations list \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api \
  --connection "$ConnectionStrings__DefaultConnection"
```

Expect latest: `20260702195029_AddTicketTimelineEvents` with no `(Pending)` entries.

**Option B — SQL (if `psql` available on ops workstation):**

```sql
SELECT COUNT(*) FROM "__EFMigrationsHistory";  -- expect 18
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;
-- expect 20260702195029_AddTicketTimelineEvents
```

- [ ] Confirm `TicketTimelineEvents` table exists (EF update success or SQL EXISTS check)

---

## Phase C — Deploy Services

### C1. Deploy API

- [ ] Push to branch connected to Railway API service
- [ ] Dockerfile: `deploy/railway/Dockerfile.api`
- [ ] Health check path: `/api/health`
- [ ] Deploy succeeds; logs show `Configuration validated`

### C2. Deploy Bot

- [ ] Deploy bot worker after API is healthy
- [ ] Logs show `Logged in as <bot>#<discriminator>`
- [ ] No repeated API auth failures in logs

### C3. Deploy Dashboard

- [ ] Build with correct `apiUrl`
- [ ] Deploy to Vercel or Railway
- [ ] Confirm dashboard loads at `https://<YOUR_DASHBOARD_DOMAIN>`

---

## Phase D — Post-Deploy Verification (Smoke Tests)

Run immediately after deploy. Record pass/fail.

### R-002 automated results (2026-07-02)

Production API: `https://discord-bot-production-b872.up.railway.app`

| Test | Result | Notes |
|------|--------|-------|
| **Health** `GET /api/health` | ✅ **PASS** | `200` — `"status":"healthy"`, `"database":"connected"`, `"environment":"Production"` |
| Auth login `GET /api/auth/discord/login` | ✅ **PASS** | `200` |
| Unauthorized `GET /api/guilds` | ✅ **PASS** | `401` |
| Bot API key `GET /api/bot/tickets/pending-cleanups` | ✅ **PASS** | `401` without key |
| Swagger `GET /swagger` | ✅ **PASS** | `404` (disabled in Production) |
| Conversation route (no auth) | ❌ **BLOCKED — PENDING REDEPLOY** | `404` — production missing CM-003; expect **401/403** after redeploy |
| Transcript route (no auth) | ❌ **BLOCKED — PENDING REDEPLOY** | `404` — production missing CM-004; expect **401/403** after redeploy |

**Post-redeploy gate (required before beta):**

| Test | Unacceptable | Acceptable |
|------|--------------|------------|
| `GET .../tickets/{ticketId}/conversation` (no auth) | **404** | **401 or 403** |
| `GET .../tickets/{ticketId}/transcript` (no auth) | **404** | **401 or 403** |

**Redeploy steps before re-test:**

1. Redeploy **API** with CM-003 + CM-004 code
2. Redeploy **Bot** if archive/transcript link changes are in this release
3. Redeploy **Dashboard** with transcript route
4. Re-run automated checks above

Local builds (R-002):

| Test | Result |
|------|--------|
| `dotnet build DiscordBot.sln` | ✅ PASS (0 errors) |
| `npm run build` (dashboard) | ✅ PASS — bundle ~683 KB vs 550 KB budget warning (**not a release blocker**) |
| EF migrations (local) | ✅ PASS — latest `20260702195029_AddTicketTimelineEvents`, none pending |

### API

| Test | Command / action | Expected | R-002 |
|------|------------------|----------|-------|
| Health | `GET https://<API>/api/health` | `200`, `"database":"connected"` | ✅ Pass |
| Auth login | `GET https://<API>/api/auth/discord/login` | `200` or redirect | ✅ Pass |
| Unauthorized guilds | `GET https://<API>/api/guilds` (no token) | `401` | ✅ Pass |
| Bot API key required | `GET https://<API>/api/bot/tickets/pending-cleanups` (no key) | `401` | ✅ Pass |
| Swagger disabled | `GET https://<API>/swagger` | `404` | ✅ Pass |

### Authentication (manual)

- [ ] Open dashboard → **Login with Discord** → lands on Servers page
- [ ] `GET /api/auth/me` with token returns user (via browser devtools or curl)

### Guild (manual)

- [ ] Invite bot to test server
- [ ] Run `/setup` in Discord
- [ ] Server appears in dashboard after refresh
- [ ] **Sync Discord Data** populates channel/role dropdowns

### Modules (manual)

- [ ] Enable/disable a module on Modules page
- [ ] Plan-locked module shows lock message on free plan

### Tickets (manual)

- [ ] `/ticket setup` + `/ticket open` creates ticket
- [ ] Ticket appears in dashboard list with pagination
- [ ] Dashboard reply queues (check within ~30s in Discord)
- [ ] Conversation panel loads on open ticket
- [ ] Close ticket → archive digest in Discord archive channel (if configured)
- [ ] **View transcript** on closed ticket loads Timeline entries
- [ ] Transcript works after ticket channel deleted

### Permissions (manual)

- [ ] Staff role with `ViewTickets` only can list tickets
- [ ] Staff with `ReplyToTickets` can send dashboard reply
- [ ] Staff with `CloseTickets` can close from dashboard
- [ ] User without permissions gets 404 / no access on guild pages

### Dashboard (manual)

- [ ] Navigation: Overview, Settings, Modules, Tickets, Logs, Staff
- [ ] Switch language EN ↔ AR on a tickets page
- [ ] Trigger API error (e.g. invalid save) → toast/error message shown

### Bot (manual)

- [ ] Slash commands respond (`/setup`, `/ticket`, `/warn`)
- [ ] Guild maintenance worker runs (ticket cleanup within 30s of dashboard close)

### Logs (manual)

- [ ] Enable logs module + log channel in Settings
- [ ] Trigger event (e.g. ticket open) → appears in dashboard Logs
- [ ] Optional: Discord log channel receives embed

### Monitoring

- [ ] Configure external uptime check on `GET /api/health` (UptimeRobot, Better Stack, etc.)
- [ ] Railway log alerts enabled for API 5xx (if available)

---

## Phase E — Beta Customer Onboarding

- [ ] Send `docs/beta-tester-guide.md` + `docs/releases/beta-known-limitations.md`
- [ ] Confirm customer Discord ID for platform admin (if needed)
- [ ] Schedule check-in within 48 hours of onboarding

---

## Phase F — Rollback Plan

If deploy fails:

1. Stop onboarding new beta customers
2. Restore Postgres from backup if migration caused issues
3. Redeploy previous known-good API/bot/dashboard images
4. Post incident note in team channel

---

## Configuration Consistency Matrix

All four must use the **same dashboard origin**:

| Setting | Location |
|---------|----------|
| `Discord__DashboardUrl` | Railway API |
| `Platform__DashboardUrl` | Railway Bot |
| `apiUrl` | Dashboard production build |
| OAuth redirect | Discord Portal → API callback only (not dashboard) |

All three must use the **same API origin**:

| Setting | Location |
|---------|----------|
| Railway API public URL | API service |
| `Api__BaseUrl` | Railway Bot |
| `apiUrl` | Dashboard production build |

---

## Sign-off

| Role | Name | Date | Decision |
|------|------|------|----------|
| Release engineer | R-002 | 2026-07-02 | **GO WITH LIMITATIONS — PENDING REDEPLOY** |
| Platform admin | | | Pending redeploy + manual smoke |

**GO WITH LIMITATIONS — PENDING REDEPLOY means:**

- ✅ Codebase builds; local migrations current; production API healthy + DB connected
- ❌ **Not deploy-ready yet** — production returns **404** for conversation/transcript (CM-003/CM-004 not deployed)
- ⚠️ **Required before beta:** redeploy API + bot + dashboard; re-test routes (401/403 OK, 404 NOT OK)
- ⚠️ Manual smoke (Phases D–E) after redeploy passes
- ⚠️ Accept documented beta limitations (`beta-known-limitations.md`)

**Release 0.1 is approved for closed beta only after redeploy verification passes.**

**Related docs:** `release-0.1.md` · `beta-known-limitations.md` · `migration-verification-report.md` · `docs/progress/2026-07-02-R-002-beta-launch-execution.md` · `docs/step-24-beta-readiness.md`
