# Frontend Performance Audit — PERF-001

**Date:** 2026-07-03  
**Status:** Report only — no code modified  
**Scope:** `dashboard/DiscordBot.Dashboard` (Angular 16 production build)  
**Build command:** `npm run build` (`ng build --configuration production`)

---

## Executive summary

The dashboard initial load is **~1.01 MB raw (~199 KB gzip transfer)** and **fails the 1 MB hard budget by ~12 KB**. The dominant cause is architectural: **all 66 components are declared in `AppModule` and every route uses eager `component:` imports**. There is **zero lazy loading**, so Angular, RxJS, ngx-translate, and the entire feature surface ship in a single `main.*.js` chunk.

Third-party libraries are lean (no chart/UI kit). The bloat is almost entirely **self-inflicted bundle shape** plus **large monolithic feature components** (especially Settings) and **~100 KB of component-scoped CSS** inlined into JS.

| Priority | Action | Est. impact | Risk |
|----------|--------|-------------|------|
| 1 | Route-level lazy loading (feature modules or standalone lazy routes) | **−300–500 KB** initial raw | Medium (routing/module refactor) |
| 2 | Lazy-load Admin area first (5 routes, rarely used) | **−40–80 KB** initial | Low |
| 3 | Split Settings into lazy tab routes or child modules | **−80–120 KB** initial | Medium–High |
| 4 | Extract `SharedUiModule` + keep shell eager | Organizational; enables lazy | Low |
| 5 | CSS cleanup (CLEANUP-003 follow-up) | **−3–8 KB** styles + component CSS | Low |
| 6 | Icon registry refactor | **−1–3 KB** | Low |
| 7 | i18n split by feature (after lazy routes) | Faster TTI per route; not initial JS | Medium |

---

## 1. Current bundle sizes

Production build output (`dist/discord-bot.dashboard/`, hash `dab1eef80377376d`):

| Asset | Raw size | Est. transfer (gzip) | Notes |
|-------|----------|----------------------|-------|
| `main.*.js` | **950.68 KB** | **179.10 KB** | Single chunk: framework + all app code + inlined component CSS |
| `styles.*.css` | **51.48 KB** | **8.60 KB** | Global design system + workspace layouts |
| `polyfills.*.js` | **33.05 KB** | **10.66 KB** | `zone.js` |
| `runtime.*.js` | **918 B** | **523 B** | Webpack runtime |
| **Initial total** | **1.01 MB** | **~198.87 KB** | Budget error: **+12.10 KB** over 1 MB limit |

Additional runtime assets (not in initial JS bundle):

| Asset | Source size | Loaded when |
|-------|-------------|-------------|
| `assets/i18n/en.json` | **83.8 KB** | App bootstrap via `TranslateHttpLoader` |
| `assets/i18n/ar.json` | **106.0 KB** | On language switch (one language at a time) |
| Google Fonts (Inter + Noto Sans Arabic) | External CDN | `@import` in `styles.css` — separate network request |

**Budget warnings (angular.json):**

- `initial` warning at 550 KB — exceeded by **486 KB**
- `initial` error at 1 MB — exceeded by **12 KB**
- `settings.component.css` — **6.46 KB** (468 B over 6 KB component style warning)

**Lazy chunks:** **None.** Build emits exactly one application chunk plus polyfills/runtime/styles.

---

## 2. Why the initial bundle is ~1 MB

### 2.1 Root cause: no code splitting

```28:124:dashboard/DiscordBot.Dashboard/src/app/app-routing.module.ts
const routes: Routes = [
  { path: 'login', component: LoginComponent },
  // ... every route uses `component:` with statically imported classes
  { path: 'admin/plans', component: AdminPlansComponent, canActivate: [AdminGuard] },
  { path: '**', redirectTo: 'servers' }
];
```

```84:153:dashboard/DiscordBot.Dashboard/src/app/app.module.ts
@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    // ... 64 more components — all features + all subcomponents + shared UI
  ],
```

Every route component and every child component (filter bars, detail panels, editors, mission-control widgets) is compiled into `main.*.js` before the user navigates anywhere.

### 2.2 Estimated main-chunk composition

Webpack `stats.json` was generated but module-level attribution was unavailable in this audit environment. Composition is estimated from source metrics and typical Angular 16 production ratios:

| Bucket | Est. share of `main.*.js` | Evidence |
|--------|---------------------------|----------|
| Angular platform (core, common, compiler runtime, forms, router, animations, browser) | **~35–45%** (~330–430 KB) | `@angular/*` in `package.json`; `BrowserAnimationsModule`, `ReactiveFormsModule`, `FormsModule` all imported globally |
| RxJS + zone.js (zone mostly in polyfills) | **~8–12%** | Global observables across all services/components |
| `@ngx-translate/core` + http-loader | **~2–4%** | `TranslateModule.forRoot` in AppModule |
| Application TypeScript (features) | **~25–35%** | **~230 KB** source across `src/app/features/**` |
| Application TypeScript (core services) | **~8–12%** | **~113 KB** source; `guild.service.ts` alone ~17 KB |
| Application TypeScript (shared UI) | **~3–5%** | **~27 KB** source |
| Inlined component CSS | **~8–12%** | **~99.5 KB** source across 52 `.component.css` files |
| Templates + metadata | Included above | Large inline templates (e.g. `UiIconComponent`, Settings HTML) |

### 2.3 Largest application contributors (source)

**TypeScript by feature folder:**

| Feature | TS source | Notes |
|---------|-----------|-------|
| `settings` | ~29 KB | **907 lines** — Welcome, Auto Role, embed config, tabs in one component |
| `staff` | ~23 KB | Role management + editor mode + 4 subcomponents |
| `subscription` | ~23 KB | Billing flows, history, payment instructions |
| `overview` | ~22 KB | Mission control, timeline mappers, context drawer |
| `tickets` | ~20 KB | Queue workspace + context drawer + transcript |
| `admin` | ~19 KB | 5 admin pages (most users never visit) |
| `moderation` | ~15 KB | Master/detail workspace |
| `logs` | ~13 KB | Master/detail workspace |
| `reaction-roles` | ~13 KB | Master/detail workspace |

**Core services (always eager with current architecture):**

| Service | TS source | Used by |
|---------|-----------|---------|
| `guild.service.ts` | ~17 KB | Nearly every guild route |
| `activity-timeline-mapper.service.ts` | ~7 KB | Overview |
| `mission-mapper.service.ts` | ~6 KB | Overview |
| `context-drawer-mapper.service.ts` | ~5 KB | Overview |
| Other core services | ~78 KB | Auth, admin, analytics, onboarding, etc. |

**Component CSS inlined into JS (top files):**

| File | Source size |
|------|-------------|
| `settings.component.css` | **7.97 KB** (also exceeds component budget) |
| `activity-timeline.component.css` | 5.94 KB |
| `staff-role-editor.component.css` | 5.62 KB |
| `tickets-context-drawer.component.css` | 5.50 KB |
| `context-drawer.component.css` | 5.16 KB |
| **Total (52 files)** | **~99.5 KB** |

### 2.4 Feature components loaded eagerly

All **21 route-level feature components** and **~45 feature subcomponents** are in `AppModule.declarations`:

| Route area | Route components | Eager subcomponents |
|------------|------------------|---------------------|
| Auth | Login, Callback | — |
| Shell | DashboardLayout, Servers | OnboardingChecklist |
| Overview | Overview | StatusStrip, ActivityTimeline, ContextDrawer |
| Settings | Settings | WelcomeEditor, WelcomeDiscordPreview, WelcomeTestSection, AutoRoleEditor, AutoRoleAssignmentPreview, AutoRoleNotes |
| Tickets | Tickets, TicketTranscript | FilterBar, QueueCard, ContextDrawer |
| Moderation | Moderation, ModerationSettings | FilterBar, EntryCard, DetailPanel |
| Logs | Logs | FilterBar, EntryCard, DetailPanel |
| Reaction roles | ReactionRoles | FilterBar, PanelCard, DetailPanel |
| Staff | Staff | FilterBar, RoleCard, RoleEditor, DetailPanel |
| Profile | Profile | ProfilePreview |
| Modules | Modules | ModuleCard |
| Subscription | Subscription | ChangeFlow, History, PaymentInstructions |
| Admin | AdminHome, AdminGuilds, AdminUsers, AdminUpgradeRequests, AdminPlans | — |

A user opening **only** `/login` or `/servers` still downloads code for ticket transcripts, admin plans, staff role editor, subscription billing, etc.

