# PERF-002 — Lazy Loading Phase 1

**Date:** 2026-07-03  
**Task ID:** PERF-002  
**Status:** Complete  
**Parent:** PERF-001  
**Scope:** Admin lazy module, ticket transcript lazy module, SharedUiModule extraction

---

## Summary

Implemented Phase 1 lazy loading: admin routes and ticket transcript moved out of the eager `AppModule` bundle into lazy-loaded feature modules. Shared UI components extracted into `SharedUiModule` imported by both the app shell and lazy modules.

No UI, API, routing URLs, permissions, or business logic changes.

---

## Modules created

| Module | Path | Purpose |
|--------|------|---------|
| `SharedUiModule` | `src/app/shared/shared-ui.module.ts` | Shared UI declarations + exports |
| `AdminModule` | `src/app/features/admin/admin.module.ts` | 5 admin page components |
| `AdminRoutingModule` | `src/app/features/admin/admin-routing.module.ts` | Child routes under `/admin` |
| `TicketTranscriptModule` | `src/app/features/tickets/ticket-transcript.module.ts` | Transcript page |
| `TicketTranscriptRoutingModule` | `src/app/features/tickets/ticket-transcript-routing.module.ts` | Empty-path route to component |

---

## Routes lazy-loaded

| URL (unchanged) | Lazy module | Guard |
|-----------------|-------------|-------|
| `/admin` | `AdminModule` | `AdminGuard` |
| `/admin/guilds` | `AdminModule` | `AdminGuard` |
| `/admin/users` | `AdminModule` | `AdminGuard` |
| `/admin/upgrade-requests` | `AdminModule` | `AdminGuard` |
| `/admin/plans` | `AdminModule` | `AdminGuard` |
| `/guilds/:id/tickets/:ticketId/transcript` | `TicketTranscriptModule` | `GuildAccessGuard` (`moderation`) |

**Routing change:** Five flat admin routes replaced by one `loadChildren` entry at `path: 'admin'`. Child paths in `AdminRoutingModule` preserve exact URLs.

---

## SharedUiModule contents

Moved from `AppModule.declarations` into `SharedUiModule`:

- `UiIconComponent`
- `LanguageSwitcherComponent`
- `ProfileMenuComponent`
- `ServerSwitcherComponent`
- `BreadcrumbsComponent`
- `EmptyStateComponent`
- `LoadingStateComponent`
- `MemberSelectComponent`
- `PageWorkspaceHeroComponent`
- `SectionHeaderComponent`
- `StatusBadgeComponent`
- `ErrorStateComponent`
- `PageNoticeComponent`

**Also imports/exports:** `CommonModule`, `RouterModule`, `FormsModule`, `ReactiveFormsModule`, `TranslateModule`

**Remains in AppModule:** `ToastContainerComponent`, `OnboardingChecklistComponent` (import `SharedUiModule` for `UiIcon`)

---

## AppModule declarations removed

| Removed | Count |
|---------|------:|
| Admin components | 5 |
| `TicketTranscriptComponent` | 1 |
| Shared UI components | 13 |
| **Total removed** | **19** |

| Metric | Before | After |
|--------|-------:|------:|
| `AppModule` declarations | 66 | **47** |
| Lazy feature modules | 0 | **2** |

---

## Bundle sizes — before / after

**Before (PERF-001 baseline, pre-Phase 1):**

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 950.68 KB | 179.10 KB |
| `styles.*.css` | 51.48 KB | 8.60 KB |
| `polyfills.*.js` | 33.05 KB | 10.66 KB |
| `runtime.*.js` | 918 B | 523 B |
| **Initial total** | **1.01 MB** | **~198.87 KB** |
| Lazy chunks | **0** | — |

**After (PERF-002, `npm run build`):**

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 964.04 KB | 182.97 KB |
| `styles.*.css` | 51.48 KB | 8.60 KB |
| `polyfills.*.js` | 33.05 KB | 10.67 KB |
| `runtime.*.js` | 2.70 KB | 1.28 KB |
| **Initial total** | **1.03 MB** | **~203.51 KB** |

**New lazy chunks (loaded on demand):**

| Chunk | Name | Raw | Transfer |
|-------|------|-----|----------|
| `206.*.js` | `features-admin-admin-module` | 46.35 KB | 7.60 KB |
| `424.*.js` | `features-tickets-ticket-transcript-module` | 10.82 KB | 2.81 KB |

---

## Analysis

### What improved

- **Admin and transcript code verified out of `main.*.js`** (grep: `app-admin-home`, `transcript-page` only in lazy chunks).
- **Typical guild user** (never opens `/admin` or transcript) avoids downloading **~57 KB raw (~10 KB transfer)** on first session.
- **Architecture** ready for Phase 2: `SharedUiModule` can be imported by future lazy feature modules without circular deps.

### Why initial `main` grew slightly (+13 KB raw)

- Webpack lazy-loading bootstrap added to main + **runtime chunk** grew (918 B → 2.70 KB).
- `SharedUiModule` NgModule boundary adds factory metadata (same components, different packaging).
- Admin/transcript removal did not fully offset overhead in the **initial** chunk table — deferred bytes now live in lazy chunks instead.

### Budget status

- Initial bundle still **fails** 1 MB hard limit: **1.03 MB** (+27 KB vs limit; was +12 KB pre-Phase 1).
- `settings.component.css` warning unchanged (6.46 KB).

Phase 2 (lazy-loading remaining guild workspaces) is required for meaningful initial bundle reduction.

---

## Issues encountered

| Issue | Resolution |
|-------|------------|
| `BreadcrumbsComponent`: `routerLink` not recognized in `SharedUiModule` | Added `RouterModule` to `SharedUiModule` imports/exports |
| Build budget error | Expected — compile succeeds; budget failure pre-existing and slightly worse on initial total |

---

## Files changed

| File | Action |
|------|--------|
| `src/app/shared/shared-ui.module.ts` | **Created** |
| `src/app/features/admin/admin.module.ts` | **Created** |
| `src/app/features/admin/admin-routing.module.ts` | **Created** |
| `src/app/features/tickets/ticket-transcript.module.ts` | **Created** |
| `src/app/features/tickets/ticket-transcript-routing.module.ts` | **Created** |
| `src/app/app.module.ts` | **Modified** — removed 19 declarations; import `SharedUiModule` |
| `src/app/app-routing.module.ts` | **Modified** — `loadChildren` for admin + transcript |

---

## Validation

```bash
cd dashboard/DiscordBot.Dashboard && npm run build
```

- Compile: **pass**
- Lazy chunks: **2 created**
- Budget: **fail** (initial 1.03 MB > 1 MB limit)
- No UI/routing URL changes

---

## Not in scope (Phase 2+)

- Lazy-load Subscription, Staff, Logs, Tickets workspace, Settings tabs, etc.
- i18n split by feature
- CSS cleanup
- Budget threshold adjustment

---

*Phase 1 complete. Stopped before Phase 2 per instruction.*
