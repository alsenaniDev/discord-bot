# Naming Conventions

## C# / .NET

| Element | Convention | Example |
|---------|------------|---------|
| Namespace | `DiscordBot.{Layer}.{Area}` | `DiscordBot.Infrastructure.Services` |
| Class | PascalCase | `GuildPermissionResolver` |
| Interface | `I` + PascalCase | `IGuildService` |
| Method | PascalCase + `Async` suffix | `GetAccessAsync` |
| Property | PascalCase | `DiscordGuildId` |
| Private field | `_camelCase` | `_dbContext` |
| Constant | PascalCase | `ModuleKeys.Tickets` |
| Enum | PascalCase singular | `GuildPermissions`, `TicketStatus` |
| Enum member | PascalCase | `UseWarn`, `Active` |
| DTO | PascalCase + suffix | `GuildAccessDto`, `CreateTicketRequest` |
| Controller | `{Name}Controller` | `GuildsController` |
| Service | `{Name}Service` | `TicketService` |
| Configuration class | `{Entity}Configuration` | `GuildConfiguration` |
| Migration | `{timestamp}_{Description}` | `20260702151245_UnifyGuildPermissions` |
| Options class | `{Name}Options` | `JwtOptions` |
| Config section | PascalCase in JSON | `"Discord"`, `"Jwt"` |
| Env var (.NET) | Double underscore | `Discord__ClientId` |

## Database

| Element | Convention | Example |
|---------|------------|---------|
| Table | PascalCase plural | `GuildPermissionRoles` |
| Column | PascalCase | `DiscordGuildId` |
| FK column | `{Entity}Id` | `GuildId` |
| Index | `IX_{Table}_{Columns}` | EF default |
| Discord snowflake columns | `Discord{Thing}Id` | `DiscordUserId`, `DiscordRoleId` |

## Module and plan keys

**kebab-case** string constants:

- `welcome`, `tickets`, `moderation`, `logs`, `auto-role`, `reaction-roles`
- `free`, `basic`, `pro`, `premium`

Defined in `ModuleKeys.cs`, `PlanKeys.cs`.

## Permission keys (current enum)

PascalCase enum names serialized as strings:

- `UseWarn`, `ViewTickets`, `ManageModeration`

**Future Phase 2:** dot-notation string keys (`moderation.warn`, `tickets.reply`).

## API routes

- Prefix: `/api/`
- kebab-case segments: `/permission-roles`, `/upgrade-requests`
- Resource IDs: GUID for internal guild id, string for Discord snowflakes in bot routes
- Bot prefix: `/api/bot/`

## Angular / TypeScript

| Element | Convention | Example |
|---------|------------|---------|
| Component selector | `app-kebab-case` | `app-staff` |
| Component class | PascalCase | `StaffComponent` |
| File | kebab-case | `staff.component.ts` |
| Service | PascalCase | `GuildService` |
| Interface | PascalCase | `GuildAccess` |
| Property (TS) | camelCase | `canManageSettings` |
| i18n key | dot.notation | `staff.permissions.useWarn` |
| Route path | kebab-case | `/guilds/:id/moderation/settings` |

## Discord

| Element | Convention |
|---------|------------|
| Slash command | lowercase, hyphenated | `reaction-role`, `ticket` |
| Subcommand | lowercase | `setup`, `open`, `close` |
| Button custom ID | prefixed constants in `DiscordCustomIds.cs` |

## Documentation

| Element | Convention | Example |
|---------|------------|---------|
| Handbook doc | kebab-case.md | `permission-system.md` |
| Step guide | `step-{NN}-{topic}.md` | `step-23-railway-deployment.md` |
| Progress report | `YYYY-MM-DD-{task-slug}.md` | `2026-07-02-unified-permissions.md` |
| ADR | `NNNN-{title}.md` | `0001-unified-permissions.md` |

## Related docs

- `coding-standards.md`, `folder-structure.md`, `api-design.md`
