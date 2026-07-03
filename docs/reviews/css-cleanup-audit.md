# CSS Cleanup Audit — CLEANUP-003

**Date:** 2026-07-03  
**Status:** Report only — no CSS modified  
**Scope:** Dashboard global styles + obvious feature duplication  
**Files audited:**

| File | Lines |
|------|------:|
| `dashboard/DiscordBot.Dashboard/src/styles/components.css` | 1,122 |
| `dashboard/DiscordBot.Dashboard/src/styles/design-system.css` | 621 |
| `dashboard/DiscordBot.Dashboard/src/styles/workspace-layouts.css` | 960 |
| `dashboard/DiscordBot.Dashboard/src/styles/rtl.css` | 82 |
| `dashboard/DiscordBot.Dashboard/src/app/features/profile/profile-preview/profile-preview.component.css` | 154 |
| `dashboard/DiscordBot.Dashboard/src/app/features/tickets/tickets-context-drawer/tickets-context-drawer.component.css` | (conversation dup) |

**Method:** Static grep across `src/**/*.html`, `src/**/*.ts` (inline templates), and `src/**/*.css`. Risk levels reflect template usage + cross-CSS references. Dynamic `[class.*]` bindings checked manually for `ws-*` utilities.

---

## Executive summary

| Category | Safe | Probably safe | Needs visual review | Keep |
|----------|-----:|--------------:|--------------------:|-----:|
| Unused `.ds-*` aliases (buttons, forms, tables, stats) | 8 blocks | 6 blocks | 2 blocks | 4 blocks |
| Unused `.card-*` variants (`design-system.css`) | 12 blocks | 2 blocks | 0 | 0 |
| Unused `.ws-*` utilities | 0 | 6 blocks | 1 block | 18+ blocks |
| Duplicate badge/button/table/dialog | — | — | 5 groups | 3 groups active |
| Profile preview vs `ws-discord-*` | — | — | 1 group (~150 lines) | Both active today |
| Page-width utilities | 0 | 1 | 0 | 3 |

**Key finding:** DS v2 adoption uses **legacy class names** (`.btn`, `.badge`, `.card`, `.input`) in templates. Parallel `.ds-*` selectors in `components.css` are mostly **alias duplicates** on shared rules — removing `.ds-*` halves is low-risk; removing entire rules is not.

**Estimated removable CSS (after verification pass):** ~350–550 lines globally, plus ~150 lines if profile preview migrates to `ws-discord-*`.

---

## 1. Unused `.ds-*` classes

### 1.1 Buttons — `components.css` L59–148 + `design-system.css` L186–220

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-btn`, `.ds-btn-primary`, `.ds-btn-secondary`, `.ds-btn-ghost`, `.ds-btn-danger`, `.ds-btn-sm` | `components.css` | ~90 | DS v1 button aliases on same rules as `.btn*` | **0 HTML** uses `ds-btn*`; **40+ HTML** use `btn btn-primary`, `btn-ghost`, etc. | **Probably safe** | Remove `.ds-*` halves from comma-grouped selectors only; keep `.btn*` |
| `.ds-btn-success`, `.ds-btn.is-loading` | `design-system.css` | ~35 | Success + loading button states | **0 HTML** for `ds-btn-success` or `is-loading` on buttons | **Safe** | No template or TS binding found |
| `.ds-icon-btn` | `components.css` | ~18 | Icon button alias | **0 HTML** for `ds-icon-btn`; **8+ HTML** use `icon-btn` | **Probably safe** | Alias only; `.icon-btn` is canonical |

### 1.2 Cards & surfaces — `components.css` L36–48

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-card` | `components.css` | ~13 | Card alias on `.card` rule | **0 HTML** for `ds-card`; **20+ HTML** use `class="card"` | **Probably safe** | Alias on shared rule with `.card` |

