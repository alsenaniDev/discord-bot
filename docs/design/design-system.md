# Design System — Discord Bot Platform Dashboard

**Version:** PP-001 (Foundation)  
**Status:** Active — all new dashboard UI must follow this document  
**Source of truth:** `dashboard/DiscordBot.Dashboard/src/styles/`

---

## Overview

The dashboard design system provides one visual language across owner, staff, and admin experiences. It is implemented as **CSS tokens + global utility classes** (no separate component library yet).

**Import order** (in `styles.css`):

1. `tokens.css` — design tokens  
2. `base.css` — element defaults  
3. `components.css` — core primitives (cards, buttons, tables, forms)  
4. `design-system.css` — PP-001 extensions (variants, dialogs, typography, status)  
5. `layout.css` — shell (sidebar, topbar)  
6. `animations.css` / `rtl.css`

---

## Design tokens

Defined in `styles/tokens.css`.

### Spacing

| Token | Value |
|-------|-------|
| `--space-1` … `--space-12` | 0.25rem → 3rem scale |

Use spacing tokens for all padding, margin, and gap. **Never use raw `20px` margins.**

### Radius

| Token | Value | Use |
|-------|-------|-----|
| `--radius-sm` | 6px | Small buttons, inputs |
| `--radius-md` | 10px | Buttons, inputs, inset surfaces |
| `--radius-lg` | 14px | Cards, dialogs |
| `--radius-xl` | 18px | Hero elements |
| `--radius-full` | 9999px | Badges, pills |

### Elevation

| Token | Use |
|-------|-----|
| `--elevation-0` | Flat panels |
| `--elevation-1` / `--shadow-sm` | Default cards |
| `--elevation-2` / `--shadow-md` | Dropdowns |
| `--elevation-3` / `--shadow-lg` | Dialogs |
| `--elevation-brand` / `--shadow-glow` | Primary buttons |

### Typography

| Token | Size |
|-------|------|
| `--text-xs` | 0.75rem |
| `--text-sm` | 0.8125rem |
| `--text-base` | 0.9375rem |
| `--text-lg` | 1.0625rem |
| `--text-xl` | 1.25rem |
| `--text-2xl` | 1.5rem |
| `--text-3xl` | 1.875rem |

Fonts: `--font-sans` (Inter), `--font-ar` (Noto Sans Arabic), `--font-mono`.

### Surfaces

| Token | Use |
|-------|-----|
| `--color-bg-app` | Page background |
| `--color-bg-card` | Primary cards |
| `--color-bg-elevated` | Inset surfaces, secondary panels |
| `--color-bg-panel` | Table headers, secondary cards |
| `--color-bg-input` | Form fields |

**Legacy aliases** (PP-001 — do not use in new code):

- `--surface-elevated` → `--color-bg-elevated`
- `--border-color` → `--color-border`
- `--text-primary` → `--color-text`
- `--text-muted` → `--color-text-muted`

### Status colors

| Semantic | Background soft | Text |
|----------|-----------------|------|
| Success | `--color-success-soft` | `--color-text-success` |
| Warning | `--color-warning-soft` | `--color-text-warning` |
| Danger | `--color-danger-soft` | `--color-text-danger` |
| Info | `--color-info-soft` | `--color-text-info` |
| Neutral | `--color-neutral-soft` | `--color-text-muted` |
| Online | `--color-online-soft` | `--color-text-success` |
| Offline | `--color-offline-soft` | `--color-text-warning` |
| Pending | `--color-pending-soft` | `--color-text-warning` |
| Review | `--color-review-soft` | `--color-text-info` |
| Activated | `--color-activated-soft` | `--color-text-success` |
| Expired | `--color-expired-soft` | `--color-text-danger` |

**Rule:** Never invent new hex colors in feature CSS. Use tokens only.

### Container widths

| Class | Token | Width | Use for |
|-------|-------|-------|---------|
| `.page-narrow` | `--page-width-narrow` | 760px | Forms, profile, modules, staff |
| `.page-medium` | `--page-width-medium` | 960px | Settings, subscription, moderation |
| `.page-wide` | `--page-width-wide` | 1100px | Tables, tickets, logs, admin lists |
| `.page-full` | `--page-width-full` | 1200px | Overview, servers, admin guilds |

**Page shell pattern:**

```html
<div class="page-content page-medium">
  <!-- page content -->
</div>
```

### Breakpoints (reference)

| Token | Value |
|-------|-------|
| `--breakpoint-sm` | 640px |
| `--breakpoint-md` | 720px |
| `--breakpoint-lg` | 900px |
| `--breakpoint-xl` | 1024px |

