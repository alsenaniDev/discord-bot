# API Design

## Base URL and versioning

- Base path: `/api/`
- **No version prefix** (no `/api/v1/`) — breaking changes require careful migration
- Production example: `https://discord-bot-production-b872.up.railway.app`

Swagger: `/swagger` (Development only)

## Authentication summary

| Route prefix | Auth |
|--------------|------|
| `/api/auth/*` | Mixed (see authentication.md) |
| `/api/guilds/*`, `/api/plans`, `/api/onboarding/*` | JWT Bearer |
| `/api/admin/*` | JWT + Platform Admin |
| `/api/bot/*` | `X-Bot-Api-Key` |
| `/api/health` | Anonymous |

## Response conventions

### Success

- `200 OK` — GET, PUT, PATCH, POST returning data
- `201 Created` — not widely used; most POST return 200

### Errors

JSON body typically:

```json
{ "message": "Human-readable error description." }
```

| Status | Usage |
|--------|-------|
| 400 | Validation failure, business rule (`InvalidOperationException`) |
| 401 | Missing/invalid JWT |
| 403 | Explicit forbidden (rare; subscription PUT uses this) |
| 404 | Not found OR access denied (intentionally vague) |
| 500 | Unhandled exception (middleware) |

Enums serialized as **strings** in JSON.

## Controller reference

### AuthController — `/api/auth`

| Method | Route | Auth |
|--------|-------|------|
| GET | `/discord/login` | Anonymous |
| GET | `/discord/callback` | Anonymous |
| POST | `/token` | Anonymous |
| GET | `/me` | JWT |

### GuildsController — `/api/guilds` [JWT]

| Group | Routes |
|-------|--------|
| Guild | GET `/`, GET `/{id}/overview` |
| Settings | GET/PUT `/{id}/settings` |
| Tickets | GET `/{id}/tickets`, PATCH `/{id}/tickets/{ticketId}/close`, POST `/{id}/tickets/{ticketId}/messages` |
| Auto-replies | GET/POST `/{id}/auto-replies`, PUT/DELETE `/{id}/auto-replies/{ruleId}` |
| Resources | GET `/{id}/channels`, `/roles`, `/members`, `/categories`, POST `/{id}/sync-resources` |
| Moderation data | GET `/{id}/warnings`, GET `/{id}/moderation-cases` |
| Modules | GET/PUT `/{id}/modules`, PUT `/{id}/modules/{moduleKey}` |
| Logs | GET/DELETE `/{id}/logs` |
| Reaction roles | GET/DELETE `/{id}/reaction-roles`, DELETE `/{id}/reaction-roles/{reactionRoleId}` |
| Subscription | GET/PUT `/{id}/subscription`, GET/POST `/{id}/subscription/upgrade-requests` |
| Access | GET `/{id}/access` |
| Permission roles | GET/POST `/{id}/permission-roles`, PUT/DELETE `/{id}/permission-roles/{roleId}` |
| Profile | GET/PUT `/{id}/profile` |

### PlansController — `/api/plans` [JWT]

| Method | Route |
|--------|-------|
| GET | `/` |

### OnboardingController — `/api/onboarding` [JWT]

| Method | Route |
|--------|-------|
| GET | `/status` |

### AdminController — `/api/admin` [JWT + PlatformAdmin]

| Group | Routes |
|-------|--------|
| Stats | GET `/stats` |
| Guilds | GET `/guilds`, GET `/guilds/{id}`, PUT `/guilds/{id}/subscription` |
| Users | GET `/users` |
| Upgrade requests | GET `/upgrade-requests`, POST `/upgrade-requests/{id}/approve`, POST `/upgrade-requests/{id}/reject` |
| Subscription ops | POST `/guilds/{id}/subscription/extend`, POST `/guilds/{id}/subscription/cancel` |
| Plans CRUD | GET/POST `/plans`, PUT/DELETE `/plans/{id}` |

### HealthController

| Method | Route |
|--------|-------|
| GET | `/api/health` |

### Bot controllers — `/api/bot/*` [BotApiKey]

#### BotGuildsController — `/api/bot/guilds`

| Method | Route |
|--------|-------|
| POST | `/join` |
| GET | `/{discordGuildId}/settings` |
| GET | `/{discordGuildId}/auto-replies` |
| GET | `/sync-requests` |
| POST | `/{discordGuildId}/resources`, `/sync-resources` |
| GET | `/{discordGuildId}/modules/{moduleKey}` |
| POST | `/{discordGuildId}/permissions/evaluate` |
| GET | `/{discordGuildId}/profile` |
| POST | `/{discordGuildId}/dashboard-access/evaluate` |

#### BotModerationController — `/api/bot/moderation`

| Method | Route |
|--------|-------|
| POST | `/warnings` |
| POST | `/cases` |
| GET | `/warnings?discordGuildId=&targetUserId=` |

#### BotTicketsController — `/api/bot/tickets`

| Method | Route |
|--------|-------|
| POST | `/` |
| GET | `/by-channel/{channelDiscordId}` |
| PATCH | `/{id}/close` |
| GET | `/pending-cleanups` |
| POST | `/{ticketId}/ack-cleanup` |
| GET | `/pending-messages` |
| POST | `/messages/{messageId}/ack` |

#### BotTicketSetupController

| Method | Route |
|--------|-------|
| POST | `/api/bot/guilds/{discordGuildId}/tickets/setup` |

#### BotLogsController — `/api/bot/logs`

| Method | Route |
|--------|-------|
| POST | `/` |

#### BotReactionRolesController — `/api/bot/reaction-roles`

| Method | Route |
|--------|-------|
| POST | `/` |
| GET | `/by-button/{customId}` |

#### BotCommandPanelController — `/api/bot/command-panels`

| Method | Route |
|--------|-------|
| GET | `/pending` |
| POST | `/{discordGuildId}/ack` |

## Naming conventions

| Element | Convention | Example |
|---------|------------|---------|
| Route prefix | kebab-case plural | `/permission-roles` |
| Route params | camelCase in docs; `{id:guid}` in code | `{guildId}` |
| Controller | `{Name}Controller` | `GuildsController` |
| DTO | `{Name}Dto`, `{Action}Request` | `GuildAccessDto` |
| Query params | camelCase | `?targetUserId=` |

## Versioning strategy

**Current:** implicit v1, no header negotiation.

**Recommended future:**

- Introduce `/api/v1/` when external integrations exist
- Maintain backward compat for bot client during rollout
- Document breaking changes in `changelog.md`

## Pagination

**Not implemented.** List endpoints use hard limits (e.g. logs: 200 entries). Future: cursor-based pagination for tickets, logs, admin guild list.

## Related docs

- `authentication.md`, `authorization.md`
- `backend-architecture.md`