### 1.3 Form controls — `components.css` L186–240

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-input`, `.ds-select`, `.ds-textarea` | `components.css` | ~55 | Form control aliases | **0 HTML** for `ds-input*`; templates use `.input`, native `select`, `textarea` | **Probably safe** | Aliases on element + class grouped selectors |

### 1.4 Badges — `components.css` L333–375 + `design-system.css` L274–337

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-badge`, `.ds-badge-success/error/warning/brand` | `components.css` | ~45 | Badge tone aliases | **0 HTML** for `ds-badge*`; templates use `.badge`, `[class.badge-open]`, `data-status` | **Probably safe** | Remove alias halves; keep `.badge-*` |
| `.ds-badge-info/neutral/online/offline/pending/review/activated/expired/plan` | `design-system.css` | ~65 | Extended badge palette | **0 HTML** for these classes; `status-badge` uses `data-status` attribute | **Safe** | Defined but never applied in templates |

### 1.5 Tables — `components.css` L434–480

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-table-wrap`, `.ds-table` (+ th/td/hover rules) | `components.css` | ~47 | DS table primitives | **0 HTML** uses `ds-table`; admin pages use `.table-card` + plain `<table>` | **Safe** | Table styling likely inherited from `.table-card` / element rules |
| `html[dir='rtl'] .ds-table th/td` | `rtl.css` | 4 | RTL alignment for ds-table | Parent selectors unused | **Probably safe** | Remove with `.ds-table` block |

### 1.6 Stats & loading — `components.css` L495–560

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-stats-grid`, `.ds-stat-card` | `components.css` | ~22 | Admin stats grid | **0 HTML** references | **Safe** | Legacy admin layout experiment |
| `.ds-spinner`, `.ds-skeleton` | `components.css` | ~14 | Loading primitives | **0 HTML** for `ds-spinner`/`ds-skeleton`; `.skeleton` class used directly | **Safe** | `.skeleton` kept elsewhere |
| `.ds-loading` | `components.css` + `loading-state.component.ts` | ~9 | Loading panel modifier | **1 TS** — `loading-state` uses `class="loading-panel ds-loading"` | **Keep** | Active in shared loading component |
| `.ds-empty` | `empty-state.component.ts` | ~4 | Empty state modifier | **1 TS** — `class="empty-state ds-empty"` | **Keep** | Active in shared empty state |

### 1.7 Dropdowns — `components.css` L637–677

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ds-dropdown`, `.ds-dropdown-menu`, `.ds-dropdown-item` | `components.css` | ~41 | Shell dropdown menus | **6+ usages** in layout, profile-menu, server-switcher, language-switcher | **Keep** | Canonical dropdown system — actively used |

---

## 2. Unused `.card-*` variants — `design-system.css` L70–174

Templates use base `.card`, `.card-section`, `.card-section-header` from `components.css`. No template applies DS variant class names.

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.card-primary` | `design-system.css` | 3 | Documentation alias (empty rule) | **0 HTML** | **Safe** | Comment-only alias |
| `.card-secondary`, `.card-elevated` | `design-system.css` | 10 | Visual card variants | **0 HTML** | **Safe** | Never applied |
| `.card-metric`, `.stat-card` | `design-system.css` | 4 | Metric card padding | **0 HTML** for `card-metric`; check `stat-card` — **0 HTML** | **Safe** | MetricCard component removed in CLEANUP-001 |
| `.card-status` (+ `.is-success/warning/danger/info/brand`) | `design-system.css` | 24 | Status border accent cards | **0 HTML**; feature cards use BEM (`modules-module-card-status`) | **Safe** | Superseded by workspace/feature patterns |
| `.card-action` (+ `:hover`) | `design-system.css` | 12 | Clickable card | **0 HTML** | **Safe** | Never applied |
| `.card-info`, `.card-empty` | `design-system.css` | 8 | Semantic card variants | **0 HTML** | **Safe** | Never applied |
| `.card-header-row` (+ `.is-centered`, nested h2/h3) | `design-system.css` | 17 | Card header layout helper | **0 HTML**; settings uses `.card-section-header` instead | **Safe** | Parallel unused helper |
| `.card-footer`, `.card-compact`, `.card-flush` | `design-system.css` | 17 | Card structure modifiers | **0 HTML** | **Safe** | Never applied |
| `.surface-inset` | `design-system.css` | 6 | Inset nested surface | **0 HTML** | **Safe** | Never applied |

