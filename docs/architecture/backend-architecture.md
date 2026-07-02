# Backend Architecture

## Overview

The backend is a **layered monolith**: ASP.NET Core 9 API + EF Core Infrastructure + pure Domain.

Business logic lives in `Infrastructure/Services/*.cs`. Controllers are thin HTTP adapters.

## Startup pipeline

**File:** `src/DiscordBot.Api/Program.cs`

1. Load configuration (appsettings + local + env vars)
2. Bind to `0.0.0.0:{PORT}` when Railway injects `PORT`
3. Register controllers, Swagger, Infrastructure, JWT auth, CORS
4. Validate required configuration (fail fast in production)
5. Middleware: CORS → ExceptionHandling → RequestLogging → Auth
6. Map controllers

## Layering

| Layer | Location | Contains |
|-------|----------|----------|
| HTTP | `DiscordBot.Api/Controllers/` | Route definitions, `[Authorize]`, status codes |
| Application | `DiscordBot.Infrastructure/Services/` | Business rules, authorization, EF queries |
| Domain | `DiscordBot.Domain/` | Entities, enums, constants |
| Persistence | `DiscordBot.Infrastructure/Data/` | DbContext, configurations, migrations |

## Service registration

**File:** `src/DiscordBot.Infrastructure/DependencyInjection.cs`

All services are **Scoped** (per HTTP request):

- Auth: `IAuthService`, `IJwtTokenService`, `IDiscordOAuthService`, `IAuthCodeService`
- Guild: `IGuildService`, `IGuildResourceService`, `IGuildAccessService`, `IGuildPermissionResolver`, `IGuildPermissionRoleService`, `IGuildProfileService`
- Features: `ITicketService`, `IModerationService`, `ILogService`, `IReactionRoleService`, `IAutoReplyService`, `ICommandPanelService`
- Platform: `IModuleService`, `ISubscriptionService`, `IPlanUpgradeRequestService`, `IPlatformAdminService`, `IAdminService`, `IOnboardingService`

**Hosted services** (run on API startup):

- `ModuleSeeder` — ensures 6 module catalog rows
- `SubscriptionPlanSeeder` — ensures 4 plan rows
- `PlatformAdminSeeder` — seeds admin from `Admin:DiscordUserId`
- `DevelopmentDataSeeder` — dev-only sample data

## Controller groups

| Controller | Audience | Auth |
|------------|----------|------|
| AuthController | Dashboard login | Mixed |
| GuildsController | Dashboard guild ops | JWT |
| PlansController | Dashboard plan list | JWT |
| OnboardingController | Dashboard checklist | JWT |
| AdminController | Platform operators | JWT + PlatformAdmin |
| HealthController | Load balancers | Anonymous |
| Bot*Controller (6) | Bot worker | BotApiKey |

See `api-design.md` for full endpoint list.

## Middleware

| Middleware | Purpose |
|------------|---------|
| `ExceptionHandlingMiddleware` | Catches unhandled exceptions → JSON error response |
| `RequestLoggingMiddleware` | Logs request method, path, status, duration |

## Authorization in services

Pattern used across services:

```csharp
var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, ct);
if (access is null || !access.CanManageSettings)
    return null; // controller returns 404 or 403
```

Guild list visibility: `GuildService.GetAccessibleGuildsAsync` returns owned guilds + guilds where user has a matching `GuildPermissionRole`.

## DTO conventions

- Request/response types in `Infrastructure/Models/`
- Suffix: `*Dto`, `*Request`, `*Response`
- API serializes enums as strings (`JsonStringEnumConverter`)
- No EF entities returned directly from controllers

## Database access rules

- Services inject `AppDbContext`
- Use `AsNoTracking()` for read-only queries
- Use explicit includes only when needed (most queries project to DTOs)
- Bulk delete: `ExecuteDeleteAsync` (logs clear)

## Error handling

- Validation errors → `400 BadRequest` with `{ message }`
- Not found / access denied → often `404` (intentionally vague for security)
- Business rule violations → `InvalidOperationException` caught in controller → `400`
- Unhandled → `500` via middleware

## Assumptions

- **No rate limiting** middleware (future hardening)
- **No API versioning** prefix (`/api/v1/`) — all routes under `/api/`
- **Swagger** enabled in Development only

## Related docs

- `authentication.md`, `authorization.md`
- `database.md`, `api-design.md`
- `subscription-system.md`, `permission-system.md`
