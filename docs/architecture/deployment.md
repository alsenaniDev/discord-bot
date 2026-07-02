# Deployment

## Overview

| Environment | API | Bot | Database | Dashboard |
|-------------|-----|-----|----------|-----------|
| Local | `dotnet run` :5217 | `dotnet run` worker | Docker PostgreSQL | `npm start` :4200 |
| Production | Railway Docker | Railway Docker | Railway PostgreSQL | Vercel or Railway nginx |

## Local development

### Prerequisites

.NET 9 SDK, Node 18+, Docker, Discord application.

### Steps

```bash
# 1. PostgreSQL
docker compose up -d

# 2. Migrations
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

# 3. Config (gitignored)
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json
cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json

# 4. Run (3 terminals)
dotnet run --project src/DiscordBot.Api --launch-profile http
dotnet run --project src/DiscordBot.Bot
cd dashboard/DiscordBot.Dashboard && npm start
```

Open http://localhost:4200

**Reference:** root `README.md`, `docs/step-27-configuration.md`

## Production — Railway

**Primary guide:** `docs/step-23-railway-deployment.md`

**Deploy assets:** `deploy/railway/`

| File | Service |
|------|---------|
| `Dockerfile.api` | ASP.NET API |
| `Dockerfile.bot` | Bot worker |
| `Dockerfile.dashboard` | Angular build + nginx |
| `nginx.dashboard.conf` | SPA routing |
| `railway.api.toml` | Health check `/api/health` |
| `railway.bot.toml` | Bot service |
| `railway.dashboard.toml` | Dashboard service |
| `migrate.sh` | EF migrations on deploy |
| `railway.env.example` | Environment variable template |

### API binding

`Program.cs` binds `http://0.0.0.0:{PORT}` when Railway injects `PORT`.

### Migration on deploy

Run `migrate.sh` or equivalent before/with API deploy:

```bash
dotnet ef database update --project ... --startup-project ...
```

**Order:** migrate database → deploy API → deploy bot → deploy dashboard.

## Production — Vercel (dashboard alternative)

**File:** `dashboard/DiscordBot.Dashboard/vercel.json`

- Build: `npm ci && npm run build`
- Output: `dist/discord-bot.dashboard`
- SPA rewrite to `index.html`
- Cache headers for `index.html` and i18n JSON

Set `apiUrl` in `environment.production.ts` before build.

Enable `Discord:AllowVercelOrigins=true` on API for CORS.

**Note:** Correct production dashboard URL must match deployed Vercel project (not a stale/wrong domain).

## Docker Compose (local DB only)

**File:** `docker-compose.yml`

PostgreSQL 16, port 5432, credentials postgres/postgres, database discordbot.

## Build verification

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
```

## CI/CD

**Not implemented.** Deployments are manual via Railway/Vercel dashboards or CLI.

**Assumption:** GitHub Actions pipeline is a future Phase 2 item.

## Rollback strategy

1. Revert Railway deployment to previous image
2. Database rollback: EF migrations are forward-only — plan reversible migrations for breaking schema changes
3. Dashboard: redeploy previous Vercel deployment

## Related docs

- `environments.md`, `security.md`
- `docs/step-22-beta-deployment.md`, `docs/step-24-beta-readiness.md`
