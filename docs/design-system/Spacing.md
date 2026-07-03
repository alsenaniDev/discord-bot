# Spacing

All spacing uses tokens from `src/styles/tokens.css`.

| Token | Value |
|-------|-------|
| `--space-1` | 0.25rem |
| `--space-2` | 0.5rem |
| `--space-3` | 0.75rem |
| `--space-4` | 1rem |
| `--space-5` | 1.25rem |
| `--space-6` | 1.5rem |
| `--space-8` | 2rem |
| `--space-10` | 2.5rem |
| `--space-12` | 3rem |

## Workspace rhythm

| Token | Default | Purpose |
|-------|---------|---------|
| `--ws-zone-gap` | `clamp(--space-10, 4vw, --space-14)` | Page vertical rhythm |
| `--ws-block-gap` | `--space-8` | Grid column gap |
| `--ws-section-gap` | `--space-10` | Section stack gap |

## Rules

- Use tokens only — no raw `px` for spacing in new code.
- Prefer logical properties: `padding-block`, `margin-inline`, `inset-block-start`.
- Panel padding standard: `ws-panel-pad` → `var(--space-5)`.
