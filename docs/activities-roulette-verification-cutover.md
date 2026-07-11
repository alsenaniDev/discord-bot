# Activities Roulette verification and controlled cutover

Date: 2026-07-11

This phase adds automated tests, a controlled React routing flag, platform wallet reservations, Activities JWT wiring, SignalR client plumbing, and go/no-go criteria for the new Activities-side Roulette runtime.

## Test project structure

Added:

- `tests/DiscordBot.Activities.UnitTests`
- `tests/DiscordBot.Activities.IntegrationTests`

Current coverage:

- Roulette state-machine transition rules.
- Feature-error metadata contract.
- Activities API rejects missing/invalid JWT.
- Activities API rejects mismatched trusted guild context before runtime logic.
- Activities API rejects mismatched or missing trusted Activity instance context before runtime logic.
- SignalR negotiate rejects missing JWT.
- Power-up endpoint returns structured `feature_not_available`.
- Roulette capabilities endpoint reports `activities-v1`, reconnect enabled, power-ups disabled.
- Pilot diagnostics endpoint requires service authentication.

Run:

```bash
dotnet test DiscordBot.sln --no-build --no-restore --disable-build-servers
```

In the local sandbox, `dotnet test` needs permission to create MSBuild named pipes.

## Roulette state machine

Centralized helper:

- `RouletteRuntimeStates`

Current states:

- `Waiting` / WaitingForPlayers
- `InProgress` / BettingOpen
- `BettingClosed`
- `Spinning`
- `Settling`
- `Completed`
- `Cancelled`
- `Expired`

Allowed transitions:

| From | To |
| --- | --- |
| Waiting | InProgress, Cancelled, Expired |
| InProgress | Spinning, Settling, Completed, Cancelled |
| BettingClosed | Spinning, Cancelled |
| Spinning | Settling, Cancelled |
| Settling | InProgress, Completed, Cancelled |
| Completed / Cancelled / Expired | none |

The current React-compatible API shape still returns `Waiting` and `InProgress`, but server-side checks now go through centralized state helpers instead of scattered raw comparisons.

## Wallet reservation design

Platform-owned table:

- `WalletReservations`

Migration:

- `AddWalletReservations`

Fields:

- `ReservationId`
- `IdempotencyKey`
- `GuildId`
- `DiscordUserId`
- `GameKey`
- `Amount`
- `Currency`
- `Status`
- `ExpiresAtUtc`
- `CommittedAtUtc`
- `ReleasedAtUtc`
- `FailureReason`

Statuses:

- `Pending`
- `Committed`
- `Released`
- `Expired`

Internal endpoints:

- `POST /api/internal/activities/wallet/reservations`
- `POST /api/internal/activities/wallet/reservations/{reservationId}/commit`
- `POST /api/internal/activities/wallet/reservations/{reservationId}/release`
- `POST /api/internal/activities/wallet/credits`

Current guarantees:

- service-key authentication required;
- reservation creation is idempotent by `IdempotencyKey`;
- commit is idempotent;
- release is idempotent;
- committed reservation cannot be released;
- released/expired reservation cannot be committed;
- balance cannot go below zero;
- concurrent platform reservations are transaction-protected with serializable transactions;
- expired pending reservations are marked expired during reservation creation;
- current wallet is integer coins, so fractional reservations are rejected.

## Activities Roulette wallet behavior

Positive whole-coin bet requests now use the platform reservation workflow:

```text
reserve wallet amount -> persist RouletteBet -> commit reservation
```

If persistence fails after a reservation, Activities API attempts to release the reservation.

Bet payment states now use the existing `RouletteBets.Status` field:

- `PendingCommit` — bet row exists and wallet reservation commit is outstanding.
- `Accepted` — commit succeeded or the bet had zero amount.
- `CommitFailed` — commit failed definitively; the runtime attempts to release the reservation and does not accept the bet.

A background reconciliation service scans stale `PendingCommit` bets and retries idempotent reservation commits. Successful retries mark the bet `Accepted`; failures remain recoverable and are logged with reservation and bet IDs.

## Roulette payout model

Activities-owned table:

- `RoulettePayouts`

Migration:

- `AddRoulettePayouts`

Fields:

- `RouletteRoundId`
- `DiscordUserId`
- `Amount`
- `Currency`
- `IdempotencyKey`
- `Status`
- `RetryCount`
- `LastAttemptAtUtc`
- `LastError`
- `PaidAtUtc`

Statuses:

- `PendingPayout`
- `Processing`
- `Paid`
- `RetryableFailed`
- `Failed`

Payout guarantees currently implemented:

- payout records are persisted before platform wallet credit;
- platform wallet credits are idempotent by `PayoutId` through `GameWalletTransactions.ReferenceId`;
- Activities API never directly updates platform wallet balances;
- payout reconciliation retries pending/retryable records with bounded backoff;
- permanent validation rejection is marked `Failed`;
- successful payout marks the payout `Paid`;
- when every payout on a round is paid, the round is marked `Completed`;
- SignalR publication is not the source of truth; reconnect snapshots reconstruct current state.

