# Configuration Runbook

Permanent reference for how configuration works in this project after **Step 27 — Clean Development and Production Configuration**.

Use this file when setting up a new machine, deploying to production, or debugging config issues.

Related docs: [`step-27-configuration.md`](step-27-configuration.md), [`README.md`](../README.md), [`.env.example`](../.env.example), [`deploy/railway/railway.env.example`](../deploy/railway/railway.env.example).

---

## 1. What changed

Step 27 cleaned the configuration system so **Development runs locally from gitignored files** and **Production runs from environment variables only**. No secrets belong in GitHub.

### Summary of goals

| Goal | How it was achieved |
|------|---------------------|
| Safe commits | Committed JSON/TS files use placeholders only |
| Local secrets | `appsettings.Development.local.json` (gitignored) |
| Production secrets | Railway/Vercel environment variables |
| Startup safety | Strict validation in Production; warnings in Development |
| Clear templates | `*.example.json` and `*.example.ts` files to copy from |

### Files changed (Step 27)

| File | Change | Why |
|------|--------|-----|
| `src/DiscordBot.Api/appsettings.json` | Replaced real-looking defaults with placeholders | Safe to commit; forces local/prod config |
| `src/DiscordBot.Api/Program.cs` | Loads `appsettings.{Environment}.local.json`; fixed middleware formatting | Local override support |
| `src/DiscordBot.Api/Extensions/ConfigurationValidationExtensions.cs` | Added `Admin`, `Jwt:Issuer/Audience`, `REPLACE_WITH`, localhost/HTTPS checks | Production rejects bad config |
| `src/DiscordBot.Api/appsettings.Development.example.json` | Added `_comment`, clarified copy instructions | Template for local file |
| `src/DiscordBot.Api/appsettings.example.json` | Updated comment for Railway env vars | Production reference |
| `src/DiscordBot.Bot/appsettings.json` | Placeholders only (HTTPS template URLs) | Safe to commit |
| `src/DiscordBot.Bot/Program.cs` | Loads `appsettings.{Environment}.local.json` | Local override support |
| `src/DiscordBot.Bot/Extensions/ConfigurationValidationExtensions.cs` | Added `REPLACE_WITH`, localhost/HTTPS checks | Production rejects bad config |
| `src/DiscordBot.Bot/appsettings.Development.example.json` | Added `_comment`, copy instructions | Template for local file |
| `src/DiscordBot.Bot/appsettings.example.json` | Updated comment for Railway env vars | Production reference |
| `dashboard/.../environment.ts` | Placeholder production URL (removed real Railway URL) | Safe to commit |
| `dashboard/.../environment.production.ts` | **Added** — used by `npm run build` | Production API URL |
| `dashboard/.../environment.production.example.ts` | Updated template | Copy reference |
| `dashboard/.../environment.local.example.ts` | **Added** — optional local override template | Optional dashboard override |
| `dashboard/DiscordBot.Dashboard/angular.json` | Production build replaces `environment.ts` with `environment.production.ts` | Correct prod build |
| `.gitignore` | Added `*.local.json` pattern | Catch all local JSON overrides |
| `README.md` | Rewrote config sections | Quick reference |
| `docs/step-27-configuration.md` | **Added** — technical config guide | Step 27 detail |

### Files removed

None. No configuration files were deleted; secrets were **removed from committed files** and moved to gitignored local files or env vars.

### API configuration files

| File | Committed? | Purpose |
|------|------------|---------|
| `appsettings.json` | **Yes — safe** | Base placeholders for all environments. Not enough to run alone. |
| `appsettings.Development.json` | **Yes — safe** | Dev-only: logging levels, seed flags, default Docker PostgreSQL connection string (no Discord/JWT secrets). |
| `appsettings.Development.local.json` | **No — never commit** | Your real local secrets. Create by copying the example file. |
| `appsettings.example.json` | **Yes — safe** | Production/Railway template reference. Do not copy to git; use env vars instead. |
| `appsettings.Development.example.json` | **Yes — safe** | Copy this → `appsettings.Development.local.json` and fill in values. |

