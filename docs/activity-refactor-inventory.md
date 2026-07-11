# Discord Activity refactor inventory

Date: 2026-07-11

This inventory captures the first safe boundary cut for moving the Discord Games Activity runtime out of the main platform API. The intent is to avoid breaking the currently working React Activity and existing `/api/games/activity/*` endpoints while a new Angular Activity frontend and dedicated Activities backend are built beside them.

## Current frontend inventory

Existing Activity frontend:

- Project: `activity/DiscordBot.Activity`
- Framework: React + Vite
- Entry/routes:
  - `src/App.tsx`
  - `/games`
  - `/games/quiz`
  - `/games/roulette`
  - `/games/roulette/room/:roomId`
  - `/store`
  - `/leaderboard`
- Discord SDK/session:
  - `src/context/ActivityProvider.tsx`
  - `src/lib/discordSdk.ts`
- API client:
  - `src/lib/api.ts`
- Game screens/components:
  - `src/pages/GamesHubPage.tsx`
  - `src/pages/QuizPage.tsx`
  - `src/pages/RoulettePage.tsx`
  - `src/pages/RouletteRoomPage.tsx`
  - `src/pages/StorePage.tsx`
  - `src/pages/LeaderboardPage.tsx`
  - `src/components/roulette/RouletteWheel.tsx`
- Styling:
  - `src/styles.css`
  - `src/styles/game-theme.css`
  - `src/pages/GamesHubPage.css`
  - `src/pages/RoulettePage.css`

Compatibility rule for this phase: keep this React app and its old API calls operational until the Angular Activity app is feature-complete and verified inside Discord.

## Current platform backend inventory

Existing platform API controllers related to games:

- `ActivityGamesController`
  - Current Activity auth/context/session/leaderboard/roulette runtime endpoints.
  - Target: move runtime endpoints to `DiscordBot.Activities.Api`; keep compatibility routes during migration.
- `DiscordActivityController`
  - Discord Activity OAuth exchange for the current React app.
  - Target: replace with `DiscordBot.Activities.Api` OAuth exchange once the new frontend uses the new backend.
- `GameRuntimeController`
  - Game runtime/plugin event endpoints.
  - Target: move runtime event handling to Activities backend.
- `GameRuntimeTokenController`
  - Runtime token issuing for plugin flows.
  - Target: reassess after the new Activities JWT/session model is adopted.
- `GameIntegrationsController`
  - Integration/plugin-facing game endpoints.
  - Target: keep platform-owned catalog/version metadata here; move live gameplay calls out.
- `GuildGamesController`
  - Server-owner dashboard settings for games.
  - Target: stay in platform API.
- `AdminGamesController`
  - Platform admin game catalog/version/sandbox management.
  - Target: stay in platform API.
- `BotGamesController`
  - Bot-facing game publishing and roulette join intent endpoints.
  - Target: bot publish queue can stay platform-side until publish orchestration is separately extracted.
- `InternalActivitiesController`
  - New service-to-service seam added in this phase.
  - Target: platform-only internal validation and future wallet reservation/commit/release.

Existing game services:

- `GameHubService`
  - Owns game catalog, plan checks, guild settings, sandbox version selection, leaderboard/session logic.
  - Target: split platform responsibilities from runtime responsibilities. Catalog, plan checks, guild settings, and sandbox selection stay platform-side. Live session/gameplay state moves to Activities.
- `RouletteService`
  - Owns current roulette runtime, rooms, wallets/store/power-ups, join intents and publish actions.
  - Target: move room/session/action runtime to Activities after data migration strategy is ready. Wallet source-of-truth needs a platform reservation seam.
- `GamePluginService`
  - Owns plugin runtime/version/content/event support.
  - Target: plugin catalog/version stays platform-side; gameplay event execution moves to Activities when plugin host contracts are finalized.

Existing domain entities to classify:

### Stay in platform API/database

These are platform management, entitlement, catalog, or server settings records:

