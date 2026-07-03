# PP-001 — Design System Foundation

**Date:** 2026-07-03  
**Status:** Complete  
**Sprint:** PP-001  
**Scope:** UI consistency only — no feature or API changes

---

## Summary

Established a **unified visual foundation** for the Discord Bot Platform dashboard by extending design tokens, introducing `design-system.css`, consolidating duplicate CSS across 20+ feature files, standardizing page widths, dialogs, badges, and RTL fixes.

**Build:** `npm run build` — **passed** (717 KB initial bundle, pre-existing budget warning).

**Outcome:** Pages now share one token-backed language. New pages should follow [`docs/design/design-system.md`](../design/design-system.md).

---

## Phase 1 — Audit inventory

| Element | Before | After |
|---------|--------|-------|
| Cards | `.card`, local overrides in overview/subscription | Variants in `design-system.css` |
| Buttons | `.btn`, overview `.quick-action-btn` | `.btn` + `.action-tile` |
| Badges | Global + overview local `.badge` + admin-plans duplicate | Single badge system + status variants |
| Dialogs | 3 copies with z-index 100 vs 1000 | One system at `--z-dialog` (1050) |
| Forms | `.form-field` + bare labels | Documented; global input styles unchanged |
| Tables | Shared `.data-table`; margin `20px` magic number | Token margins |
| Empty states | Card-in-card padding issue | `[nested]="true"` on `app-empty-state` |
| Page widths | 720–1200px per component CSS | 4 utility classes on HTML |
| Tokens | Undefined `--surface-elevated`, etc. | Aliases + extended status palette |

---

## Components unified

- **Cards:** primary, secondary, elevated, metric, status, action, info, compact, flush, inset (`.surface-inset`)
- **Buttons:** primary, secondary, ghost, danger, success, sm, icon, loading, action tiles
- **Badges:** success, error, warning, brand, info, neutral, online, offline, pending, review, activated, expired, health, priority
- **Dialogs:** overlay + dialog + footer actions (sm/md/lg)
- **Banners:** warning, info, success, brand/beta
- **Typography:** `.type-page-title` through `.type-overline`
- **Conversation timeline:** shared ticket/transcript styles with RTL-safe borders
- **Metric tiles:** `.metric-tile-grid` / `.metric-tile` (overview stats)

---

## Tokens added

**File:** `dashboard/.../styles/tokens.css`

- Page widths: `--page-width-narrow|medium|wide|full`
- Breakpoints: `--breakpoint-sm|md|lg|xl`
- Elevation aliases: `--elevation-0` … `--elevation-brand`
- Z-index scale: `--z-dropdown` … `--z-toast`
- Legacy aliases: `--surface-elevated`, `--border-color`, `--text-primary`, `--text-muted`
- Extended status: online, offline, pending, review, activated, expired, neutral
- Dialog: `--dialog-overlay`, `--dialog-width-sm|md|lg`

---

## Files changed

### New

| File | Purpose |
|------|---------|
| `src/styles/design-system.css` | PP-001 component extensions |
| `docs/design/design-system.md` | Authoritative DS documentation |

### Core styles

| File | Changes |
|------|---------|
| `src/styles/tokens.css` | Extended tokens + aliases |
| `src/styles.css` | Import `design-system.css` |
| `src/styles/components.css` | Page width tokens, table-card margin, z-index |

### Shared components

| File | Changes |
|------|---------|
| `shared/ui/empty-state/empty-state.component.ts` | `nested` input for in-card empties |
| `shared/ui/member-select/member-select.component.css` | RTL logical positioning |

### Feature pages (HTML width classes + CSS cleanup)

| Area | HTML | CSS reduced |
|------|------|-------------|
| Overview | `page-full`, action tiles, nested empties, metric tiles | ~180 lines removed |
| Subscription | `page-medium`, status card | Dialog CSS removed |
| Logs | `page-wide` | Dialog CSS removed |
| Tickets + transcript | `page-wide` / `page-medium` | Conversation styles moved to DS |
| Admin upgrade requests | `page-wide` | Dialog/form duplicate CSS removed |
| All other guild/admin pages | Width class assigned | `max-width` overrides removed |

**Total feature CSS files touched:** 18  
**HTML templates updated:** 17

---

## CSS removed (duplicate patterns)

| Pattern | Removed from |
|---------|--------------|
| `.confirm-overlay` / `.confirm-dialog` | subscription, logs, admin-upgrade-requests |
| Local `.badge` block | overview |
| `.quick-action-btn` / `.quick-actions-grid` | overview (→ `.action-tile`) |
| `.stats-grid-compact` | overview (→ `.metric-tile-grid`) |
| Duplicate `.progress-bar` styling | overview (uses global) |
| `.page-content { max-width }` | 12 component CSS files |
| `.badge-success` / `.badge-muted` duplicate | admin-plans |
| Conversation item duplicates | tickets, ticket-transcript |
| Magic `20px` table margins | components.css → `--space-5` |

**Estimated duplicate CSS removed:** ~350 lines

---

## Screens affected

All dashboard routes:

- `/servers`, `/guilds/:id/overview`, `/modules`, `/settings`, `/subscription`
- `/tickets`, `/logs`, `/moderation`, `/staff`, `/moderation-settings`
- `/reaction-roles`, `/profile`
- `/admin/*` (home, guilds, users, plans, subscription changes)

Navigation shell unchanged (layout.css).

---

## Validation

| Check | Result |
|-------|--------|
| `npm run build` | Pass |
| Dark mode | Supported (single dark theme) |
| RTL | Ticket borders + member-select fixed; logical properties in DS |
| LTR | Default — unchanged behavior |
| Visual regressions | Not manually screenshot-tested; build + class audit clean |

---

## Remaining inconsistencies

| Item | Priority | Notes |
|------|----------|-------|
| Emoji empty-state icons | P3 | Documented; SVG set future sprint |
| Settings tabs unique pattern | OK | Documented as acceptable variant |
| `.card-section-header` vs `.card-header-row` | P2 | Both exist; prefer `.card-header-row` |
| Dialog templates still add `.card` class | P3 | Redundant padding source; harmless |
| Bundle size 717 KB | P2 | Pre-existing; not DS scope |
| Some pages use bare `<label>` not `.form-field` | P2 | Migrate in form polish sprint |
| Admin 13-column table mobile | P1 | Layout sprint, not DS foundation |
| Angular components for Card/Button not extracted | P2 | CSS-first DS; component wrappers future |

---

## Suggested next sprint

**PP-002 — Component Extraction & Form Polish**

1. Angular `PageShell`, `ConfirmDialog`, `FormField` wrappers  
2. Migrate settings/logs forms to `.form-field` consistently  
3. Replace emoji empty states with SVG illustration set  
4. ESLint/stylelint rule: ban undefined CSS vars in feature CSS  
5. Admin table responsive card layout on mobile  

---

## Related

- [Design System Reference](../design/design-system.md)
- [Product Review PR-001](../reviews/product-review-001.md)