---

## 3. Unused `.ws-*` utilities — `workspace-layouts.css`

### 3.1 Confirmed **Keep** (active in templates)

| Selector | Used by | Risk |
|----------|---------|------|
| `.ws-page`, `.ws-page--compact` | modules, profile, tickets, logs, etc. | **Keep** |
| `.ws-layout` | profile, moderation, tickets, subscription, reaction-roles, modules, logs | **Keep** |
| `.ws-atf`, `.ws-atf--band` | workspace heroes on moderation, logs, RR, profile, subscription | **Keep** |
| `.ws-workspace`, `.ws-workspace--sections` | all workspace pages; modules uses `--sections` | **Keep** |
| `.ws-toolbar` | filter bars (tickets, logs, moderation, RR, welcome) | **Keep** |
| `.ws-panel-pad`, `.ws-panel-border-start` | profile, subscription, welcome test | **Keep** |
| `.ws-page-notice`, `.ws-page-notice--accent` | `page-notice.component.ts` binding | **Keep** |
| `.ws-master-detail*`, `.ws-placeholder-panel*` | tickets, logs, moderation, RR detail rails | **Keep** |
| `.ws-aside--sticky` | profile, subscription aside rails | **Keep** |
| `.ws-grid--action-main`, `.ws-grid--main-action` | subscription layout toggle | **Keep** |
| `.ws-section-head--compact` | `section-header.component.ts` `[class.ws-section-head--compact]` | **Keep** |
| `.ws-section-lead--wide` | `section-header.component.ts` `[class.ws-section-lead--wide]` | **Keep** |
| `.ws-discord-*` | welcome preview, settings deep overrides | **Keep** |
| `.ws-detail-panel-close` | moderation, logs, RR detail panels | **Keep** |
| `.ws-feed-card*`, `.ws-filter-pill*` | activity timeline, filter bars | **Keep** |

### 3.2 Probably safe (no template references)

| Selector | File | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|------|----------:|---------|-------------------------|------|-----|
| `.ws-sticky-rail` | `workspace-layouts.css` | 4 (+ shared media queries) | Sticky rail helper (welcome removed class) | **0 HTML**; grouped with `.ws-aside--sticky` which **is used** | **Needs visual review** | Cannot delete whole rule block — split selector from `.ws-aside--sticky` first |
| `.ws-divider`, `.ws-divider--section` | `workspace-layouts.css` | 9 | Section dividers | **0 HTML** | **Probably safe** | Planned utility never adopted |
| `.ws-info-row`, `.ws-info-list` | `workspace-layouts.css` | 13 | Key-value info lists | **0 HTML** | **Probably safe** | Never adopted |
| `.ws-page-footer-hint` | `workspace-layouts.css` | 6 | Footer hint text | **0 HTML** | **Probably safe** | Never adopted |
| `.ws-section-title--emphasis` | `workspace-layouts.css` | 4 | Section title modifier | **0 HTML** | **Probably safe** | Never adopted |
| `.ws-grid--2` | `workspace-layouts.css` | 3 | Two-column grid | **0 HTML** | **Probably safe** | Never adopted |
| `.ws-grid--main-rail` | `workspace-layouts.css` | 3 | Main + rail grid variant | **0 HTML** | **Probably safe** | Never adopted |

---

## 4. Duplicate badge styles

| Location | Selectors | Canonical usage | Risk | Why |
|----------|-----------|-----------------|------|-----|
| `components.css` L333–375 | `.badge-success/open/error/closed/warning/brand` + `.ds-badge-*` aliases | Templates: `[class.badge-open]`, `[class.badge-success]`, `badge` + `data-status` | **Needs visual review** | Remove `.ds-badge-*` alias halves; keep tone rules |
| `design-system.css` L274–337 | `.badge-info/neutral/online/...` + `.ds-badge-*` | **0 direct class usage**; `status-badge` uses `[attr.data-status]` | **Safe** (extended palette) | Attribute-based badges may not need these classes |
| `status-badge.component.ts` | `class="badge"` + `[attr.data-status]` | Used in logs, moderation, RR, settings | **Keep** | Component relies on `[data-status]` CSS in `components.css`, not DS extended palette |