**Why `appsettings.Development.json` still has a connection string:** Docker Compose defaults are not secrets. Your `.local.json` overrides it if needed.

### Bot configuration files

| File | Committed? | Purpose |
|------|------------|---------|
| `appsettings.json` | **Yes — safe** | Base placeholders. Not enough to run alone. |
| `appsettings.Development.json` | **Yes — safe** | Dev-only: debug logging. No tokens. |
| `appsettings.Development.local.json` | **No — never commit** | Your real bot token, API URL, API key. |
| `appsettings.example.json` | **Yes — safe** | Production/Railway template reference. |
| `appsettings.Development.example.json` | **Yes — safe** | Copy this → `appsettings.Development.local.json`. |

### Dashboard environment files

| File | Committed? | Purpose |
|------|------------|---------|
| `environment.ts` | **Yes — safe** | Default file; placeholder prod URL. Overridden at build time. |
| `environment.development.ts` | **Yes — safe** | Used by `npm start` → `apiUrl: http://localhost:5217` |
| `environment.production.ts` | **Yes — safe** | Used by `npm run build` → set your Railway API URL here before Vercel deploy |
| `environment.production.example.ts` | **Yes — safe** | Copy/reference template |
| `environment.local.example.ts` | **Yes — safe** | Optional; copy to `environment.local.ts` (gitignored) |
| `environment.local.ts` | **No — never commit** | Optional local override |

### Also gitignored (never commit)

- `.env`, `.env.local`, etc. (except `.env.example`)
- `*.local.json`
- `appsettings.Production.json`
- `appsettings.Production.local.json`
- `appsettings.local.json`
- `appsettings.Secrets.json`
- `secrets.json`

---

## 2. Configuration load order

Both **DiscordBot.Api** and **DiscordBot.Bot** use the same pattern.

### Load order (first → last)

| Order | Source | Committed? | Typical use |
|-------|--------|------------|-------------|
| 1 | `appsettings.json` | Yes | Placeholder base |
| 2 | `appsettings.{Environment}.json` | Yes | Dev logging/seed (API) |
| 3 | User secrets | Dev only | Optional; rarely used in this project |
| 4 | **Environment variables** | No | **Production (Railway)** |
| 5 | Command-line arguments | No | Rare |
| 6 | `appsettings.{Environment}.local.json` | **No** | **Local development secrets** |

`Program.cs` explicitly adds step 6 after `CreateBuilder` / `CreateApplicationBuilder` defaults.

### Which value wins?

**The last source in the list wins** for any given key.

| Scenario | Winner |
|----------|--------|
| **Local development** (you created `.local.json`) | `appsettings.Development.local.json` overrides everything above it, including env vars |
| **Production on Railway** (no `.local.json` file) | **Environment variables** override committed JSON |
| Key only in `appsettings.json` | Uses placeholder (Production validation fails; Development warns) |
| Key in `Development.json` + `.local.json` | `.local.json` wins |

### .NET env var naming

Double underscore maps to nested JSON:

| JSON path | Environment variable |
|-----------|---------------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Discord:ClientSecret` | `Discord__ClientSecret` |
| `Jwt:Secret` | `Jwt__Secret` |
| `Bot:ApiKey` | `Bot__ApiKey` |
| `Admin:DiscordUserId` | `Admin__DiscordUserId` |
| `Api:BaseUrl` (Bot) | `Api__BaseUrl` |
| `Platform:DashboardUrl` (Bot) | `Platform__DashboardUrl` |

### Production validation

When `ASPNETCORE_ENVIRONMENT=Production`, startup **throws** if required values are:

- Missing or empty
- Placeholders containing: `YOUR_`, `CHANGE_ME`, `REPLACE_WITH`, `your-domain.com`
- Public URLs using `http://` (must be HTTPS)
- Public URLs containing `localhost` or `127.0.0.1`

In **Development**, the same issues produce **warnings** only — the app starts, but OAuth/bot features fail with clear errors until you fix config.

