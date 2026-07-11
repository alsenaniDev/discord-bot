# Production deployment

This guide prepares the production deployment for:

- `DiscordBot.Api` — Platform API
- `DiscordBot.Activities.Api` — Discord Activities/Roulette pilot runtime API
- `DiscordBot.Bot` — Discord gateway worker
- `DiscordBot.Activity` — React Discord Activity frontend
- `DiscordBot.Dashboard` — Angular Dashboard

Do not run EF migrations from application startup. Run migrations once from CI/CD, a dedicated SDK migration job, or a Railway one-off command before deploying app replicas.

## Production architecture

```text
Angular Dashboard ───────► DiscordBot.Api
React Activity ──────────► DiscordBot.Api
React Activity ──────────► DiscordBot.Activities.Api
DiscordBot.Activities.Api ─► DiscordBot.Api using X-Activities-Service-Key
DiscordBot.Bot ──────────► DiscordBot.Api using Bot:ApiKey
DiscordBot.Bot ──────────► Discord Gateway + Lavalink
```

Roulette legacy runtime remains in `DiscordBot.Api`. The new Activities runtime is enabled per frontend pilot guild list.

## Railway services required

| Service | Type | Dockerfile | Public | Health check |
| --- | --- | --- | --- | --- |
| `discord-bot-api` | Web | `deploy/railway/Dockerfile.api` | Yes | `/health` |
| `discord-activities-api` | Web | `deploy/railway/Dockerfile.activities-api` | Yes | `/health/ready` |
| `discord-bot` | Worker | `deploy/railway/Dockerfile.bot` | No | None |
| `lavalink` | Worker/private | `deploy/railway/Dockerfile.lavalink` | No | None |
| `platform-postgres` | PostgreSQL 16 | Railway managed | No | Railway managed |
| `activities-postgres` | PostgreSQL 16 | Railway managed | No | Railway managed |

Config-as-code examples:

- `deploy/railway/railway.api.toml`
- `deploy/railway/railway.activities-api.toml`
- `deploy/railway/railway.bot.toml`

## Database setup

Use separate PostgreSQL databases:

- Platform database for `DiscordBot.Api`
- Activities database for `DiscordBot.Activities.Api`

Do not point both services at the same database unless you intentionally change the architecture.

## Environment variables by service

See [production-configuration-reference.md](./production-configuration-reference.md) for the complete table.

Important shared secrets:

- `DiscordBot.Bot` `Api__ApiKey` must match `DiscordBot.Api` `Bot__ApiKey`.
- `DiscordBot.Activities.Api` `PlatformApi__ServiceToken` must match `DiscordBot.Api` `ActivitiesIntegration__ServiceToken`.
- Dashboard and Activity frontend domains must be included in Platform API CORS config.
- Activity frontend domain must be included in Activities API `Cors__AllowedOrigins__0`.

## Docker image build commands

```bash
docker build -f deploy/railway/Dockerfile.api -t discordbot-api:local .
docker build -f deploy/railway/Dockerfile.activities-api -t discordbot-activities-api:local .
docker build -f deploy/railway/Dockerfile.bot -t discordbot-bot:local .
```

Shortcut:

```bash
./scripts/deploy/build-images.sh
```

## Migration deployment strategy

Preferred strategy: run migrations from CI/CD or a dedicated SDK migration service before application deployment.

Why: runtime images use .NET runtime images, not SDK images. They intentionally do not include `dotnet-ef`, and app startup should not mutate the database from multiple replicas.

Platform:

```bash
ConnectionStrings__DefaultConnection='Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true' \
  ./deploy/railway/migrate-platform.sh
```

Activities:

```bash
ConnectionStrings__ActivitiesDatabase='Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true' \
  ./deploy/railway/migrate-activities.sh
```

The scripts restore the local tool manifest and run `dotnet tool run dotnet-ef`.

## Health checks

Platform:

```bash
curl -fsS https://YOUR_API_DOMAIN/health
```

Activities:

```bash
curl -fsS https://YOUR_ACTIVITIES_API_DOMAIN/health
curl -fsS https://YOUR_ACTIVITIES_API_DOMAIN/health/live
curl -fsS https://YOUR_ACTIVITIES_API_DOMAIN/health/ready
```

Shortcut:

```bash
PLATFORM_API_URL=https://YOUR_API_DOMAIN \
ACTIVITIES_API_URL=https://YOUR_ACTIVITIES_API_DOMAIN \
./scripts/deploy/smoke-test.sh
```

## React Activity deployment

Set Vercel/build variables:

```text
VITE_DISCORD_CLIENT_ID=YOUR_DISCORD_CLIENT_ID
VITE_API_BASE_URL=
VITE_PLATFORM_API_BASE_URL=
VITE_ACTIVITIES_API_BASE_URL=/activities-api
VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS=
VITE_ENVIRONMENT=production
```

