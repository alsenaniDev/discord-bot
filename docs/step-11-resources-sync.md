# Step 11 — Discord Resource Synchronization

Stop typing Discord snowflake IDs in the dashboard. The bot syncs channels and roles to PostgreSQL; the dashboard shows dropdowns with human-readable names.

---

## What was added

### Database

| Table | Stores |
|-------|--------|
| `DiscordChannels` | Text, category, and voice channels (`DiscordChannelId`, `Name`, `Type`, `Position`) |
| `DiscordRoles` | Server roles (`DiscordRoleId`, `Name`, `Color`, `Position`, `IsManaged`) |

`Guilds` also has:

- `ResourceSyncRequested` — set when dashboard requests a sync
- `ResourcesSyncedAt` — last successful sync time

On every sync, existing channel/role rows for that guild are **deleted and replaced** (no duplicates).

### API

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /api/guilds/{id}/channels` | JWT (owner) | List synced channels |
| `GET /api/guilds/{id}/categories` | JWT (owner) | List synced category channels |
| `GET /api/guilds/{id}/roles` | JWT (owner) | List synced roles |
| `POST /api/guilds/{id}/sync-resources` | JWT (owner) | Request sync from bot |
| `GET /api/bot/guilds/sync-requests` | Bot API key | Guild IDs waiting for sync |
| `POST /api/bot/guilds/{discordGuildId}/resources` | Bot API key | Bot uploads channel/role lists |

The bot endpoint also accepts `POST .../sync-resources` for backward compatibility.

### Bot

- `ResourceCollector` — reads text channels, categories, voice channels, and roles from Discord
- `ResourceSyncService` — sends data to the API
- `GuildResourceSyncWorker` — polls pending sync requests every 30 seconds

### Dashboard

Settings page uses **dropdowns** instead of text inputs:

| Setting | Dropdown shows |
|---------|----------------|
| Welcome channel | `#channel-name` |
| Log channel | `#channel-name` |
| Auto role | Role name (non-managed roles only) |
| Ticket category | Category name (when tickets enabled) |

**Sync Discord Data** button requests a sync from the bot. Toasts show `✔ Synced successfully` or `❌ Failed to sync`.

---

## When sync runs

| Trigger | What happens |
|---------|----------------|
| Bot joins guild | Registers guild + syncs resources |
| Bot ready (existing guilds) | Register + sync on startup |
| `/setup` in Discord | Register + sync |
| `/sync` in Discord | Sync channels and roles immediately |
| `/ticket setup` | Enables tickets + sync (includes new category) |
| Dashboard **Sync Discord Data** | Sets flag; bot picks it up within ~30s |

No SignalR, webhooks, or real-time updates — data refreshes on the triggers above.

---

## Migration

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

If upgrading from `GuildChannels`/`GuildRoles`, the migration recreates tables as `DiscordChannels`/`DiscordRoles`. Re-run `/sync` or **Sync Discord Data** after applying.

---

## How to test

### 1. Start stack

```bash
docker compose up -d
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
dotnet run --project src/DiscordBot.Api --launch-profile http
dotnet run --project src/DiscordBot.Bot
cd dashboard/DiscordBot.Dashboard && npm start
```

### 2. Join bot / initial sync

1. Invite the bot to a test server (or use an existing one).
2. Bot logs on startup should show: `Synced X channels and Y roles for guild …`
3. Or run **`/setup`** or **`/sync`** in Discord.

### 3. Verify PostgreSQL

```bash
docker exec -it discordbot-postgres psql -U postgres -d discordbot \
  -c 'SELECT "Name", "Type", "Position" FROM "DiscordChannels" LIMIT 10;'

docker exec -it discordbot-postgres psql -U postgres -d discordbot \
  -c 'SELECT "Name", "Position", "IsManaged" FROM "DiscordRoles" LIMIT 10;'
```

### 4. Dashboard dropdowns

1. Open `http://localhost:4200` → **Settings** for your server.
2. Confirm **Welcome channel**, **Log channel**, **Role**, and **Ticket category** dropdowns show names (e.g. `#general`, `Member`, `Tickets`).
3. Select values and **Save settings** — Discord IDs are stored internally, not shown to the user.

### 5. Manual sync from dashboard

1. Create a new channel or role in Discord.
2. Click **Sync Discord Data** on the settings page.
3. Wait up to ~30 seconds (bot must be online).
4. Dropdowns reload automatically — new items should appear.

### 6. Bot still uses IDs internally

After saving settings, run **`/server`** in Discord — welcome/log channel IDs and auto role ID should match your dropdown selections (stored in `GuildSettings`).

### 7. API check (optional)

Swagger: `http://localhost:5217/swagger`

- `GET /api/guilds/{id}/channels`, `/categories`, `/roles` — JWT required
- Bot endpoints need `X-Bot-Api-Key` header

---

## Troubleshooting

| Problem | Check |
|---------|--------|
| Empty dropdowns | Run `/setup`, `/sync`, or click **Sync Discord Data** with bot online |
| Sync button fails | Bot running? Bot in the server? API reachable? |
| Role missing from auto-role list | Bot-managed roles are stored but hidden from the assignable dropdown |
| Category missing | Run `/sync` after creating categories |

---

## Out of scope

Real-time sync on channel/role changes, permission overwrites, emoji/sticker sync, RBAC, caching.
