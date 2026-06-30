# Step 19 — Customer Onboarding Flow

Improved first-time setup for guild owners — no new bot features.

## Customer journey

1. **Login** with Discord on the dashboard
2. **Invite** the bot to their server
3. **Run `/setup`** (or `/sync`) in Discord
4. **Configure** plan, modules, and settings in the dashboard
5. **Use** the bot in Discord

## API

### `GET /api/onboarding/status` (JWT)

Returns:

- `hasGuilds` — whether the user owns any registered servers
- `botInviteUrl` — Discord OAuth invite link built from `Discord:ClientId`
- `dashboardUrl` — from `Discord:DashboardUrl`
- `guilds[]` — each guild with a `checklist` object

### Checklist (per guild)

| Item | Complete when |
|------|----------------|
| Bot invited | Guild is registered and active |
| Resources synced | `resourcesSyncedAt` set and channels/roles exist |
| Plan selected | Guild has a subscription record (auto-assigned Free on `/setup`) |
| Modules enabled | At least one module toggled on |
| Welcome configured | Welcome enabled + channel set |
| Tickets configured | Tickets enabled + category set (or `/ticket setup`) |

Also included on `GET /api/guilds/{id}/overview` as `onboarding`.

## Dashboard

### No guilds (`/servers`)

Shows a friendly onboarding page instead of an empty list:

- **Invite Bot** button (opens Discord invite URL)
- Step-by-step guide (`/setup`, `/sync`, refresh)
- **I invited the bot — refresh** button
- Setup checklist (all incomplete)

### Has guilds

- Server cards show setup progress percentage
- **Overview** shows checklist + progress bar until 100% complete
- Quick links to Settings, Modules, Subscription

## Bot

### `/setup` (improved)

1. Registers the guild with the API
2. Syncs Discord channels and roles
3. Responds with a single embed confirming:
   - Server registered
   - Resources synced (or prompt to run `/sync`)
   - Next steps (plan, modules, welcome, tickets)
   - Dashboard URL (`Platform:DashboardUrl` in bot config)

## Bot invite URL

Generated server-side:

```
https://discord.com/oauth2/authorize?client_id={ClientId}&permissions=...&scope=bot%20applications.commands
```

Uses `Discord:ClientId` from API appsettings.

## Bot config

Add to bot `appsettings.json`:

```json
"Platform": {
  "DashboardUrl": "http://localhost:4200"
}
```

## Test from zero

1. New Discord account logs into dashboard → onboarding page with Invite Bot
2. Click **Invite Bot** → add bot to a test server
3. In Discord (as server owner), run `/setup`
4. Back on dashboard, click refresh → server appears with partial checklist
5. Open overview → complete plan, modules, welcome, tickets steps
6. Checklist reaches 100% when all items are done

## Not included

Payments, email, multi-step wizard UI, multi-bot support.
