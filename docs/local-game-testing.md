# Local game testing

This guide is for testing Discord Activities games locally before production deployment. It keeps the same browser paths used in production:

- `/api` → Platform API
- `/activities-api` → Activities API
- `/` → React Activity

## Prerequisites

- .NET SDK 9
- Node.js 20+
- Docker Desktop
- Discord test application + bot
- Two Discord test users for real multiplayer testing
- Trusted ASP.NET Core dev certificate:

```bash
dotnet dev-certs https --trust
```

## Local ports

| Service | URL |
| --- | --- |
| Platform API | `https://localhost:5001` |
| Activities API | `https://localhost:7001` |
| React Activity | `http://localhost:5173` |
| PostgreSQL | `localhost:5432` |
| Seq, optional | `http://localhost:5341` |

## Environment

Copy the Activity env template:

```bash
cp activity/DiscordBot.Activity/.env.example activity/DiscordBot.Activity/.env.local
```

Use these frontend values for local proxy testing:

```env
VITE_DISCORD_CLIENT_ID=<discord-client-id>
VITE_API_BASE_URL=/api
VITE_PLATFORM_API_BASE_URL=/api
VITE_ACTIVITIES_API_BASE_URL=/activities-api
VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS=<test-guild-discord-id>
VITE_ENVIRONMENT=development
```

Do not put Discord client secrets in frontend files.

## Vite proxy

`activity/DiscordBot.Activity/vite.config.ts` proxies:

```text
/api            -> https://localhost:5001
/activities-api -> https://localhost:7001
```

The `/activities-api` prefix is stripped, so:

```text
/activities-api/api/auth/discord/exchange
```

becomes:

```text
https://localhost:7001/api/auth/discord/exchange
```

## Start everything

Fast path:

```bash
./scripts/local/run-all.sh
```

Manual path:

```bash
docker compose -f docker-compose.local.yml up -d postgres
./scripts/local/migrate.sh
./scripts/local/run-platform-api.sh
./scripts/local/run-activities-api.sh
./scripts/local/run-bot.sh
./scripts/local/run-activity.sh
```

Logs from `run-all.sh` go to:

```text
.local/logs/
```

Stop local app processes:

```bash
./scripts/local/stop-all.sh
```

Reset local databases:

```bash
./scripts/local/reset-databases.sh
```

## Browser-only local testing

Use the Vite app at:

```text
http://localhost:5173
```

This validates routing, API calls, SignalR, and production-like base paths. Discord SDK calls still require a real Discord Activity context; for complete auth and launch behavior use the real Discord flow below.

## Real Discord local testing

Use a tunnel such as Cloudflare Tunnel or ngrok to expose:

```text
http://localhost:5173
```

In Discord Developer Portal URL mappings, map:

```text
/api             -> tunnel/platform proxy or deployed Platform API
/activities-api  -> tunnel/activities proxy or deployed Activities API
/                 -> tunnel React Activity
```

For local API tunneling, put a reverse proxy in front of:

```text
/api             -> https://localhost:5001
/activities-api  -> https://localhost:7001
/                 -> http://localhost:5173
```

Then run the bot locally with a test guild and test games channel.

## Roulette two-player checklist

1. Player A opens `/games`.
2. Player A opens Roulette.
3. Player A creates a room.
4. Confirm the room announcement is posted in Discord.
5. Player B clicks the Discord join button.
6. Player B enters the room.
7. Player A clicks Start.
8. Confirm one round starts and no HTTP 500 occurs.
9. Create another room with A, B, and C.
10. Player A leaves.
11. Confirm Player B becomes host.
12. Confirm Player B sees Start.
13. Player B starts successfully.

## Stale-room checklist

Use PostgreSQL or tests to simulate old room timestamps:

- Waiting room with `ExpiresAtUtc` in the past becomes `Expired`.
- In-progress room with old `UpdatedAtUtc` becomes `Abandoned`.
- `/api/roulette/sessions/my-active` does not return expired or abandoned rooms.
- React does not redirect to previous-day rooms.

## Verification command

Run the focused Roulette suite:

```bash
./scripts/local/test-roulette.sh
```

It runs:

- Activities PostgreSQL integration tests for Roulette
- React Activity tests
- React production build with `/api` + `/activities-api`

## Useful health checks

```bash
curl -k https://localhost:5001/swagger
curl -k https://localhost:7001/health
curl -k https://localhost:7001/health/live
curl -k https://localhost:7001/health/ready
```

## Debugging SignalR

- Confirm `VITE_ACTIVITIES_API_BASE_URL=/activities-api`.
- Confirm Activities JWT exists before connecting.
- Watch Activities API logs for `/hubs/games`.
- Watch React console for failed API diagnostics and correlation id.

## Debugging join intents

- Bot logs should show `Prepared Activities Roulette join intent`.
- Activities API logs should show consumed join intent.
- Duplicate consume should return a structured 409, not 500.
- Join intent expiry defaults to 5 minutes.

## Lifecycle defaults

Configured under `Roulette` in Activities API:

```json
{
  "WaitingRoomExpirationMinutes": 60,
  "InProgressAbandonmentMinutes": 180,
  "ResumeWindowMinutes": 720,
  "JoinIntentExpirationMinutes": 5,
  "CleanupIntervalSeconds": 60
}
```

No production secret is required by the frontend.
