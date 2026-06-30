# Step 24 — Final Beta Readiness

Final preparation before first beta users. **No new product features** — deployment safety, logging, docs, and verification only.

**Chosen stack:**

| Component | Platform |
|-----------|----------|
| PostgreSQL | Railway |
| API | Railway |
| Bot worker | Railway (no public HTTP) |
| Dashboard | **Vercel** |

---

## Production safety

### Startup validation

| Service | Behavior |
|---------|----------|
| **API** | `ValidateRequiredConfiguration()` in `Program.cs` — **strict in Production** (throws with clear message), warnings in Development |
| **Bot** | Same pattern in `Program.cs` before `host.Run()` |

Production checks include:

- All required env vars present
- No placeholder values (`YOUR_`, `CHANGE_ME`, `your-domain.com`, `REPLACE_WITH`)
- HTTPS URLs for `Discord:RedirectUri`, `Discord:DashboardUrl`, `Api:BaseUrl`, `Platform:DashboardUrl`
- `Jwt:Secret` ≥ 32 characters

### Secrets in repo

Committed configs use **placeholders only**. Real values go in:

- Railway service variables (Production)
- `appsettings.Development.local.json` (local, gitignored)

Verify before push:

```bash
grep -rE "ClientSecret|BotToken|\.Gg" src --include="*.json" | grep -v example | grep -v YOUR_
```

Should return **no real secrets**.

### `.gitignore`

Ignores: `.env`, `*.local.json`, `appsettings.Production.json`, `dist/`, `node_modules/`, `environment.local.ts`

---

## Health checks

| Endpoint | Expected |
|----------|----------|
| `GET /api/health` | `200` when healthy |

Response includes:

```json
{
  "status": "healthy",
  "service": "DiscordBot.Api",
  "database": "connected",
  "environment": "Production",
  "timestamp": "..."
}
```

Returns `503` if PostgreSQL is unreachable. Railway health check path: `/api/health`

---

## Logging

| Area | Status |
|------|--------|
| API | `ILogger` via middleware + startup validation logs |
| Bot | `ILogger` in hosted services, API client, sync worker |
| `Console.WriteLine` | None in `src/` |
| Bot startup | Logs Gateway connection start + "Logged in as …" |
| API failures | `BotApiClient` logs warnings/errors with guild IDs |
| Sync failures | `ResourceSyncService` + `GuildResourceSyncWorker` log errors |

View logs: Railway → Service → **Deployments** → **View Logs**

---

## Railway — API service

| Setting | Value |
|---------|-------|
| Source | GitHub repo |
| Dockerfile | `deploy/railway/Dockerfile.api` |
| Config file | `deploy/railway/railway.api.toml` |
| Public networking | **On** — generate domain |
| Health check | `/api/health` |

### API variables (exact names)

```
ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true

Discord__ClientId=YOUR_DISCORD_CLIENT_ID
Discord__ClientSecret=YOUR_DISCORD_CLIENT_SECRET
Discord__BotToken=YOUR_DISCORD_BOT_TOKEN
Discord__RedirectUri=https://YOUR_API_DOMAIN/api/auth/discord/callback
Discord__DashboardUrl=https://YOUR_VERCEL_DOMAIN

Jwt__Secret=YOUR_LONG_RANDOM_SECRET_MIN_32_CHARS
Jwt__Issuer=DiscordBot
Jwt__Audience=DiscordBot.Dashboard

Bot__ApiKey=YOUR_STRONG_BOT_API_KEY
Admin__DiscordUserId=YOUR_DISCORD_USER_ID
```

Replace `YOUR_API_DOMAIN` with Railway API domain (e.g. `discordbot-api-production.up.railway.app`).

Replace `YOUR_VERCEL_DOMAIN` with Vercel domain (e.g. `discordbot-dashboard.vercel.app`).

**CORS:** `Discord__DashboardUrl` must exactly match the Vercel URL (scheme + host, no trailing slash mismatch).

---

## Railway — Bot worker

| Setting | Value |
|---------|-------|
| Dockerfile | `deploy/railway/Dockerfile.bot` |
| Config file | `deploy/railway/railway.bot.toml` |
| Public networking | **Off** |

### Bot variables

