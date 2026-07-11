# Activities service local development

The new `DiscordBot.Activities.Api` service is a dedicated backend for Discord Activity gameplay runtime. It is added beside the current platform API; the existing React Activity and old endpoints remain available during migration.

## Projects

- `src/DiscordBot.Activities.Api` — HTTP API, auth, health checks, SignalR hub.
- `src/DiscordBot.Activities.Infrastructure` — EF Core Activities database, Discord OAuth client, platform API client.
- `src/DiscordBot.Activities.Application` — service interfaces and DTOs.
- `src/DiscordBot.Activities.Domain` — runtime entities.
- `src/DiscordBot.Shared` — cross-project result primitives.

## Required configuration

Activities API:

```bash
ConnectionStrings__ActivitiesDatabase="Host=localhost;Port=5432;Database=discordbot_activities;Username=postgres;Password=postgres"
Discord__ClientId="your-discord-application-client-id"
Discord__ClientSecret="your-discord-application-client-secret"
Discord__RedirectUri="https://your-activity-url/.proxy/api/auth/discord/callback"
Jwt__Issuer="DiscordBot.Activities"
Jwt__Audience="DiscordBot.Activity"
Jwt__SigningKey="replace-with-at-least-32-characters"
ActivitiesAuth__AllowMissingActivityInstanceInDevelopment="false"
PlatformApi__BaseUrl="https://localhost:5001/"
PlatformApi__ServiceToken="same-long-secret-as-platform"
Cors__AllowedOrigins__0="https://your-activity-origin"
```

Platform API:

```bash
ActivitiesIntegration__ServiceToken="same-long-secret-as-activities"
```

The shared service token is sent by Activities API as:

```text
X-Activities-Service-Key: {PlatformApi__ServiceToken}
```

## Database setup

Restore local EF tooling and apply the new Activities migration to the Activities database:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Activities.Infrastructure \
  --startup-project src/DiscordBot.Activities.Api \
  --context ActivitiesDbContext
```

Current Activities migrations:

- `InitialActivitiesRuntime`
- `AddActivitiesRouletteRuntime`
- `AddActivityInstanceRouletteIndexes`
- `AddRoulettePayouts`

## Local startup

Start the platform API first because Activities API validates game access through it:

```bash
dotnet run --project src/DiscordBot.Api
```

Then start the Activities API:

```bash
dotnet run --project src/DiscordBot.Activities.Api
```

Health check:

```text
GET /health
GET /health/live
GET /health/ready
GET /api/internal/diagnostics/pilot
```

SignalR hub:

```text
/hubs/games
```

For SignalR clients, pass the Activities JWT as `access_token` in the query string when connecting to `/hubs/games`.

## New API flow

1. Activity frontend exchanges Discord OAuth code:
   - `POST /api/auth/discord/exchange`
2. Activities API exchanges the code server-side, validates the Discord user, and returns:
   - a short-lived Activities JWT for Activities API and SignalR;
   - the Discord access token needed by `discordSdk.commands.authenticate`.
3. Frontend creates an Activity session:
   - `POST /api/activity-sessions`
4. Activities API calls the platform API:
   - `POST /api/internal/activities/game-access/validate`
5. Platform API returns selected game/version/sandbox entitlement data.
6. Activities API creates `ActivitySession` and first `ActivityPlayer`.
7. Frontend connects to:
   - `/hubs/games`
8. Frontend calls hub method:
   - `JoinActivitySession(activitySessionId)`

## Trusted Discord context

Activities JWTs include trusted claims when Discord provides context:

- `discord_user_id`
- `discord_guild_id`
- `discord_channel_id`
- `activity_instance_id`

Runtime endpoints reject requests when the signed guild/channel claims do not match the requested guild/channel. Activity instance values sent by React are treated only as hints; if the signed claim exists, it overrides the body value.

Production must include an Activity instance claim. For local-only development, this can be relaxed deliberately:

```bash
ActivitiesAuth__AllowMissingActivityInstanceInDevelopment="true"
```

This option defaults to `false`, is only honored in `Development`, and logs a warning when used.

## Current limitations

- Roulette gameplay routes are not moved yet.
- The Angular Activity frontend is not created yet.
- Platform wallet reservation endpoints are available through the platform API internal Activities integration.
- Existing React Activity and `/api/games/activity/*` routes are still the production-compatible path.
- `dotnet-ef` on this machine is version 8.0.10; migration generation worked, but the tool warned that EF runtime is 9.0.4.

## Deployment notes

- Deploy Activities API as a separate service with its own database connection string.
- Configure CORS to the Discord Activity web origin.
- Configure both services with the same service-to-service token.
- Keep the platform API deployed because Activities API depends on it for entitlement/version validation.
- Rollback is low-risk in this phase because no existing Activity routes were removed.
