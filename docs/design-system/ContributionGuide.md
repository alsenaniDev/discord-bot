# Contribution Guide

## Before adding page CSS

1. Check `workspace-layouts.css` and `design-system.css` for an existing utility.
2. Check `shared/ui/` for an existing component.
3. Only add page CSS for behavior unique to that feature domain.

## Adding a component

1. Place under `src/app/shared/ui/<name>/`.
2. Declare in `app.module.ts` (project uses NgModule).
3. Document in `Components.md` with when / do / don't.
4. Use design tokens — no magic numbers.

## Badge tones

Add new tones to `components.css` `[data-status]` mapping **and** `StatusBadgeTone` type if using `app-status-badge`.

## Buttons

Allowed variants: `btn-primary`, `btn-secondary`, `btn-ghost`, `btn-danger`, link-style anchors. Sizes: `btn-sm`, default. No page-specific button skins.

## Visual parity rule

Refactors must preserve computed layout. When migrating to shared classes, compare spacing and typography token-for-token.

## Out of scope for DS sprints

- API / DTO / service changes
- Routing or permission changes
- Redesigning page information architecture

## Verification

```bash
cd dashboard/DiscordBot.Dashboard && npm run build
```

Test RTL (Arabic) and mobile breakpoints for any layout utility changes.