### 2.5 Shared UI in AppModule

**15 shared/shell components** declared eagerly:

| Component | Role | Safe to keep eager? |
|-----------|------|---------------------|
| `UiIconComponent` | Nav + workspace icons | **Yes** — used in layout on every authenticated page |
| `LanguageSwitcherComponent` | Header | **Yes** |
| `ProfileMenuComponent` | Header | **Yes** |
| `ServerSwitcherComponent` | Header | **Yes** |
| `BreadcrumbsComponent` | Layout | **Yes** |
| `ToastContainerComponent` | Global | **Yes** |
| `PageWorkspaceHeroComponent` | 10+ workspace pages | **Yes** (or import via SharedUiModule into lazy features) |
| `EmptyStateComponent` | Widespread | **Yes** |
| `LoadingStateComponent` | Widespread | **Yes** |
| `StatusBadgeComponent` | Widespread | **Yes** |
| `ErrorStateComponent` | Error surfaces | **Yes** |
| `SectionHeaderComponent` | Section headings | **Yes** |
| `PageNoticeComponent` | Inline notices | **Yes** |
| `MemberSelectComponent` | Settings, moderation forms | **Lazy with Settings/Moderation** |
| `OnboardingChecklistComponent` | Overview/onboarding | **Lazy with Overview** |

Shared UI is not the primary problem — **feature volume** is. However, declaring shared components in `AppModule` alongside every feature prevents tree-shaking unused shared pieces when lazy modules are introduced unless a `SharedUiModule` is extracted first.

### 2.6 Translation files

| File | Size | Lines (approx.) | Top namespaces by content |
|------|------|-----------------|---------------------------|
| `en.json` | 83.8 KB | ~2,116 | `overview`, `subscription`, `staff`, `autoRole`, `settings` |
| `ar.json` | 106.0 KB | ~2,127 | Same order; Arabic strings run longer |

**Loading model:** `TranslateModule.forRoot` + `TranslateHttpLoader('./assets/i18n/')`. Translations are **not bundled into `main.*.js`** but fetched over HTTP at runtime. Default language (`en`) loads on first translate use; `ar` loads on switch.

**Impact on initial load:** Adds **~84 KB JSON parse** after JS/CSS — noticeable on slow networks but separate from the 1 MB budget failure.

**Unused groups:** No automated dead-key scan was run in this audit. Namespaces for routes that can be lazy-loaded (`admin`, `adminPlans`, `subscription`, `reactionRoles`, etc.) are candidates for **feature-scoped JSON files** once routes split.

### 2.7 CSS bundle

**Global styles pipeline** (`src/styles.css`):

| Source file | Size |
|-------------|------|
| `components.css` | 21.7 KB |
| `workspace-layouts.css` | 19.9 KB |
| `design-system.css` | 7.7 KB |
| `tokens.css` | 5.5 KB |
| `layout.css` | 4.7 KB |
| `rtl.css` | 2.7 KB |
| `base.css`, `animations.css` | 2.3 KB |
| **Total source** | **~64.9 KB** → **51.5 KB** built (minified) |

**External:** Google Fonts `@import` — not counted in `styles.*.css` size; adds render-blocking font CSS + font files.

**Feature CSS:** ~99.5 KB source lives in component files and is **bundled into `main.*.js`**, not `styles.css`. This is a hidden contributor to JS size.

**Duplication / unused CSS:** See `docs/reviews/css-cleanup-audit.md` — estimated **~350–550 lines** of safe/probably-safe global CSS removal after verification.

**Route-level CSS split:** Not possible without lazy-loaded routes. Angular can scope component CSS to lazy chunks automatically once features are split.

### 2.8 Icons

`UiIconComponent` embeds **29 SVG icons** in a single inline template with `ngSwitch`:

```34:98:dashboard/DiscordBot.Dashboard/src/app/shared/ui/ui-icon/ui-icon.component.ts
@Component({
  selector: 'app-ui-icon',
  template: `
    <svg ...>
      <ng-container [ngSwitch]="name">
        <g *ngSwitchCase="'home'">...</g>
        <!-- 28 more icon groups -->
      </ng-container>
    </svg>
  `,
})
export class UiIconComponent {
  @Input() name: IconName = 'home';
```

