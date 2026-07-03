# Motion

## Tokens

| Token | Value |
|-------|-------|
| `--duration-fast` / `--motion-fast` | 150ms |
| `--duration-normal` / `--motion-normal` | 250ms |
| `--duration-slow` / `--motion-slow` | 400ms |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` |

## Keyframes (`animations.css`)

- `fadeIn`, `scaleIn`, `slideUp`, `spin`, `shimmer`

## Rules

- Use token durations on transitions.
- Respect `prefers-reduced-motion: reduce` — disable sticky rails and backdrop blur where implemented.
- Hero and card hovers use `--duration-fast`.