---

## 3. Development setup

### Step 1 — Create API local config

```bash
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json
```

Edit `src/DiscordBot.Api/appsettings.Development.local.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres"
  },
  "Discord": {
    "ClientId": "YOUR_DISCORD_CLIENT_ID",
    "ClientSecret": "YOUR_DISCORD_CLIENT_SECRET",
    "BotToken": "YOUR_DISCORD_BOT_TOKEN",
    "RedirectUri": "http://localhost:5217/api/auth/discord/callback",
    "DashboardUrl": "http://localhost:4200"
  },
  "Bot": {
    "ApiKey": "dev-bot-api-key-change-me"
  },
  "Jwt": {
    "Secret": "dev-only-change-me-use-32-chars-minimum!!",
    "Issuer": "DiscordBot",
    "Audience": "DiscordBot.Dashboard",
    "ExpiresMinutes": 60
  },
  "Admin": {
    "DiscordUserId": "YOUR_DISCORD_USER_ID"
  }
}
```

Replace `YOUR_*` values with real Discord credentials and your Discord user ID.

### Step 2 — Create Bot local config

```bash
cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json
```

Edit `src/DiscordBot.Bot/appsettings.Development.local.json`:

```json
{
  "Discord": {
    "Token": "YOUR_DISCORD_BOT_TOKEN"
  },
  "Api": {
    "BaseUrl": "http://localhost:5217",
    "ApiKey": "dev-bot-api-key-change-me"
  },
  "Platform": {
    "DashboardUrl": "http://localhost:4200"
  }
}
```

**Important:** `Api:ApiKey` must exactly match `Bot:ApiKey` in the API local config.

### Step 3 — Discord Developer Portal (local redirect)

In [Discord Developer Portal](https://discord.com/developers/applications) → your app → **OAuth2** → **Redirects**, add:

```
http://localhost:5217/api/auth/discord/callback
```

Also enable **Server Members Intent** under Bot settings if you use welcome messages.

### Step 4 — Start local database

```bash
docker compose up -d
```

### Step 5 — Run migrations

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

### Step 6 — Run API

```bash
dotnet run --project src/DiscordBot.Api --launch-profile http
```

API listens on **http://localhost:5217**.

### Step 7 — Run Bot

```bash
dotnet run --project src/DiscordBot.Bot
```

### Step 8 — Run Dashboard

```bash
cd dashboard/DiscordBot.Dashboard
npm install
npm start
```

`npm start` uses `environment.development.ts` → API at `http://localhost:5217`.

### Step 9 — Open the dashboard

```
http://localhost:4200
```

Click **Login with Discord**, invite the bot, run `/setup` in your server.

---

## 4. Production setup

**Stack:** PostgreSQL + API + Bot on **Railway**; Dashboard on **Vercel**.

Do **not** commit `appsettings.Production.json`. Set all secrets as Railway/Vercel environment variables.

### Railway — PostgreSQL

Create a PostgreSQL service. Railway provides connection variables. Use them in the API service.

Example connection string format:

```
Host=YOUR_HOST;Port=5432;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Run migrations against Railway **once** (from your machine or a deploy hook):

```bash
ConnectionStrings__DefaultConnection="Host=...;..." \
  dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

See `deploy/railway/migrate.sh` and `docs/step-23-railway-deployment.md`.

### Railway — API service variables

| Variable | Example / notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Railway PostgreSQL connection string |
| `Discord__ClientId` | Discord application client ID |
| `Discord__ClientSecret` | Discord application client secret |
| `Discord__BotToken` | Bot token |
| `Discord__RedirectUri` | `https://YOUR_API_DOMAIN/api/auth/discord/callback` |
| `Discord__DashboardUrl` | `https://YOUR_VERCEL_DOMAIN` |
| `Jwt__Secret` | Random string, **minimum 32 characters** |
| `Jwt__Issuer` | `DiscordBot` |
| `Jwt__Audience` | `DiscordBot.Dashboard` |
| `Bot__ApiKey` | Strong random secret (shared with Bot worker) |
| `Admin__DiscordUserId` | Your Discord user ID for platform admin |

Optional: Railway sets `PORT` automatically; the API binds to it.

### Railway — Bot worker variables

| Variable | Example / notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Discord__Token` | Same bot token as API `Discord__BotToken` |
| `Api__BaseUrl` | `https://YOUR_API_DOMAIN` (no trailing slash) |
| `Api__ApiKey` | **Must match** API `Bot__ApiKey` |
| `Platform__DashboardUrl` | `https://YOUR_VERCEL_DOMAIN` |

### Vercel — Dashboard

**How `apiUrl` is configured:**

1. Edit `dashboard/DiscordBot.Dashboard/src/environments/environment.production.ts`
2. Set `apiUrl` to your Railway API URL (HTTPS, no trailing slash):

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_API_DOMAIN'
};
```

3. Commit the **domain only** (not secrets) or set via your deploy pipeline
4. Deploy to Vercel; `npm run build` uses `environment.production.ts` via `angular.json` file replacements

**Do not** leave `apiUrl` as `http://localhost:5217` in production builds.

