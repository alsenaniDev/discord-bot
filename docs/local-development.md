# Local development

This guide boots the full Discord bot solution locally without committing real secrets.

## Prerequisites

- .NET SDK 9.x
- Node.js 20.x or newer
- Docker Desktop with Docker Compose
- PostgreSQL 16 if you are not using Docker
- Discord Developer Portal access for your bot/application
- macOS: run `dotnet dev-certs https --trust` once so Angular/React can call the HTTPS APIs

Windows notes:

- Use PowerShell scripts in `scripts/local/*.ps1`.
- If browser HTTPS calls fail, trust the ASP.NET Core dev certificate with `dotnet dev-certs https --trust`.

## Local ports

| Component | URL |
| --- | --- |
| Platform API (`DiscordBot.Api`) | `https://localhost:5001` and `http://localhost:5000` |
| Activities API (`DiscordBot.Activities.Api`) | `https://localhost:7001` and `http://localhost:7000` |
| React Activity | `http://localhost:5173` |
| Angular Dashboard | `http://localhost:4200` |
| PostgreSQL | `localhost:5432` |
| Seq, optional profile | `http://localhost:5341` |

## Database names

- Platform database: `discordbot_platform`
- Activities database: `discordbot_activities`

`docker-compose.local.yml` starts one PostgreSQL container. `POSTGRES_DB` creates `discordbot_platform`; `scripts/local/postgres-init/01-create-databases.sql` creates `discordbot_activities`.

## Fresh clone setup

From the repository root:

```bash
dotnet dev-certs https --trust
cp activity/DiscordBot.Activity/.env.example activity/DiscordBot.Activity/.env.local
./scripts/local/setup.sh
```

PowerShell:

```powershell
dotnet dev-certs https --trust
Copy-Item activity/DiscordBot.Activity/.env.example activity/DiscordBot.Activity/.env.local
./scripts/local/setup.ps1
```

The setup script restores .NET tools/packages, installs frontend dependencies, starts PostgreSQL, and applies both migration sets.

## Required user secrets

Safe local defaults are committed in `appsettings.Development.json`. Real Discord credentials and shared service tokens should go into .NET User Secrets or `appsettings.Development.local.json` files, which are gitignored.

Use the same bot API key in `DiscordBot.Api` and `DiscordBot.Bot`.

```bash
dotnet user-secrets set --project src/DiscordBot.Api "Discord:ClientId" "<discord-client-id>"
dotnet user-secrets set --project src/DiscordBot.Api "Discord:ClientSecret" "<discord-client-secret>"
dotnet user-secrets set --project src/DiscordBot.Api "Discord:BotToken" "<discord-bot-token>"
dotnet user-secrets set --project src/DiscordBot.Api "Bot:ApiKey" "<local-shared-bot-api-key>"
dotnet user-secrets set --project src/DiscordBot.Api "ActivitiesIntegration:ServiceToken" "<local-activities-service-token>"
dotnet user-secrets set --project src/DiscordBot.Api "Jwt:Secret" "local-development-jwt-signing-key-change-me-123456789"
dotnet user-secrets set --project src/DiscordBot.Api "Admin:DiscordUserId" "<your-discord-user-id>"

dotnet user-secrets set --project src/DiscordBot.Activities.Api "Discord:ClientId" "<discord-client-id>"
dotnet user-secrets set --project src/DiscordBot.Activities.Api "Discord:ClientSecret" "<discord-client-secret>"
dotnet user-secrets set --project src/DiscordBot.Activities.Api "PlatformApi:ServiceToken" "<local-activities-service-token>"
dotnet user-secrets set --project src/DiscordBot.Activities.Api "ActivitiesDiagnostics:ServiceToken" "<local-activities-diagnostics-token>"
dotnet user-secrets set --project src/DiscordBot.Activities.Api "Jwt:SigningKey" "local-development-activities-jwt-signing-key-change-me"

dotnet user-secrets set --project src/DiscordBot.Bot "Discord:Token" "<discord-bot-token>"
dotnet user-secrets set --project src/DiscordBot.Bot "Api:ApiKey" "<local-shared-bot-api-key>"
```

