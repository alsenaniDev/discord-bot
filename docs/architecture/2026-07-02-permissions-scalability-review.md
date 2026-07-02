# Architecture Review — Guild Permissions Scalability

**Date:** 2026-07-02  
**Scope:** Post-unification permission system (`GuildPermissionRoles` + `GuildPermissions` flags enum)  
**Context:** Commercial SaaS Discord bot platform with module catalog, subscription tiers, dashboard, and bot command authorization  
**Review type:** Read-only — no code changes

---

## 1. Current Design Summary

The platform now uses **one authorization model** centered on Discord roles.

### Data model

| Concept | Implementation |
|---------|----------------|
| Role assignment | `GuildPermissionRole` — maps a Discord role (`DiscordRoleId`) to a permission set |
| Permission storage | Single `Permissions` column: `GuildPermissions` `[Flags]` enum stored as `int` bitmask |
| Role membership | `DiscordGuildMembers.DiscordRoleIdsJson` (synced by bot) or live role IDs passed at bot evaluate time |
| Resolution | `GuildPermissionResolver` loads matched rows, OR-merges bitmasks, returns `ResolvedGuildPermissions` |
| Dashboard access | `GuildPermissionMapper.ToAccessDto` → coarse booleans (`CanManageSettings`, `CanAccessModeration`, etc.) |
| Bot commands | `GuildPermissionMapper.ToEvaluatePermissionsResponse` → command-specific booleans |
| Owner bypass | `GuildPermissionDefaults.OwnerPermissions` full bitmask |
| Module enablement | **Separate system** — `Module` / `GuildModule` toggles whether a feature exists for a guild, not who may use it |

### Removed concepts

- **`GuildStaff`** — user-based staff list (`DiscordUserId` + `GuildStaffRole`). Dropped; was never wired into resolvers.
- **`ModerationPermissionRoles`** — boolean columns per Discord role. Merged into `GuildPermissionRoles` via migration.

### API surface

- Dashboard CRUD: `GET/POST/PUT/DELETE /api/guilds/{id}/permission-roles`
- Access check: `GET /api/guilds/{id}/access`
- Bot evaluate: `POST /api/bot/guilds/{id}/permissions/evaluate`, `POST .../dashboard-access/evaluate`

### Dashboard UX

- **Staff page** — flat checklist of 20 hardcoded permission keys
- **Moderation Settings page** — separate UI writing the same backend via adapter/merge logic in `GuildService`

### Current permission count

20 flags, bits 0–19 used. **12 bits remain** in a 32-bit integer before the storage type becomes a hard ceiling.

---

## 2. Strengths

### Unified mental model

One table, one resolver, one merge rule. This eliminates the previous split where dashboard staff and bot moderation were configured independently and could drift.

### Discord-native assignment

Mapping permissions to Discord roles matches how Discord servers actually operate. Most guild admins already think in terms of `@Moderator`, `@Support`, etc.

### Correct separation from module toggles

`GuildModule` answers *“Is tickets enabled for this guild?”* Permissions answer *“Can this user use tickets?”* Keeping these separate is the right boundary for a modular SaaS product.

### Efficient read path (today)

Resolution is a small number of indexed queries:

1. Guild lookup
2. Optional platform-admin check
3. Member role IDs (or use live IDs from bot)
4. `GuildPermissionRoles WHERE GuildId AND DiscordRoleId IN (...)`

OR-merge in memory is O(matched roles). At current scale this is fine.

### Migration-friendly short term

Preserving bits 0–7 and accepting legacy key aliases (`AccessModeration`, `Warn`) reduces breakage during rollout.

### Bot/dashboard parity

Both paths use the same resolver. Command behavior stays consistent with dashboard configuration.

---

## 3. Weaknesses

### 1. `[Flags]` enum on `int` is a hard scalability ceiling

`GuildPermissions` is stored as an `int`. With 20 flags consumed, **~12 remain**. A platform targeting 100+ permissions cannot grow inside this column without:

- Switching to `long` (kicks the can down the road), or
- Redesigning storage entirely

Every new permission also requires:

- C# enum change
- Mapper updates (`HasModerationPageAccess`, `ToAccessDto`, etc.)
- Angular `GUILD_PERMISSION_OPTIONS` hardcoded list
- i18n strings (en + ar)
- API DTO understanding

This is **deploy-coupled permission growth**, not data-driven growth.

