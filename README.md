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

### 1. Clone and configure secrets

**Never commit real tokens.** Use local config files (gitignored):

```bash
# API — copy example and fill in Discord OAuth + JWT
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json

# Bot — copy example and set bot token
cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json
```

Edit the `.local.json` files (or set env vars from `.env.example`):

| Setting | Where | Notes |
|---------|-------|-------|
| `Discord:ClientId` / `ClientSecret` | API local config | Developer Portal → OAuth2 |
| `Discord:BotToken` | API + Bot local config | Developer Portal → Bot |
| `Discord:Token` | Bot local config | Same bot token |
| `Jwt:Secret` | API local config | At least 32 characters |
| `Bot:ApiKey` / `Api:ApiKey` | API + Bot | Must match (dev default in example: `dev-bot-api-key-change-me`) |
| `Admin:DiscordUserId` | API local config | Your Discord user ID for platform admin |
| `Discord:RedirectUri` | API | `http://localhost:5217/api/auth/discord/callback` |
| `Discord:DashboardUrl` | API | `http://localhost:4200` (CORS) |
| `Platform:DashboardUrl` | Bot | `http://localhost:4200` (shown in `/setup`) |

Register OAuth redirect URL in Discord:

```
http://localhost:5217/api/auth/discord/callback
```

Enable **Server Members Intent** for welcome messages.

> **Security:** If secrets were ever committed to git, rotate Bot Token, Client Secret, JWT secret, and Bot API key in the Developer Portal / your config before deploying.

### 2. Start PostgreSQL

```bash
docker compose up -d
```

Wait until healthy (~5 seconds), then apply migrations:

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

Default connection (matches `docker-compose.yml`):

```
Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres
```

### 3. Run the stack (3 terminals)

```bash
# Terminal 1 — API (http://localhost:5217)
dotnet run --project src/DiscordBot.Api --launch-profile http

# Terminal 2 — Bot
dotnet run --project src/DiscordBot.Bot

# Terminal 3 — Dashboard (http://localhost:4200)
cd dashboard/DiscordBot.Dashboard && npm install && npm start
```

### 4. First login

1. Open http://localhost:4200
2. **Login with Discord**
3. Invite bot to your server → run `/setup` in Discord
4. Select your server → configure modules, welcome, tickets
5. Test welcome by joining with another account (bot must be running)

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

## Configuration files

| File | Committed? | Purpose |
|------|------------|---------|
| `appsettings.json` | Yes | Placeholders only — copy from example for prod |
| `appsettings.example.json` | Yes | Production/beta template |
| `appsettings.Development.json` | Yes | Dev logging, seed flags (no secrets) |
| `appsettings.Development.example.json` | Yes | Local dev template with safe dev defaults |
| `appsettings.Development.local.json` | **No** (gitignored) | Your real local secrets |
| `appsettings.Production.json` | **No** (gitignored) | Beta/production secrets |
| `.env` | **No** (gitignored) | Optional env var overrides |
| `environment.ts` | Yes | Production Angular API URL (edit before deploy) |
| `environment.development.ts` | Yes | Local dev API URL (`localhost:5217`) |
| `environment.production.example.ts` | Yes | Copy template for beta builds |

## Build verification

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
docker compose config
```

## Beta deployment

See **`docs/step-22-beta-deployment.md`** for generic beta deployment.

See **`docs/step-23-railway-deployment.md`** for Railway-specific setup (PostgreSQL, API, Bot worker, dashboard on Railway/Vercel).

See **`docs/step-24-beta-readiness.md`** for the final beta deployment checklist (Vercel dashboard + Railway API/Bot/Postgres).

Share **`docs/beta-tester-guide.md`** with beta testers.

**Railway quick links:**

| Resource | Path |
|----------|------|
| API Dockerfile | `deploy/railway/Dockerfile.api` |
| Bot Dockerfile | `deploy/railway/Dockerfile.bot` |
| Dashboard Dockerfile | `deploy/railway/Dockerfile.dashboard` |
| Env variable reference | `deploy/railway/railway.env.example` |
| Migrations script | `deploy/railway/migrate.sh` |

**Before beta build**, set the dashboard API URL:

```bash
cp dashboard/DiscordBot.Dashboard/src/environments/environment.production.example.ts \
   dashboard/DiscordBot.Dashboard/src/environments/environment.ts
# Edit apiUrl → https://api.your-domain.com
npm run build
```

## Push to GitHub

```bash
git init
git branch -M main
git add .
git commit -m "Initial commit — Discord bot platform"
git remote add origin https://github.com/YOUR_USER/discord-bot-platform.git
git push -u origin main
```

Verify no secrets before pushing:

```bash
grep -rE "ClientSecret|BotToken|\.Gg" src --include="*.json" | grep -v example | grep -v YOUR_
```

## EF Core migrations

```bash
dotnet ef migrations add YourMigrationName \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

Reset dev database:

```bash
docker compose down -v && docker compose up -d
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

## Troubleshooting

### API fails: "Jwt:Secret must be at least 32 characters"

Create `appsettings.Development.local.json` from the example and set `Jwt:Secret`.

### Dashboard: "Cannot reach the API"

- API on port **5217**? (`--launch-profile http`)
- `npm start` uses `environment.development.ts` → `http://localhost:5217`
- CORS: `Discord:DashboardUrl` must be `http://localhost:4200`

### OAuth / invalid redirect

- Redirect URI in Discord Portal must exactly match `Discord:RedirectUri`
- Production: use HTTPS URLs on both API and Portal

### Empty server list

- Run `/setup` in Discord after inviting the bot
- Your Discord user must own the guild

### Bot: "Failed to register guild with API"

- API running; `Api:ApiKey` on bot matches `Bot:ApiKey` on API

## Learning path

See `docs/step-01` through `docs/step-24` for incremental build and deployment notes.

## Ports (local)

| Service | URL |
|---------|-----|
| API | http://localhost:5217 |
| Dashboard | http://localhost:4200 |
| PostgreSQL | localhost:5432 |
