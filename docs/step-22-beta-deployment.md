# Step 22 — GitHub Push + Beta Deployment

Prepare the repo for GitHub and a beta environment. **Do not commit secrets.**

---

## Security first

If real Discord tokens or JWT secrets were ever committed locally, **rotate them immediately** in the [Discord Developer Portal](https://discord.com/developers/applications):

1. Regenerate **Bot Token**
2. Regenerate **Client Secret**
3. Generate a new **JWT secret** (32+ random characters)
4. Generate a new **Bot API key** and set the same value on API + Bot

---

## GitHub readiness

| Item | Location |
|------|----------|
| Gitignore | `.gitignore` |
| Env reference | `.env.example` |
| API config template | `src/DiscordBot.Api/appsettings.example.json` |
| API local dev template | `src/DiscordBot.Api/appsettings.Development.example.json` |
| Bot config template | `src/DiscordBot.Bot/appsettings.example.json` |
| Bot local dev template | `src/DiscordBot.Bot/appsettings.Development.example.json` |
| Dashboard API URL | `dashboard/.../environment.production.example.ts` |
| Setup guide | `README.md` |

**Committed configs use placeholders only.** Real values go in gitignored files:

- `appsettings.Development.local.json`
- `appsettings.Production.json`
- `.env` (optional)

---

## Environment variables

| Variable | Service | Purpose |
|----------|---------|---------|
| `ConnectionStrings__DefaultConnection` | API | PostgreSQL connection |
| `Discord__ClientId` | API | OAuth app ID |
| `Discord__ClientSecret` | API | OAuth secret |
| `Discord__BotToken` | API | Bot token (invite URL generation) |
| `Discord__RedirectUri` | API | OAuth callback (must match Discord Portal) |
| `Discord__DashboardUrl` | API | CORS origin + post-login redirect |
| `Bot__ApiKey` | API | Validates bot HTTP calls |
| `Jwt__Secret` | API | Signs dashboard JWT (32+ chars) |
| `Admin__DiscordUserId` | API | Platform admin Discord user ID |
| `Discord__Token` | Bot | Discord gateway token |
| `Api__BaseUrl` | Bot | API base URL |
| `Api__ApiKey` | Bot | Must match `Bot__ApiKey` |
| `Platform__DashboardUrl` | Bot | Shown in `/setup` embed |
| `apiUrl` in `environment.ts` | Dashboard | API URL baked into Angular build |

See `.env.example` for copy-paste templates.

---

## Build verification

Run from repo root:

```bash
# .NET solution
dotnet build DiscordBot.sln

# Angular dashboard
cd dashboard/DiscordBot.Dashboard && npm run build

# PostgreSQL only (no app Dockerfiles yet)
docker compose config
docker compose up -d
```

---

## Deployment plan

### Architecture (beta)

```
[Users] → HTTPS → Angular static site (dashboard)
                 ↘
[Users] → Discord OAuth → HTTPS → API (.NET)
[Bot worker] ←→ Discord Gateway
[Bot worker] → HTTPS → API (X-Bot-Api-Key)
[API] → PostgreSQL
```

### 1. PostgreSQL

**Option A — Docker on VPS**

```bash
# On server, clone repo and start DB only
docker compose up -d postgres
```

**Option B — Managed Postgres (Railway, Supabase, RDS, etc.)**

Create database `discordbot` and note the connection string.

**Apply migrations (from dev machine or CI with DB access):**

```bash
export ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=discordbot;Username=...;Password=..."
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

### 2. API (.NET 9)

**Publish:**

```bash
dotnet publish src/DiscordBot.Api/DiscordBot.Api.csproj \
  -c Release \
  -o ./publish/api
```

**Configure** via `appsettings.Production.json` (gitignored) or environment variables (see `.env.example`).

**Run behind HTTPS reverse proxy (nginx/Caddy):**

```bash
cd publish/api
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS=http://0.0.0.0:5217 \
dotnet DiscordBot.Api.dll
```

Example public URL: `https://api.your-domain.com`

**Production checklist:**

- [ ] `Discord__RedirectUri` = `https://api.your-domain.com/api/auth/discord/callback`
- [ ] Same URL registered in Discord Developer Portal → OAuth2 → Redirects
- [ ] `Discord__DashboardUrl` = `https://dashboard.your-domain.com` (CORS)
- [ ] Strong `Jwt__Secret` (32+ random chars)
- [ ] Strong `Bot__ApiKey`
- [ ] `Admin__DiscordUserId` set to your Discord user ID

### 3. Bot worker (.NET 9)

**Publish:**

```bash
dotnet publish src/DiscordBot.Bot/DiscordBot.Bot.csproj \
  -c Release \
  -o ./publish/bot
```

**Configure** `appsettings.Production.json` or env vars:

- `Discord__Token`
- `Api__BaseUrl` = `https://api.your-domain.com`
- `Api__ApiKey` = same as API `Bot__ApiKey`
- `Platform__DashboardUrl` = `https://dashboard.your-domain.com`

**Run as systemd service or background process:**

```bash
cd publish/bot
ASPNETCORE_ENVIRONMENT=Production \
dotnet DiscordBot.Bot.dll
```

Bot must stay running 24/7 for slash commands, welcome, tickets, etc.

### 4. Angular dashboard

**Set API URL before build:**

```bash
cp dashboard/DiscordBot.Dashboard/src/environments/environment.production.example.ts \
   dashboard/DiscordBot.Dashboard/src/environments/environment.ts
# Edit environment.ts → apiUrl: 'https://api.your-domain.com'
```

**Build:**

```bash
cd dashboard/DiscordBot.Dashboard
npm ci
npm run build
```

**Deploy** `dist/discord-bot.dashboard/` to static hosting (nginx, Cloudflare Pages, S3+CloudFront, Netlify, etc.).

Example public URL: `https://dashboard.your-domain.com`

---

## Production notes

| Setting | Dev | Beta / Production |
|---------|-----|-------------------|
| API URL | `http://localhost:5217` | `https://api.your-domain.com` |
| Dashboard URL | `http://localhost:4200` | `https://dashboard.your-domain.com` |
| OAuth redirect | `http://localhost:5217/api/auth/discord/callback` | `https://api.your-domain.com/api/auth/discord/callback` |
| CORS | `Discord:DashboardUrl` | Must exactly match dashboard HTTPS origin |
| JWT secret | Dev placeholder in example file | Long random string (rotate if exposed) |
| Bot API key | `dev-bot-api-key-change-me` locally | Strong random string, same on API + Bot |

**Discord Developer Portal:**

1. OAuth2 → Redirects → add production callback URL
2. Bot → enable **Server Members Intent** (welcome)
3. OAuth2 URL Generator → `identify`, `guilds` scopes for dashboard login

---

## GitHub commands

Run from project root (`discord bots/`). Replace `YOUR_GITHUB_USER` and repo name.

```bash
# Initialize (skip if already a git repo)
git init
git branch -M main

# Review what will be committed — ensure no secrets
git status
git diff

# Stage and commit
git add .
git commit -m "Prepare Discord bot platform for beta deployment"

# Add remote and push
git remote add origin https://github.com/YOUR_GITHUB_USER/discord-bot-platform.git
git push -u origin main
```

**Before pushing**, verify:

```bash
# Should return nothing sensitive in tracked source files
grep -r "ClientSecret\|BotToken\|YOUR_DISCORD" src --include="*.json" | grep -v example | grep -v YOUR_
```

---

## Beta test checklist

Use this checklist with real beta testers on the deployed HTTPS URLs.

### Auth & onboarding

- [ ] Open dashboard URL → **Login with Discord** succeeds
- [ ] Redirect returns to dashboard with session
- [ ] **Invite bot** button works
- [ ] Run `/setup` in Discord → server appears in dashboard
- [ ] Onboarding checklist progress updates

### Configuration

- [ ] **Modules** — enable/disable features; plan limits respected
- [ ] **Subscription** — view current plan (dev override if applicable)
- [ ] **Settings → Welcome** — set channel + message; new member receives welcome (bot running, Members Intent on)
- [ ] **Settings → Sync Discord Data** — channels/roles populate dropdowns

### Discord features

- [ ] **Tickets** — `/ticket open` creates ticket; appears in dashboard; close flow works
- [ ] **Reaction roles** — create panel in Discord; appears in dashboard; deactivate works
- [ ] **Moderation** — `/warn`, `/kick`, or `/clear` (as configured); cases appear in dashboard
- [ ] **Logs** — activity appears after above actions

### Dashboard UX

- [ ] **English** — all pages load, no missing translation keys
- [ ] **Arabic (RTL)** — switch language; sidebar, forms, tables layout correctly
- [ ] **Admin** (platform admin user only) — `/admin` stats, guilds, users load

### Infrastructure

- [ ] API health — `GET /swagger` (dev) or a known endpoint responds
- [ ] Bot online in Discord member list
- [ ] PostgreSQL persists data after API restart
- [ ] CORS — dashboard can call API without browser errors
- [ ] OAuth — logout and re-login works

---

## What is not included (yet)

- Dockerfiles for API / Bot / Dashboard (only `docker-compose.yml` for PostgreSQL)
- CI/CD pipelines
- Automated deploy scripts

Add these in a future step if needed.
