# Step 13 — Moderation MVP

Basic moderation commands in Discord with records stored in PostgreSQL and visible on the dashboard.

---

## Bot commands

| Command | Description | Permission |
|---------|-------------|------------|
| `/warn user reason` | Record a warning | Manage Messages **or** Kick Members |
| `/warnings user` | List warnings for a member | Manage Messages **or** Kick Members |
| `/clear amount` | Delete 1–100 recent messages in the current channel | Manage Messages **or** Kick Members |
| `/kick user reason` | Kick a member | Manage Messages **or** Kick Members (+ Kick Members to execute kick) |

Each action creates audit records in the API. `/warn` also creates a **Warning** row and a **ModerationCase** (type Warn).

---

## Database

| Table | Stores |
|-------|--------|
| `Warnings` | Target user, moderator, reason, timestamp |
| `ModerationCases` | Warn, Kick, Clear actions with optional message count / channel |

---

## API

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /api/bot/moderation/warnings` | Bot API key | Create warning + Warn case |
| `POST /api/bot/moderation/cases` | Bot API key | Create Kick/Clear case |
| `GET /api/bot/moderation/warnings` | Bot API key | Bot lookup for `/warnings` |
| `GET /api/guilds/{id}/warnings` | JWT (owner) | Dashboard warnings list |
| `GET /api/guilds/{id}/moderation-cases` | JWT (owner) | Dashboard cases list |

**Query filters** (dashboard): `targetUserId`, `type` (cases only), `from`, `to`

---

## Dashboard

Route: `/guilds/:id/moderation`

- Warnings table
- Moderation cases table
- Filters: user ID, type, date range

---

## Migration

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

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

### 2. Discord permissions

- Give yourself **Manage Messages** or **Kick Members** in the test server
- Give the bot **Manage Messages**, **Kick Members**, and **Read Message History**

### 3. Test commands

1. `/warn @user Testing moderation` — expect success embed
2. `/warnings @user` — should list the warning
3. Post a few messages, then `/clear 5` in that channel — messages deleted
4. `/kick @user Testing kick` — member removed (use a test account)

### 4. Verify PostgreSQL

```bash
docker exec -it discordbot-postgres psql -U postgres -d discordbot \
  -c 'SELECT "TargetDiscordUserId", "Reason" FROM "Warnings";'

docker exec -it discordbot-postgres psql -U postgres -d discordbot \
  -c 'SELECT "Type", "TargetDiscordUserId", "Reason", "MessageCount" FROM "ModerationCases";'
```

### 5. Dashboard

1. Open **Moderation** in the sidebar
2. Confirm warnings and cases appear
3. Filter by user ID or type — tables update
4. Filter by date — only records in range shown

### 6. Permission check

Run `/warn` as a user **without** Manage Messages or Kick Members — expect permission denied.

---

## Out of scope

Ban, mute, timeout, automod, appeals, advanced roles, subscriptions.
