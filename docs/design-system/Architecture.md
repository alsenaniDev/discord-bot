# Design System Architecture

UI-DS-001 consolidates the Discord Bot Platform dashboard visual language into shared tokens, CSS utilities, and Angular components.

## Layer model

```
tokens.css          → Design tokens (colors, spacing, typography, motion)
base.css            → Reset and element defaults
components.css      → Core primitives (buttons, badges, forms, tables)
design-system.css   → Extended patterns (cards, dialogs, metric tiles, typography)
workspace-layouts.css → Guild workspace shells, grids, rails, toolbars
layout.css          → Dashboard chrome (sidebar, topbar, content area)
animations.css      → Keyframes
rtl.css             → RTL overrides
```

Load order is fixed in `src/styles.css`. Page components should prefer shared layers before adding local CSS.

## Component layer

Shared Angular components live under `src/app/shared/ui/`:

| Component | Selector | Purpose |
|-----------|----------|---------|
| PageWorkspaceHero | `app-page-workspace-hero` | Page hero with stats, badge, CTA |
| SectionHeader | `app-section-header` | Section title + lead copy |
| StatusBadge | `app-status-badge` | Unified status badge |
| EmptyState | `app-empty-state` | Empty / no-results states |
| ErrorState | `app-error-state` | Error state with optional retry |
| LoadingState | `app-loading-state` | Spinner or skeleton loading |
| PageNotice | `app-page-notice` | Inline page hints / beta notices |
| MetricCard | `app-metric-card` | Label + value metric tile |

## Page integration pattern

Mature workspace pages compose:

1. `page-content page-workspace ws-page`
2. `ws-layout` column shell
3. `ws-atf` (+ optional `ws-atf--band`) for hero band
4. `app-page-workspace-hero`
5. `ws-workspace` / `ws-grid` for content
6. Shared state components for loading, error, empty

Page-local CSS should only cover domain-specific layout (forms, tables, master/detail mechanics).

## Adopted pages

- Overview
- Subscription
- Server Profile
- Modules
- Tickets

Logs, Moderation, and other pages remain on legacy patterns until their UI sprints.