| Metric | Value |
|--------|-------|
| Icons defined | **29** |
| Component TS + inline template | **~5.3 KB** source |
| Statically referenced in templates | **27** icon names |
| Likely unused in templates | **`home`** (default `@Input` only) |
| Dynamically bound via TS | `alert-circle`, `cloud-off`, `check-circle`, module icons, mission icons, subscription state icons |

**All icons ship eagerly** because `UiIconComponent` is in `AppModule` and the full template is one compilation unit. Angular's production build does not tree-shake individual `*ngSwitchCase` branches.

**Nav/layout uses ~15 distinct icons** on every page; the remaining icons support workspace heroes, mission control, modules, and subscription flows.

### 2.9 Third-party libraries

From `package.json` — intentionally minimal:

| Package | Purpose | Bundle note |
|---------|---------|-------------|
| `@angular/*` ^16.2 | Framework | Largest dependency; unavoidable |
| `@ngx-translate/core` ^15 | i18n runtime | Moderate; loader adds HttpClient coupling |
| `@ngx-translate/http-loader` ^8 | JSON loading | Small |
| `rxjs` ~7.8 | Reactive streams | Tree-shaken but widely imported |
| `zone.js` ~0.13 | Change detection | In polyfills chunk |
| `tslib` | TS helpers | Small |

**Not present:** Chart.js, D3, Material, Bootstrap, lodash, moment — good baseline.

---

## 3. Angular architecture

### 3.1 Eager routes (all of them)

| Path | Component | Typical access |
|------|-----------|----------------|
| `/login` | LoginComponent | Unauthenticated |
| `/auth/callback` | CallbackComponent | OAuth return |
| `/servers` | ServersComponent | Every session start |
| `/guilds/:id/overview` | OverviewComponent | High |
| `/guilds/:id/settings` | SettingsComponent | High |
| `/guilds/:id/tickets` | TicketsComponent | Moderation users |
| `/guilds/:id/tickets/:ticketId/transcript` | TicketTranscriptComponent | Occasional |
| `/guilds/:id/moderation` | ModerationComponent | Moderation users |
| `/guilds/:id/moderation/settings` | ModerationSettingsComponent | Owners |
| `/guilds/:id/modules` | ModulesComponent | Owners |
| `/guilds/:id/logs` | LogsComponent | Moderation users |
| `/guilds/:id/reaction-roles` | ReactionRolesComponent | Owners |
| `/guilds/:id/subscription` | SubscriptionComponent | Owners |
| `/guilds/:id/profile` | ProfileComponent | Owners |
| `/guilds/:id/staff` | StaffComponent | Owners |
| `/admin/*` (5 routes) | Admin* components | Platform admins only |

### 3.2 Lazy-load candidates (recommended order)

**Tier 1 — High value, low user overlap**

| Feature | Routes | Rationale |
|---------|--------|-----------|
| Admin | `/admin`, `/admin/guilds`, `/admin/users`, `/admin/upgrade-requests`, `/admin/plans` | ~19 KB TS + 5 components; tiny audience |
| Ticket transcript | `/guilds/:id/tickets/:ticketId/transcript` | Standalone page; not needed for ticket queue |
| Moderation settings | `/guilds/:id/moderation/settings` | Separate from moderation log workspace |

**Tier 2 — Medium value**

| Feature | Routes | Rationale |
|---------|--------|-----------|
| Subscription | `/guilds/:id/subscription` | Heavy TS + 3 subcomponents + large i18n |
| Reaction roles | `/guilds/:id/reaction-roles` | Full master/detail workspace |
| Staff | `/guilds/:id/staff` | Editor mode adds 5 subcomponents |
| Logs | `/guilds/:id/logs` | Master/detail pattern |
| Profile | `/guilds/:id/profile` | Preview subcomponent + CSS duplication |

**Tier 3 — Harder splits (still worth planning)**

| Feature | Challenge |
|---------|-----------|
| Settings | Single component hosts Welcome + Auto Role tabs; needs tab-level lazy routes or child components in a lazy module |
| Overview | Default landing page — keep eager or prefetch after login |
| Tickets / Moderation | Frequently used by moderators — lazy still helps owners who never open them |

### 3.3 AppModule declaration count

| Category | Count |
|----------|------:|
| Total `declarations` | **66** |
| Feature route components | 21 |
| Feature subcomponents | ~30 |
| Shared UI / shell | 15 |