### 2. Mapper logic is becoming a policy engine

`GuildPermissionMapper` encodes cross-cutting rules that will not scale:

- `HasModerationPageAccess` grants ticket page access when any ticket/log/moderation flag is set
- `ManageModeration` implicitly grants `CanViewWarnings` / `CanViewModerationCases` in bot evaluate
- `CanAccessOverview`, `CanManageSettings`, `CanManageStaff` are still **owner-only** in practice despite granular flags existing

As modules multiply, this file becomes a brittle tangle of special cases. There is no single “check permission X” primitive used everywhere.

### 3. No module namespace in the permission model

Permissions are flat (`UseWarn`, `ViewTickets`). The platform already has `Module.Key` (`tickets`, `moderation`, `logs`, …) but permissions are not grouped or namespaced under modules in storage. Future modules (analytics, automation, marketplace plugins) will produce naming collisions and UI clutter unless disciplined conventions are enforced manually.

### 4. Discord-role-only assignment limits advanced SaaS scenarios

These are **not supported** by the current model:

| Scenario | Problem |
|----------|---------|
| Ticket team per queue/category | Requires a Discord role per team or per queue |
| User-specific override (“Alice can close tickets this week”) | No user-level grant layer |
| Staff profile (display title, team, availability) | `GuildStaff` removed; no replacement |
| Moderation team separate from support team with overlapping members | Discord role explosion |
| Platform-managed staff without Discord role changes | Impossible |

For a commercial product, **ticket teams and staff profiles are eventually required**. Removing `GuildStaff` without replacing it with a profile/team concept removes a hook those features need.

### 5. Dashboard UX does not scale to 100+ permissions

The Staff page renders a flat checkbox grid from a static TypeScript array. At 100 permissions this becomes unusable without grouping, search, presets/templates, and module-scoped tabs.

The dual-page adapter (Staff + Moderation Settings merging keys client-side) adds **merge race risk** and maintenance cost as permissions grow.

### 6. Bot evaluation requires HTTP round-trip per check

Handlers call `BotApiClient.EvaluatePermissionsAsync` at command time. At high message volume across many guilds, this creates:

- Latency on every slash command
- API load proportional to Discord activity
- No documented caching layer in bot or API

Efficient at 100 guilds; questionable at 100,000 guilds without caching.

### 7. No permission catalog / plugin extensibility

Marketplace or plugin permissions cannot be registered at runtime. There is no `PermissionDefinitions` table. Third-party modules would require core code changes — unacceptable for a plugin marketplace.

### 8. Granular flags exist but guards don’t use them

The enum includes `ManageSettings`, `ViewServer`, `ClearLogs`, etc., but `GuildAccessGuard` still uses coarse `canManageSettings` vs `canAccessModeration`. Most API services call `CanAccessModerationPagesAsync` for logs, tickets, and moderation alike.

The unified model **looks granular** but **behaves coarsely**. This will confuse admins who grant `ViewLogs` only and still get ticket page access via mapper side effects.

### 9. No audit trail

Permission role changes are not logged as structured audit events (who changed which role, when, before/after). Required for enterprise SaaS compliance.

---

## 4. Scalability Assessment

### Will it support 100+ permissions?

**No — not in its current storage and delivery shape.**

| Dimension | 20 permissions (today) | 100+ permissions (target) |
|-----------|------------------------|---------------------------|
| DB storage (`int` bitmask) | Fits | **Exceeds 32-bit capacity** |
| Adding permissions | Code + i18n + dashboard deploy | **Unacceptable cadence for plugins** |
| Dashboard UI | Tolerable flat list | **Unusable without redesign** |
| Mapper maintenance | Manageable | **Unmaintainable policy soup** |
| Bot evaluation | OK with API call | **Needs cache + possibly local snapshot** |
| Ticket teams / staff profiles | Not required yet | **Requires additional entities** |
| Marketplace plugins | Not supported | **Requires permission catalog** |

### Will it support 20+ modules?

**Partially.**

Module *enablement* scales via existing `Module` / `GuildModule` tables. Module *authorization* does not — each module’s permissions must be bolted onto the enum and mapper manually.

At ~20 modules × ~5 permissions/module = 100 permissions, the current design breaks.

### Verdict on scalability

The unification **fixed duplication** but chose a **Phase-1 storage model** (bitmask enum) that is appropriate for ~30–40 permissions maximum, not a multi-year SaaS roadmap.

