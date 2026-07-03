# Components

## PageWorkspaceHero

**When:** Top of every guild workspace page.

**Inputs:** `icon`, `title`, `description`, `badge`, `stats`, `footerMessage`, `primaryAction`, `dismissible`, `loading`

**Do:** Use for page-level context, KPI strip, and one primary CTA.

**Don't:** Duplicate hero markup per page. Don't put forms inside the hero.

## SectionHeader

**When:** Repeated section titles with optional lead text (Profile sections, Modules categories, Subscription blocks).

```html
<app-section-header
  title="Section title"
  lead="Supporting copy"
  titleId="section-id"
  [emphasis]="true"
  [compact]="true"
></app-section-header>
```

**Do:** Use `emphasis` for Modules-style category headings. Use `compact` when previous local margin was 16px.

**Don't:** Define new `*-section-title` typography in page CSS.

## StatusBadge

**When:** Any inline status label.

```html
<app-status-badge label="Open" tone="success"></app-status-badge>
```

Tones map to `[data-status]` rules in `components.css`: `success`, `warning`, `danger`, `info`, `neutral`, `brand`, `premium`, `locked`, `enabled`, `disabled`, `open`, `closed`.

**Do:** Prefer `app-status-badge` or `<span class="badge" data-status="success">`.

**Don't:** Create page-local badge color rules.

## EmptyState / ErrorState / LoadingState

| State | Component |
|-------|-----------|
| Empty | `app-empty-state` |
| No results (in card) | `app-empty-state [nested]="true"` |
| Error + retry | `app-error-state` |
| Loading | `app-loading-state` |
| Skeleton loading | `app-loading-state [skeleton]="true"` |

## PageNotice

**When:** Beta notices, hints below hero.

```html
<app-page-notice accent="true">{{ 'key' | translate }}</app-page-notice>
```

## MetricCard

**When:** Standalone metric tiles outside the hero stat strip.

## CSS utilities (no component)

| Class | Use |
|-------|-----|
| `ws-page` | Page shell spacing |
| `ws-layout` | Vertical page stack |
| `ws-atf` / `ws-atf--band` | Hero band |
| `ws-grid--main-rail` | Main + sticky aside |
| `ws-grid--action-main` | Action column + reference |
| `ws-aside--sticky` | Sticky sidebar rail |
| `ws-master-detail--split` | Tickets queue + detail |
| `ws-toolbar` + `filter-pill` | Filter bars |
| `ws-placeholder-panel` | Dashed empty panel |
| `ws-icon-well` | Tone icon container |
| `hide-desktop` / `hide-mobile` | Responsive visibility |