**Verdict:** `AppModule` declares far too many feature components. This is the single architectural smell driving bundle size.

### 3.4 Feature modules vs standalone lazy routes

**Current stack:** Angular 16, NgModule-based, no standalone components.

| Approach | Pros | Cons |
|----------|------|------|
| **NgModule per feature + `loadChildren`** | Matches existing patterns; shared `SharedUiModule` exports DS components | More boilerplate; two module files per feature |
| **Standalone components + `loadComponent`** | Less boilerplate; Angular 16 supported | Requires converting 66 components; larger migration |
| **Hybrid** | Lazy NgModules first; migrate hot paths to standalone later | Two patterns temporarily |

**Recommendation:** Start with **feature NgModules + `loadChildren`** — lowest risk for this codebase. Consider standalone migration only after lazy boundaries exist.

**Shell should remain eager:**

- `AppComponent`, `DashboardLayoutComponent`
- Auth (Login, Callback)
- Servers list
- Core guards, interceptors, services
- Shared UI module (exported once, imported by lazy features)

---

## 4. Design system impact

### 4.1 Safe to keep eager (global shell)

These are small, high-frequency, and used across routes:

- `UiIconComponent`, `LanguageSwitcherComponent`, `ProfileMenuComponent`, `ServerSwitcherComponent`
- `BreadcrumbsComponent`, `ToastContainerComponent`
- `EmptyStateComponent`, `LoadingStateComponent`, `ErrorStateComponent`
- `StatusBadgeComponent`, `SectionHeaderComponent`, `PageNoticeComponent`
- Global CSS: `tokens.css`, `base.css`, `components.css` (active `.btn`, `.badge`, `.card`, `.input` — not unused `.ds-*` aliases)

### 4.2 Feature-specific bloat in initial bundle

These design-system **patterns** are duplicated per feature and all load upfront:

| Pattern | Features using it | Subcomponents |
|---------|-------------------|---------------|
| Workspace hero + toolbar + master/detail | Tickets, Logs, Moderation, Reaction roles, Staff | Filter bar, card, detail panel each |
| Mission control | Overview only | Timeline, context drawer, status strip |
| Welcome / Auto Role editors | Settings only | 6 subcomponents |
| Subscription billing UI | Subscription only | 3 subcomponents |
| Admin tables | Admin only | — |

`PageWorkspaceHeroComponent` (~4.5 KB CSS + template) is shared but **hero copy keys** (`workspaceHero.*` in i18n) pull large translation namespaces into memory even when not on that page.

### 4.3 Eager vs lazy guidance

| Keep eager | Lazy-load with feature |
|------------|------------------------|
| Layout chrome + nav | Admin pages |
| Servers picker | Ticket transcript |
| Login / callback | Subscription billing flows |
| Overview (or prefetch) | Staff role editor |
| Shared DS primitives (via SharedUiModule) | Reaction roles workspace |
| Global tokens + active component CSS | Moderation settings page |
| | Welcome/Auto Role tab bodies (within Settings lazy module) |

---

## 5. CSS deep dive

### 5.1 Global CSS remaining size

| Built | Source | Ratio |
|-------|--------|-------|
| 51.5 KB | 64.9 KB | ~79% retained after minification |

Largest source files:

1. **`components.css`** (21.7 KB) — buttons, forms, dialogs, badges; contains unused `.ds-*` alias halves per CLEANUP-003
2. **`workspace-layouts.css`** (19.9 KB) — `.ws-*` workspace grid, hero, toolbar, master/detail; core to current UI
3. **`design-system.css`** (7.7 KB) — card variants, extended badges; several unused `.card-*` blocks

### 5.2 Feature CSS duplication

| Duplication | Location | Est. overlap |
|-------------|----------|--------------|
| Profile Discord preview vs `ws-discord-*` | `profile-preview.component.css` vs `workspace-layouts.css` | ~150 lines |
| Tickets conversation | `tickets-context-drawer` vs global `.conversation-*` | ~55 lines |
| Parallel badge/button layers | `.badge-*`, `.ds-badge-*`, `[data-status]` | 3 layers |

### 5.3 Unused workspace utilities

From CLEANUP-003 (not yet removed):

