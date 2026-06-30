# Step 5 — Guild REST API

Version 1 guild endpoints with JWT protection and simple owner-based access control.

---

## Auth improvement (before Step 5)

JWT is **no longer returned in the URL**.

| Step | What happens |
|------|----------------|
| 1 | Dashboard → `GET /api/auth/discord/login` |
| 2 | User approves on Discord → API callback |
| 3 | API redirects to `http://localhost:4200/auth/callback?code=ONE_TIME_CODE` |
| 4 | Dashboard → `POST /api/auth/token` with `{ "code": "..." }` |
| 5 | API returns `{ "accessToken": "..." }` in JSON body |

The one-time code expires in **2 minutes** and can only be used once (stored in memory cache).

---

## Endpoints

All guild routes require `Authorization: Bearer {jwt}`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/guilds` | List guilds where `OwnerDiscordUserId` matches your `discord_id` claim |
| GET | `/api/guilds/{id}/settings` | Read guild settings |
| PUT | `/api/guilds/{id}/settings` | Update guild settings |

Public auth routes (unchanged + new):

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/token` | Exchange one-time code for JWT |

---

## Authorization rule (v1)

Simple owner check — no RBAC, no Discord guild sync:

```
JWT claim "discord_id"  ==  Guild.OwnerDiscordUserId
```

If the guild does not exist or you are not the owner → **404** (same message, no ID leaking).

---

## Request / response examples

### GET /api/guilds

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "discordGuildId": "123456789012345678",
    "name": "My Test Server",
    "iconUrl": null,
    "isActive": true
  }
]
```

### GET /api/guilds/{id}/settings

```json
{
  "guildId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "welcomeEnabled": true,
  "welcomeChannelId": null,
  "welcomeMessage": "Welcome {user} to {server}!",
  "autoRoleEnabled": false,
  "autoRoleId": null,
  "logsEnabled": true,
  "logChannelId": null
}
```

### PUT /api/guilds/{id}/settings

```json
{
  "welcomeEnabled": true,
  "welcomeChannelId": "987654321098765432",
  "welcomeMessage": "Hey {user}, welcome to {server}!",
  "autoRoleEnabled": true,
  "autoRoleId": "111222333444555666",
  "logsEnabled": true,
  "logChannelId": "987654321098765432"
}
```

Returns the updated settings object (same shape as GET).

---

## Test data — option A: automatic seed (Development)

In `appsettings.Development.json`:

```json
"Seed": {
  "Enabled": true,
  "OwnerDiscordUserId": "YOUR_DISCORD_USER_ID",
  "DiscordGuildId": "123456789012345678",
  "GuildName": "My Test Server"
}
```

1. Log in via Discord OAuth
2. Call `GET /api/auth/me` and copy `discordUserId`
3. Paste it into `Seed:OwnerDiscordUserId`
4. Restart the API — seeder creates one guild + default settings if none exists

---

## Test data — option B: manual SQL

Run `database/seeds/seed-test-guild.sql` after replacing `YOUR_DISCORD_USER_ID`.

---

## File reference

| File | Why it exists |
|------|----------------|
| `Auth/AuthCodeService.cs` | One-time code ↔ JWT exchange (avoids token in URL) |
| `Models/GuildDtos.cs` | Request/response shapes for guild API |
| `Services/GuildService.cs` | Queries guilds and settings with owner filter |
| `Controllers/GuildsController.cs` | HTTP layer; reads `discord_id` from JWT |
| `Extensions/ClaimsPrincipalExtensions.cs` | Helper to read `discord_id` claim |
| `Options/SeedOptions.cs` | Dev seed configuration |
| `Data/DevelopmentDataSeeder.cs` | Creates test guild on startup in Development |
| `database/seeds/seed-test-guild.sql` | Manual SQL alternative |

---

## Manual testing

### 1. Login and get JWT

```bash
# Get login URL
curl http://localhost:5217/api/auth/discord/login

# After browser OAuth, copy ?code= from redirect URL, then:
curl -X POST http://localhost:5217/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"code":"YOUR_ONE_TIME_CODE"}'
```

Save `accessToken` from the response.

### 2. Get your Discord user id

```bash
curl http://localhost:5217/api/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Set `Seed:OwnerDiscordUserId` to `discordUserId`, restart API (or run SQL seed).

### 3. List guilds

```bash
curl http://localhost:5217/api/guilds \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 4. Update settings

```bash
curl -X PUT "http://localhost:5217/api/guilds/GUILD_GUID/settings" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "welcomeEnabled": true,
    "welcomeChannelId": "987654321098765432",
    "welcomeMessage": "Welcome {user}!",
    "autoRoleEnabled": false,
    "autoRoleId": null,
    "logsEnabled": true,
    "logChannelId": null
  }'
```

### 5. Verify protection

```bash
curl http://localhost:5217/api/guilds
# Expected: 401 Unauthorized
```

---

## What is intentionally NOT in Step 5

- Discord guild list sync (`guilds` OAuth scope)
- Role-based permissions
- Subscriptions
- Angular dashboard UI
- Bot integration

---

## Next step (Step 6)

Discord.Net bot — read guild settings from API, welcome messages, slash commands.

**Step 5 is complete. Waiting for your approval before continuing.**