Limitations:

- fractional coin bets are rejected because the current platform wallet is integer-based;
- deeper settlement/payout tests are still required before enabling public betting UI;
- no React betting UI is currently wired to this endpoint.

## Feature-unavailable contract

Unsupported runtime features return:

```json
{
  "code": "feature_not_available",
  "message": "...",
  "feature": "..."
}
```

Known unsupported features on the new runtime:

- `roulette_power_ups`

## Activities authentication and trusted context

React Activity now exchanges Discord OAuth codes against Activities API when `VITE_ACTIVITIES_API_BASE_URL` is configured:

```text
Discord SDK authorize -> POST /api/auth/discord/exchange -> Activities JWT + Discord access token
```

Token usage:

- Discord access token is used only for `discordSdk.commands.authenticate` and existing legacy platform routes.
- Activities JWT is stored in memory and used for new `/api/roulette/*` calls.
- SignalR uses the Activities JWT via `accessTokenFactory`.

Trusted context rules:

- `DiscordUserId` comes from the JWT.
- `DiscordGuildId`, `DiscordChannelId`, and `ActivityInstanceId` are signed into the Activities JWT during exchange.
- Runtime endpoints reject request body/query guild or channel values that differ from JWT claims.
- Request body `ActivityInstanceId` is treated only as an untrusted hint; the signed claim is applied before runtime services are called.
- Missing `ActivityInstanceId` is rejected by default. `ActivitiesAuth__AllowMissingActivityInstanceInDevelopment=true` is development-only and logs a warning.

## React controlled rollout

React Activity now has one routing abstraction in:

- `activity/DiscordBot.Activity/src/lib/api.ts`

Configuration:

```bash
VITE_ACTIVITIES_API_BASE_URL="https://your-activities-api"
VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS="1522007756033228841,anotherGuildId"
```

Behavior:

- If the current guild is not in `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS`, Roulette keeps using legacy `/api/games/activity/roulette/*`.
- If the current guild is in the pilot list and `VITE_ACTIVITIES_API_BASE_URL` is set, Roulette runtime calls route to `/api/roulette/*`.
- Wallet/store calls stay on legacy platform endpoints.
- Power-up use on the new runtime is blocked client-side with a clear Arabic message.

Important: the React auth flow now receives both a Discord token and an Activities JWT. Automated builds pass, but a real Discord pilot test has not been run in this environment, so do not mark the auth path as pilot-passed until tested inside Discord.

## SignalR client status

Added:

- `activity/DiscordBot.Activity/src/lib/activitiesSignalR.ts`

Capabilities:

- one managed connection;
- Activities JWT access token factory;
- automatic reconnect;
- reconnect callback calls `POST /api/roulette/sessions/{gameSessionId}/reconnect`;
- successful reconnect joins the trusted SignalR group through `JoinRouletteGameSession(gameSessionId)`;
- duplicate Roulette event handler prevention by game session ID;
- cleanup/disconnect helpers;
- Arabic-facing connection errors.

Remaining before pilot sign-off:

- two-client Discord manual test;
- component-level React reconnect tests for the full page lifecycle.

Duplicate connection policy:

- multiple valid browser/Discord clients for the same user may connect during the private pilot;
- every connection must authenticate independently with an Activities JWT;
- every connection must call trusted reconnect/join before receiving room events;
- the server derives the group name as `game-session:{GameSessionId}`;
- client-supplied group names are never accepted;
- duplicate handlers for the same game session are replaced client-side.

## SignalR verification procedure

Manual Discord verification still required:

1. Start `DiscordBot.Api`.
2. Start `DiscordBot.Activities.Api`.
3. Apply platform and Activities migrations.
4. Configure `VITE_ACTIVITIES_API_BASE_URL`.
5. Configure one pilot guild ID.
6. Open the Discord Activity with two users.
7. Confirm both clients authenticate to Activities API.
8. Confirm both clients call `JoinRouletteGameSession`.
9. Confirm both clients receive:
   - `RoulettePlayerJoined`
   - `RouletteRoundStarted`
   - `RouletteRoundResult`
   - `RouletteRoundSettled`
10. Refresh one client and confirm reconnect does not duplicate subscriptions.
11. Try joining another session ID and confirm authorization rejects it.

SignalR logs include:

- `ConnectionId`
- `GroupName`
- `DiscordUserId`

Further log enrichment should add:

- `ActivityInstanceId`
- `RoundId`

## Health and diagnostics

