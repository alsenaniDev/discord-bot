# Accessibility

## Focus

- Buttons and links use `:focus-visible` outlines (brand color, 2px offset).
- `PageWorkspaceHero` primary CTA is a real `<button>`.
- Tickets detail title receives focus on ticket select (`tabindex="-1"`).

## Landmarks

- Hero: `role="region"` with `aria-label`
- Sections: `role="region"` + `aria-labelledby`
- Filter pills: `role="tablist"` / `role="tab"` with `aria-selected`
- Error states: use meaningful titles, not color alone

## RTL

- Use logical CSS properties (`inline-start`, `block-end`, `margin-inline`).
- `rtl.css` sets direction on workspace grids and ticket panels.
- Sidebar slide direction is handled in `layout.css`.

## Motion

- Sticky rails fall back to `position: static` under `prefers-reduced-motion`.
- Avoid essential information in motion-only cues.

## Loading

- `app-loading-state` uses `role="status"` and `aria-label` from message input.
- Skeleton mode preserves layout without implying loaded content.