### Z-index scale

| Token | Value | Use |
|-------|-------|-----|
| `--z-sticky` | 50 | Topbar |
| `--z-sidebar` | 100 | Sidebar |
| `--z-dropdown` | 200 | Dropdown menus |
| `--z-backdrop` | 900 | Mobile sidebar backdrop |
| `--z-dialog` | 1050 | Confirm dialogs |
| `--z-toast` | 1100 | Toasts |

---

## Typography hierarchy

Use semantic classes from `design-system.css`:

| Class | Element | Use |
|-------|---------|-----|
| `.type-page-title` | h1 | Page title (when not in topbar) |
| `.type-section-title` | h2 | Major section |
| `.type-card-title` | h3 | Card heading |
| `.type-subtitle` | p | Secondary heading text |
| `.type-body` | p | Body copy |
| `.type-caption` | span | Timestamps, hints |
| `.type-label` | span | Form labels |
| `.type-overline` | span | Section labels (uppercase) |

Base element sizes in `base.css` (`h1`–`h3`) remain valid; prefer type classes for new pages.

---

## Cards

Base: `.card` (alias `.ds-card`)

| Variant | Class | Use |
|---------|-------|-----|
| Primary | `.card` | Default content card |
| Secondary | `.card.card-secondary` | Muted background |
| Elevated | `.card.card-elevated` | Emphasized panel |
| Metric | `.card.card-metric` / `.stat-card` | KPI tiles |
| Status | `.card.card-status.is-success\|is-warning\|is-info\|is-brand` | Left accent border |
| Action | `.card.card-action` | Clickable card |
| Info | `.card.card-info` | Informational highlight |
| Empty | `.card.card-empty` | Full empty state |
| Compact | `.card.card-compact` | Reduced padding |
| Flush | `.card.card-flush` | Table wrapper (no padding) |
| Inset surface | `.surface-inset` | Nested row inside a card |

**Header pattern:**

```html
<section class="card">
  <div class="card-header-row">
    <h3 class="type-card-title">Section title</h3>
    <button class="btn btn-secondary btn-sm">Action</button>
  </div>
  <!-- body -->
</section>
```

---

## Buttons

Base: `.btn` (alias `.ds-btn`)

| Variant | Class |
|---------|-------|
| Primary | `.btn-primary` |
| Secondary | `.btn-secondary` |
| Ghost | `.btn-ghost` |
| Danger | `.btn-danger` |
| Success | `.btn-success` |
| Small | `.btn-sm` |
| Block | `.btn-block` |
| Icon | `.icon-btn` |
| Loading | `.btn.is-loading` |
| Action tile | `.action-tile` (grid: `.action-tile-grid`) |

**Focus:** All buttons use `:focus-visible` ring (brand color).

**Discord OAuth:** `.btn-discord` (full-width login)

---

## Forms

| Element | Class |
|---------|-------|
| Field wrapper | `.form-field` |
| Label + input (legacy) | `<label>` with child input |
| Checkbox row | `.checkbox` |
| Error text | `.field-error` |
| Hint text | `.hint` |
| Input class | `.input` (optional, inputs styled globally) |

Native `input`, `select`, `textarea` are styled globally. Invalid state: `.ng-invalid.ng-touched` or `.invalid`.

**Toggle:** `.toggle` with `.toggle-track`

---

## Tables

| Element | Class |
|---------|-------|
| Scroll wrapper | `.table-wrap` |
| Table | `.data-table` |
| Card wrapper | `.card.table-card` or `.card.card-flush` |
| Filters | `.filters-card` + `.filters-grid` |
| Section header | `.section-header` |
| Inline empty | `.empty-inline` |

Row hover and header styles are global. Use `.table-card` for tables inside cards (margin uses `--space-5`).

---

## Dialogs

**Always use global classes — never define `.confirm-overlay` in feature CSS.**

```html
<div class="confirm-overlay" role="presentation" (click)="close()">
  <div class="confirm-dialog is-sm" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
    <h3>Title</h3>
    <p class="muted">Body</p>
    <div class="confirm-actions">
      <button class="btn btn-secondary">Cancel</button>
      <button class="btn btn-primary">Confirm</button>
    </div>
  </div>
</div>
```

| Size | Class |
|------|-------|
| Small | `.is-sm` (28rem) |
| Default | (32rem) |
| Large | `.is-lg` (42rem) |

Z-index: `--z-dialog` (1050).

---

## Badges & status

Base: `.badge` (alias `.ds-badge`)