- `.ws-divider`, `.ws-info-row`, `.ws-footer-hint`, some grid variants — **probably safe**
- `.ws-sticky-rail` — orphaned selector grouped with active rules — **needs visual review**
- Unused `.ds-table*`, `.ds-stats-grid`, `.card-muted`/`.card-brand` etc. — **safe after grep verification**

### 5.4 Can CSS split by route?

| CSS type | Splittable? | Mechanism |
|----------|-------------|-----------|
| Component `.component.css` | **Yes** | Automatic when component is in lazy module |
| Global `styles.css` imports | **Partially** | Must stay global for shell; could trim unused rules |
| `workspace-layouts.css` | **No short-term split** | Used by many eager routes today |
| RTL overrides | Stays global | `rtl.css` is only 2.7 KB |

**After lazy loading:** expect **~30–50 KB** of component CSS to move out of initial JS into lazy chunks.

---

## 6. i18n

### 6.1 File sizes

| File | Bytes | Top 5 namespaces (by string volume) |
|------|------:|--------------------------------------|
| `en.json` | 83,789 | overview, subscription, staff, autoRole, settings |
| `ar.json` | 105,972 | overview, subscription, staff, autoRole, settings |

Both files mirror the same **~25 top-level groups** (`common`, `nav`, `titles`, `workspaceHero`, feature namespaces, `toast`, `errors`, etc.).

### 6.2 Unused translation groups

No exhaustive dead-key analysis was performed. Heuristic candidates for **future** splitting (likely low traffic until route visited):

- `admin`, `adminPlans` — admin routes only
- `reactionRoles` — single feature route
- `moderationSettings` — settings sub-route
- Large `overview` block — only overview route (but overview is default landing)

**Do not delete keys** without automated unused-key tooling — many keys are referenced from TypeScript (`translate.instant`) not just templates.

### 6.3 Lazy translation chunks — worth it later?

| Phase | Approach | Benefit |
|-------|----------|---------|
| Now | Single JSON per language | Simple; **84–106 KB** parse on load |
| After lazy routes | `TranslateHttpLoader` with path `./assets/i18n/{{lang}}/{{feature}}.json` + merge | Load `common.json` + route namespace only |
| Long term | `@ngx-translate/multi-http-loader` or custom loader | Fine-grained; aligns with lazy modules |

**Verdict:** Lazy i18n is **Phase 3** — meaningful only after route-level code splitting. Initial JS budget won't change (JSON was never in JS), but **time-to-interactive and memory** improve on mobile.

---

## 7. Icons

### 7.1 Implementation summary

- Single component, inline SVG paths, `ngSwitch` on `name` input
- Type-safe `IconName` union duplicated in `page-workspace-hero.models.ts` and mission/timeline models
- Dynamic binding: `[name]="$any(row.icon)"` in timeline, context drawer, modules, subscription flows

### 7.2 All icons bundled eagerly?

**Yes.** The entire switch template compiles into `main.*.js`.

### 7.3 More efficient approaches (future)

| Option | Savings | Tradeoff |
|--------|---------|----------|
| Remove unused `home` default icon path | Minimal | Trivial |
| Split **nav icons** vs **workspace icons** into two components | Small–medium | Two imports in lazy modules |
| External SVG sprite (`<use href="#icon-*">`) | Medium | Build step; CSP considerations |
| Icon font (not recommended) | — | Worse a11y, blur on retina |
| Per-feature icon maps with dynamic `import()` | Medium | Complexity; flash on first use |

**Practical recommendation:** Low priority vs lazy routes. If touching icons, split into `UiNavIconComponent` (15 icons, eager) and lazy-load a richer set only in Overview/Subscription modules.

---

## 8. Recommendations

### 8.1 Quick wins (low risk)

| # | Change | Est. savings | Notes |
|---|--------|--------------|-------|
| Q1 | Lazy-load **Admin** feature module (5 routes) | 40–80 KB raw initial | Smallest audience; clear boundary |
| Q2 | Lazy-load **TicketTranscript** route | 10–25 KB | Standalone route already |
| Q3 | Extract **`SharedUiModule`** | 0 KB alone; **enables** lazy | Export DS components once |
| Q4 | CSS cleanup per CLEANUP-003 (unused `.ds-*`, `.card-*`) | 3–8 KB global CSS | Report exists; visual spot-check |
| Q5 | Trim **`settings.component.css`** below 6 KB budget | Fixes component warning | Move rules to global `ws-*` where shared |
| Q6 | Adjust **`angular.json` budget** temporarily | CI green only | Does not improve user perf — document intentional debt |

