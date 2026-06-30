# Step 15 — Logs System MVP

Dashboard activity log for bot and server events.

## Log events

| Type | Source | Critical |
|------|--------|----------|
| `MemberJoined` | Bot (member join) | No |
| `WelcomeSent` | Bot (welcome message) | No |
| `AutoRoleAssigned` | Bot (auto role on join) | No |
| `TicketOpened` | API (`TicketService`) | No |
| `TicketClosed` | API (`TicketService`) | No |
| `WarningCreated` | API (`ModerationService`) | Yes |
| `MessagesCleared` | API (`ModerationService`) | Yes |
| `MemberKicked` | API (`ModerationService`) | Yes |
| `ModuleChanged` | API (`ModuleService`) | Yes |
| `SettingsUpdated` | API (`GuildService`) | Yes |
| `ResourceSyncCompleted` | API (`GuildResourceService`) | No |

**Critical** logs are always stored. **Non-critical** logs are skipped when the **Logs** module is disabled for the server.

## Database

Reuses **`LogEntries`** with updated columns:

- `Type`, `Message`, `ActorDiscordUserId`, `TargetDiscordUserId`, `ChannelDiscordId`, `MetadataJson`, `CreatedAt`

## API

Bot (`X-Bot-Api-Key`):

- `POST /api/bot/logs` — body includes `discordGuildId`, `type` (string), `message`, optional actor/target/channel/metadata

Dashboard (JWT):

- `GET /api/guilds/{id}/logs?type=&from=&to=&search=`

## Dashboard

Route: `/guilds/:id/logs`

Table columns: Date, Type, Message, Actor, Target, Channel.

Filters: type, date range, search (message and IDs).

## Module behavior

When the **Logs** module is disabled:

- Non-critical events (join, welcome, tickets, sync, etc.) are not written
- Critical events (moderation, settings, module changes) are still written

## Test end-to-end

1. Apply migration and restart API + bot + dashboard.
2. Ensure **Logs** module is **enabled** (Modules page).
3. Trigger events in Discord:
   - Have someone join → **Member joined** (+ **Welcome sent** / **Auto role** if configured)
   - Open/close a ticket → **Ticket opened/closed**
   - Run `/warn`, `/clear`, or `/kick` → moderation log entries
4. In dashboard:
   - Change settings → **Settings updated**
   - Toggle a module → **Module changed**
   - Sync Discord data → **Resource sync completed**
5. Open **Logs** — confirm entries appear with correct type, message, and IDs.
6. Disable **Logs** module, trigger a member join — join/welcome logs should stop; run `/warn` — warning log should still appear.
7. Re-enable **Logs** module and confirm routine events log again.