```
ASPNETCORE_ENVIRONMENT=Production

Discord__Token=YOUR_DISCORD_BOT_TOKEN
Api__BaseUrl=https://YOUR_API_DOMAIN
Api__ApiKey=YOUR_STRONG_BOT_API_KEY
Platform__DashboardUrl=https://YOUR_VERCEL_DOMAIN
```

`Api__ApiKey` must match API `Bot__ApiKey`.

---

## Railway — PostgreSQL

1. Add **PostgreSQL** template to project
2. Link Postgres to **API** service (variable references)
3. Run migrations:

```bash
railway login
railway link
railway run --service YOUR_API_SERVICE ./deploy/railway/migrate.sh
```

---

## Vercel — Dashboard

| Setting | Value |
|---------|-------|
| Root directory | `dashboard/DiscordBot.Dashboard` |
| Framework | Other (Angular) |
| Build command | `npm run build` |
| Output directory | `dist/discord-bot.dashboard` |
| Install command | `npm ci` |

### Before first deploy

Set production API URL in `src/environments/environment.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_API_DOMAIN'
};
```

Or copy from `environment.production.example.ts`.

`vercel.json` is included for SPA routing.

### Vercel deploy

1. Import GitHub repo at [vercel.com](https://vercel.com)
2. Set root directory to `dashboard/DiscordBot.Dashboard`
3. Deploy
4. Copy deployment URL → set API `Discord__DashboardUrl` and Bot `Platform__DashboardUrl`
5. Redeploy API if CORS changed

---

## Discord Developer Portal

| Setting | Production value |
|---------|------------------|
| OAuth2 redirect | `https://YOUR_API_DOMAIN/api/auth/discord/callback` |
| Bot intents | **Server Members Intent** enabled |
| Invite URL | `https://discord.com/api/oauth2/authorize?client_id=YOUR_CLIENT_ID&permissions=8&scope=bot%20applications.commands` |

---

## Migration command

```bash
railway run --service YOUR_API_SERVICE ./deploy/railway/migrate.sh
```

Or locally:

```bash
export ConnectionStrings__DefaultConnection="Host=...;SSL Mode=Require;Trust Server Certificate=true;..."
./deploy/railway/migrate.sh
```

---

## Verification commands

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
docker compose config
curl https://YOUR_API_DOMAIN/api/health
```

---

## Beta tester docs

Share **`docs/beta-tester-guide.md`** with beta users.

---

## Deployment checklist

Use this checklist when deploying beta for the first time.

### Pre-deploy

- [ ] GitHub repo pushed (no secrets in tracked files)
- [ ] Discord Bot Token, Client Secret, JWT secret, Bot API key are **new/rotated**
- [ ] `environment.ts` `apiUrl` set to Railway API HTTPS URL
- [ ] `dotnet build` passes
- [ ] `npm run build` passes

### Railway

- [ ] Project created
- [ ] PostgreSQL service added and healthy
- [ ] API service deployed from `Dockerfile.api`
- [ ] API public domain generated
- [ ] All API environment variables set (section above)
- [ ] Migrations applied (`migrate.sh`)
- [ ] `GET /api/health` returns `200` + `"database": "connected"`
- [ ] Bot service deployed from `Dockerfile.bot`
- [ ] Bot public networking **disabled**
- [ ] All Bot environment variables set
- [ ] Bot logs show "Logged in as …"

### Vercel

- [ ] Project imported with root `dashboard/DiscordBot.Dashboard`
- [ ] Build succeeds
- [ ] Dashboard loads at HTTPS URL
- [ ] API `Discord__DashboardUrl` updated to Vercel URL
- [ ] API redeployed after CORS URL change

### Discord Portal

- [ ] OAuth redirect URL added (HTTPS API callback)
- [ ] Server Members Intent enabled
- [ ] Invite URL tested

### Smoke test (you)

- [ ] Dashboard login with Discord works
- [ ] Invite bot + `/setup` works
- [ ] Modules toggle saves
- [ ] Settings welcome saves
- [ ] Ticket `/ticket open` appears in dashboard
- [ ] Logs show events
- [ ] Arabic language + RTL works

### Beta handoff

- [ ] Share dashboard URL with testers
- [ ] Share `docs/beta-tester-guide.md`
- [ ] Monitor Railway logs during first sessions

---

See also: `docs/step-22-beta-deployment.md`, `docs/step-23-railway-deployment.md`, `deploy/railway/railway.env.example`
