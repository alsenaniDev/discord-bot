# Bot Architecture

## Overview

`DiscordBot.Bot` is a **.NET 9 generic host worker** using Discord.Net. It connects to the Discord Gateway and calls the REST API for all persistence and configuration.

**Critical rule:** The bot has **no database connection**.

## Startup

**File:** `src/DiscordBot.Bot/Program.cs`

Registers:

- `DiscordSocketClient` with intents: Guilds, GuildMembers, GuildMessages, MessageContent
- `BotApiClient` (HttpClient with `X-Bot-Api-Key`)
- Command and interaction handlers
- `ModuleGuard` — checks module enabled via API
- Background services

**Hosted services:**

| Service | Role |
|---------|------|
| `DiscordBotHostedService` | Gateway connection, event routing |
| `GuildMaintenanceWorker` | 30s poll: command panels, ticket cleanup, outbound messages |
| `GuildResourceSyncWorker` | 30s poll: dashboard-requested resource syncs |

## Gateway events

**File:** `src/DiscordBot.Bot/Services/DiscordBotHostedService.cs`

| Event | Handler behavior |
|-------|------------------|
| `Ready` | Register slash commands; sync all guilds to API |
| `InteractionCreated` | Route slash commands, buttons, selects, modals |
| `JoinedGuild` | Register guild + per-guild commands |
| `UserJoined` | Logs, welcome (if module on), auto-role (if module on) |
| `MessageReceived` | Auto-reply rules |

## Slash commands

**Registration:** `SlashCommandRegistration.cs` — global + per-guild

| Command | Handler | Module guard |
|---------|---------|--------------|
| `ping`, `server`, `setup`, `sync` | `SlashCommandHandlers` | setup/sync: none; server: none |
| `ticket setup/open/close` | `TicketCommandHandlers` | tickets |
| `warn`, `warnings`, `clear`, `kick` | `ModerationCommandHandlers` | moderation |
| `reaction-role create` | `ReactionRoleCommandHandlers` | reaction-roles |

## Interaction routing

| Custom ID prefix / type | Handler |
|-------------------------|---------|
| Ticket buttons, select menus, modals | `TicketInteractionHandlers` |
| Command panel buttons | `PanelInteractionHandlers` |
| Reaction role toggle buttons | `ReactionRoleInteractionHandlers` |

Custom IDs defined in `UI/DiscordCustomIds.cs`.

## BotApiClient

**File:** `src/DiscordBot.Bot/Api/BotApiClient.cs`

~26 HTTP methods covering:

- Guild registration and settings
- Resource sync
- Module status
- Permission evaluation (moderation + dashboard access)
- Tickets CRUD, setup, cleanup, outbound messages
- Moderation warnings/cases
- Logs creation
- Reaction roles
- Command panel pending refreshes
- Auto-replies

Configuration: `Api:BaseUrl`, `Api:ApiKey` (must match API `Bot:ApiKey`).

## Permission evaluation flow

Before moderation commands:

1. `ModuleGuard.EnsureEnabledForInteractionAsync(moduleKey)`
2. `BotApiClient.EvaluatePermissionsAsync` → `POST /api/bot/guilds/{id}/permissions/evaluate`
3. Handler checks specific flag (`CanWarn`, `CanKick`, etc.)
4. Additional Discord native permission checks where required (`KickMembers`)

Ticket close uses `EvaluateDashboardAccessAsync` → checks `CanAccessTickets`.

## ModuleGuard

**File:** `src/DiscordBot.Bot/Services/ModuleGuard.cs`

Calls `GET /api/bot/guilds/{discordGuildId}/modules/{moduleKey}` or `IsModuleEnabledAsync` helper.

Returns user-friendly ephemeral error if module disabled.

## Background workers detail

### GuildMaintenanceWorker (30s)

1. Command panel sync — `CommandPanelSyncService`
2. Ticket channel cleanup — `TicketChannelCleanupService` (pending cleanups from API)
3. Outbound ticket messages — `TicketOutboundMessageService` (dashboard replies → Discord)

### GuildResourceSyncWorker (30s)

1. `GET /api/bot/guilds/sync-requests`
2. For each guild: collect channels/roles/members via `ResourceSyncService`
3. `POST /api/bot/guilds/{id}/resources`

## Supporting services

| Service | Purpose |
|---------|---------|
| `EmbedBuilderService` | Consistent Discord embeds |
| `ComponentBuilderService` | Buttons, selects |
| `WelcomeMessageService` | Join messages from guild settings |
| `AutoReplyMessageService` | Keyword auto-replies |
| `DiscordLogDeliveryService` | Post log embeds to Discord channel |
| `BotLogWriter` | Write activity logs via API |
| `TicketArchiveService` | Archive ticket transcripts on close |
| `ResourceSyncService` | Collect Discord guild resources |

## Configuration

| Section | Keys |
|---------|------|
| `Discord:Token` | Bot token |
| `Api:BaseUrl` | API URL |
| `Api:ApiKey` | Shared secret |
| `Platform:DashboardUrl` | Links in embeds |

Load order: appsettings → appsettings.{Env} → appsettings.{Env}.local.json → env vars.

## Assumptions

- **Single bot instance** — no guild sharding coordination
- **No local permission cache** — every command may hit API (scalability risk — see permission-system.md)
- **Server Members Intent** required for welcome and role sync

## Related docs

- `module-system.md`, `permission-system.md`
- `api-design.md` (bot endpoints)
- `deployment.md`
