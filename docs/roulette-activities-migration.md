# Roulette Activities backend migration

Date: 2026-07-11

This document records Phase 3 of the Activities refactor: adding the new Activities-owned Roulette runtime while preserving the existing React Activity and old platform API routes.

## Current implementation inventory

### Existing platform controllers/endpoints

Kept temporarily as compatibility routes:

- `ActivityGamesController`
  - `GET /api/games/activity/wallet`
  - `GET /api/games/activity/store`
  - `GET /api/games/activity/inventory`
  - `POST /api/games/activity/store/purchase`
  - `POST /api/games/activity/roulette/rooms`
  - `GET /api/games/activity/roulette/rooms/open`
  - `GET /api/games/activity/roulette/my-active-room`
  - `GET /api/games/activity/roulette/rooms/{roomId}`
  - `POST /api/games/activity/roulette/rooms/{roomId}/join`
  - `POST /api/games/activity/roulette/rooms/{roomId}/leave`
  - `POST /api/games/activity/roulette/rooms/{roomId}/start`
  - `POST /api/games/activity/roulette/rooms/{roomId}/spin`
  - `POST /api/games/activity/roulette/rooms/{roomId}/use-power-up`
  - `POST /api/games/activity/roulette/rooms/{roomId}/resolve-pending-action`
  - `GET /api/games/activity/roulette/pending-intent`
- `BotGamesController`
  - `POST /api/bot/games/roulette/rooms/{roomId}/prepare-join`
  - `GET /api/bot/games/roulette/publish-actions/pending`
  - `POST /api/bot/games/roulette/publish-actions/{id}/ack`
- `GuildGamesController`
  - dashboard Roulette settings endpoints.

### Existing platform services

- `RouletteService`
  - Current production runtime.
  - Kept temporarily for React compatibility.
- `GameHubService`
  - Platform catalog/access/sandbox/version/leaderboard logic.
  - Still platform-owned.

### Existing platform entities/tables

Keep in `DiscordBot.Api` / platform database:

- `RouletteGuildSettings`
- `GameWallet`
- `GameWalletTransaction`
- `GamePowerUpDefinition`
- `GuildPowerUpSetting`
- `PlayerPowerUpInventory`
- `RouletteJoinIntent`
- `RoulettePublishAction`
- game catalog/version/sandbox entities.

Keep temporarily as compatibility runtime tables until cutover is verified:

- `RouletteRooms`
- `RouletteRoomPlayers`
- `RouletteRoundActions`
- `RoulettePowerUpUsages`

### Existing React calls

The current React app still calls:

- `/api/games/activity/roulette/*`
- `/api/games/activity/wallet`
- `/api/games/activity/store`

These calls were not changed in this phase.

## New Activities ownership

New runtime service:

- `RouletteRuntimeService`

New Activities API controller:

- `RouletteController`

New Activities runtime entities:

- `RouletteGameSession`
- `RoulettePlayer`
- `RouletteRound`
- `RouletteBet`

New migration:

- `AddActivitiesRouletteRuntime`

New Activities tables:

- `RouletteGameSessions`
- `RoulettePlayers`
- `RouletteRounds`
- `RouletteBets`

The new Roulette runtime uses the generic Activities entities:

- `ActivitySession`
- `ActivityPlayer`
- `GameSession`
- `GameEvent`
- `GameResult`

## New endpoint map