### Discord Developer Portal (production)

Add redirect URL:

```
https://YOUR_API_DOMAIN/api/auth/discord/callback
```

Remove or keep localhost redirect depending on whether you still test locally with the same Discord app.

---

## 5. Development vs Production table

| Setting | Development value | Production value | Where to configure |
|---------|-------------------|------------------|-------------------|
| **Environment** | `Development` (default locally) | `Production` | `ASPNETCORE_ENVIRONMENT` on Railway |
| **Database** | `localhost:5432` (Docker) | Railway PostgreSQL | API `.local.json` / `ConnectionStrings__DefaultConnection` |
| **API URL** | `http://localhost:5217` | `https://YOUR_API_DOMAIN` | Bot `Api:BaseUrl` / Dashboard `apiUrl` |
| **Dashboard URL** | `http://localhost:4200` | `https://YOUR_VERCEL_DOMAIN` | API `Discord:DashboardUrl`, Bot `Platform:DashboardUrl` |
| **Discord Redirect URI** | `http://localhost:5217/api/auth/discord/callback` | `https://YOUR_API_DOMAIN/api/auth/discord/callback` | API config + Discord Portal |
| **Discord Client ID** | Your app ID | Same app ID | API `.local.json` / `Discord__ClientId` |
| **Discord Client Secret** | Your secret | Same secret | API `.local.json` / `Discord__ClientSecret` |
| **Discord Bot Token** | Your token | Same token | API `Discord:BotToken`, Bot `Discord:Token` |
| **JWT Secret** | Dev string (32+ chars) | Strong random secret | API `.local.json` / `Jwt__Secret` |
| **Bot ↔ API key** | e.g. `dev-bot-api-key-change-me` | Strong random secret | API `Bot:ApiKey`, Bot `Api:ApiKey` |
| **Platform Admin** | Your Discord user ID | Same | API `Admin:DiscordUserId` |
| **HTTPS required** | No (localhost OK) | Yes | Production validation enforces |
| **Config source** | `appsettings.Development.local.json` | Environment variables | — |
| **Dashboard build** | `npm start` → `environment.development.ts` | `npm run build` → `environment.production.ts` | Angular `angular.json` |

---

## 6. Secrets safety

### Rules

- **Never commit** real Discord tokens, client secrets, JWT secrets, API keys, or database passwords.
- **Never commit** `appsettings.Development.local.json`, `appsettings.Production.json`, or `.env`.
- **Never commit** `environment.local.ts` with production secrets (it should only override `apiUrl` locally if needed).
- Committed files should only contain `YOUR_*`, `CHANGE_ME`, or `your-domain.com` placeholders.

### If secrets were exposed (git history, screenshot, chat)

Rotate immediately:

| Secret | Where to rotate |
|--------|-----------------|
| Discord Bot Token | Discord Developer Portal → Bot → Reset Token |
| Discord Client Secret | Discord Developer Portal → OAuth2 → Reset Secret |
| PostgreSQL password | Railway PostgreSQL → reset password → update `ConnectionStrings__DefaultConnection` |
| JWT secret | Generate new 32+ char string → update `Jwt__Secret` (invalidates existing login sessions) |
| Bot API key | Generate new key → update both `Bot__ApiKey` (API) and `Api__ApiKey` (Bot) |

### Verify files are ignored

```bash
git check-ignore -v src/DiscordBot.Api/appsettings.Development.local.json
git check-ignore -v src/DiscordBot.Bot/appsettings.Development.local.json
git check-ignore -v dashboard/DiscordBot.Dashboard/src/environments/environment.local.ts
```

### Verify nothing secret is staged

```bash
git status
git diff
```

### Untrack a file that was accidentally committed (keeps file on disk)

```bash
git rm --cached src/DiscordBot.Api/appsettings.Development.local.json
git rm --cached .env
```

Then commit the removal and rotate any exposed secrets.

### Pre-push scan

```bash
grep -rE "ClientSecret|BotToken|\.Gg[A-Za-z0-9_-]{20,}" src --include="*.json" | grep -v example | grep -v YOUR_
```

---

## 7. Troubleshooting

### Discord OAuth redirect mismatch

**Symptom:** After login, Discord shows "Invalid OAuth2 redirect_uri".

**Fix:**
- `Discord:RedirectUri` in API config must **exactly** match a URL in Discord Portal → OAuth2 → Redirects
- No trailing slash mismatch; check `http` vs `https`
- Local: `http://localhost:5217/api/auth/discord/callback`

### CORS error in browser

**Symptom:** Dashboard cannot call API; browser console shows CORS blocked.

**Fix:**
- API `Discord:DashboardUrl` must exactly match the dashboard origin
- Local: `http://localhost:4200`
- Production: `https://YOUR_VERCEL_DOMAIN` (no trailing slash)
- Restart API after changing

### API says "DashboardUrl must use HTTPS" (Production)

**Symptom:** API fails to start on Railway.

**Fix:**
- Set `Discord__DashboardUrl=https://YOUR_VERCEL_DOMAIN`
- Must not be `http://` or contain `localhost`

### Bot says Discord token is placeholder

**Symptom:** Bot logs warning or fails: `Discord:Token is still a placeholder value`.

**Fix (local):** Create/fill `appsettings.Development.local.json` with real token.

**Fix (production):** Set `Discord__Token` on Railway Bot service.

### Bot API key mismatch

**Symptom:** Bot logs "Failed to register guild with API" or API returns 401.

**Fix:**
- API `Bot:ApiKey` (or `Bot__ApiKey`) must **exactly equal** Bot `Api:ApiKey` (or `Api__ApiKey`)
- No extra spaces; redeploy both services after changing

### PostgreSQL connection string format error

**Symptom:** API fails at startup: cannot connect to database.

**Fix (local):**
```
Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres
```

**Fix (Railway):** Include SSL:
```
Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

### `relation "..." does not exist`

**Symptom:** API errors referencing missing tables.

**Fix:** Migrations not applied to the database you're connected to:

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

Ensure `ConnectionStrings__DefaultConnection` points to the **correct** database (local vs Railway).

### Migrations applied to local DB instead of Railway

**Symptom:** Local works; production API fails with missing tables.

**Fix:** Run migrations with Railway connection string:

```bash
ConnectionStrings__DefaultConnection="YOUR_RAILWAY_CONNECTION_STRING" \
  dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

Or use `deploy/railway/migrate.sh`.

### Vercel dashboard calls localhost API

**Symptom:** Production dashboard tries `http://localhost:5217`.

**Fix:**
- Edit `environment.production.ts` → set `apiUrl` to Railway API HTTPS URL
- Rebuild and redeploy: `npm run build`
- Confirm Vercel build uses production configuration (not development)

### Arabic / English not loading

**Symptom:** UI shows keys like `common.appName` instead of translated text.

