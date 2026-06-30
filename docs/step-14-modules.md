# Step 14 — Module Management System

Per-server enable/disable switches for platform features.

## Modules

| Key | Name | Bot behavior when disabled |
|-----|------|----------------------------|
| `welcome` | Welcome | Join welcome message skipped |
| `tickets` | Tickets | Ticket slash commands and buttons reply with embed |
| `moderation` | Moderation | Moderation slash commands reply with embed |
| `logs` | Logs | Non-critical log writes skipped when disabled; moderation/settings logs still stored |
| `auto-role` | Auto Role | Auto role on join skipped |
| `reaction-roles` | Reaction Roles | Reaction role command and buttons reply with module disabled embed |

## Database

- **`Modules`** — global catalog (`Key`, `Name`, `Description`). Seeded on API startup.
- **`GuildModules`** — per guild row linking `GuildId` + `ModuleId` with `IsEnabled` (default `true`).

New guilds get all modules enabled when registered. Existing guilds are backfilled on first module API read.

## API

Dashboard (JWT):

- `GET /api/guilds/{id}/modules` — list modules with status
- `PUT /api/guilds/{id}/modules/{moduleKey}` — body: `{ "isEnabled": true|false }`

Bot (X-Bot-Api-Key):

- `GET /api/bot/guilds/{discordGuildId}/modules/{moduleKey}` — `{ "key", "isEnabled" }`

## Dashboard

Route: `/guilds/:id/modules`

Shows module name, description, status pill, and enable/disable toggle.

## Bot checks

`ModuleGuard` calls the bot API before running feature logic:

- **Slash commands / interactions** — respond with info embed: *"This module is disabled for this server."*
- **Background events** (welcome, auto role on join) — silently skip

Module status is separate from per-feature settings (e.g. `WelcomeEnabled` in guild settings). Both must be enabled for the feature to run.

## Test

1. Run API, bot, dashboard, and PostgreSQL.
2. Open **Modules** for a server in the dashboard.
3. Disable **Moderation**, run `/warn` in Discord — expect the disabled-module embed.
4. Disable **Welcome**, have someone join — no welcome message (even if welcome is configured in Settings).
5. Re-enable modules from the dashboard and confirm behavior returns.
