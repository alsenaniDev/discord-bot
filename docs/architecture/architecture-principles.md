# Architecture Principles

Non-negotiable rules for this codebase. Future tasks must not violate these without an ADR.

---

## 1. Dependency direction

```
Dashboard → Api (HTTP only)
Bot → Api (HTTP only)
Api → Infrastructure → Domain
Bot → Domain (types only, no Infrastructure reference)
```

- **Domain** has zero external dependencies.
- **Bot never accesses PostgreSQL.** All persistence goes through API.
- **Dashboard never accesses PostgreSQL.** All data via REST.

## 2. Multi-tenant isolation

Every guild-scoped operation must:

1. Resolve the guild (by `Id` or `DiscordGuildId`)
2. Verify the caller has access (owner, staff role, platform admin, or bot key)
3. Filter queries with `GuildId`

Never return data from guild A when the request targets guild B.

## 3. Module enablement before authorization

Two-step check for features:

```
Is module enabled for guild? (GuildModule + subscription plan)
AND
Does user have permission? (GuildPermissionRoles)
```

Bot uses `ModuleGuard` before permission checks. Dashboard should hide or disable UI for modules not in plan (partially implemented).

## 4. Single permission model

Authorization is **Discord role → `GuildPermissionRoles` → `GuildPermissions` flags**.

Do not introduce:

- A parallel user-based staff permission table
- Separate bot-only permission tables
- Hardcoded permission checks bypassing the resolver

Phase 2 may change **storage** (catalog + junction tables) but not the **concept** (role-based, unified).

## 5. Owner and platform admin bypass

Guild owner (`Guild.OwnerDiscordUserId`) and platform admin receive full permissions via `GuildPermissionDefaults.OwnerPermissions`.

This bypass is centralized in `GuildPermissionResolver` — do not duplicate elsewhere.

## 6. Thin controllers, fat services

- Controllers: routing, auth attributes, input validation, HTTP status codes
- Services (`*Service.cs` in Infrastructure): business logic, EF queries, authorization checks
- No business logic in Bot command handlers beyond Discord-specific checks (native Discord permissions like `KickMembers`)

## 7. Bot native permissions are a third layer

For actions Discord requires natively (kick, manage roles):

```
Platform permission (API evaluate)
AND Discord.GuildPermissions (KickMembers, ManageRoles, etc.)
AND hierarchy checks (bot role position)
```

## 8. Configuration via options pattern

Use `IOptions<T>` classes in `Infrastructure/Options/` bound from configuration sections. Validate required settings at startup (`ValidateRequiredConfiguration` in API).

Secrets never committed — use `*.local.json` (gitignored) or environment variables.

## 9. EF Core as sole data access

- No Dapper or raw SQL in services except migrations
- Configurations in `Data/Configurations/`
- Migrations in `Infrastructure/Migrations/`
- `SaveChangesAsync` on `AppDbContext` sets `UpdatedAt` on modified `BaseEntity`

## 10. Explicit over clever

- No MediatR, no CQRS, no generic repository abstractions (unless ADR approved)
- Prefer readable service methods over deep inheritance
- DTOs for API boundaries (`Infrastructure/Models/`)
- **Read Models** for query surfaces — see `read-model-architecture.md` (AR-001). Dashboard and analytics consume Read Models, not aggregate graphs.

## 11. Documentation follows code

When architecture changes:

1. Update this handbook (relevant doc)
2. Add ADR if decision is significant
3. Add progress report for completed tasks

## 12. Internationalization

User-facing dashboard strings go through `@ngx-translate` — add keys to both `en.json` and `ar.json`.

## Assumptions

- **No event bus** — synchronous HTTP and polling workers are intentional for v1.
- **No Application layer project** — services in Infrastructure are the application layer.
- **JWT in localStorage** — acceptable for beta; consider httpOnly cookies for hardened production (future ADR).