If you use the committed development placeholders, non-production startup validation warns instead of failing. Real Discord flows require valid values.

## React Activity environment

Edit `activity/DiscordBot.Activity/.env.local`:

```env
VITE_DISCORD_CLIENT_ID=<discord-client-id>
VITE_API_BASE_URL=https://localhost:5001
VITE_PLATFORM_API_BASE_URL=https://localhost:5001
VITE_ACTIVITIES_API_BASE_URL=https://localhost:7001
VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS=<test-guild-discord-id>
VITE_ENVIRONMENT=development
```

Do not put the Discord client secret in frontend files.

## Discord Developer Portal setup

For local testing:

- OAuth2 redirect for the Platform API login: `https://localhost:5001/api/auth/discord/callback`
- Activity frontend origin/URL mapping: `http://localhost:5173`
- If you test through Discord Activity URL mapping, map the Activity route to the local tunnel or deployed URL that points at the Vite Activity frontend.
- Enable the platforms you need for the Activity, including desktop/mobile if you are testing on phone.

## Migrations

```bash
dotnet tool restore

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Activities.Infrastructure \
  --startup-project src/DiscordBot.Activities.Api
```

Shortcut:

```bash
./scripts/local/migrate.sh
```

## Start the solution

Use separate terminals:

```bash
docker compose -f docker-compose.local.yml up -d postgres
./scripts/local/run-platform-api.sh
./scripts/local/run-activities-api.sh
./scripts/local/run-bot.sh
./scripts/local/run-activity.sh
./scripts/local/run-dashboard.sh
```

The bot also expects Lavalink for music features. The existing root `docker-compose.yml` includes `lavalink`; start it if you test music:

```bash
docker compose up -d lavalink
```

## Health checks

```bash
curl -k https://localhost:5001/swagger
curl -k https://localhost:7001/health
curl -k https://localhost:7001/health/live
curl -k https://localhost:7001/health/ready
```

`/health/ready` checks Activities configuration and database migrations. It is expected to fail if Discord/Platform service secrets are placeholders or if migrations are pending.

## CORS

Platform API local CORS is controlled by:

- `Discord:DashboardUrl` → `http://localhost:4200`
- `Discord:ActivityUrl` → `http://localhost:5173`

Activities API local CORS is controlled by:

- `Cors:AllowedOrigins` → `http://localhost:5173`, `https://localhost:5173`

## Roll back Roulette to legacy runtime locally

Roulette uses the Activities runtime only when:

- `VITE_ACTIVITIES_API_BASE_URL` is set, and
- the current guild id is listed in `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS`.

To force legacy Roulette locally, leave `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS` empty and restart Vite.

## Reset local databases

This deletes local Docker PostgreSQL data:

```bash
docker compose -f docker-compose.local.yml down -v
docker compose -f docker-compose.local.yml up -d postgres
./scripts/local/migrate.sh
```

## Common startup errors

- `Jwt:SigningKey must be configured and at least 32 characters.`  
  Set `Jwt:SigningKey` for `DiscordBot.Activities.Api`.

- `Jwt:Secret must be at least 32 characters.`  
  Set `Jwt:Secret` for `DiscordBot.Api`.

- `Invalid activities service key.`  
  `DiscordBot.Activities.Api` `PlatformApi:ServiceToken` must match `DiscordBot.Api` `ActivitiesIntegration:ServiceToken`.

- Bot cannot login to Discord.  
  Set `Discord:Token` for `DiscordBot.Bot` to a real bot token.

- Browser cannot call `https://localhost:5001` or `https://localhost:7001`.  
  Run `dotnet dev-certs https --trust`, then restart the browser.

## Verification commands

```bash
dotnet tool restore
dotnet restore
dotnet build
dotnet test
npm install --prefix activity/DiscordBot.Activity
npm test --prefix activity/DiscordBot.Activity
npm run build --prefix activity/DiscordBot.Activity
npm install --prefix dashboard/DiscordBot.Dashboard
npm run build --prefix dashboard/DiscordBot.Dashboard
```
