# Production configuration reference

Do not commit real values. Configure these in Railway, Vercel, or your secret manager.

| Key | Service | Required | Secret | Example format | Description | Must match another service |
| --- | --- | --- | --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Platform API | Yes | No | `Production` | Enables production validation. | No |
| `ConnectionStrings__DefaultConnection` | Platform API | Yes | Yes | `Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` | Platform PostgreSQL connection. | No |
| `Discord__ClientId` | Platform API | Yes | No | Discord snowflake | Discord app/client id. | Activity `VITE_DISCORD_CLIENT_ID`, Activities API `Discord__ClientId` |
| `Discord__ClientSecret` | Platform API | Yes | Yes | random string | Discord OAuth client secret. | Activities API can use same Discord app secret |
| `Discord__BotToken` | Platform API | Yes | Yes | bot token | Discord bot token for platform-side Discord calls. | Bot `Discord__Token` |
| `Discord__RedirectUri` | Platform API | Yes | No | `https://api.example.com/api/auth/discord/callback` | OAuth callback registered in Discord Developer Portal. | Discord Developer Portal |
| `Discord__DashboardUrl` | Platform API | Yes | No | `https://dashboard.example.com` | Dashboard CORS origin. | Dashboard deployment URL |
| `Discord__ActivityUrl` | Platform API | Yes | No | `https://activity.example.com` | React Activity CORS origin. | Activity deployment URL |
| `Discord__AllowVercelOrigins` | Platform API | Optional | No | `false` | Allows Vercel preview origins when true. | No |
| `Bot__ApiKey` | Platform API | Yes | Yes | long random string | Shared key for bot internal endpoints. | Bot `Api__ApiKey` |
| `ActivitiesIntegration__ServiceToken` | Platform API | Yes | Yes | long random string | Shared key for Activities API internal endpoints. | Activities API `PlatformApi__ServiceToken` |
| `Jwt__Secret` | Platform API | Yes | Yes | 32+ chars | Dashboard JWT signing secret. | No |
| `Jwt__Issuer` | Platform API | Yes | No | `DiscordBot` | Dashboard JWT issuer. | Dashboard token validation expectations |
| `Jwt__Audience` | Platform API | Yes | No | `DiscordBot.Dashboard` | Dashboard JWT audience. | Dashboard token validation expectations |
| `Jwt__ExpiresMinutes` | Platform API | Optional | No | `60` | Dashboard JWT lifetime. | No |
| `Admin__DiscordUserId` | Platform API | Recommended | No | Discord user id | Platform admin bootstrap id. | No |
| `ASPNETCORE_ENVIRONMENT` | Activities API | Yes | No | `Production` | Enables production validation. | No |
| `ConnectionStrings__ActivitiesDatabase` | Activities API | Yes | Yes | `Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` | Activities PostgreSQL connection. | No |
| `Discord__ClientId` | Activities API | Yes | No | Discord snowflake | Discord app/client id for Activity OAuth. | Platform API `Discord__ClientId`, Activity `VITE_DISCORD_CLIENT_ID` |
| `Discord__ClientSecret` | Activities API | Yes | Yes | random string | Discord OAuth client secret. | Platform API may use same Discord app secret |
| `Discord__RedirectUri` | Activities API | Yes | No | `https://activity.example.com` | Activity OAuth redirect/origin. | Discord Activity URL mapping |
| `Jwt__Issuer` | Activities API | Yes | No | `DiscordBot.Activities` | Activities JWT issuer. | No |
| `Jwt__Audience` | Activities API | Yes | No | `DiscordBot.Activity` | Activities JWT audience. | No |
| `Jwt__SigningKey` | Activities API | Yes | Yes | 32+ chars | Activities JWT signing key. | No |
| `Jwt__AccessTokenMinutes` | Activities API | Optional | No | `30` | Activities token lifetime. | No |
| `PlatformApi__BaseUrl` | Activities API | Yes | No | `https://api.example.com` | Platform API base URL. | Platform API public URL |
| `PlatformApi__ServiceToken` | Activities API | Yes | Yes | long random string | Sent as `X-Activities-Service-Key`. | Platform API `ActivitiesIntegration__ServiceToken` |
| `ActivitiesDiagnostics__ServiceToken` | Activities API | Recommended | Yes | long random string | Diagnostics access token. | Smoke/diagnostics tooling |
| `Cors__AllowedOrigins__0` | Activities API | Yes | No | `https://activity.example.com` | Explicit Activity frontend origin. | Activity deployment URL |
| `DOTNET_ENVIRONMENT` | Bot | Yes | No | `Production` | Worker environment. | No |
| `Discord__Token` | Bot | Yes | Yes | bot token | Discord Gateway login token. | Platform API `Discord__BotToken` |
| `Api__BaseUrl` | Bot | Yes | No | `https://api.example.com` | Platform API URL. | Platform API public URL |
| `Api__ApiKey` | Bot | Yes | Yes | long random string | Shared key for bot internal endpoints. | Platform API `Bot__ApiKey` |
| `Platform__DashboardUrl` | Bot | Yes | No | `https://dashboard.example.com` | Dashboard link base URL. | Dashboard deployment URL |
| `Activity__Enabled` | Bot | Optional | No | `true` | Enables Discord Activity launch flow. | No |
| `Lavalink__Host` | Bot | Yes for music | No | `lavalink.railway.internal` | Lavalink private hostname. | Lavalink service |
| `Lavalink__Port` | Bot | Yes for music | No | `2333` | Lavalink port. | Lavalink service |
| `Lavalink__Password` | Bot | Yes for music | Yes | long random string | Lavalink password. | Lavalink `LAVALINK_SERVER_PASSWORD` |
| `Lavalink__Secure` | Bot | Optional | No | `false` | Use TLS for Lavalink when true. | Lavalink service |
| `Lavalink__SearchPrefix` | Bot | Optional | No | `ytsearch` | Search prefix for text music searches. | No |
| `VITE_DISCORD_CLIENT_ID` | React Activity | Yes | No | Discord snowflake | Public Discord client id. | Platform/Activities Discord client id |
| `VITE_API_BASE_URL` | React Activity | Yes | No | `https://api.example.com` | Platform API URL. | Platform API public URL |
| `VITE_PLATFORM_API_BASE_URL` | React Activity | Optional alias | No | `https://api.example.com` | Alias for Platform API URL. | Platform API public URL |
| `VITE_ACTIVITIES_API_BASE_URL` | React Activity | Yes | No | `https://activities.example.com` | Activities API URL. | Activities API public URL |
| `VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS` | React Activity | Optional | No | `152...,...` | Comma-separated pilot guild ids; empty means legacy runtime for all. | No |
| `VITE_ENVIRONMENT` | React Activity | Optional | No | `production` | Build environment marker. | No |
| `environment.production.ts apiUrl` | Angular Dashboard | Yes | No | `https://api.example.com` | Platform API URL. | Platform API public URL |