**Duplication note:** Badge tones exist in three layers: (1) `.badge-*` in `components.css`, (2) extended palette in `design-system.css`, (3) `[data-status]` rules in `components.css` / feature CSS. Consolidate to `[data-status]` + minimal `.badge-*` in a future pass.

---

## 5. Duplicate button styles

| Location | Selectors | Canonical usage | Risk | Why |
|----------|-----------|-----------------|------|-----|
| `components.css` L59–148 | `.btn*` + `.ds-btn*` comma groups | **40+ HTML files** use `.btn`, `.btn-primary`, `.btn-ghost`, `.btn-sm` | **Needs visual review** | Strip `.ds-*` aliases; do not remove `.btn*` |
| `design-system.css` L186–220 | `.btn-success`, `.ds-btn-success`, focus/loading | **0 HTML** for success/loading button classes | **Safe** | Unused extensions |
| Feature CSS | `.modules-module-card-primary`, `.page-workspace-hero-cta` | Local overrides on top of `.btn` | **Keep** | Intentional feature scoping |

---

## 6. Duplicate table styles

| Location | Selectors | Canonical usage | Risk | Why |
|----------|-----------|-----------------|------|-----|
| `components.css` `.ds-table*` | DS table wrapper | **0 HTML** | **Safe** | Unused |
| `components.css` `.table-card` | Admin/staff table containers | **6 HTML** — admin-guilds, admin-users, admin-plans, staff, moderation-settings, admin-upgrade-requests | **Keep** | Active admin pattern |
| Admin HTML | Plain `<table>` inside `.table-card` | No `ds-table` class | **Keep** | Styling from `.table-card table` rules in `components.css` |

---

## 7. Duplicate dialog styles

| Location | Selectors | Canonical usage | Risk | Why |
|----------|-----------|-----------------|------|-----|
| `design-system.css` L380–435 | `.confirm-dialog`, `.ds-dialog`, `.ds-dialog-overlay`, `.ds-dialog-footer` | **5 HTML** use `confirm-dialog card` (subscription, admin-upgrade-requests, logs) | **Keep** | Active — templates use `.confirm-dialog`, not `.ds-dialog` alone |
| Overlap | `.confirm-dialog` and `.ds-dialog` share same rule block | Only `confirm-dialog` appears in HTML | **Probably safe** | Remove `.ds-dialog` alias selectors from shared rules; keep `.confirm-dialog` |
| `.ds-dialog-overlay` | Overlay backdrop | **0 HTML** — dialogs use inline overlay markup in feature CSS | **Needs visual review** | May be applied via parent class in feature CSS; verify overlay markup before delete |

---

## 8. Page-width utilities — `components.css` L686–702

| Selector | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|----------:|---------|-------------------------|------|-----|
| `.page-narrow` | 3 | Narrow admin pages (720px) | **1 HTML** — `admin-plans` | **Keep** | Active |
| `.page-wide` | 3 | Wide admin pages | **3 HTML** — admin-home, admin-users, admin-upgrade-requests | **Keep** | Active |
| `.page-full` | 3 | Full-width admin/servers | **2 HTML** — servers, admin-guilds | **Keep** | Active |
| `.page-medium` | 3 | Medium width (960px) | **0 HTML** | **Probably safe** | Defined but never used |
| `.page-content` | 7 | Base page wrapper | **All feature pages** | **Keep** | Core layout |

---

## 9. Typography helpers — `design-system.css` L9–66