Configure Discord Developer Portal URL Mappings before deploying:

```text
/activities-api -> YOUR_ACTIVITIES_API_DOMAIN
/api            -> YOUR_API_DOMAIN/api
/               -> YOUR_ACTIVITY_FRONTEND_DOMAIN
```

`VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS` may be empty. Empty means all guilds use the legacy Roulette runtime.

The Activity production build fails clearly if client id, Platform API URL, or Activities API URL are missing.

## Angular Dashboard deployment

The Dashboard uses `dashboard/DiscordBot.Dashboard/src/environments/environment.production.ts`.

Confirm `apiUrl` points to the production Platform API before deployment. The Dashboard does not call `DiscordBot.Activities.Api` directly.

## Discord Developer Portal

Configure:

- OAuth redirect: `https://YOUR_API_DOMAIN/api/auth/discord/callback`
- Activity URL mapping/origin for the React Activity domain
- Supported platforms for the Activity
- Bot token/permissions/intents

For mobile Activity support, enable iOS/Android in the Developer Portal and make sure the Activity frontend is deployed over HTTPS.

## Production CORS

Platform API:

- `Discord__DashboardUrl=https://YOUR_DASHBOARD_DOMAIN`
- `Discord__ActivityUrl=https://YOUR_ACTIVITY_DOMAIN`

Activities API:

- `Cors__AllowedOrigins__0=https://YOUR_ACTIVITY_DOMAIN`

Do not use wildcard origins with credentials.

## Deployment order

1. Provision Platform PostgreSQL and Activities PostgreSQL.
2. Create Railway services for Platform API, Activities API, Bot, and Lavalink.
3. Configure all environment variables and shared secrets.
4. Build/validate CI.
5. Apply Platform migrations.
6. Apply Activities migrations.
7. Deploy Platform API.
8. Deploy Activities API.
9. Deploy Lavalink.
10. Deploy Bot.
11. Deploy React Activity.
12. Deploy Angular Dashboard.
13. Configure Discord Developer Portal URLs.
14. Run smoke tests.
15. Enable pilot guild ids gradually.

## Smoke tests

```bash
PLATFORM_API_URL=https://YOUR_API_DOMAIN \
ACTIVITIES_API_URL=https://YOUR_ACTIVITIES_API_DOMAIN \
./scripts/deploy/smoke-test.sh
```

Then test:

- Dashboard login
- `/games` Activity launch
- Quiz legacy flow
- Roulette legacy flow for non-pilot guild
- Roulette Activities runtime for pilot guild

## Diagnostics checks

- Platform API logs should show configuration validation success.
- Activities API logs should show safe startup config: environment, sanitized DB host/database, Platform API URL, CORS origins.
- Bot logs should show Gateway connected and Platform API requests returning `200`.

## Rollback

Safe rollback options:

1. Remove guild id from `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS`, rebuild/redeploy React Activity. This routes Roulette back to the legacy runtime.
2. Roll back `discord-activities-api` to previous Railway deployment.
3. Roll back `discord-bot-api` and `discord-bot` to previous Railway deployments.
4. Do not roll back database migrations unless you have an explicit backup/restore plan.

## Common CI errors

- `Run "dotnet tool restore" to make the "dotnet-ef" command available.`  
  Use `dotnet tool restore` and `dotnet tool run dotnet-ef`, not global `dotnet ef`.

- `--no-connect` not recognized or migration command behaves differently.  
  The workflow now validates against PostgreSQL 16 CI databases and does not use `--no-connect`.

- EF cannot create DbContext in CI.  
  Ensure `ConnectionStrings__DefaultConnection` and `ConnectionStrings__ActivitiesDatabase` point to CI databases, not production.

## Common Railway errors

- Health check fails on Activities API.  
  Check `ConnectionStrings__ActivitiesDatabase`, pending migrations, `PlatformApi__ServiceToken`, and `Cors__AllowedOrigins__0`.

- Platform API rejects Activities calls.  
  Ensure `ActivitiesIntegration__ServiceToken` equals Activities API `PlatformApi__ServiceToken`.

- Bot cannot call API.  
  Ensure `Api__BaseUrl` is the public Platform API URL and `Api__ApiKey` equals Platform API `Bot__ApiKey`.

- Activity auth fails.  
  Ensure Discord Developer Portal Activity URL mapping and `VITE_DISCORD_CLIENT_ID` are correct.

## Rotate secrets safely

1. Generate new secret.
2. Add it to both matching services, keeping old deployment running until both are updated.
3. Redeploy dependent services in a controlled order.
4. Verify smoke tests.
5. Remove old secret values.

For shared API keys, rotate during a low-traffic window because old and new values are not currently accepted at the same time.
