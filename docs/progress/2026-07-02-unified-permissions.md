# Summary

Unified the Discord Bot Platform permission model into a single role-based system backed by `GuildPermissionRoles` and the `GuildPermissions` flags enum. Removed legacy `GuildStaff` (user-based) and `ModerationPermissionRoles` (boolean columns). Bot moderation evaluation, dashboard access, and both dashboard UIs now read/write the same data. Existing moderation role data is migrated into `GuildPermissionRoles` without changing command behavior.

---

# Objective

Replace three overlapping permission concepts (`GuildStaff`, `GuildPermissionRoles`, `ModerationPermissionRoles`) with one clear Discord role-based model that supports dashboard access, ticket permissions, moderation commands, logs, and reaction roles — while preserving existing behavior and not losing data.

---

# Architecture Decisions

## 1. Extend `GuildPermissions` instead of creating a fourth model

**Decision:** Keep `GuildPermissionRole` as the single entity and expand the `[Flags]` enum.

**Why:** Task requirement explicitly preferred `GuildPermissionRoles`. Bit positions 0–7 were preserved so existing `GuildPermissionRoles.Permissions` integers remain valid (legacy names like `AccessModeration` map to `ManageModeration` at the same bit).

**Alternatives considered:**
- Separate bot vs dashboard tables — rejected (continues duplication).
- User-based `GuildStaff` migration — rejected (no role mapping possible; table was unused by resolvers).

## 2. Single resolver (`GuildPermissionResolver`) for bot and dashboard

**Decision:** Removed `ModerationPermissionResolver`. Bot `POST .../permissions/evaluate` now uses `GuildPermissionResolver` + `GuildPermissionMapper.ToEvaluatePermissionsResponse`.

**Why:** One OR-merge path over matched Discord roles. Same owner/platform-admin bypass.

**Behavior preserved:** Bot `CanAccessModeration` is true when any bot moderation flag is set (same as before). `ManageModeration` dashboard flag also grants `CanViewWarnings` / `CanViewModerationCases` in bot responses (mirrors old dashboard cross-access).

## 3. Data migration via SQL before dropping tables

**Decision:** Migration `20260702151245_UnifyGuildPermissions` OR-merges `ModerationPermissionRoles` boolean columns into `GuildPermissionRoles.Permissions`, inserts missing rows, then drops legacy tables.

**Why:** No data loss for configured moderation roles. `GuildStaff` rows are dropped — they were never used by permission evaluation.

## 4. Dashboard: two pages, one backend

**Decision:** Staff page and Moderation Settings page remain separate (no redesign). Moderation Settings reads/writes `/api/guilds/{id}/permission-roles` through `GuildService` adapter methods that merge permission keys.

**Why:** Preserves UX while eliminating duplicate backend models.

## 5. Legacy permission key aliases

**Decision:** Backend `ParsePermissions` and frontend `normalizePermissionKeys` accept old names (`AccessModeration`, `Warn`, etc.).

**Why:** Backward compatibility for API clients and stored permission key strings during transition.

---

# Files Changed