| Selector | Est. lines | Grep / reference result | Risk | Why |
|----------|----------:|-------------------------|------|-----|
| `.type-label` | 6 | **30+ HTML** | **Keep** | Heavily used |
| `.type-section-title` | 8 | **3 HTML** + subscription components | **Keep** | Active |
| `.type-card-title` | 7 | **1 HTML** — activity timeline | **Keep** | Active |
| `.type-overline`, `.type-caption` | 12 | **activity-timeline**, **status-strip** | **Keep** | Active |
| `.type-page-title` | 7 | **0 HTML** | **Safe** | Unused heading scale |
| `.type-subtitle` | 6 | **0 HTML** as class; referenced in `.confirm-dialog .type-subtitle` descendant rule | **Needs visual review** | Descendant rule may still apply if subtitle class added inside dialogs |
| `.type-body` | 5 | **0 HTML** | **Safe** | Unused |

---

## 10. Other `design-system.css` blocks

| Selector | Est. lines | Purpose | Grep / reference result | Risk | Why |
|----------|----------:|---------|-------------------------|------|-----|
| `.action-tile-grid`, `.action-tile` | 38 | Clickable action tiles (overview drawer pattern) | **0 HTML** | **Safe** | Never adopted in templates |
| `.metric-tile-grid`, `.metric-tile` | 15 | Metric inset tiles | **0 HTML** after MetricCard removal | **Probably safe** | Orphaned with CLEANUP-001 |
| `.conversation-list`, `.conversation-item*` | 64 | Ticket transcript timeline | **1 HTML** — `ticket-transcript.component.html` | **Keep** | Active on transcript page |
| `.delivery-indicator*`, `.delivery-badge` | 45 | Ticket delivery status | Check tickets feature CSS | **Needs visual review** | May overlap tickets-context-drawer local styles |
| `.internal-badge` | 8 | Internal note badge | Grep tickets — verify | **Needs visual review** | Possible transcript-only usage |

---

## 11. Profile preview vs `ws-discord-*` duplication

### Parallel implementations

| Aspect | Profile preview | Welcome / shared |
|--------|-----------------|------------------|
| **CSS file** | `profile-preview.component.css` (154 lines) | `workspace-layouts.css` L747–960 (~210 lines) |
| **Class prefix** | `.profile-preview-embed-*` (19 selectors) | `.ws-discord-*` (~30 selectors) |
| **HTML** | `profile-preview.component.html` | `welcome-discord-preview.component.html` |
| **Structure** | Static embed fields (support, website, rules) | Message + embed author + channel bar |
| **Shared concepts** | accent bar, embed body, title, description, thumb, fields, footer | accent bar, embed body, title, description, author avatar, footer |

### Selector mapping (consolidation candidates)

| Profile selector | Nearest `ws-discord-*` equivalent | Risk |
|------------------|-----------------------------------|------|
| `.profile-preview-embed` | `.ws-discord-embed` | **Needs visual review** |
| `.profile-preview-embed-accent` | `.ws-discord-embed-accent` | **Needs visual review** |
| `.profile-preview-embed-body` | `.ws-discord-embed-body` | **Needs visual review** |
| `.profile-preview-embed-title` | `.ws-discord-embed-title` | **Needs visual review** |
| `.profile-preview-embed-description` | `.ws-discord-embed-description` | **Needs visual review** |
| `.profile-preview-embed-thumb*` | *(no direct equivalent — welcome uses message avatar)* | **Keep** until shared thumb pattern added |
| `.profile-preview-embed-fields*` | *(no direct equivalent — profile-specific dl grid)* | **Keep** profile-only field grid |
| `.profile-preview-embed-footer` | `.ws-discord-embed-footer` | **Needs visual review** |

**Recommendation:** Phase 2 consolidation — migrate profile preview HTML to shared `ws-discord-*` classes where structure matches; retain profile-only field grid as a small local addon (~40 lines). **Do not delete either file until visual QA on Profile page.**

---

## 12. Feature CSS duplication (obvious)

