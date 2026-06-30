# Step 7 — Angular Dashboard MVP

Simple dashboard to log in with Discord and edit guild settings.

---

## Folder structure

```
src/app/
├── core/
│   ├── models/           ← TypeScript interfaces matching API JSON
│   ├── services/         ← AuthService, GuildService
│   ├── interceptors/     ← Attaches JWT to HTTP requests
│   └── guards/           ← Blocks unauthenticated routes
├── features/
│   ├── auth/
│   │   ├── login/        ← Discord login button
│   │   └── callback/     ← Exchanges one-time code for JWT
│   ├── servers/          ← Lists guilds from API
│   └── settings/         ← Edit welcome / auto-role / logs
├── app.module.ts
└── app-routing.module.ts
```

---

## Routes

| Path | Guard | Page |
|------|-------|------|
| `/login` | — | Login with Discord |
| `/auth/callback` | — | OAuth return (exchange code) |
| `/servers` | AuthGuard | Your servers list |
| `/guilds/:id/settings` | AuthGuard | Settings form |

---

## Login flow

1. User clicks **Login with Discord** → `GET /api/auth/discord/login`
2. Browser redirects to Discord OAuth
3. After approval, API redirects to `/auth/callback?code=ONE_TIME_CODE`
4. Callback page → `POST /api/auth/token` with `{ code }`
5. JWT saved to `localStorage` → redirect to `/servers`

---

## JWT storage

- Key: `discord_bot_jwt` in `localStorage`
- `AuthInterceptor` adds `Authorization: Bearer {token}` to every HTTP call
- Logout clears `localStorage`

---

## Settings flow

1. `/servers` → click a server → `/guilds/{id}/settings`
2. `GET /api/guilds/{id}/settings` loads form
3. User edits → **Save** → `PUT /api/guilds/{id}/settings`
4. Bot reads same settings on next member join

---

## Run locally

### 1. Start API (port 5217)

```bash
dotnet run --project src/DiscordBot.Api --launch-profile http
```

Ensure `Discord:DashboardUrl` is `http://localhost:4200` in API config.

### 2. Start dashboard

```bash
cd dashboard/DiscordBot.Dashboard
npm start
```

Open http://localhost:4200

### 3. Prerequisites

- Discord OAuth credentials configured in API
- SQL Server running with migrations applied
- Your Discord user is **owner** of at least one registered guild (via bot `/setup` or seed data)
- `Seed:OwnerDiscordUserId` or bot registration sets `OwnerDiscordUserId` to your Discord id

### 4. Browser test checklist

- [ ] `/login` shows Discord button
- [ ] OAuth completes → lands on `/servers`
- [ ] Server list shows your guild(s)
- [ ] Click server → settings form loads
- [ ] Change welcome settings → Save → success message
- [ ] Join Discord server with alt account → welcome message appears (bot running)

---

## Configuration

`src/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5217'
};
```

CORS on the API must allow `http://localhost:4200` (configured via `Discord:DashboardUrl`).

---

## Not included (v1)

- Dark mode, subscriptions, tickets, moderation UI, RBAC, NgRx

---

## Next steps (future)

- Move JWT to httpOnly cookie
- Server picker if user has many guilds
- Channel/role pickers instead of raw snowflake IDs

**Step 7 is complete.**
