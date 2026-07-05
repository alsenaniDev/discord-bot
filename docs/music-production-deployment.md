# Music MVP production deployment

This runbook adds one private Lavalink v4 service to the existing Railway API, Bot, and PostgreSQL project. The Vercel dashboard remains unchanged. The MVP remains one bot, one active voice room per guild, and one Lavalink node.

## 1. Railway Lavalink service

Create a service named `lavalink` from this repository and set its config-as-code path to `deploy/railway/railway.lavalink.toml`. For a one-off CLI deployment from this checkout:

```bash
railway add --service lavalink
railway up deploy/lavalink --path-as-root --service lavalink --environment production
```

Keep **Public Networking off**, do not generate a domain, and do not add a TCP proxy. Railway private DNS is then `lavalink.railway.internal` when the service is named `lavalink`.

Set only these Lavalink service variables:

```env
LAVALINK_SERVER_PASSWORD=<strong-random-secret>
SERVER_PORT=2333
_JAVA_OPTIONS=-Xms256M -Xmx512M
```

The password is injected into `deploy/lavalink/application.yml`; never commit it. Successful logs include `Lavalink is ready to accept connections.`

## 2. Railway Bot variables

Add these to `discord-bot-worker`. A Railway reference keeps both passwords identical:

```env
Lavalink__Host=lavalink.railway.internal
Lavalink__Port=2333
Lavalink__Password=${{lavalink.LAVALINK_SERVER_PASSWORD}}
Lavalink__Secure=false
Lavalink__SearchPrefix=ytsearch
Lavalink__IdleTimeoutSeconds=60
```

Use `ytsearch` without a trailing colon because Lavalink4NET formats the search identifier. Never use `localhost` in the Railway Bot service.

## 3. Production migration

Confirm `railway status` says environment `production` and linked service `discord-bot-api`, then run:

```bash
railway run --service discord-bot-api ./deploy/railway/migrate.sh
```

This applies existing migration `20260705134350_AddGuildMusicSettings`. Do not create another migration. Verify it appears without `(Pending)`:

```bash
railway run --service discord-bot-api dotnet ef migrations list \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api --no-build
```

## 4. API and Vercel

- API `ConnectionStrings__DefaultConnection` must reference Railway PostgreSQL.
- API `Discord__DashboardUrl` must exactly match the Vercel production origin.
- Keep `Discord__AllowVercelOrigins=true` if preview deployments need API access.
- The dashboard production API URL is `dashboard/DiscordBot.Dashboard/src/environments/environment.production.ts` and must be the Railway API HTTPS URL.
- Vercel continues using `dashboard/DiscordBot.Dashboard/vercel.json`; no frontend move or additional runtime variable is required by the current compile-time configuration.
- Verify authenticated `GET` and `PUT /api/dashboard/guilds/{guildId}/music-settings`, plus bot-safe `GET /api/bot/guilds/{discordGuildId}/music-settings`.

## 5. Discord scopes and permissions

Required OAuth scopes:

- `bot`
- `applications.commands`

Required permissions:

- View Channels
- Send Messages
- Use Application Commands
- Embed Links
- Connect
- Speak
- Use Voice Activity

Re-invite the bot if its existing installation lacks either scope or the voice permissions. Commands are registered globally and per guild on startup; guild registration appears immediately while global propagation can take longer.

## 6. Production verification

- [ ] Lavalink has no public domain or TCP proxy.
- [ ] Lavalink logs report it is ready.
- [ ] Bot uses `lavalink.railway.internal`, not `localhost`.
- [ ] Lavalink and Bot password variables match through the Railway reference.
- [ ] API and Bot start successfully after deployment.
- [ ] `20260705134350_AddGuildMusicSettings` is applied to production PostgreSQL.
- [ ] Vercel Music Settings opens and can enable Music for a guild.
- [ ] `/music play lofi music` joins the caller's voice channel and starts playback.
- [ ] `/music queue`, `/music pause`, `/music resume`, and `/music stop` work.
- [ ] A play request from another voice channel is rejected.

## Troubleshooting

### Bot still connects to localhost

Set `Lavalink__Host=lavalink.railway.internal` on `discord-bot-worker`, then redeploy that service. Committed `localhost` is only the local-development fallback.

### Password mismatch or unauthorized Lavalink response

Set `Lavalink__Password=${{lavalink.LAVALINK_SERVER_PASSWORD}}` on the Bot. Restart Lavalink and redeploy the Bot after changing the secret.

### Private service unreachable

Confirm both services are in the same Railway project/environment, private networking is enabled, the service name is `lavalink`, and port `2333` is used. Public networking is neither required nor desired.

### Lavalink is publicly exposed

Remove its generated domain and TCP proxy in Railway Networking. The Bot should use private DNS only.

### Voice join times out

Confirm `GuildVoiceStates` is enabled in the bot gateway configuration and the bot has View Channel, Connect, Speak, and Use Voice Activity permissions.

### `/music` is missing

Restart/redeploy the Bot so command registration runs. Confirm the invite includes `applications.commands`; use the guild-registered command while global commands propagate.

### Dashboard CORS failure

Ensure API `Discord__DashboardUrl` exactly equals the Vercel origin and `Discord__AllowVercelOrigins=true` when using Vercel preview domains.

### Music settings relation does not exist

Run the production migration command above and verify `AddGuildMusicSettings` is listed without `(Pending)`.

### Queue disappears after restart

Expected for MVP: sessions and queues are in Bot memory. Persistence, multiple rooms, multiple music workers, and multiple Lavalink nodes are intentionally out of scope.