| Path | Purpose | Description |
|------|---------|-------------|
| `src/DiscordBot.Domain/Enums/GuildPermissions.cs` | Domain | Expanded flags enum with all required permissions; preserved bits 0–7 |
| `src/DiscordBot.Domain/Constants/GuildPermissionDefaults.cs` | Domain | Owner bitmask includes all flags |
| `src/DiscordBot.Infrastructure/Services/GuildPermissionResolver.cs` | Infrastructure | Enhanced `GuildPermissionMapper` with bot evaluate mapping + access helpers |
| `src/DiscordBot.Infrastructure/Services/GuildPermissionRoleService.cs` | Infrastructure | Legacy permission key alias parsing |
| `src/DiscordBot.Infrastructure/Services/LogService.cs` | Infrastructure | Clear logs allows `ClearLogs` flag (not only owner) |
| `src/DiscordBot.Infrastructure/Models/StaffDtos.cs` | Infrastructure | Removed legacy staff DTOs; added `CanClearLogs` to `GuildAccessDto` |
| `src/DiscordBot.Infrastructure/Data/AppDbContext.cs` | Infrastructure | Removed `GuildStaff` and `ModerationPermissionRoles` DbSets |
| `src/DiscordBot.Infrastructure/DependencyInjection.cs` | Infrastructure | Removed legacy service registrations |
| `src/DiscordBot.Api/Controllers/GuildsController.cs` | API | Removed `/staff` and `/moderation/permission-roles` endpoints |
| `src/DiscordBot.Api/Controllers/BotGuildsController.cs` | API | Bot evaluate uses unified resolver/mapper |
| `src/DiscordBot.Infrastructure/Migrations/20260702151245_UnifyGuildPermissions.cs` | Migration | Data merge SQL + drop legacy tables |
| `src/DiscordBot.Infrastructure/Migrations/20260702151245_UnifyGuildPermissions.Designer.cs` | Migration | EF snapshot update |
| `src/DiscordBot.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Migration | EF snapshot update |
| `dashboard/.../core/models/staff.models.ts` | Dashboard | Full permission key list + helpers |
| `dashboard/.../core/models/guild.models.ts` | Dashboard | Optional `name` on moderation create payload |
| `dashboard/.../core/services/guild.service.ts` | Dashboard | Moderation CRUD adapts to `/permission-roles` |
| `dashboard/.../features/staff/staff.component.ts` | Dashboard | Dynamic permission options + legacy key display |
| `dashboard/.../features/moderation-settings/moderation-settings.component.ts` | Dashboard | Passes Discord role name on create |
| `dashboard/.../assets/i18n/en.json` | Dashboard | Updated staff permission labels |
| `dashboard/.../assets/i18n/ar.json` | Dashboard | Updated staff permission labels (Arabic) |

### Deleted files

| Path | Reason |
|------|--------|
| `src/DiscordBot.Domain/Entities/GuildStaff.cs` | Legacy user-based staff |
| `src/DiscordBot.Domain/Entities/ModerationPermissionRole.cs` | Merged into `GuildPermissionRole` |
| `src/DiscordBot.Domain/Enums/GuildStaffRole.cs` | Only used by `GuildStaff` |
| `src/DiscordBot.Infrastructure/Services/GuildStaffService.cs` | Removed with entity |
| `src/DiscordBot.Infrastructure/Services/ModerationPermissionRoleService.cs` | Removed with entity |
| `src/DiscordBot.Infrastructure/Services/ModerationPermissionResolver.cs` | Merged into `GuildPermissionResolver` |
| `src/DiscordBot.Infrastructure/Data/Configurations/GuildStaffConfiguration.cs` | Removed with entity |
| `src/DiscordBot.Infrastructure/Data/Configurations/ModerationPermissionRoleConfiguration.cs` | Removed with entity |
| `src/DiscordBot.Infrastructure/Models/ModerationPermissionRoleDtos.cs` | Removed with API endpoints |

---

# Database Changes

## Entity changes

- **Removed:** `GuildStaff`, `ModerationPermissionRole`
- **Unchanged table:** `GuildPermissionRoles` (same schema; `Permissions` column stores expanded bitmask)

## Table changes

- **Dropped:** `GuildStaff`
- **Dropped:** `ModerationPermissionRoles`

## Column changes

None on remaining tables. `GuildPermissionRoles.Permissions` integer values at bits 0–7 unchanged semantically.

## Relationships

Removed FK relationships from dropped tables to `Guilds`.

## Indexes

Removed:
- `IX_GuildStaff_GuildId_DiscordUserId`
- `IX_ModerationPermissionRoles_GuildId_RoleDiscordId`

Retained:
- `IX_GuildPermissionRoles_GuildId_DiscordRoleId`

## Migration name

`20260702151245_UnifyGuildPermissions`

### Data migration mapping (ModerationPermissionRoles → GuildPermissions bits)

| Old column | New flag | Bit value |
|------------|----------|-----------|
| `CanWarn` | `UseWarn` | 1 |
| `CanKick` | `UseKick` | 2 |
| `CanClearMessages` | `UseClearMessages` | 8 |
| `CanViewLogs` | `ViewLogs` | 32 |
| `CanViewWarnings` | `ViewWarnings` | 262144 |
| `CanViewModerationCases` | `ViewModerationCases` | 524288 |

---

# API Changes

## Removed endpoints

| Method | Route |
|--------|-------|
| GET | `/api/guilds/{id}/staff` |
| POST | `/api/guilds/{id}/staff` |
| DELETE | `/api/guilds/{id}/staff/{staffId}` |
| GET | `/api/guilds/{id}/moderation/permission-roles` |
| POST | `/api/guilds/{id}/moderation/permission-roles` |
| PUT | `/api/guilds/{id}/moderation/permission-roles/{roleId}` |
| DELETE | `/api/guilds/{id}/moderation/permission-roles/{roleId}` |

## Updated endpoints

| Method | Route | Change |
|--------|-------|--------|
| GET | `/api/guilds/{id}/access` | Returns expanded `GuildAccessDto` with `canClearLogs` |
| GET/POST/PUT/DELETE | `/api/guilds/{id}/permission-roles` | Accepts expanded permission keys (legacy aliases supported) |
| POST | `/api/bot/guilds/{discordGuildId}/permissions/evaluate` | Uses unified resolver (response shape unchanged) |
| POST | `/api/bot/guilds/{discordGuildId}/dashboard-access/evaluate` | Unchanged route; unified resolver source |
| DELETE | `/api/guilds/{id}/logs` | Also allows users with `ClearLogs` permission |

## New endpoints

None.

---

# Bot Changes

## Permission resolution

- `POST /api/bot/guilds/{id}/permissions/evaluate` now resolves via `GuildPermissionResolver` + `GuildPermissionMapper.ToEvaluatePermissionsResponse`.
- Removed dependency on `ModerationPermissionResolver`.

## Updated handlers

- **No handler logic changes.** `ModerationCommandHandlers` and `TicketCommandHandlers` still call the same API client methods and check the same response fields.

## Command behavior

Unchanged. Only the backend resolution path changed.

---

# Dashboard Changes

## Staff page (`/guilds/:id/staff`)

- Expanded permission checkboxes to full unified list (20 flags).
- Legacy permission keys displayed via `normalizePermissionKeys`.
- Updated help text: bot and dashboard permissions share one model.

## Moderation Settings page (`/guilds/:id/moderation/settings`)

- UI unchanged (boolean toggles per bot command).
- Backend calls adapted in `GuildService` to read/write `/permission-roles`.
- Updates merge moderation keys without removing dashboard keys on the same role.

## Services

- `GuildService.getModerationPermissionRoles` → filters/maps from `getStaff`.
- Create/update/delete moderation roles → permission-roles API with key merge.

## Guards

- `GuildAccessGuard` unchanged (still uses `canManageSettings` / `canAccessModeration` from `/access`).

## UI changes

- Additional permission labels in staff form (en + ar).
- No layout redesign.

---

# Breaking Changes

| Change | Impact | Migration |
|--------|--------|-----------|
| Removed `/api/guilds/{id}/staff` | External clients using user-based staff API lose access | Use `/permission-roles` with Discord roles |
| Removed `/api/guilds/{id}/moderation/permission-roles` | Direct API consumers must use `/permission-roles` | Map boolean flags to permission keys |
| Dropped `GuildStaff` table | User-based staff records deleted | Re-create as role mappings if needed |
| Renamed enum flags in API strings | `AccessModeration` → `ManageModeration`, etc. | Legacy aliases accepted on input; output uses new names |

**Dashboard:** Updated in this task — no action needed for deployed frontend after redeploy.

**Bot:** No changes required — same evaluate endpoints and response fields.

---

# Validation Performed

| Check | Result |
|-------|--------|
| API build | ✅ `dotnet build src/DiscordBot.Api/DiscordBot.Api.csproj` succeeded |
| Bot build | ✅ `dotnet build src/DiscordBot.Bot/DiscordBot.Bot.csproj` succeeded |
| Dashboard build | ✅ `npm run build` succeeded (bundle size budget warning pre-existing) |
| Migration generation | ✅ `dotnet ef migrations add UnifyGuildPermissions` |
| Migration apply | ⚠️ Not run against a live database in this session |
| Manual end-to-end | ⚠️ Not run (requires running API + DB + Discord bot) |

---

# Risks

1. **Migration on production** — Must run `20260702151245_UnifyGuildPermissions` before deploying code that removed DbSets; otherwise startup may fail if code expects dropped tables (code no longer references them, so deploy order: migrate first, then deploy).
2. **`GuildStaff` data loss** — Any rows in `GuildStaff` are permanently removed. They were not used by permission evaluation, but external integrations may have stored data there.
3. **Dual UI editing same role** — Staff and Moderation Settings pages can edit the same role; merge logic prevents overwriting, but concurrent edits could race.
4. **`ManagePermissionRoles` still not fully enforced** — Flag exists but `CanManageStaff` remains owner/platform-admin only (preserved prior behavior).
5. **Granular new flags not wired everywhere** — e.g. `ManageSettings`, `ReplyToTickets` exist in enum/UI but dashboard guards still use coarse `canManageSettings` / `canAccessModeration` (same effective behavior as before for typical roles).

---

# Follow-up Tasks

1. **P1 — Enforce granular dashboard guards** — Map routes to specific flags (`ViewLogs`, `ManageSettings`, etc.) instead of owner/moderation binary.
2. **P1 — Apply migration to staging/production** — Verify merged moderation roles behave correctly in Discord.
3. **P2 — Consolidate dashboard UI** — Optional single “Roles & permissions” editor with grouped sections (reduce dual-page confusion).
4. **P2 — Enforce `ManagePermissionRoles`** — Allow delegated staff to manage permission roles when flag is set.
5. **P3 — Remove legacy permission key aliases** — After transition period, drop `AccessModeration`/`Warn` parsing.
6. **P3 — Implement `/ban` and `/timeout`** — Flags exist (`UseBan`, `UseTimeout`) but commands not implemented (per project scope).

---

# Technical Debt

- Moderation Settings page uses a view-model adapter (`ModerationPermissionRole`) over unified roles — extra mapping layer in `GuildService`.
- `GuildAccessDto` still exposes owner-only bot flags (`canWarn`, etc.) for dashboard even though bot uses separate evaluate endpoint.
- Permission enum growth may eventually need grouping/documentation in API OpenAPI schema.
- EF tools version (8.0.10) older than runtime (9.0.4) — warning during migration scaffold.

---

# Developer Notes

## Permission evaluation flow (after unification)

```
Discord role IDs (synced or live)
    → GuildPermissionRoles (OR-merge Permissions flags)
    → GuildPermissionMapper
        → GuildAccessDto (dashboard)
        → EvaluatePermissionsResponse (bot commands)
```

## Adding a new permission

1. Add flag to `GuildPermissions` enum (next bit).
2. Add to `GuildPermissionDefaults.OwnerPermissions`.
3. Map in `GuildPermissionMapper` (access + bot evaluate as needed).
4. Add to `GUILD_PERMISSION_OPTIONS` in `staff.models.ts` + i18n labels.
5. No new table or entity required.

## Legacy key compatibility

Backend accepts: `Warn`, `Kick`, `Timeout`, `ClearMessages`, `AccessModeration`, `AccessLogs`, `AccessTickets`.

Frontend `normalizePermissionKeys` maps the same aliases for display.

## Deploy checklist

1. `dotnet ef database update` (apply `UnifyGuildPermissions`)
2. Deploy API
3. Deploy Bot (no config change required)
4. Deploy Dashboard
