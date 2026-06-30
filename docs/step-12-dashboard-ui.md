# Step 12 — Dashboard Overview

When you open a server in the dashboard, you land on a professional **overview** page with bot status, resource counts, and feature flags.

---

## API

| Endpoint | Auth | Purpose |
|----------|------|---------|
| `GET /api/guilds/{id}/overview` | JWT (owner) | Guild summary for the dashboard |

**Response fields:**

- `name`, `iconUrl`, `isActive`
- `resourcesSyncedAt`
- `totalChannels`, `totalRoles`
- `totalTickets`, `openTickets`, `closedTickets`
- `welcomeEnabled`, `autoRoleEnabled`, `logsEnabled`, `ticketsEnabled`

---

## Dashboard

| Route | Page |
|-------|------|
| `/guilds/:id/overview` | Server overview (default after selecting a server) |
| `/guilds/:id/settings` | Settings |
| `/guilds/:id/tickets` | Tickets |

### Overview page

- **Header** — guild icon, name, action buttons
- **Stat cards** — Bot Status, Channels, Roles, Tickets, Open Tickets, Last Sync, Welcome, Auto Role, Logs, Tickets Module
- **Actions** — Settings, Tickets, Sync Discord Data, Open Discord Server

Sidebar includes **Overview** when a server is selected.

---

## How to test

### 1. Start stack

```bash
docker compose up -d
dotnet run --project src/DiscordBot.Api --launch-profile http
cd dashboard/DiscordBot.Dashboard && npm start
```

### 2. Open a server

1. Log in at `http://localhost:4200`
2. On **Your servers**, click **Open dashboard** on a server card
3. You should land on `/guilds/{id}/overview`

### 3. Verify cards

- **Bot Status** — `Active` if the guild is registered
- **Channels / Roles** — match synced Discord data (run `/sync` if zero)
- **Tickets** — total, open, and closed counts
- **Last Sync** — timestamp after a resource sync
- **Feature cards** — Enabled/Disabled based on saved settings

### 4. Verify actions

- **Settings** → settings page
- **Tickets** → tickets list
- **Sync Discord Data** → toast with ✔ or ❌; counts refresh after ~5s
- **Open Discord Server** → opens Discord (top bar + overview button)

### 5. API check (optional)

Swagger: `GET /api/guilds/{id}/overview` with JWT Bearer token.

---

## Out of scope

Dark mode, subscriptions, admin panel, new bot features.
