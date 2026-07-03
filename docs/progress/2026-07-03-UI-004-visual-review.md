# UI-004 Visual Review — Activity Timeline

**Date:** 2026-07-03  
**Status:** Awaiting manual screenshots from running app

---

## Cleanup confirmation

Dev preview route and component were fully removed from the Angular app.

| Check | Result |
|-------|--------|
| Visual review component files | Deleted |
| `dev/ui-004-review` route | Removed |
| `AppModule` declaration | Removed |
| `npm run build` | Pass |
| Visual-review in production `dist/` | None |

---

## Visual review process

Review is **manual only** — real screenshots from `/guilds/:id/overview` while the app is running locally.

**Capture:**

1. English LTR — switch language to EN, open Overview
2. Arabic RTL — switch language to AR, open Overview

**Save to:** `docs/screenshots/ui-004/` (suggested names: `overview-en-ltr.png`, `overview-ar-rtl.png`)

**Verify:**

- Timeline not too decorative; spine subtle
- Panel does not overpower Mission Card
- Arabic activity readable; no raw English API strings
- Mission Card remains primary; Pulse does not compete with Activity
- RTL alignment correct

---

## Do not proceed

**UI-005 Context Drawer** starts only after manual screenshots are reviewed and approved.

No dev routes. No mock UI routes. No JWT capture automation.
