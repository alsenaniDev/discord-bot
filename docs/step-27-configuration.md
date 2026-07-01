# Step 27 — Clean Development and Production Configuration

This step separates local development config from production secrets.

## Rules

| Rule | Detail |
|------|--------|
| Committed files | Placeholders only — safe to push to GitHub |
| Local secrets | `appsettings.Development.local.json` (gitignored) |
| Production secrets | Environment variables on Railway/Vercel only |
| Never commit | `.env`, `*.local.json`, `appsettings.Production.json`, real tokens |

## Configuration load order

Both **API** and **Bot** load settings in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `appsettings.{Environment}.local.json` (optional, gitignored)
4. Environment variables

Production on Railway sets `ASPNETCORE_ENVIRONMENT=Production` and supplies env vars — no local files are used.

## Local setup

### 1. API local config

```bash
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json
```

Fill in:

| Key | Example (local) |
|-----|-----------------|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres` |
| `Discord:ClientId` | From Discord Developer Portal |
| `Discord:ClientSecret` | From Discord Developer Portal |
| `Discord:BotToken` | From Discord Developer Portal → Bot |
| `Discord:RedirectUri` | `http://localhost:5217/api/auth/discord/callback` |
| `Discord:DashboardUrl` | `http://localhost:4200` |
| `Jwt:Secret` | At least 32 characters |
| `Jwt:Issuer` | `DiscordBot` |
| `Jwt:Audience` | `DiscordBot.Dashboard` |
| `Bot:ApiKey` | Must match bot worker (example: `dev-bot-api-key-change-me`) |
| `Admin:DiscordUserId` | Your Discord user ID |

Register OAuth redirect in Discord Portal:

```
http://localhost:5217/api/auth/discord/callback
```

### 2. Bot local config

```bash
cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json
```

Fill in:

| Key | Example (local) |
|-----|-----------------|
| `Discord:Token` | Same bot token as API |
| `Api:BaseUrl` | `http://localhost:5217` |
| `Api:ApiKey` | Same as API `Bot:ApiKey` |
| `Platform:DashboardUrl` | `http://localhost:4200` |

### 3. Dashboard local config

`npm start` uses `environment.development.ts` → `apiUrl: 'http://localhost:5217'`.

Optional override without editing committed files:

```bash
cp dashboard/DiscordBot.Dashboard/src/environments/environment.local.example.ts \
   dashboard/DiscordBot.Dashboard/src/environments/environment.local.ts
```

(Add `environment.local.ts` to `angular.json` fileReplacements manually if you use this pattern.)

### 4. Run locally

```bash
docker compose up -d
dotnet ef database update --project src/DiscordBot.Infrastructure --startup-project src/DiscordBot.Api
dotnet run --project src/DiscordBot.Api --launch-profile http
dotnet run --project src/DiscordBot.Bot
cd dashboard/DiscordBot.Dashboard && npm start
```

## Production environment variables

### Railway — API service

| Variable | Example |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Discord__ClientId` | Discord app client ID |
| `Discord__ClientSecret` | Discord app client secret |
| `Discord__BotToken` | Bot token |
| `Discord__RedirectUri` | `https://YOUR_API/api/auth/discord/callback` |
| `Discord__DashboardUrl` | `https://YOUR_DASHBOARD` |
| `Jwt__Secret` | 32+ char random secret |
| `Jwt__Issuer` | `DiscordBot` |
| `Jwt__Audience` | `DiscordBot.Dashboard` |
| `Bot__ApiKey` | Strong shared secret |
| `Admin__DiscordUserId` | Your Discord user ID |

### Railway — Bot worker

| Variable | Example |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Discord__Token` | Bot token |
| `Api__BaseUrl` | `https://YOUR_API` |
| `Api__ApiKey` | Same as API `Bot__ApiKey` |
| `Platform__DashboardUrl` | `https://YOUR_DASHBOARD` |

### Vercel — Dashboard

Edit `src/environments/environment.production.ts` before deploy:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_API'
};
```

Or set at build time and replace via CI. `npm run build` uses `environment.production.ts` via Angular file replacements.

### Discord Developer Portal (production)

Add redirect URL:

```
https://YOUR_API/api/auth/discord/callback
```

## Development vs Production

| Setting | Development | Production |
|---------|-------------|------------|
| Config source | `appsettings.Development.local.json` | Environment variables |
| API URL | `http://localhost:5217` | `https://api.your-domain.com` |
| Dashboard URL | `http://localhost:4200` | `https://dashboard.your-domain.com` |
| OAuth redirect | `http://localhost:5217/api/auth/discord/callback` | `https://api.../api/auth/discord/callback` |
| Database | Local Docker PostgreSQL | Railway PostgreSQL |
| HTTPS required | No (localhost OK) | Yes |
| Placeholder values | Warning only | Startup rejected |
| Validation strict | Warnings | Throws on missing/invalid config |

## Production validation

Startup **rejects** in Production when values are:

- Empty or missing
- Placeholder fragments: `YOUR_`, `CHANGE_ME`, `REPLACE_WITH`, `your-domain.com`
- `http://` for public URLs (must be HTTPS)
- `localhost` or `127.0.0.1` in public URLs

Development logs warnings but continues — features needing missing config fail with clear errors at runtime.

## Verify before push

```bash
grep -rE "ClientSecret|BotToken|\.Gg[A-Za-z0-9_-]{20,}" src --include="*.json" | grep -v example | grep -v YOUR_
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
```

See also: `deploy/railway/railway.env.example`, `.env.example`, `docs/step-24-beta-readiness.md`.