**Fix:**
- Hard refresh (Ctrl+Shift+R)
- Check browser network tab: `assets/i18n/en.json` and `assets/i18n/ar.json` return 200
- Run dashboard from project root: `cd dashboard/DiscordBot.Dashboard && npm start`
- Vercel: ensure SPA routing (`vercel.json`) serves `index.html` for all routes

### API configuration warnings on startup (Development)

**Symptom:** Log shows "API configuration issues detected" but app runs.

**Fix:** Create `appsettings.Development.local.json` from the example and fill all required values.

---

## 8. Final checklists

### Local development checklist

- [ ] `appsettings.Development.local.json` exists for **API** with real Discord/JWT values
- [ ] `appsettings.Development.local.json` exists for **Bot** with matching API key
- [ ] Discord redirect `http://localhost:5217/api/auth/discord/callback` registered
- [ ] `docker compose up -d` — PostgreSQL healthy
- [ ] Migrations applied (`dotnet ef database update`)
- [ ] **API starts** on http://localhost:5217 (no critical config errors)
- [ ] **Bot starts** and logs in to Discord
- [ ] **Dashboard starts** on http://localhost:4200
- [ ] **Login works** — Discord OAuth completes
- [ ] **`/setup` works** in Discord — server appears in dashboard
- [ ] **Settings save** — change a setting, refresh, value persists

### Production checklist

- [ ] Railway PostgreSQL provisioned; connection string set on API
- [ ] All API Railway variables set (see Section 4)
- [ ] All Bot Railway variables set; `Api__ApiKey` matches `Bot__ApiKey`
- [ ] `ASPNETCORE_ENVIRONMENT=Production` on API and Bot
- [ ] Migrations applied to **Railway** database
- [ ] **`/api/health`** returns healthy (database connected)
- [ ] **Bot logs** show `Logged in as YourBot#1234`
- [ ] **`environment.production.ts`** has correct HTTPS `apiUrl`
- [ ] Vercel dashboard deployed from production build
- [ ] Discord production redirect URL registered (HTTPS)
- [ ] **Dashboard login works**
- [ ] **OAuth redirect works** end-to-end
- [ ] **CORS works** — no browser CORS errors
- [ ] **Bot can register guild** — `/setup` in Discord succeeds
- [ ] **Settings save** from production dashboard
- [ ] **Tickets work** — open/close flow if module enabled

---

## 9. Quick reference

### What you create manually (one-time per machine)

```bash
cp src/DiscordBot.Api/appsettings.Development.example.json \
   src/DiscordBot.Api/appsettings.Development.local.json

cp src/DiscordBot.Bot/appsettings.Development.example.json \
   src/DiscordBot.Bot/appsettings.Development.local.json
```

Then fill in Discord credentials, JWT secret, and admin user ID.

### Railway — set these variables

**API:** `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection`, `Discord__ClientId`, `Discord__ClientSecret`, `Discord__BotToken`, `Discord__RedirectUri`, `Discord__DashboardUrl`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Bot__ApiKey`, `Admin__DiscordUserId`

**Bot:** `ASPNETCORE_ENVIRONMENT`, `Discord__Token`, `Api__BaseUrl`, `Api__ApiKey`, `Platform__DashboardUrl`

### Vercel — set this

Edit `dashboard/DiscordBot.Dashboard/src/environments/environment.production.ts`:

```typescript
apiUrl: 'https://YOUR_RAILWAY_API_DOMAIN'
```

Redeploy after changing.

### Discord Developer Portal — set these

| Environment | Redirect URL |
|-------------|--------------|
| Local | `http://localhost:5217/api/auth/discord/callback` |
| Production | `https://YOUR_API_DOMAIN/api/auth/discord/callback` |

### Exact local run commands

```bash
docker compose up -d

dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet run --project src/DiscordBot.Api --launch-profile http

dotnet run --project src/DiscordBot.Bot

cd dashboard/DiscordBot.Dashboard && npm start
```

Open **http://localhost:4200**.