| Old compatibility route | New Activities route | Status |
| --- | --- | --- |
| `POST /api/games/activity/roulette/rooms` | `POST /api/roulette/sessions` | Added |
| `GET /api/games/activity/roulette/rooms/open` | `GET /api/roulette/sessions/open` | Added |
| `GET /api/games/activity/roulette/my-active-room` | `GET /api/roulette/sessions/my-active` | Added |
| `GET /api/games/activity/roulette/rooms/{roomId}` | `GET /api/roulette/sessions/{gameSessionId}` | Added |
| `POST /api/games/activity/roulette/rooms/{roomId}/join` | `POST /api/roulette/sessions/{gameSessionId}/join` | Added |
| `POST /api/games/activity/roulette/rooms/{roomId}/leave` | `POST /api/roulette/sessions/{gameSessionId}/leave` | Added |
| `POST /api/games/activity/roulette/rooms/{roomId}/start` | `POST /api/roulette/sessions/{gameSessionId}/rounds/start` | Added |
| `POST /api/games/activity/roulette/rooms/{roomId}/spin` | `POST /api/roulette/sessions/{gameSessionId}/spin` | Added |
| `POST /api/games/activity/roulette/rooms/{roomId}/resolve-pending-action` | `POST /api/roulette/sessions/{gameSessionId}/resolve-pending-action` | Added |
| none | `POST /api/roulette/sessions/{gameSessionId}/reconnect` | Added |
| future betting UI | `POST /api/roulette/sessions/{gameSessionId}/bets` | Added; wallet-backed bets disabled |
| `POST /api/games/activity/roulette/rooms/{roomId}/use-power-up` | `POST /api/roulette/sessions/{gameSessionId}/use-power-up` | Stub returns 501 until wallet/inventory migration |

## Platform validation

Before creating or joining a Roulette session, Activities API calls:

```http
POST /api/internal/activities/game-access/validate
```

The platform API validates:

- guild is linked and active;
- games are enabled;
- current channel is the configured games channel;
- Roulette is enabled globally and for the guild;
- selected game version/sandbox access;
- subscription plan access.

The validation response now includes a `RouletteSettings` snapshot so Activities API can use min/max players and reward settings without reading the platform database.

## Realtime events

Activities API publishes through the existing SignalR hub group:

```text
game-session:{gameSessionId}
```

Current events:

- `RouletteSessionUpdated`
- `RoulettePlayerJoined`
- `RoulettePlayerLeft`
- `RouletteRoundStarted`
- `RouletteRoundResult`
- `RouletteRoundSettled`

The client still must call `GameHub.JoinActivitySession(activitySessionId)` or equivalent group join flow after creating/loading the Activity session. Group membership remains server-authorized.

## Server authority

The Activities runtime now generates the trusted spin target server-side using `RandomNumberGenerator.GetInt32`.

The client receives:

- selected player;
- selected index;
- trusted result metadata.

The browser should only render the animation from this server result.

## Idempotency

Implemented idempotency/uniqueness foundations:

- unique player per Roulette session;
- unique round number per Roulette session;
- unique round idempotency key per Roulette session;
- unique bet idempotency key per round/player;
- game events carry idempotency keys.

The current React app does not send idempotency keys yet. Generated keys are used for non-retry client calls. The Angular migration should send stable keys from the client for retry-sensitive operations.

## Wallet and power-ups

Wallet ownership remains in `DiscordBot.Api`.

The new Activities runtime does not perform unsafe wallet deductions or reward writes. Wallet-backed bets and power-up usage are disabled in the new runtime until platform wallet reservation endpoints are fully implemented:

- reserve;
- commit;
- release;
- all idempotent;
- transaction-protected.

The current React app still uses the old platform paths for wallet, store, and power-ups.

## Data migration status

No historical Roulette data was migrated.

Reason:

- the new runtime is added beside the current production runtime;
- old React traffic still uses old tables;
- moving active rooms mid-session would risk mismatched room IDs and player state.

Rollback:

1. Stop routing any clients to `/api/roulette/*`.
2. Keep using old `/api/games/activity/roulette/*` routes.
3. Do not drop old platform Roulette tables.
4. If needed, revert the `AddActivitiesRouletteRuntime` migration from the Activities database only.

## Known limitations

- React frontend is not switched to Activities API yet.
- Power-ups in the new runtime return 501.
- Wallet-backed bets return 501 for positive amounts.
- Bot room invite/publish actions still use the old platform runtime.
- Full automated backend tests are still needed around the new service once a test project is introduced.

## Next recommended phase

1. Add a backend test project for Activities runtime.
2. Implement idempotent platform wallet reservation/commit/release.
3. Add a temporary React feature flag to route Roulette calls to Activities API in a test guild.
4. Verify multiplayer flow through SignalR in Discord desktop and mobile.
5. Only after verification, retire old platform Roulette runtime routes.

