# PERF-003 — Lazy Loading Phase 2

**Date:** 2026-07-03  
**Task ID:** PERF-003  
**Status:** Complete  
**Parent:** PERF-002  
**Scope:** Lazy-load Settings, Subscription, Staff, Reaction Roles

---

## Summary

Moved four large feature areas out of `AppModule` into lazy-loaded feature modules. Initial bundle dropped from **1.03 MB → 806.93 KB raw** (main **964 KB → 719.58 KB**). The **1 MB hard budget now passes** (was failing by 27 KB after Phase 1).

No UI, URL, API, permission, or business logic changes.

---

## Modules created

| Module | Routing module | Components |
|--------|----------------|------------|
| `SettingsModule` | `SettingsRoutingModule` | `SettingsComponent`, `WelcomeEditorComponent`, `WelcomeDiscordPreviewComponent`, `WelcomeTestSectionComponent`, `AutoRoleEditorComponent`, `AutoRoleAssignmentPreviewComponent`, `AutoRoleNotesComponent` |
| `SubscriptionModule` | `SubscriptionRoutingModule` | `SubscriptionComponent`, `SubscriptionChangeFlowComponent`, `SubscriptionHistoryComponent`, `SubscriptionPaymentInstructionsComponent` |
| `StaffModule` | `StaffRoutingModule` | `StaffComponent`, `StaffFilterBarComponent`, `StaffRoleCardComponent`, `StaffRoleEditorComponent`, `StaffDetailPanelComponent` |
| `ReactionRolesModule` | `ReactionRolesRoutingModule` | `ReactionRolesComponent`, `ReactionRolesFilterBarComponent`, `ReactionRolesPanelCardComponent`, `ReactionRolesDetailPanelComponent` |

All lazy modules import `SharedUiModule` only — no shared components redeclared.

---

## Routes lazy-loaded (URLs unchanged)

| URL | Lazy module | Guard | Data |
|-----|-------------|-------|------|
| `/guilds/:id/settings` | `SettingsModule` | `GuildAccessGuard` | `owner` |
| `/guilds/:id/subscription` | `SubscriptionModule` | `GuildAccessGuard` | `owner` |
| `/guilds/:id/staff` | `StaffModule` | `GuildAccessGuard` | `owner` |
| `/guilds/:id/reaction-roles` | `ReactionRolesModule` | `GuildAccessGuard` | `owner` |

Settings tab behavior (`activeTab`, welcome/auto-role workspaces, command panel, logs settings) remains in `SettingsComponent` — no query-param routing was present before; unchanged.

---

## AppModule declarations removed

| Feature | Removed |
|---------|--------:|
| Settings + welcome + auto-role | 7 |
| Subscription | 4 |
| Staff | 5 |
| Reaction roles | 4 |
| **Phase 2 total** | **20** |

| Metric | PERF-002 end | PERF-003 end |
|--------|-------------:|-------------:|
| `AppModule` declarations | 47 | **27** |
| Lazy feature modules | 2 | **6** |

---

## Bundle sizes — incremental builds

### Baseline (PERF-002 end, before Phase 2)

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 964.04 KB | 182.97 KB |
| **Initial total** | **1.03 MB** | **203.51 KB** |
| Lazy chunks | 2 (admin, transcript) | — |

### After SettingsModule

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 867.21 KB | 171.24 KB |
| **Initial total** | **954.45 KB** | **191.80 KB** |
| New lazy | `features-settings-settings-module` — **98.40 KB** | 15.65 KB |

### After SubscriptionModule

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 814.16 KB | 164.16 KB |
| **Initial total** | **901.43 KB** | **184.74 KB** |
| New lazy | `features-subscription-subscription-module` — **53.89 KB** | 9.94 KB |

### After StaffModule + ReactionRolesModule (final)

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | **719.58 KB** | **153.41 KB** |
| `styles.*.css` | 51.48 KB | 8.60 KB |
| `polyfills.*.js` | 33.05 KB | 10.67 KB |
| `runtime.*.js` | 2.83 KB | 1.35 KB |
| **Initial total** | **806.93 KB** | **174.02 KB** |

**Lazy chunks (all):**

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `features-settings-settings-module` | 98.40 KB | 15.65 KB |
| `features-staff-staff-module` | 62.10 KB | 11.27 KB |
| `features-subscription-subscription-module` | 53.89 KB | 9.94 KB |
| `features-admin-admin-module` | 46.35 KB | 7.60 KB |
| `features-reaction-roles-reaction-roles-module` | 33.20 KB | 7.02 KB |
| `features-tickets-ticket-transcript-module` | 10.82 KB | 2.81 KB |
| `common` (shared lazy) | 964 B | 519 B |

### Phase 2 impact on initial bundle

| Metric | Before Phase 2 | After Phase 2 | Change |
|--------|---------------:|--------------:|-------:|
| `main.*.js` | 964.04 KB | 719.58 KB | **−244.46 KB (−25%)** |
| Initial total | 1.03 MB | 806.93 KB | **−244 KB (−24%)** |
| Initial transfer | 203.51 KB | 174.02 KB | **−29.5 KB** |
| 1 MB budget | **Fail (+27 KB)** | **Pass (−217 KB headroom)** | Fixed |

---

## Routes affected

**None** — all paths, guards, and `data.guildAccess` values preserved. Only loading mechanism changed from eager `component:` to `loadChildren:`.

---

## Issues

| Issue | Status |
|-------|--------|
| Circular dependencies | **None** — lazy modules import `SharedUiModule` only; core services remain `providedIn: 'root'` |
| Compile errors | **None** |
| `settings.component.css` budget warning | Pre-existing (6.46 KB > 6 KB warning); unchanged |
| Initial 550 KB warning budget | Still exceeded (807 KB vs 550 KB warning) — hard 1 MB limit now passes |

---

## Files changed

| File | Action |
|------|--------|
| `src/app/features/settings/settings.module.ts` | Created |
| `src/app/features/settings/settings-routing.module.ts` | Created |
| `src/app/features/subscription/subscription.module.ts` | Created |
| `src/app/features/subscription/subscription-routing.module.ts` | Created |
| `src/app/features/staff/staff.module.ts` | Created |
| `src/app/features/staff/staff-routing.module.ts` | Created |
| `src/app/features/reaction-roles/reaction-roles.module.ts` | Created |
| `src/app/features/reaction-roles/reaction-roles-routing.module.ts` | Created |
| `src/app/app.module.ts` | Removed 20 declarations |
| `src/app/app-routing.module.ts` | 4 routes → `loadChildren` |

---

## Validation

```bash
cd dashboard/DiscordBot.Dashboard && npm run build
```

- Compile: **pass**
- Assets copy: **pass** (i18n assets now included in dist)
- Initial 1 MB budget: **pass**
- Lazy chunks: **6 feature modules + 1 common chunk**

---

## Not in scope (deferred)

- Lazy-load Tickets, Logs, Moderation, Profile, Modules, Overview
- i18n splitting
- CSS cleanup

---

*Phase 2 complete. Stopped before Phase 3 per instruction.*
