# Environments

## Environment names

| Name | ASP.NET `ASPNETCORE_ENVIRONMENT` | Purpose |
|------|-----------------------------------|---------|
| Development | `Development` | Local dev |
| Production | `Production` | Railway/cloud |

Angular uses `environment.ts` (dev) and `environment.production.ts` (prod build).

## Configuration load order (.NET)

```
appsettings.json
  → appsettings.{Environment}.json
  → appsettings.{Environment}.local.json  (optional, gitignored)
  → environment variables               (highest priority)
```

**File:** `src/DiscordBot.Api/Program.cs` re-applies env vars after local JSON so CLI overrides work.

Bot follows same pattern in its `Program.cs`.

## Local environment

### Ports

| Service | URL |
|---------|-----|
| API | http://localhost:5217 |
| Dashboard | http://localhost:4200 |
| PostgreSQL | localhost:5432 |

### Required local settings

| Setting | Example |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | `Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres` |
| `Discord:ClientId`, `ClientSecret` | From Discord Developer Portal |
| `Discord:RedirectUri` | `http://localhost:5217/api/auth/discord/callback` |
| `Discord:DashboardUrl` | `http://localhost:4200` |
| `Jwt:Secret` | Min 32 characters |
| `Bot:ApiKey` / `Api:ApiKey` | Must match (e.g. `dev-bot-api-key-change-me`) |
| `Admin:DiscordUserId` | Your Discord user ID |
| `Discord:Token` (Bot) | Bot token |
| `Api:BaseUrl` (Bot) | `http://localhost:5217` |

### Discord Developer Portal (local)

Register redirect URI:

```
http://localhost:5217/api/auth/discord/callback
```

Enable **Server Members Intent** for welcome messages and member sync.

### Config files

| File | Committed |
|------|-----------|
| `appsettings.Development.example.json` | Yes — copy to `.local.json` |
| `appsettings.Development.local.json` | **No** |
| `environment.development.ts` | Yes |

## Production environment

### Railway services

Typically three services + PostgreSQL plugin:

1. **API** — public HTTPS
2. **Bot** — private worker
3. **Dashboard** — optional on Railway OR Vercel

### Environment variables (API)

Use double-underscore nesting:

```
ConnectionStrings__DefaultConnection
Discord__ClientId
Discord__ClientSecret
Discord__RedirectUri
Discord__DashboardUrl
Discord__AllowVercelOrigins
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Bot__ApiKey
Admin__DiscordUserId
ASPNETCORE_ENVIRONMENT=Production
```

**Template:** `deploy/railway/railway.env.example`, `.env.example`

### Environment variables (Bot)

```
Discord__Token
Api__BaseUrl
Api__ApiKey
Platform__DashboardUrl
```

### Dashboard production

Edit `environment.production.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api.railway.app'
};
```

Or use build-time replacement on Vercel.

### Production validation

API runs `ValidateRequiredConfiguration()` at startup — **fails fast** on missing secrets in Production.

Development logs warnings for placeholder values.

## CORS

**Policy:** `Dashboard` — configured in `AuthenticationExtensions.cs`

Allowed origin: `Discord:DashboardUrl` + optional `*.vercel.app` when `AllowVercelOrigins=true`.

## Secrets management

| Rule | Detail |
|------|--------|
| Never commit | Tokens, client secrets, JWT secret, API keys |
| Gitignored | `*.local.json`, `.env`, `environment.local.ts` |
| Rotate if leaked | Bot token, client secret, JWT secret, API key |

Pre-push check in root README:

```bash
grep -rE "ClientSecret|BotToken" src --include="*.json" | grep -v example
```

## Assumption

No separate **staging** environment exists today. Recommended: add staging Railway project mirroring production before commercial launch.

## Related docs

- `deployment.md`, `security.md`
- `docs/step-27-configuration.md`, `docs/configuration-runbook.md`
