# Step 18 — Platform Admin MVP

Simple admin area for the bot owner to manage customers, guilds, subscriptions, and platform health.

## Database

**`PlatformAdmins`**

- `Id`
- `DiscordUserId` (unique)
- `CreatedAt`

Seeded on API startup from config:

```json
"Admin": {
  "DiscordUserId": "YOUR_DISCORD_USER_ID"
}
```

## Admin authorization

Every admin API route requires:

1. Valid JWT (`Authorization: Bearer …`) from Discord login
2. The user's Discord ID must exist in **`PlatformAdmins`**

Guild owners who are not platform admins get **403 Forbidden** on `/api/admin/*`.

The dashboard uses `GET /api/auth/me` → `isAdmin: true` to show admin navigation and allow `/admin` routes.

## API

All routes under `/api/admin` (JWT + platform admin):

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/stats` | Platform health stats |
| GET | `/api/admin/guilds` | All guilds |
| GET | `/api/admin/guilds/{id}` | Guild detail |
| PUT | `/api/admin/guilds/{id}/subscription` | Change plan (body: `{ "planKey": "pro" }`) |
| GET | `/api/admin/users` | Dashboard users |

**Stats response**

- `totalGuilds`, `activeGuilds`
- `totalUsers`
- `totalTickets`, `openTickets`
- `planCounts` — guild count per plan
- `moduleUsageCounts` — enabled module count per module

## Dashboard

Routes (admin only):

- `/admin` — stats cards + plan/module breakdown
- `/admin/guilds` — all guilds table, change subscription plan
- `/admin/users` — dashboard users table

**Guilds table:** name, owner Discord ID, plan (dropdown), modules count, tickets count, last sync, active status.

**Users table:** username, Discord user ID, last login, created date.

Admin nav appears in the sidebar only when `isAdmin` is true.

## Seed yourself as admin

1. Log into the dashboard with Discord.
2. Call `GET /api/auth/me` (or check browser devtools after login) and copy `discordUserId`.
3. Set it in `appsettings.Development.json` (or `.local.json`):

```json
"Admin": {
  "DiscordUserId": "123456789012345678"
}
```

4. Apply migration and restart the API:

```bash
dotnet ef database update --project src/DiscordBot.Infrastructure --startup-project src/DiscordBot.Api
dotnet run --project src/DiscordBot.Api --launch-profile http
```

On startup, `PlatformAdminSeeder` inserts your Discord ID into `PlatformAdmins` if it is not already there.

5. Log out and log back in (or refresh) so `/api/auth/me` returns `isAdmin: true`.

## Test admin dashboard

1. Confirm a non-admin account cannot open `/admin` (redirects to `/servers`).
2. Confirm non-admin calls to `/api/admin/stats` return 403.
3. As admin, open **Platform Admin → Overview** — stats cards load.
4. Open **All Guilds** — table lists servers; change a plan from the dropdown.
5. Open **Users** — lists people who logged into the dashboard.
6. Verify the guild owner still manages their own server at `/guilds/:id/*` but cannot access admin routes.

## Not included

Payments, invoices, email, complex RBAC, platform support tickets, multi-admin roles.
