# PERF-001 — Frontend Bundle Optimization Audit

**Date:** 2026-07-03  
**Task ID:** PERF-001  
**Status:** Complete (report only)  
**Deliverables:** `docs/reviews/frontend-performance-audit.md`

---

## Summary

Performed a read-only frontend performance audit of the Angular dashboard before adding more pages or features. Production build confirms the initial bundle at **1.01 MB raw (~199 KB transfer)**, **12 KB over the 1 MB hard budget**. Root cause is **zero lazy loading**: 66 components declared in `AppModule`, single `main.*.js` chunk, no `loadChildren` routes.

No dashboard source files were modified.

---

## Objective

Understand why the initial bundle reached ~1 MB, identify largest contributors, and produce a prioritized optimization plan covering bundle size, Angular architecture, design system impact, CSS, i18n, and icons — **without implementing changes**.

---

## Method

1. Ran `npm run build --configuration production` and captured chunk sizes and budget warnings.
2. Audited `app.module.ts` (66 declarations) and `app-routing.module.ts` (all eager `component:` routes).
3. Measured source sizes: feature TS by folder, 52 component CSS files, global CSS imports, i18n JSON namespaces.
4. Reviewed `UiIconComponent` (29 inline SVGs, all eager).
5. Cross-referenced CSS duplication findings from CLEANUP-003.
6. Drafted quick wins, safe vs risky changes, and phased implementation plan.

---

## Current bundle sizes

| Chunk | Raw | Transfer |
|-------|-----|----------|
| `main.*.js` | 950.68 KB | 179.10 KB |
| `styles.*.css` | 51.48 KB | 8.60 KB |
| `polyfills.*.js` | 33.05 KB | 10.66 KB |
| `runtime.*.js` | 918 B | 523 B |
| **Initial total** | **1.01 MB** | **~198.87 KB** |

**Budget:** Fails `initial` maximumError (1 MB) by **12.10 KB**.  
**Warning:** `settings.component.css` at 6.46 KB (+468 B over 6 KB component budget).

**Lazy chunks:** None.

---

## Top causes

| Rank | Cause | Evidence |
|------|-------|----------|
| 1 | **No route-level code splitting** | 0 `loadChildren`; 1 application chunk |
| 2 | **Monolithic AppModule** | 66 eager declarations — all features + subcomponents |
| 3 | **Large feature components in main** | Settings ~907 lines TS; Subscription, Staff, Overview ~20–30 KB TS each |
| 4 | **~100 KB component CSS inlined into JS** | 52 `.component.css` files bundled into `main.*.js` |
| 5 | **Angular framework baseline** | `@angular/*`, forms, animations, router — global imports |
| 6 | **Global workspace CSS** | `workspace-layouts.css` + `components.css` ≈ 42 KB source |

Secondary (not in initial JS but affects load):

- i18n: `en.json` 84 KB, `ar.json` 106 KB (HTTP-loaded)
- Google Fonts external `@import`

---

## Quick wins

| Action | Est. impact | Risk |
|--------|-------------|------|
| Lazy-load Admin module (5 routes) | −40–80 KB initial | Low |
| Lazy-load TicketTranscript route | −10–25 KB initial | Low |
| Extract `SharedUiModule` | Enables lazy; organizational | Low |
| CSS cleanup (CLEANUP-003 follow-up) | −3–8 KB styles | Low (visual QA) |
| Trim `settings.component.css` | Fixes component budget warning | Low |

---

## Safe changes (recommended next sprint)

1. **Phase 1:** `SharedUiModule` + Admin lazy module + transcript lazy route  
2. **Phase 2:** Lazy feature modules for Subscription, Staff, Logs, ReactionRoles, Moderation settings, Profile  
3. **Phase 3:** Split Settings into lazy module with tab child routes  
4. **Phase 4:** Split i18n JSON by feature namespace; load with route activation  

Expected outcome after Phase 2: initial bundle **≤ 650 KB raw**, **8–12 lazy chunks**.

---

## Risky changes (defer or isolate)

- Full standalone + `loadComponent` migration in one pass  
- Removing `BrowserAnimationsModule`  
- Deleting i18n keys without automated unused-key tooling  
- Aggressive global CSS removal without visual regression pass  
- Raising budget without actual splitting (CI green but no user benefit)

---

## Key findings by audit area

### Bundle / architecture

- 21 route components + ~45 subcomponents all eager  
- Third-party deps are lean (Angular + ngx-translate + rxjs only)  
- Development config uses `vendorChunk: true`; production merges everything into `main`

### Design system

- Shared UI (hero, empty-state, badges, icons) should stay eager or live in `SharedUiModule`  
- Master/detail workspace subcomponents (tickets, logs, moderation, staff, reaction-roles) bloat initial bundle  
- `PageWorkspaceHeroComponent` used on 10+ pages — correct to share, wrong to co-bundle with admin/subscription

### CSS

- Global built CSS: 51.5 KB (source 64.9 KB)  
- Component CSS: 99.5 KB source → inlined in JS  
- ~350–550 lines removable global CSS per CLEANUP-003 (not yet applied)

### i18n

- 25 top-level namespaces; largest: `overview`, `subscription`, `staff`  
- Lazy translation chunks worthwhile **after** lazy routes (Phase 3)  
- Not a contributor to 1 MB JS budget

### Icons

- 29 SVGs in one `UiIconComponent` template — all eager, no per-icon tree-shaking  
- Only `home` appears unused in templates (default input value)  
- Dynamic icons (`alert-circle`, `cloud-off`, etc.) used from mission/subscription mappers

---

## Files changed

| File | Action |
|------|--------|
| `docs/reviews/frontend-performance-audit.md` | **Created** — full audit report |
| `docs/progress/2026-07-03-PERF-001-frontend-performance-audit.md` | **Created** — this progress report |
| Dashboard source | **Not modified** |

---

## Validation performed

- Production build executed; sizes and budget errors recorded  
- Static grep: routing, app module, icon usage, workspace hero references  
- Source byte counts: TS features, component CSS, global CSS, i18n JSON  
- Cross-reference: `docs/reviews/css-cleanup-audit.md`

**Not performed:** webpack-bundle-analyzer UI, Lighthouse, automated i18n dead-key scan.

---

## Recommended implementation plan

| Phase | Duration | Work |
|-------|----------|------|
| 0 | 0.5 day | Bundle analyzer in CI; baseline chunk map |
| 1 | 1–2 days | SharedUiModule, Admin lazy, transcript lazy |
| 2 | 3–5 days | Lazy modules per guild workspace feature |
| 3 | 2–3 days | i18n split + CLEANUP-003 CSS execution |
| 4 | Optional | Icon split, route prefetch, standalone for new features |

**Do not implement until approved** — this sprint was audit-only per PERF-001 scope.

---

## Next steps (when approved)

1. Create ticket **PERF-002** — Phase 1 lazy loading (Admin + SharedUiModule)  
2. Re-run production build and attach chunk comparison table  
3. Update `angular.json` budgets after initial bundle drops below 800 KB

---

*Report-only sprint. No UI redesign. No file deletions.*
