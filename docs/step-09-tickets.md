# Step 9 — Tickets MVP

A minimal support ticket system: Discord slash commands, private channels, SQL persistence via the API, and a dashboard list view.

---

## What was added

### Domain & database

| Item | Purpose |
|------|---------|
| `Ticket` entity | Guild, number, owner, channel, status, closed date |
| `TicketStatus` enum | `Open`, `Closed` |
| `GuildSettings.TicketsEnabled` | Whether tickets are active |
| `GuildSettings.TicketCategoryId` | Discord category for new ticket channels |

### API

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /api/bot/tickets` | Bot API key | Create ticket after channel is created |
| `GET /api/bot/tickets/by-channel/{channelDiscordId}` | Bot API key | Look up ticket when closing |
| `PATCH /api/bot/tickets/{id}/close` | Bot API key | Mark ticket closed |
| `POST /api/bot/guilds/{discordGuildId}/tickets/setup` | Bot API key | Enable tickets + save category ID |
| `GET /api/guilds/{guildId}/tickets` | JWT (guild owner) | List tickets for dashboard |

### Bot commands

| Command | Who | What |
|---------|-----|------|
| `/ticket setup` | Manage Server | Creates a **Tickets** category and enables tickets in the API |
| `/ticket open` | Any member | Creates a private channel; owner + admin roles can see it |
| `/ticket close` | Owner or staff | Closes ticket in API and deletes the channel |

### Dashboard

- Route: `/guilds/:id/tickets`
- Lists ticket number, status, owner Discord ID, channel ID, created/closed dates
- **Tickets** button on the servers page

---

## Migration

After pulling this step, apply the new migration:

```bash
cd src/DiscordBot.Api
dotnet ef database update --project ../DiscordBot.Infrastructure
```

---

## How to test

### 1. Start infrastructure

```bash
docker compose up -d
```

### 2. Apply migration (if not done)

```bash
cd src/DiscordBot.Api
dotnet ef database update --project ../DiscordBot.Infrastructure
```

### 3. Start API, bot, and dashboard

Three terminals:

```bash
cd src/DiscordBot.Api && dotnet run
cd src/DiscordBot.Bot && dotnet run
cd dashboard/DiscordBot.Dashboard && npm start
```

Ensure `appsettings.Development.local.json` (API + Bot) has matching `Bot:ApiKey` / `Api:ApiKey` and a valid Discord bot token.

### 4. Discord — register server

1. Invite the bot to your test server (needs **Manage Channels** for ticket channels).
2. Run `/setup` if the guild is not registered yet.

### 5. Discord — enable tickets

1. As a user with **Manage Server**, run `/ticket setup`.
2. Confirm a **Tickets** category appears and the bot replies with success.

### 6. Discord — open a ticket

1. Run `/ticket open` as a normal member.
2. You should get a private `#ticket-1` (or similar) channel.
3. Only you and users with admin/manage-server roles should see it.

### 7. Discord — close a ticket

1. Inside the ticket channel, run `/ticket close`.
2. Ticket is marked closed in the database; channel is deleted after a short delay.

### 8. Dashboard

1. Open `http://localhost:4200`, log in with Discord.
2. On **Your servers**, click **Tickets** for your guild.
3. Verify open and closed tickets show with correct status, IDs, and dates.

### 9. API (optional)

With Swagger at `http://localhost:5217/swagger`, bot endpoints require header:

`X-Bot-Api-Key: <your-dev-key>`

---

## Troubleshooting

| Problem | Check |
|---------|--------|
| "Tickets are not set up" | Run `/ticket setup` first |
| "You already have an open ticket" | Close existing ticket or only one open per user |
| Channel not private | Bot needs **Manage Channels**; category permissions apply |
| Dashboard empty / 404 | Run `/setup`; you must be guild **owner** in the database |
| Bot can't reach API | API running? `Api:BaseUrl` and matching API keys? |

---

## Out of scope (by design)

Categories (beyond auto-created one), transcripts, claim, priorities, buttons, modals, advanced permission matrices.
