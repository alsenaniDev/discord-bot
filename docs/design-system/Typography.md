# Typography

## Scale (`tokens.css`)

| Token | Size |
|-------|------|
| `--text-xs` | 0.75rem |
| `--text-sm` | 0.8125rem |
| `--text-base` | 0.9375rem |
| `--text-lg` | 1.0625rem |
| `--text-xl` | 1.25rem |
| `--text-2xl` | 1.5rem |
| `--text-3xl` | 1.875rem |

## Semantic roles

Use utility classes from `design-system.css`:

| Class | Role |
|-------|------|
| `.type-page-title` | Page H1 equivalent |
| `.type-section-title` | Section H2 |
| `.type-card-title` | Card headings |
| `.type-body` | Body copy |
| `.type-caption` | Muted supporting text |
| `.type-label` | Form / metric labels |
| `.type-overline` | Uppercase labels |

## Section headers

`app-section-header` applies:

- Default: `.ws-section-title` (`--text-lg`, weight 600)
- Emphasis: `.ws-section-title--emphasis` (`--text-xl`, weight 700) — Modules categories
- Lead: `.ws-section-lead` (`--text-sm`, muted)

## Rules

- Do not set `font-size` in page CSS for titles or leads.
- Use `app-section-header` or `.type-*` / `.ws-section-*` classes.
- Hero title typography is owned by `PageWorkspaceHeroComponent`.