| Feature file | Duplicates global | Est. overlap | Risk | Why |
|--------------|-------------------|-------------:|------|-----|
| `tickets-context-drawer.component.css` | `design-system.css` `.conversation-*` | ~55 lines | **Needs visual review** | Tickets drawer uses `tickets-conversation-*` BEM; transcript page uses global `.conversation-*`. Two parallel timeline stylings. |
| `tickets-filter-bar.component.css` | `.ws-toolbar` in `workspace-layouts.css` | ~15 lines | **Keep** | Local responsive overrides on top of `ws-toolbar` |
| `page-workspace-hero.component.css` | `.ws-atf` patterns | N/A | **Keep** | Hero is shared component with intentional local styling |
| Feature `::ng-deep` ATF overrides | `.page-workspace-hero` spacing | ~8 files | **Keep** | Per-page hero tuning (profile, logs, moderation, etc.) |

---

## 13. RTL — `rtl.css`

| Selector / block | Est. lines | Grep result | Risk | Why |
|------------------|----------:|-------------|------|-----|
| `html[dir='rtl'] .ds-table th/td` | 4 | Parent `.ds-table` unused | **Probably safe** | Remove with ds-table cleanup |
| `html[dir='rtl'] .ws-discord-preview` | 4 | Welcome preview active | **Keep** | RTL for live preview |
| Workspace/page RTL blocks (logs, tickets, welcome, RR) | ~60 | Matching workspace pages | **Keep** | Active RTL coverage |
| `.welcome-variables` | — | **Removed in CLEANUP-002** | — | Already deleted |

---

## 14. Miscellaneous `components.css` candidates

| Selector | Est. lines | Grep result | Risk | Why |
|----------|----------:|-------------|------|-----|
| `.request-card` | 6 | **0 HTML** | **Safe** | Unused admin layout helper |
| `.btn-block` | 3 | **0 HTML** | **Probably safe** | Utility never adopted |
| `.alert-success` | 6 | **0 HTML**; `.alert-error` used in login | **Needs visual review** | Remove success variant only if confirmed unused |
| `.alert-error` | 6 | **1 HTML** — login | **Keep** | Active |
| `.card-section-header` | 7 | **6 HTML** — settings tabs | **Keep** | Active settings pattern |

---

## 15. Recommended cleanup phases (future — not executed)

| Phase | Target | Est. lines | Risk gate |
|-------|--------|----------:|-----------|
| **CLEANUP-004a** | Remove `.ds-*` alias halves from comma-grouped rules in `components.css` | ~80 | Grep confirms 0 `ds-*` in templates |
| **CLEANUP-004b** | Delete unused `design-system.css` card/badge/action-tile/metric-tile blocks | ~200 | Visual spot-check admin + settings |
| **CLEANUP-004c** | Delete unused `.ws-*` utilities (not `.ws-aside--sticky` block) | ~40 | Split `.ws-sticky-rail` first |
| **CLEANUP-004d** | Remove `.ds-table*`, `.page-medium`, `.request-card`, `.type-page-title`, `.type-body` | ~70 | Build + RTL smoke test |
| **CLEANUP-005** | Migrate profile preview → `ws-discord-*` | ~110 net saved | Profile page visual QA EN + AR |
| **CLEANUP-006** | Consolidate ticket conversation styles | ~55 | Transcript + tickets drawer QA |

**Estimated bundle impact:** 15–35 KB raw CSS (~3–8 KB transferred after minification) if all Safe + Probably safe items removed.

---

## 16. Do not remove (confirmed active)

- `.btn*`, `.badge`, `.card`, `.input`, `.icon-btn` — canonical template classes
- `.ds-dropdown*` — shell menus
- `.ds-loading`, `.ds-empty` — shared state components
- `.ws-*` workspace shell (see §3.1)
- `.confirm-dialog` — modal confirmations
- `.conversation-list` — ticket transcript
- `.type-label`, `.type-section-title`, `.type-card-title`, `.type-caption`, `.type-overline`
- `.table-card`, `.page-content`, `.page-narrow/wide/full`
- `.ws-discord-*` — welcome preview studio
- `.profile-preview-embed-*` — until migration complete

---

*Generated by CLEANUP-003. No source CSS was modified.*
