# Step 23 — Railway Beta Deployment

Deploy the beta stack to [Railway](https://railway.app) with PostgreSQL, .NET API, .NET Bot worker, and Angular dashboard (Railway or Vercel/Firebase).

**Do not commit secrets.** Set all values in Railway service variables only.

---

## Architecture on Railway

```
                    ┌─────────────────────┐
                    │  Angular Dashboard  │
                    │  (Railway / Vercel) │
                    └──────────┬──────────┘
                               │ HTTPS
                               ▼
┌──────────────┐    HTTPS     ┌─────────────────────┐
│ Discord OAuth│─────────────▶│  DiscordBot API     │
└──────────────┘              │  (Railway service)  │
                              └──────────┬──────────┘
                                         │
┌──────────────┐    HTTPS                 │ PostgreSQL
│ Bot Worker   │◀───────────────────────┤
│ (no public   │    X-Bot-Api-Key         ▼
│  HTTP)       │              ┌─────────────────────┐
└──────┬───────┘              │  Railway PostgreSQL │
       │                      └─────────────────────┘
       ▼
 Discord Gateway
```

Deployment files live in `deploy/railway/`:

| File | Purpose |
|------|---------|
| `Dockerfile.api` | API container |
| `Dockerfile.bot` | Bot worker container |
| `Dockerfile.dashboard` | Dashboard static site (nginx) |
| `railway.api.toml` | API Railway config (health check) |
| `railway.bot.toml` | Bot worker config |
| `railway.dashboard.toml` | Dashboard config |
| `migrate.sh` | EF Core migrations against Railway Postgres |
| `railway.env.example` | Variable name reference |

---

## 1. Railway project setup

### Create project

1. Go to [railway.app](https://railway.app) → **New Project**
2. Choose **Deploy from GitHub repo** and connect your repository
3. You will add **four services** (or three if dashboard is on Vercel):

| Service | Type | Public URL |
|---------|------|------------|
| `postgres` | PostgreSQL template | Private only |
| `api` | GitHub repo + Dockerfile | **Yes** — generate domain |
| `bot` | GitHub repo + Dockerfile | **No** — disable public networking |
| `dashboard` | GitHub repo + Dockerfile (optional) | **Yes** — or use Vercel |

### Add PostgreSQL

1. In the project → **+ New** → **Database** → **PostgreSQL**
2. Note the service name (e.g. `Postgres`)
3. Link it to the **API** service: API → **Variables** → **Add Reference** → select Postgres connection variables

### Add API service

1. **+ New** → **GitHub Repo** → select this repository
2. **Settings** → **Build**:
   - Builder: **Dockerfile**
   - Dockerfile path: `deploy/railway/Dockerfile.api`
   - Root directory: `/` (repository root)
3. **Settings** → **Deploy** → **Config-as-code**: `deploy/railway/railway.api.toml` (optional)
4. **Settings** → **Networking** → **Generate Domain** (e.g. `https://discordbot-api-production.up.railway.app`)
5. Set environment variables (section 8 below)

### Add Bot worker service

1. **+ New** → **GitHub Repo** → same repository
2. **Settings** → **Build**:
   - Dockerfile path: `deploy/railway/Dockerfile.bot`
3. **Settings** → **Networking** → **Public Networking: OFF** (worker only, no HTTP)
4. Set bot environment variables (section 8)
5. **Settings** → **Deploy** → Config: `deploy/railway/railway.bot.toml`

The bot runs as a long-lived `dotnet DiscordBot.Bot.dll` process. Railway restarts it on failure.

### Alternative: Nixpacks (no Docker)

If you prefer Nixpacks instead of Docker for the API:

| Setting | Value |
|---------|-------|
| Root directory | `src/DiscordBot.Api` |
| Build command | `dotnet publish -c Release -o /app` |
| Start command | `dotnet /app/DiscordBot.Api.dll` |
| Health check path | `/api/health` |

The API `Program.cs` reads Railway's `PORT` environment variable automatically.

---

## 2. API deployment

### Environment-driven configuration

The API uses standard .NET configuration. Railway variables map to `appsettings.json` keys with double underscores:

| Railway variable | Config key |
|------------------|------------|
| `ConnectionStrings__DefaultConnection` | Database |
| `Discord__ClientId` | OAuth |
| `Jwt__Secret` | JWT signing |

No secrets are stored in the repo. Set everything in Railway **Variables**.

### PORT binding

Railway injects `PORT`. The API binds:

```
http://0.0.0.0:$PORT
```

Implemented in `Program.cs` — no manual `ASPNETCORE_URLS` required (but you may set it if needed).

### HTTPS

Railway terminates TLS at the edge. The container listens on HTTP internally. HTTPS redirection is **disabled in Production** to avoid redirect loops.

### Health check

| Path | Expected |
|------|----------|
| `GET /api/health` | `200` with `{ "status": "healthy", "database": "connected" }` |

Configured in `deploy/railway/railway.api.toml`:

```toml
healthcheckPath = "/api/health"
healthcheckTimeout = 120
```

Returns `503` if PostgreSQL is unreachable (Railway marks deploy unhealthy).

### Start command

Dockerfile entrypoint (default):

```bash
dotnet DiscordBot.Api.dll
```

---

## 3. Bot deployment

| Requirement | How |
|-------------|-----|
| No public HTTP | Disable **Public Networking** on the bot service |
| Stays running | `DiscordBotHostedService` keeps the process alive |
| Discord token | `Discord__Token` variable |
| API URL | `Api__BaseUrl=https://YOUR_API_DOMAIN` (Railway API public URL) |
| Shared secret | `Api__ApiKey` = same as API `Bot__ApiKey` |
| Dashboard link in `/setup` | `Platform__DashboardUrl=https://YOUR_DASHBOARD_DOMAIN` |

Start command (Dockerfile):

```bash
dotnet DiscordBot.Bot.dll
```

---

## 4. PostgreSQL on Railway

### Connection string (API service)

Link Postgres to the API service, then set:

```
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
```

Replace `Postgres` with your Postgres service name if different.

### Run migrations

**Option A — Railway CLI (recommended)**

```bash
# Install CLI: https://docs.railway.app/develop/cli
railway login
railway link          # select project + API service

railway run --service api ./deploy/railway/migrate.sh
```

**Option B — Local machine with Railway connection**

```bash
# Copy DATABASE_URL or connection string from Railway Postgres → Connect
export ConnectionStrings__DefaultConnection="Host=...;Port=...;Database=railway;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"

./deploy/railway/migrate.sh
```

**Option C — One-off Railway shell**

In Railway → API service → **Shell**:

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

(Requires .NET SDK in shell or run migrate locally via Option B.)

Seeders (`ModuleSeeder`, `SubscriptionPlanSeeder`, `PlatformAdminSeeder`) run automatically on API startup.

---

## 5. Dashboard deployment

Set `apiUrl` to your **Railway API public URL** before building.

### Option A — Railway (Docker)

1. **+ New** service → same GitHub repo
2. Dockerfile: `deploy/railway/Dockerfile.dashboard`
3. **Build variable** (Railway → Variables → Build-time):

   ```
   API_URL=https://YOUR_API_DOMAIN
   ```

4. Generate public domain (e.g. `https://discordbot-dashboard.up.railway.app`)
5. Config: `deploy/railway/railway.dashboard.toml`

### Option B — Vercel (recommended for static Angular)

1. Import GitHub repo at [vercel.com](https://vercel.com)
2. **Root Directory**: `dashboard/DiscordBot.Dashboard`
3. **Framework**: Angular (or Other)
4. Before deploy, set production API URL in `src/environments/environment.ts`:

   ```typescript
   export const environment = {
     production: true,
     apiUrl: 'https://YOUR_API_DOMAIN'
   };
   ```

5. Build command: `npm run build`  
   Output: `dist/discord-bot.dashboard`  
   (`vercel.json` included for SPA routing)

6. Deploy → note dashboard URL for CORS / OAuth config

### Option B — Firebase Hosting

```bash
cd dashboard/DiscordBot.Dashboard
# Set environment.ts apiUrl to Railway API URL
npm run build
firebase init hosting   # public dir: dist/discord-bot.dashboard
firebase deploy
```

---

## 6. Discord Developer Portal

After Railway domains are live:

### OAuth2 redirect URL

Add to **OAuth2 → Redirects**:

```
https://YOUR_API_DOMAIN/api/auth/discord/callback
```

Set matching API variable:

```
Discord__RedirectUri=https://YOUR_API_DOMAIN/api/auth/discord/callback
```

### Bot intents

**Bot → Privileged Gateway Intents**:

- ✅ **Server Members Intent** (welcome messages)

### Invite URL

Use your real Client ID:

```
https://discord.com/api/oauth2/authorize?client_id=YOUR_DISCORD_CLIENT_ID&permissions=8&scope=bot%20applications.commands
```

Adjust permissions as needed. The dashboard invite button uses the API-generated URL from `Discord__ClientId`.

---

## 7. CORS

The API allows exactly one dashboard origin from config:

```
Discord__DashboardUrl=https://YOUR_DASHBOARD_DOMAIN
```

Must match the deployed dashboard URL **exactly** (scheme + host, no trailing slash mismatch).

After changing CORS, redeploy the API service.

---

## 8. Railway environment variables

### API service

| Variable | Example / notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | See section 4 (Postgres reference) |
| `Discord__ClientId` | Your Discord application ID |
| `Discord__ClientSecret` | OAuth client secret |
| `Discord__BotToken` | Bot token (invite URL generation) |
| `Discord__RedirectUri` | `https://YOUR_API_DOMAIN/api/auth/discord/callback` |
| `Discord__DashboardUrl` | `https://YOUR_DASHBOARD_DOMAIN` |
| `Jwt__Secret` | 32+ random characters |
| `Jwt__Issuer` | `DiscordBot` |
| `Jwt__Audience` | `DiscordBot.Dashboard` |
| `Bot__ApiKey` | Strong random string |
| `Admin__DiscordUserId` | Your Discord user ID |

### Bot worker service

| Variable | Example / notes |
|----------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Discord__Token` | Bot token |
| `Api__BaseUrl` | `https://YOUR_API_DOMAIN` |
| `Api__ApiKey` | Same as API `Bot__ApiKey` |
| `Platform__DashboardUrl` | `https://YOUR_DASHBOARD_DOMAIN` |

### Dashboard (Railway Docker build arg)

| Variable | When |
|----------|------|
| `API_URL` | Build-time → `https://YOUR_API_DOMAIN` |

See `deploy/railway/railway.env.example` for a copy-paste template.

---

## 9. Verification checklist

Run after all services are deployed and migrations applied.

### Infrastructure

- [ ] `GET https://YOUR_API_DOMAIN/api/health` → `200`, `"database": "connected"`
- [ ] Railway API deploy shows **Healthy**
- [ ] Railway Bot service is **Running** (no public URL)
- [ ] PostgreSQL tables exist (migrations applied)

### Discord & auth

- [ ] Bot shows **online** in Discord server member list
- [ ] Dashboard **Login with Discord** completes without redirect error
- [ ] OAuth redirect URL matches Discord Portal exactly

### Core flows

- [ ] Invite bot → run `/setup` → server appears in dashboard
- [ ] **Modules** — toggle saves
- [ ] **Settings** — welcome channel/message saves; sync Discord data works
- [ ] **Tickets** — `/ticket open` in Discord; ticket visible in dashboard
- [ ] **Logs** — events appear after actions
- [ ] **Language** — switch English ↔ Arabic (RTL layout)

### CORS / URLs

- [ ] Browser network tab shows no CORS errors from dashboard → API
- [ ] `Discord__DashboardUrl` matches dashboard origin
- [ ] `Api__BaseUrl` on bot matches API public URL

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| API deploy unhealthy | Check `/api/health` — likely DB connection string or migrations not run |
| OAuth redirect mismatch | `Discord__RedirectUri` must exactly match Discord Portal |
| CORS error in browser | `Discord__DashboardUrl` must match dashboard URL; redeploy API |
| Bot offline | Check bot service logs; verify `Discord__Token` |
| Bot can't register guild | API running? `Api__ApiKey` matches `Bot__ApiKey`? |
| Empty server list | Run `/setup` in Discord; user must be guild owner |
| JWT startup error | `Jwt__Secret` must be 32+ characters |

---

## Local vs Railway

| | Local | Railway |
|---|-------|---------|
| API URL | `http://localhost:5217` | `https://YOUR_API_DOMAIN` |
| Dashboard | `http://localhost:4200` | `https://YOUR_DASHBOARD_DOMAIN` |
| Database | `docker compose` Postgres | Railway PostgreSQL |
| Secrets | `*.local.json` (gitignored) | Railway Variables |

See also: `docs/step-22-beta-deployment.md`, `README.md`, `.env.example`.
