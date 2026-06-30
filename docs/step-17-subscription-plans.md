# Step 17 — Subscription Plans MVP

Plan-based module limits without payment integration.

## Plans

| Key | Name | Modules |
|-----|------|---------|
| `free` | Free | welcome, logs |
| `basic` | Basic | welcome, logs, reaction-roles |
| `pro` | Pro | welcome, logs, reaction-roles, tickets, moderation |
| `premium` | Premium | all modules (`*`) |

## Database

**`SubscriptionPlans`** — catalog:

- `Key`, `Name`, `Description`, `AllowedModulesJson`, `IsActive`

**`GuildSubscriptions`** — one row per guild:

- `GuildId`, `SubscriptionPlanId`

New guilds default to **Free**. Existing guilds get **Free** on first subscription/modules access.

`AllowedModulesJson` is a JSON string array of module keys, or `["*"]` for all modules.

## API

- `GET /api/plans` — list active plans (JWT)
- `GET /api/guilds/{id}/subscription` — current guild plan
- `PUT /api/guilds/{id}/subscription` — body `{ "planKey": "pro" }` (owner only, dev testing)

Bot module status (`GET /api/bot/guilds/{id}/modules/{key}`) now returns:

- `isEnabled` — guild toggle
- `allowedByPlan` — plan includes module

## Module limits

A module runs in Discord only when **both** are true:

1. Included in the guild's plan (`allowedByPlan`)
2. Enabled on the **Modules** page (`isEnabled`)

**Dashboard**

- Toggle disabled for modules not in plan (with label)
- Can still disable modules that were enabled before a downgrade

**Bot**

- Plan blocked → *"This module is not available in your current plan."*
- Module disabled → *"This module is disabled for this server."*

**Plan downgrade**

- Modules outside the new plan are auto-disabled when the plan changes

## Dashboard

Route: `/guilds/:id/subscription`

- Shows current plan and included modules
- Lists all plans with module lists
- **Switch to …** buttons for manual plan changes (no payment)

## Test changing plans

1. Apply migration, restart API (seeds plans).
2. Open **Subscription** for a server — should show **Free**.
3. Open **Modules** — only Welcome and Logs can be enabled; others show "Not included in your current plan".
4. Switch to **Pro** on Subscription page.
5. **Modules** — tickets/moderation/reaction-roles toggles unlock.
6. Enable **Tickets**, then switch back to **Free** — tickets auto-disables.
7. In Discord, run a ticket command on Free plan → plan limit embed.
8. Switch to **Premium**, enable all modules, confirm bot features work.