---

## 5. Staff vs Permissions

These are **different concerns** and should not be conflated or fully merged.

| Concept | What it is | Should it exist? |
|---------|------------|------------------|
| **Discord role** | Native Discord authorization primitive; source of membership | Yes — primary assignment mechanism |
| **Permission role** (`GuildPermissionRole`) | Platform mapping: Discord role → capability set | Yes — core authorization config |
| **Staff profile** | Platform record about a person: display name, team, notes, status, shift | Yes — **separate from permissions** |
| **Module-specific assignment** | Scoped grant: “on ticket team A”, “moderation reviewer”, “analytics viewer” | Yes — likely needs teams/queues, not just global role flags |
| **Permission (capability)** | Atomic action: `tickets.reply`, `moderation.warn` | Yes — should be catalog-driven |

### Should `GuildStaff` have been removed?

**Removing it as a permission source was correct.** It was unused by resolvers and duplicated the role model poorly.

**Removing it entirely as a domain concept was premature.** The name “staff” conflated two things:

1. **Authorization** — who can do what → belongs in permission roles
2. **Identity / roster** — who is on the team → belongs in a staff member profile

For ticket teams, you eventually need:

```
GuildStaffMember (profile)
  └── team memberships, user id, optional linked Discord role
GuildPermissionRole (authorization)
  └── Discord role → permissions
GuildTicketTeam (future)
  └── team → members and/or roles → queue scope
```

**Recommendation:** Reintroduce a **`GuildStaffMember`** (or `GuildMemberProfile`) entity for roster/team metadata. Do **not** use it as the primary permission store. Use it for assignments that Discord roles alone cannot express.

### Should `ModerationPermissionRoles` have been removed?

**Yes as a separate table.** It duplicated `GuildPermissionRoles` with a different schema.

**No as a conceptual grouping.** Moderation permissions should remain **module-scoped in the catalog**, not a separate physical table. The mistake was duplicate storage, not module grouping itself.

---

## 6. Recommended Architecture

### Design principles

1. **Authorization** — role-based, Discord-first, catalog-driven string keys  
2. **Module enablement** — keep existing `Module` / `GuildModule`  
3. **Staff/teams** — separate roster entities for people and team assignments  
4. **Evaluation** — resolve once, cache, check many  
5. **Plugins** — register permissions in catalog without DB migrations per permission  

### Recommended tables

```
ModuleDefinitions          (existing Module — add metadata: icon, sort order, isPlugin)
PermissionDefinitions      (catalog — NEW)
  Id, Key (unique: "tickets.reply"), ModuleId, Name, Description,
  Scope (Dashboard | Bot | Both), IsDeprecated, SortOrder

GuildPermissionRoles       (keep — assignment header)
  Id, GuildId, Name, DiscordRoleId, CreatedAt

GuildRolePermissions       (normalized grants — NEW, replaces int bitmask)
  GuildPermissionRoleId, PermissionDefinitionId
  PK (GuildPermissionRoleId, PermissionDefinitionId)

GuildStaffMembers          (roster profile — NEW, not authorization source)
  Id, GuildId, DiscordUserId, DisplayTitle, IsActive, Notes

GuildStaffTeamMembers      (optional — NEW, for ticket/moderation teams)
  TeamId, StaffMemberId

GuildTicketTeams           (optional — future)
  Id, GuildId, Name, CategoryFilter, ...

GuildPermissionOverrides   (optional — advanced)
  Id, GuildId, DiscordUserId, PermissionDefinitionId, ExpiresAt
  (explicit user grants when Discord roles are insufficient)
```

### Why hybrid (catalog + normalized junction) over enum flags

| Approach | Pros | Cons | Fit for 100+ perms |
|----------|------|------|---------------------|
| Enum flags (`int`) | Fast bitwise OR | 32-bit limit, deploy-coupled | **Poor** |
| String keys in JSON column | Flexible | Hard to query/index, merge logic messy | Mediocre |
| Normalized junction rows | Queryable, plugin-friendly, no bit limit | More rows, slightly more join cost | **Good** |
| Pure string keys in code only | Simple | No admin UI introspection | Poor for SaaS |

**Recommended storage:** `GuildRolePermissions` junction referencing `PermissionDefinitions`.