### 8.2 Safe changes (medium effort, high reward)

| # | Change | Est. savings |
|---|--------|--------------|
| S1 | Lazy modules for Subscription, Staff, ReactionRoles, Logs, Profile, ModerationSettings | 150–300 KB cumulative |
| S2 | Lazy **Settings** module with tab child routes (`welcome`, `auto-role`, …) | 80–120 KB |
| S3 | Move **`MemberSelectComponent`** + **`OnboardingChecklistComponent`** into feature modules | 5–15 KB |
| S4 | Split i18n: `common.json` + per-feature files loaded with lazy routes | Runtime memory/parse, not JS |

### 8.3 Risky changes

| # | Change | Risk |
|---|--------|------|
| R1 | Convert entire app to **standalone + loadComponent** in one pass | Large migration; easy to break DI/providers |
| R2 | Remove **`BrowserAnimationsModule`** | Breaks any `[@trigger]` animations |
| R3 | Delete i18n keys without tooling | Runtime missing-translation errors |
| R4 | Aggressive global CSS deletion without visual QA | Layout regressions on edge pages |
| R5 | Replace ngx-translate with build-time `$localize` | Full i18n pipeline rewrite |
| R6 | Remove **`guild.service.ts`** methods "unused on first paint" | Breaks guards/resolvers if added later |

### 8.4 Recommended implementation plan

**Phase 0 — Baseline (0.5 day)**  
- Add webpack bundle analyzer to CI (`ng build --stats-json` + `webpack-bundle-analyzer`)  
- Document current chunk map in repo  
- Optionally relax budget to 1.05 MB until Phase 1 lands (explicit debt ticket)

**Phase 1 — Quick splits (1–2 days)**  
1. Create `SharedUiModule` — move 15 shared declarations + exports  
2. Create `AdminModule` — lazy `loadChildren` for `/admin/**`  
3. Lazy `TicketTranscriptComponent` route  
4. Verify guards, breadcrumbs, and translate keys still resolve

**Phase 2 — Feature modules (3–5 days)**  
1. One lazy module per guild workspace: Tickets, Moderation (+ settings), Logs, Staff, ReactionRoles, Subscription, Profile, Modules  
2. Keep Overview + Settings shell eager OR lazy Settings with default redirect  
3. Confirm component CSS moves to lazy chunks (check `ng build` output for new chunk files)

**Phase 3 — i18n + CSS (2–3 days)**  
1. Split JSON: `assets/i18n/en/common.json`, `overview.json`, `subscription.json`, …  
2. Custom loader: load `common` + namespace matching activated route  
3. Execute CLEANUP-003 CSS removals (~350–550 lines)  
4. Consolidate profile preview CSS onto `ws-discord-*`

**Phase 4 — Polish (optional)**  
1. Icon split (nav vs feature)  
2. Prefetch lazy modules after login (`router.preload` strategy for likely routes)  
3. Evaluate standalone migration for new features only

**Target after Phase 2:**

| Metric | Current | Target |
|--------|---------|--------|
| Initial raw | 1.01 MB | **≤ 650 KB** |
| Initial transfer | ~199 KB | **≤ 140 KB** |
| Lazy chunks | 0 | **8–12** |
| Budget | Failing | Passing with headroom |

---

## 9. Validation performed

- Production build: `npm run build` — bundle table captured  
- Source measurement: TS/CSS/i18n byte counts via filesystem  
- Static analysis: routing module, app module, ui-icon, styles pipeline  
- Cross-reference: `docs/reviews/css-cleanup-audit.md` for CSS duplication  
- Icon usage: template grep + dynamic TS bindings  

**Not performed:** webpack module-level attribution (stats.json modules array empty in CLI output), automated i18n dead-key scan, Lighthouse trace, runtime bundle analyzer UI.

---

## 10. Related documents

| Document | Relevance |
|----------|-----------|
| `docs/reviews/css-cleanup-audit.md` | Unused global CSS candidates |
| `docs/progress/2026-07-03-PP-001-design-system.md` | DS v2 added `workspace-layouts.css` weight |
| `angular.json` budgets | 550 KB warning / 1 MB error thresholds |

---

*End of audit — no files were modified in the dashboard source tree.*
