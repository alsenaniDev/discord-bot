# Colors

Defined in `src/styles/tokens.css`.

## Brand

- `--color-brand`, `--color-brand-hover`, `--color-brand-soft`

## Surfaces

| Token | Alias | Use |
|-------|-------|-----|
| `--color-bg-app` | — | App background |
| `--color-bg-card` | `--surface` | Cards, panels |
| `--color-bg-card-hover` | `--surface-hover` | Hover states |
| `--color-bg-elevated` | `--surface-raised` | Nested surfaces |
| `--color-bg-panel` | `--surface-overlay` | Toolbars, inset areas |

## Text

- `--color-text`, `--color-text-secondary`, `--color-text-muted`
- Semantic: `--color-text-success`, `--color-text-warning`, `--color-text-danger`, `--color-text-info`, `--color-text-brand`

## Status

- `--color-success` / `--color-success-soft`
- `--color-warning` / `--color-warning-soft`
- `--color-error` / `--color-error-soft`
- `--color-info` / `--color-info-soft`
- `--color-neutral` / `--color-neutral-soft`

## Usage

- Status colors via badges (`data-status` or `.badge-*` classes).
- Borders: `--color-border`, `--color-border-strong`, `--color-border-focus`.
- Do not hardcode hex values in feature CSS.