Keep an optional **resolved snapshot cache** (Redis or in-memory) keyed by `(GuildId, DiscordUserId)` → `HashSet<string>` permission keys for bot/API hot paths.

### Permission key convention

Use module-scoped keys aligned with existing `Module.Key`:

```
dashboard.view
dashboard.manage_settings
tickets.view
tickets.reply
tickets.close
moderation.warn
moderation.kick
logs.view
logs.clear
reaction_roles.manage
analytics.export          (future)
automation.edit_workflows (future)
plugin.{pluginId}.{action} (marketplace)
```

### Resolver shape (target)

```
Resolve(guildId, discordUserId, liveRoleIds?)
  1. Owner / platform admin → all permissions from catalog
  2. Load matched GuildPermissionRoles by DiscordRoleId
  3. Join GuildRolePermissions → PermissionDefinitions
  4. Union permission keys (+ optional user overrides)
  5. Return ResolvedPermissions { Keys: HashSet<string>, ... }
```

Check becomes `keys.Contains("tickets.reply")` — no mapper special cases per module.

### Link permissions to module enablement

Before checking authorization, enforce:

```
moduleEnabled(guild, "tickets") AND hasPermission(user, "tickets.reply")
```

This keeps the existing module guard pattern and makes subscription tiers meaningful.

---

## 7. Migration Strategy

### Should we keep the current PR?

**Yes — merge it**, with explicit acknowledgment that it is **Phase 1 cleanup**, not the final architecture.

The PR correctly:

- Eliminates duplicate permission systems
- Unifies bot and dashboard resolution
- Migrates moderation role data

The PR incorrectly assumes (implicitly) that the enum can carry the long-term roadmap. It cannot.

### Modify before merge?

**Minor documentation-only changes at merge time.** No code rewrite required before merge if Phase 2 is scheduled immediately after.

Optional pre-merge (nice-to-have, not blocking):

- Add ADR comment in codebase: “Permissions stored as int bitmask is temporary; target is PermissionDefinitions + GuildRolePermissions”
- Fix misleading granular flags vs coarse guards (follow-up, not merge blocker)

### Revert parts?

**Do not revert unification.** Reverting `ModerationPermissionRoles` or dual resolvers would restore known debt.

**Do consider reintroducing `GuildStaff` as `GuildStaffMember`** in Phase 2 — different schema and purpose from the deleted table.

### Follow-up migration (Phase 2 — recommended within next sprint)

1. Create `PermissionDefinitions` seeded from current enum values  
2. Create `GuildRolePermissions` junction  
3. Migration script: expand each `GuildPermissionRoles.Permissions` bitmask into junction rows  
4. Add `PermissionDefinitions` API for dashboard dynamic UI  
5. Switch resolver to key-based checks; keep reading bitmask during transition  
6. Drop `Permissions` int column after validation  
7. Add `GuildStaffMembers` for roster (no permission logic)  
8. Add Redis/in-memory cache for resolved permissions  

### Follow-up migration (Phase 3 — when ticket teams ship)

1. `GuildTicketTeams` + team ↔ role/member assignments  
2. Scoped permission checks (`tickets.reply` scoped to team queue)  
3. Dashboard team management UI  

---

## 8. Dashboard UX

### Current state

Flat 20-checkbox grid + separate moderation page with client-side merge. Works for early product; fails at scale.

### Recommended UX

**Single “Roles & Permissions” area** with module-grouped sections driven by `PermissionDefinitions` API:

```
┌─────────────────────────────────────────────────────┐
│ Role: Support Lead    Discord Role: @Support        │
├─────────────────────────────────────────────────────┤
│ ▼ Dashboard                                         │
│   ☐ Access dashboard   ☐ View overview              │
│   ☐ Manage settings    ☐ Manage modules             │
│ ▼ Tickets                                           │
│   ☐ View  ☐ Reply  ☐ Close  ☐ Manage configuration  │
│ ▼ Moderation                                        │
│   ☐ Dashboard access   ☐ /warn  ☐ /kick  ...      │
│ ▼ Logs                                              │
│   ☐ View  ☐ Clear                                   │
│ ▼ Reaction Roles                                    │
│   ☐ Manage panels                                   │
│ ▼ Analytics (future — hidden if module disabled)    │
└─────────────────────────────────────────────────────┘
```

### UX rules