Health endpoints:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`

Readiness checks:

- Activities database connectivity;
- pending Activities migrations;
- JWT issuer/audience/signing key configuration;
- Discord Activity OAuth configuration;
- Platform API base URL and service token configuration.

Protected diagnostics endpoint:

- `GET /api/internal/diagnostics/pilot`
- header: `X-Activities-Service-Key`

Diagnostics includes:

- runtime version;
- database connection and pending migration names;
- pending wallet commit count;
- pending/failed payout counts;
- active Roulette session count;
- pilot guild config source.

## EF tooling

Repository-local tool manifest:

- `.config/dotnet-tools.json`
- `dotnet-ef` version `9.0.4`

Use:

```bash
dotnet tool restore
dotnet tool run dotnet-ef --version
dotnet tool run dotnet-ef migrations list --project src/DiscordBot.Activities.Infrastructure --startup-project src/DiscordBot.Activities.Api --context ActivitiesDbContext
```

## PostgreSQL/Testcontainers coverage

Added PostgreSQL 16 Testcontainers fixture:

- starts PostgreSQL 16;
- creates isolated Activities and Platform databases;
- applies EF Core migrations for both contexts;
- uses real Npgsql/EF relational constraints.

PostgreSQL tests currently cover:

- duplicate Roulette player membership;
- duplicate bet idempotency key;
- duplicate round number per Roulette session;
- duplicate payout idempotency / round-user payout reference;
- foreign key enforcement;
- decimal precision and state persistence;
- duplicate platform wallet reservation idempotency key;
- duplicate wallet credit payout reference;
- concurrent duplicate player inserts;
- concurrent duplicate bet inserts;
- fresh Processing payout is not reclaimed;
- stale Processing payout is reclaimed and paid;
- two payout workers do not double-credit the same stale payout;
- permanent payout rejection is marked Failed and not retried;
- SignalR group helper consistency.

Current automated count:

- .NET unit tests: 11
- .NET integration tests: 24
- PostgreSQL/Testcontainers tests: 15
- React/Vitest tests: 4

## Rollback

Rollback remains straightforward:

1. Remove all guild IDs from `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS`.
2. Redeploy the React Activity.
3. Existing clients return to legacy `/api/games/activity/roulette/*`.
4. Keep old platform Roulette tables intact.
5. Do not drop `WalletReservations`; it is additive.
6. If needed, rollback Activities migrations:
   - `AddRoulettePayoutLeases`
   - `AddRoulettePayouts`
   - `AddActivityInstanceRouletteIndexes`
   - `AddActivitiesRouletteRuntime`

## Go/no-go

Current recommendation: **No-go for full cutover.**

Reasons:

- Real PostgreSQL/Testcontainers coverage exists and passes, but service-level end-to-end settlement tests are still limited.
- Wallet reservation endpoints, payout records, idempotent platform credit, and payout reconciliation exist and pass focused tests.
- React Activity token flow is switched to Activities JWT for the pilot route, but it still needs manual Discord verification.
- PostgreSQL migration application and focused Testcontainers concurrency coverage pass.
- Two-client SignalR verification inside Discord has not been performed in this environment.
- Pilot guild test inside Discord has not been performed.

Safe status:

- Safe to deploy backend additions with pilot list empty.
- Safe to run platform wallet reservation endpoints internally.
- Safe to test `/api/roulette/*` manually with a valid Activities JWT and matching trusted guild/channel/activity instance claims.
- Safe to enable exactly one sandbox/private pilot guild only after an operator is ready to monitor logs and rollback.
- Not safe to make Activities Roulette default yet.

## Private pilot checklist status

Not run manually in Discord yet.

Before enabling exactly one private guild in `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS`, record:

- tester:
- date:
- Discord client type:
- pilot guild:
- correlation/log IDs:
- result:
- issue found:
- pass/fail:

Startup and authentication:

- Activity opens from the configured games channel.
- `/api/auth/discord/exchange` returns an Activities JWT and Discord access token.
- Roulette create/join/open calls use the Activities JWT.
- no Activities JWT is stored in `localStorage`.
- second user authenticates independently.

Session and multiplayer:

- Wrong guild, wrong channel, and wrong Activity instance are rejected.
- Two clients can join the same room.
- SignalR updates are received by both clients.
- Refresh/reopen reconnects to the same room without duplicate handlers.

Betting and wallet:

- Duplicate join/bet/spin clicks remain idempotent.
- Platform API temporary failure produces Arabic recoverable errors.
- Power-up UI remains blocked on the new runtime.

Round and payout:

- start round once.
- attempt duplicate start.
- spin once.
- both clients receive the same durable result.
- winning payout is credited once.
- refresh both clients and verify the same durable result.

Rollback:

- Removing the guild from `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS` returns the client to legacy runtime after redeploy.

## Next recommended phase

1. Enable one private sandbox pilot guild.
2. Run the two-client Discord checklist and record tester/date/client/log IDs.
3. Add service-level end-to-end settlement tests around `RouletteRuntimeService`.
4. Add component-level React tests for the full Roulette room reconnect UI.
5. Only after passing pilot tests, enable controlled rollout percentage.