- `Guild`
- `GuildGamesSettings`
- `GuildGameSetting`
- `GuildPowerUpSetting`
- `PlatformGameDefinition`
- `GameVersion`
- `GameSandboxAccess`
- `GameContent`
- `GamePowerUpDefinition`
- `RouletteGuildSettings`
- `GameBotPublishAction`
- `GameResultPublishAction`
- `RoulettePublishAction`
- `RouletteJoinIntent` for the current bot handoff flow, until Activity launch/join handoff is redesigned.
- `GameWallet` and existing `GameWalletTransaction` as the current wallet source of truth.

### Move to Activities API/database

These are live runtime records and should not remain coupled to the dashboard/platform database long term:

- `GameSession`
- `GamePlayer`
- `GameEvent`
- Roulette rooms/players/round actions:
  - `RouletteRoom`
  - `RouletteRoomPlayer`
  - `RouletteRoundAction`
  - `RoulettePowerUpUsage`

### Shared contracts

Cross-service contracts should live in `DiscordBot.Shared` or explicit client DTOs, not by referencing platform EF entities:

- Activity auth/user identity DTOs.
- Game access validation request/response.
- Wallet reservation request/response.
- Game result publish command.
- Stable error/problem response shape.

## New backend boundary added in this phase

New projects:

- `src/DiscordBot.Shared`
- `src/DiscordBot.Activities.Domain`
- `src/DiscordBot.Activities.Application`
- `src/DiscordBot.Activities.Infrastructure`
- `src/DiscordBot.Activities.Api`

New Activities backend responsibilities:

- Exchange Discord Activity OAuth code for an Activities JWT.
- Create Activity sessions after validating guild/channel/game access with the platform API.
- Own new Activities runtime database schema.
- Provide SignalR hub foundation for real-time game state.
- Validate SignalR group joins server-side; clients cannot choose arbitrary groups.
- Communicate with the platform API via service-to-service calls, authenticated with `X-Activities-Service-Key`.

New platform internal seam:

- `POST /api/internal/activities/game-access/validate`
  - Validates guild, channel, plan, game enablement, sandbox/published version, and returns the selected version metadata.
- `POST /api/internal/activities/wallet/reservations`
  - Scaffolded only. Returns 501 until wallet reservation implementation is added.
- `POST /api/internal/activities/wallet/reservations/{reservationId}/commit`
  - Scaffolded only. Returns 501.
- `POST /api/internal/activities/wallet/reservations/{reservationId}/release`
  - Scaffolded only. Returns 501.

## New Activities schema

Migration:

- `InitialActivitiesRuntime`

Tables:

- `ActivitySessions`
- `ActivityPlayers`
- `GameSessions`
- `GamePlayers`
- `GameEvents`
- `GameResults`
- `GameWalletTransactions`

The Activities database intentionally stores Discord IDs and platform version IDs as references, not duplicated platform `Guild`, `Plan`, or catalog entities.

## Endpoint migration map

| Current endpoint | Target endpoint | Phase |
| --- | --- | --- |
| `POST /api/discord/activity/token` | `POST /api/auth/discord/exchange` | New endpoint added; old endpoint remains |
| `GET /api/games/activity/context` | Platform validation + future Activities game listing/session bootstrap | Future migration |
| `POST /api/games/activity/start-session` | `POST /api/activity-sessions` | New endpoint added for Activity session bootstrap |
| `POST /api/games/activity/complete-session` | Future `POST /api/game-sessions/{id}/complete` | Not moved yet |
| `GET /api/games/activity/leaderboard` | Future Activities read endpoint or platform leaderboard read model | Not moved yet |
| `/api/games/activity/roulette/*` | Future `/api/roulette/*` on Activities API | Not moved yet |
| SignalR none/current polling | `/hubs/games` | Hub foundation added |

## Migration rules

1. Do not delete the old React app until the Angular Activity app is verified inside Discord desktop and mobile.
2. Do not delete old `/api/games/activity/*` endpoints until route-level traffic has been switched and observed.
3. Do not let Activities API access platform EF entities or platform `AppDbContext` directly.
4. Keep platform-owned data in the platform database: guilds, plans, game catalog, versions, sandbox access, server settings, and wallet balances.
5. Keep Activities-owned data in the Activities database: live sessions, players, gameplay events, game results, and idempotent runtime transactions.
6. Use service-to-service endpoints for entitlement and wallet reservation.
7. Every runtime write must have an idempotency key before money/coins or game-result publishing is moved.

