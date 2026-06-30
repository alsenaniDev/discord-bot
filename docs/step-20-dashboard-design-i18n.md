# Step 20 — Dashboard UI/UX Redesign + Internationalization

Presentation-only redesign of the Angular dashboard. No backend or API changes.

## Design system

CSS design tokens live in `src/styles/`:

| File | Purpose |
|------|---------|
| `tokens.css` | Colors, typography, spacing, radius, shadows, motion |
| `base.css` | Reset, body, headings, focus states |
| `components.css` | Buttons, cards, inputs, tables, badges, loading, skeletons, dropdowns |
| `layout.css` | Sidebar, topbar, breadcrumbs, responsive shell |
| `animations.css` | fadeIn, slideDown, shimmer, reduced-motion |
| `rtl.css` | RTL overrides for Arabic |

**Visual direction:** Dark gaming SaaS (Discord / Guilded inspired) — blurple accent `#5865f2`, dark surfaces, no flashy RGB.

**Global entry:** `src/styles.css` imports all partials.

## Folder structure

```
src/
├── assets/i18n/
│   ├── en.json
│   └── ar.json
├── styles/
│   ├── tokens.css
│   ├── base.css
│   ├── components.css
│   ├── layout.css
│   ├── animations.css
│   └── rtl.css
└── app/
    ├── core/services/language.service.ts
    ├── features/          # Page components (unchanged routes)
    └── shared/
        ├── ui/            # Reusable UI primitives
        │   ├── ui-icon/
        │   ├── language-switcher/
        │   ├── profile-menu/
        │   ├── server-switcher/
        │   ├── breadcrumbs/
        │   ├── empty-state/
        │   └── loading-state/
        ├── onboarding-checklist/
        └── toast-container/
```

## Component structure

### Layout shell (`dashboard-layout`)
- **Sidebar:** brand, server switcher, navigation (servers, guild, admin)
- **Topbar:** breadcrumbs, page title, Discord link, notifications placeholder, language switcher, profile menu
- **Content:** `<router-outlet>`

### Shared UI
- `app-ui-icon` — consistent Lucide-style SVG icons
- `app-loading-state` — spinner or skeleton loading
- `app-empty-state` — empty/error states with optional actions
- `app-language-switcher` — EN / AR runtime switch
- `app-profile-menu` — user avatar + logout
- `app-server-switcher` — switch between owned guilds

## Translation strategy

**Library:** `@ngx-translate/core` + `@ngx-translate/http-loader` (runtime switching).

**Files:** `assets/i18n/en.json`, `assets/i18n/ar.json`

**Usage in templates:**
```html
{{ 'common.settings' | translate }}
{{ 'onboarding.progress' | translate:{ completed: 3, total: 6 } }}
```

**Language service:** `LanguageService` (`core/services/language.service.ts`)
- Detects browser language (`ar*` → Arabic, else English)
- Persists choice in `localStorage` (`dashboard_lang`)
- Sets `document.documentElement.lang` and `dir` (`rtl` / `ltr`)

**Init:** `AppComponent.ngOnInit()` → `language.init()`

## RTL / LTR

- **English:** `dir="ltr"`, Inter font
- **Arabic:** `dir="rtl"`, Noto Sans Arabic font
- Logical CSS properties used where possible (`inset-inline-end`, `padding-inline-start`)
- Additional overrides in `styles/rtl.css`

## Language switcher

Top navigation → globe button → choose English or Arabic. Preference is remembered.

## How to add a new language

1. Copy `assets/i18n/en.json` → `assets/i18n/{code}.json` and translate all keys.
2. Add the code to `LanguageService.supportedLanguages` and `AppLanguage` type.
3. Add a label in `LanguageSwitcherComponent` (or extend the dropdown dynamically).
4. If the language is RTL, ensure `LanguageService.applyLanguage` sets `dir="rtl"`.

## How to add a new page

1. Create feature component under `features/{name}/` (`.ts`, `.html`, minimal `.css`).
2. Declare in `app.module.ts` and add route in `app-routing.module.ts`.
3. Add translation keys to **both** `en.json` and `ar.json` (e.g. `titles.myPage`, `myPage.loading`).
4. Use design system classes: `.card`, `.btn`, `.ds-table`, `.stats-grid`, etc.
5. Use shared components:
   ```html
   <app-loading-state [message]="'myPage.loading' | translate"></app-loading-state>
   <app-empty-state [title]="'myPage.empty' | translate"></app-empty-state>
   ```
6. Add sidebar link in `dashboard-layout.component.html` with `app-ui-icon` + translate pipe.
7. Extend `dashboard-layout.component.ts` `updateTitles()` for breadcrumbs and page title keys.

## Accessibility

- Focus-visible outlines on interactive elements
- ARIA labels on icon buttons (translated)
- `role="menu"` on dropdowns
- Color contrast tuned for dark theme
- `prefers-reduced-motion` respected in animations

## Run

```bash
cd dashboard/DiscordBot.Dashboard && npm start
```

Switch language from the top bar to verify RTL layout and Arabic strings.