| Variant | Class |
|---------|-------|
| Success / Open | `.badge-success`, `.badge-open` |
| Error / Closed | `.badge-error`, `.badge-closed` |
| Warning | `.badge-warning` |
| Brand / Plan | `.badge-brand`, `.badge-plan` |
| Info | `.badge-info` |
| Neutral / Muted | `.badge-neutral`, `.badge-muted` |
| Online | `.badge-online` |
| Offline | `.badge-offline` |
| Pending | `.badge-pending` |
| Review | `.badge-review` |
| Activated | `.badge-activated` |
| Expired | `.badge-expired` |
| Health | `.badge-health[data-level='…']` |
| Priority | `.priority-badge[data-priority='High\|Medium\|Low']` |

Status pills (modules): `.status-pill.enabled`, `.disabled`, `.locked`

---

## Empty states

Component: `<app-empty-state>`

| Prop | Use |
|------|-----|
| `icon` | Emoji or character illustration |
| `title` | Heading |
| `description` | Supporting text |
| `nested="true"` | Inside cards — uses `.empty-state-nested` (no double card padding) |

Full-page empty: default (wraps in `.card`).  
In-card empty: `[nested]="true"`.

---

## Loading & skeletons

| Pattern | Class / Component |
|---------|-------------------|
| Full panel | `<app-loading-state>` / `.loading-panel` |
| Spinner | `.spinner`, `.spinner-lg`, `.spinner.inline` |
| Skeleton | `.skeleton`, `.skeleton-text`, `.skeleton-title`, `.skeleton-card` |

---

## Banners

| Variant | Class |
|---------|-------|
| Warning | `.banner-warning`, `.warning-banner` |
| Info | `.banner-info` |
| Success | `.banner-success` |
| Brand / Beta | `.banner-brand`, `.beta-notice` |

Beta billing notice uses `.card.beta-notice` (brand left border).

---

## Icons

Use `<app-ui-icon name="…" size="sm|md|lg">` for SVG icons.  
Emoji allowed in empty states only (until illustration set ships).

---

## RTL rules

1. Use **logical properties**: `margin-inline-*`, `padding-inline-*`, `inset-inline-*`, `border-inline-*`
2. Never use physical `left`/`right` for layout (except icon mirroring in `rtl.css`)
3. Tables: `text-align: start` (already global)
4. Select chevron position handled in `components.css` for `html[dir='rtl']`

---

## Page assignment (PP-001)

| Page | Width class |
|------|-------------|
| Overview | `page-full` |
| Servers | `page-full` |
| Modules, Profile, Staff, Mod Settings, Admin Plans | `page-narrow` |
| Settings, Subscription, Moderation, Transcript | `page-medium` |
| Tickets, Logs, Reaction Roles, Admin Home/Users/Changes | `page-wide` |
| Admin Guilds | `page-full` |

---

## Future rules (mandatory for new work)

1. **No page-local `max-width`** on `.page-content` — use width utility classes only  
2. **No duplicate dialog CSS** in feature components  
3. **No local `.badge` overrides** — extend global badge variants  
4. **No hardcoded hex** in component CSS — tokens only  
5. **No raw px margins** where a spacing token exists  
6. **No new button classes** — use `.btn` variants or `.action-tile`  
7. **Nested empty states** must use `[nested]="true"` on `app-empty-state`  
8. **New status colors** must map to existing semantic tokens  
9. **Feature CSS** is for layout unique to that page only — not for reinventing primitives  
10. **PR review gate:** Any PR adding component CSS for cards/buttons/dialogs/badges should be rejected

---

## Examples

### Standard settings section

```html
<div class="page-content page-medium page-stack">
  <section class="card">
    <div class="card-header-row">
      <h2 class="type-section-title">Welcome</h2>
    </div>
    <label class="form-field">
      <span>Channel ID</span>
      <input type="text" class="input" />
      <span class="hint">Users see this when they join.</span>
    </label>
    <div class="form-actions">
      <button class="btn btn-primary">Save</button>
    </div>
  </section>
</div>
```

### Table with filters

```html
<section class="card filters-card">
  <h2 class="type-section-title">Filters</h2>
  <div class="filters-grid">…</div>
</section>
<section class="card table-card card-flush">
  <div class="table-wrap">
    <table class="data-table">…</table>
  </div>
</section>
```

---

## Related documents

- [Product Review PR-001](../reviews/product-review-001.md) — audit that drove PP-001  
- [Progress PP-001](../progress/2026-07-03-PP-001-design-system.md)