| Rule | Reason |
|------|--------|
| Group by module | Matches admin mental model and subscription modules |
| Hide permissions for disabled modules | Avoid configuring access to unavailable features |
| Role templates (“Support”, “Moderator”, “Admin”) | Faster onboarding for guild owners |
| Search/filter when catalog > 30 | Required at scale |
| One editor, not two pages | Eliminates merge races from current dual UI |
| Show effective permissions preview | “Members with @Support can: …” |

Moderation Settings page should become a **deep link / filtered view** of the same editor (`?module=moderation`), not a separate data path.

---

## 9. Bot Evaluation Flow

### Current flow

```
Slash command
  → BotApiClient.EvaluatePermissionsAsync (HTTP)
    → GuildPermissionResolver (DB)
    → GuildPermissionMapper.ToEvaluatePermissionsResponse
  → Handler checks CanWarn, etc.
```

Every command pays network + DB cost. Mapper produces coarse booleans.

### Recommended flow

```
Slash command
  → Local PermissionCache.Get(guildId, userId, roleIdsHash)
      miss → HTTP batch evaluate OR sync snapshot
  → ResolvedPermissionSet (HashSet<string> keys)
  → handler: keys.Contains("moderation.warn")
```

### API endpoints (target)

| Endpoint | Purpose |
|----------|---------|
| `POST /api/bot/guilds/{id}/permissions/evaluate` | Return full key set (keep) |
| `GET /api/bot/guilds/{id}/permission-roles` | Optional: bot-side cache refresh on role sync |
| `POST /api/bot/guilds/{id}/permissions/check` | Optional: batch check `{ keys: [...] }` |

### Efficiency tactics

1. **Cache resolved keys** in bot (TTL 30–120s, invalidate on member/role update events)  
2. **Pass live role IDs** (already supported) — avoid stale synced member data  
3. **Prefetch on interaction autocomplete** for heavy commands  
4. **API-side cache** keyed by `(guildId, userId, rolesHash)`  
5. **Avoid DTO boolean explosion** — return keys, let bot check locally  

### Module guard stays separate

```
EnsureModuleEnabled("moderation")
AND EnsurePermission("moderation.warn")
AND EnsureDiscordNativePermission(KickMembers)  // where applicable
```

Native Discord permissions (`KickMembers`, `ManageRoles`) remain a third layer for actions Discord itself must authorize.

---

## 10. Final Decision

### **Approve with changes**

| Verdict | Detail |
|---------|--------|
| **Merge the unification PR** | Correct direction; removes real duplication and bugs |
| **Do not treat enum flags as the long-term model** | Plan Phase 2 migration to catalog + junction tables before adding more modules |
| **Reintroduce staff as profile/team concept** | Not as a permission system — as roster metadata for ticket/moderation teams |
| **Consolidate dashboard to one permission editor** | Module-grouped, API-driven from `PermissionDefinitions` |
| **Add caching before scale** | Bot permission evaluate will become a bottleneck |

### What would make this a reject?

If the team intended this PR to be the **final** permission architecture for a 100+ permission, plugin-enabled marketplace SaaS — it would warrant rejection and redesign first.

As **Phase 1 consolidation**, it is acceptable **provided Phase 2 is scheduled and funded** before shipping analytics, automation, or marketplace features.

### Priority follow-ups (ordered)

1. **P0** — ADR + Phase 2 plan: `PermissionDefinitions` + `GuildRolePermissions`  
2. **P1** — Bot/API permission cache  
3. **P1** — Dashboard single editor grouped by module (dynamic catalog)  
4. **P1** — Align guards/services to check specific keys, remove mapper cross-grants  
5. **P2** — `GuildStaffMembers` + ticket team model  
6. **P3** — User-level permission overrides (optional)  
7. **P3** — Permission change audit log  

---

## Appendix: Comparison to Current vs Target

| Aspect | Current (post-unification) | Target (SaaS-scale) |
|--------|---------------------------|---------------------|
| Storage | `int` bitmask enum | Normalized junction + catalog |
| Permission count | ~20, max ~32 in int | Unlimited |
| New permission cost | Code deploy | Seed row in catalog |
| Staff concept | Removed | Profile/team entity (separate) |
| Moderation roles table | Removed (good) | Module-scoped keys (good) |
| Dashboard | Static TS list | Dynamic from API |
| Bot check | HTTP + boolean DTO | Cached key set |
| Plugins | Not supported | `plugin.{id}.{action}` keys |
