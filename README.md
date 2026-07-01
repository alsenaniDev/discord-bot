# Discord Bot Platform

A Discord bot SaaS platform: .NET API, PostgreSQL, Discord.Net bot worker, and Angular dashboard with i18n (EN/AR).

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.x |
| Node.js | 18+ |
| Docker | For PostgreSQL locally |
| Discord Application | [Developer Portal](https://discord.com/developers/applications) |

## Quick start (local)

### 1. Create local config files (gitignored)

**Never commit real tokens.** Copy the example files and fill in your values:

```bash
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json

cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json
```

See **`docs/step-27-configuration.md`** for the full list of required keys.

| Setting | Local value |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres` |
| `Discord:RedirectUri` | `http://localhost:5217/api/auth/discord/callback` |
| `Discord:DashboardUrl` | `http://localhost:4200` |
| `Api:BaseUrl` (Bot) | `http://localhost:5217` |
| `Platform:DashboardUrl` (Bot) | `http://localhost:4200` |
| `Bot:ApiKey` / `Api:ApiKey` | Must match (example: `dev-bot-api-key-change-me`) |
| `Jwt:Secret` | At least 32 characters |
| `Admin:DiscordUserId` | Your Discord user ID |

Register OAuth redirect URL in Discord:

```
http://localhost:5217/api/auth/discord/callback
```

Enable **Server Members Intent** for welcome messages.

### 2. Start PostgreSQL

```bash
docker compose up -d
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

### 3. Run the stack (3 terminals)

```bash
dotnet run --project src/DiscordBot.Api --launch-profile http
dotnet run --project src/DiscordBot.Bot
cd dashboard/DiscordBot.Dashboard && npm install && npm start
```

Open http://localhost:4200 → **Login with Discord**.

## Configuration: Development vs Production

| | Development | Production |
|---|-------------|------------|
| **Secrets** | `appsettings.Development.local.json` | Railway/Vercel env vars |
| **API URL** | `http://localhost:5217` | `https://api.your-domain.com` |
| **Dashboard** | `http://localhost:4200` | `https://dashboard.your-domain.com` |
| **Database** | Docker PostgreSQL | Railway PostgreSQL |
| **Validation** | Warnings if placeholders missing | Startup fails on invalid config |

### Load order (.NET API & Bot)

```
appsettings.json
  → appsettings.{Environment}.json
  → appsettings.{Environment}.local.json (optional, gitignored)
  → environment variables (highest priority in Production)
```

### Production env vars (Railway)

**API:** `ConnectionStrings__DefaultConnection`, `Discord__ClientId`, `Discord__ClientSecret`, `Discord__BotToken`, `Discord__RedirectUri`, `Discord__DashboardUrl`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Bot__ApiKey`, `Admin__DiscordUserId`

**Bot:** `Discord__Token`, `Api__BaseUrl`, `Api__ApiKey`, `Platform__DashboardUrl`

**Dashboard (Vercel):** Edit `environment.production.ts` → set `apiUrl` to your API URL before `npm run build`.

Full reference: **`docs/step-27-configuration.md`**, **`deploy/railway/railway.env.example`**, **`.env.example`**

## Configuration files

| File | Committed? | Purpose |
|------|------------|---------|
| `appsettings.json` | Yes | Safe placeholders only |
| `appsettings.example.json` | Yes | Production template reference |
| `appsettings.Development.json` | Yes | Dev logging/seed flags (no secrets) |
| `appsettings.Development.example.json` | Yes | Copy → `.local.json` for local dev |
| `appsettings.Development.local.json` | **No** | Your real local secrets |
| `appsettings.Production.json` | **No** | Optional; prefer env vars |
| `*.local.json` | **No** | Any local override file |
| `.env` | **No** | Optional env var overrides |
| `environment.development.ts` | Yes | Local API URL (`localhost:5217`) |
| `environment.production.ts` | Yes | Production API URL (edit before deploy) |
| `environment.local.ts` | **No** | Optional dashboard override |

## Project structure

```
discord bots/
├── src/
│   ├── DiscordBot.Api/           REST API + OAuth
│   ├── DiscordBot.Domain/        Entities
│   ├── DiscordBot.Infrastructure/ EF Core, services
│   └── DiscordBot.Bot/           Discord.Net worker
├── dashboard/DiscordBot.Dashboard/  Angular UI
├── docker-compose.yml            PostgreSQL only
├── docs/                         Step-by-step guides
├── .env.example                  Environment variable reference
└── DiscordBot.sln
```

## Build verification

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
docker compose config
```

## Beta deployment

| Doc | Purpose |
|-----|---------|
| `docs/step-22-beta-deployment.md` | Generic beta deployment |
| `docs/step-23-railway-deployment.md` | Railway setup |
| `docs/step-24-beta-readiness.md` | Final checklist |
| `docs/step-27-configuration.md` | Dev vs prod config |
| `docs/beta-tester-guide.md` | Beta tester walkthrough |

## Verify no secrets before push

```bash
grep -rE "ClientSecret|BotToken|\.Gg" src --include="*.json" | grep -v example | grep -v YOUR_
```

If secrets were ever committed, rotate Bot Token, Client Secret, JWT secret, and Bot API key before deploying.

## EF Core migrations

```bash
dotnet ef migrations add YourMigrationName \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

## Troubleshooting

### API warns: configuration issues detected

Create `appsettings.Development.local.json` from the example and fill in Discord/JWT values.

### Dashboard: "Cannot reach the API"

- API on port **5217**? (`--launch-profile http`)
- `npm start` uses `environment.development.ts` → `http://localhost:5217`
- CORS: `Discord:DashboardUrl` must be `http://localhost:4200`

### OAuth / invalid redirect

- Redirect URI in Discord Portal must exactly match `Discord:RedirectUri`
- Production: HTTPS URLs only

### Bot: "Failed to register guild with API"

- API running; `Api:ApiKey` on bot matches `Bot:ApiKey` on API

## Ports (local)

| Service | URL |
|---------|-----|
| API | http://localhost:5217 |
| Dashboard | http://localhost:4200 |
| PostgreSQL | localhost:5432 |
