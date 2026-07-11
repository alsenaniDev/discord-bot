# Local configuration reference

Secret values belong in .NET User Secrets, environment variables, or gitignored `appsettings.Development.local.json` / `.env.local` files.

| Configuration key | Project | Required | Secret | Local default | Description |
| --- | --- | --- | --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `DiscordBot.Api` | Yes | No for Docker local | `Host=localhost;Port=5432;Database=discordbot_platform;Username=postgres;Password=postgres` | Platform PostgreSQL connection. |
| `Discord:ClientId` | `DiscordBot.Api` | Yes for Discord login/activity auth | No | `YOUR_DISCORD_CLIENT_ID` | Discord application/client id. |
| `Discord:ClientSecret` | `DiscordBot.Api` | Yes for OAuth | Yes | Placeholder | Discord OAuth client secret. |
| `Discord:BotToken` | `DiscordBot.Api` | Required by validation and some platform flows | Yes | Placeholder | Bot token used by platform-side Discord calls. |
| `Discord:RedirectUri` | `DiscordBot.Api` | Yes | No | `https://localhost:5001/api/auth/discord/callback` | Discord OAuth callback. |
| `Discord:DashboardUrl` | `DiscordBot.Api` | Yes | No | `http://localhost:4200` | Dashboard CORS origin. Comma-separated values are supported. |
| `Discord:ActivityUrl` | `DiscordBot.Api` | Optional unless Activity calls Platform API | No | `http://localhost:5173` | React Activity CORS origin. |
| `Discord:AllowVercelOrigins` | `DiscordBot.Api` | No | No | `false` | Allows Vercel preview origins when true. |
| `Bot:ApiKey` | `DiscordBot.Api` | Yes for bot/internal endpoints | Yes | `local-development-bot-api-key-change-me` | Must match `DiscordBot.Bot` `Api:ApiKey`. |
| `ActivitiesIntegration:ServiceToken` | `DiscordBot.Api` | Yes for Activities API wallet/access calls | Yes | `local-development-activities-service-token-change-me` | Must match Activities API `PlatformApi:ServiceToken`. Sent as `X-Activities-Service-Key`. |
| `Jwt:Secret` | `DiscordBot.Api` | Yes | Yes | `local-development-jwt-signing-key-change-me-123456789` | Dashboard JWT signing secret, at least 32 chars. |
| `Jwt:Issuer` | `DiscordBot.Api` | Yes | No | `DiscordBot` | Dashboard JWT issuer. |
| `Jwt:Audience` | `DiscordBot.Api` | Yes | No | `DiscordBot.Dashboard` | Dashboard JWT audience. |
| `Jwt:ExpiresMinutes` | `DiscordBot.Api` | No | No | `60` | Dashboard JWT lifetime. |
| `Admin:DiscordUserId` | `DiscordBot.Api` | Optional local seed/admin setup | No | `YOUR_DISCORD_USER_ID` | Platform admin Discord user id. |
| `Seed:Enabled` | `DiscordBot.Api` | No | No | `false` | Enables development data seed when supported. |
| `Seed:OwnerDiscordUserId` | `DiscordBot.Api` | No | No | Empty | Test guild owner id for seed data. |
| `Seed:DiscordGuildId` | `DiscordBot.Api` | No | No | `123456789012345678` | Test guild id for seed data. |
| `Seed:GuildName` | `DiscordBot.Api` | No | No | `My Test Server` | Test guild name for seed data. |
| `PORT` | `DiscordBot.Api` | Production/PaaS only | No | Empty | Overrides API bind URL on platforms like Railway. |
| `ConnectionStrings:ActivitiesDatabase` | `DiscordBot.Activities.Api` | Yes | No for Docker local | `Host=localhost;Port=5432;Database=discordbot_activities;Username=postgres;Password=postgres` | Activities PostgreSQL connection. |
| `Discord:ClientId` | `DiscordBot.Activities.Api` | Yes for Activity OAuth | No | `YOUR_DISCORD_CLIENT_ID` | Discord application/client id. |
| `Discord:ClientSecret` | `DiscordBot.Activities.Api` | Yes for Activity OAuth | Yes | Placeholder | Discord OAuth client secret. |
| `Discord:RedirectUri` | `DiscordBot.Activities.Api` | Yes | No | `http://localhost:5173` | Activity frontend redirect/origin used by code exchange. |
| `Jwt:Issuer` | `DiscordBot.Activities.Api` | Yes | No | `DiscordBot.Activities` | Activities JWT issuer. |
| `Jwt:Audience` | `DiscordBot.Activities.Api` | Yes | No | `DiscordBot.Activity` | Activities JWT audience. |
| `Jwt:SigningKey` | `DiscordBot.Activities.Api` | Yes | Yes | `local-development-activities-jwt-signing-key-change-me` | Activities JWT signing key, at least 32 chars. |
| `Jwt:AccessTokenMinutes` | `DiscordBot.Activities.Api` | No | No | `30` | Activities token lifetime. |
| `PlatformApi:BaseUrl` | `DiscordBot.Activities.Api` | Yes | No | `https://localhost:5001` | Platform API base URL. |
| `PlatformApi:ServiceToken` | `DiscordBot.Activities.Api` | Yes | Yes | `local-development-activities-service-token-change-me` | Must match Platform API `ActivitiesIntegration:ServiceToken`. |
| `ActivitiesAuth:AllowMissingActivityInstanceInDevelopment` | `DiscordBot.Activities.Api` | No | No | `false` | Development bypass for missing Discord Activity instance id. |
| `ActivitiesDiagnostics:ServiceToken` | `DiscordBot.Activities.Api` | Optional diagnostics | Yes | `local-development-activities-diagnostics-token-change-me` | Shared token for diagnostics endpoints if used. |
| `Cors:AllowedOrigins` | `DiscordBot.Activities.Api` | Yes for browser Activity | No | `http://localhost:5173`, `https://localhost:5173` | Activity CORS origins. |
| `Discord:Token` | `DiscordBot.Bot` | Yes to run bot | Yes | Placeholder | Discord bot token. |
| `Api:BaseUrl` | `DiscordBot.Bot` | Yes | No | `https://localhost:5001` | Platform API base URL used by bot. |
| `Api:ApiKey` | `DiscordBot.Bot` | Yes | Yes | `local-development-bot-api-key-change-me` | Must match Platform API `Bot:ApiKey`. |
| `Platform:DashboardUrl` | `DiscordBot.Bot` | Yes for links | No | `http://localhost:4200` | Dashboard URL used in bot messages/links. |
| `Activity:Enabled` | `DiscordBot.Bot` | No | No | `true` | Enables Discord Activity launch flow. |
| `Lavalink:Host` | `DiscordBot.Bot` | Required for music | No | `localhost` | Lavalink host. |
| `Lavalink:Port` | `DiscordBot.Bot` | Required for music | No | `2333` | Lavalink port. |
| `Lavalink:Password` | `DiscordBot.Bot` | Required for music | Local shared secret | `youshallnotpass` | Must match Lavalink server passphrase. |
| `Lavalink:Secure` | `DiscordBot.Bot` | No | No | `false` | Uses HTTPS/WSS when true. |
| `Lavalink:SearchPrefix` | `DiscordBot.Bot` | No | No | `ytsearch` | Prefix for text music searches. |
| `Lavalink:IdleTimeoutSeconds` | `DiscordBot.Bot` | No | No | `30` | Music idle disconnect timeout. |
| `DOTNET_ENVIRONMENT` | `DiscordBot.Bot` | Yes for local profile | No | `Development` | Worker environment name. |
| `ASPNETCORE_ENVIRONMENT` | `DiscordBot.Api`, `DiscordBot.Activities.Api` | Yes for local profile | No | `Development` | Web app environment name. |
| `VITE_DISCORD_CLIENT_ID` | React Activity | Yes in Discord | No | Empty | Public Discord client id. |
| `VITE_API_BASE_URL` | React Activity | Yes for legacy Activity endpoints | No | `https://localhost:5001` | Platform API base URL. |
| `VITE_PLATFORM_API_BASE_URL` | React Activity | Optional alias | No | `https://localhost:5001` | Alias for `VITE_API_BASE_URL`; code uses it if `VITE_API_BASE_URL` is empty. |
| `VITE_ACTIVITIES_API_BASE_URL` | React Activity | Yes for new Activities runtime | No | `https://localhost:7001` | Activities API base URL. |
| `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS` | React Activity | Optional pilot control | No | Empty | Comma-separated guild Discord ids that use the new Activities Roulette runtime. |
| `VITE_ENVIRONMENT` | React Activity | Optional | No | `development` | Local environment marker for humans/scripts. |
| `environment.apiUrl` | Angular Dashboard | Yes | No | `https://localhost:5001` | Platform API base URL. |
