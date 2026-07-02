# Dashboard Architecture

## Overview

`dashboard/DiscordBot.Dashboard` is an **Angular 16 SPA** that manages guild configuration through the REST API.

Stack: Angular 16, RxJS 7.8, `@ngx-translate/core` 15 (EN/AR), plain CSS (no component library).

## Application structure

```
src/app/
├── app.module.ts           # Root module (not standalone components)
├── app-routing.module.ts   # All routes
├── core/
│   ├── guards/             # auth, guild-access, admin
│   ├── interceptors/       # auth.interceptor.ts (JWT header, 401 logout)
│   ├── models/             # TypeScript interfaces per domain
│   ├── services/           # API clients
│   └── utils/              # api-error.util.ts
├── features/               # Page components (one folder per feature)
├── shared/                 # toast, empty-state, loading, onboarding-checklist
└── assets/i18n/            # en.json, ar.json
```

## Routing architecture

**File:** `src/app/app-routing.module.ts`

Layout: `DashboardLayoutComponent` wraps authenticated guild routes with sidebar navigation.

### Guard chain

```
AuthGuard → GuildAccessGuard (guild routes) → component
AuthGuard → AdminGuard (admin routes) → component
```

| Guard | Checks |
|-------|--------|
| `AuthGuard` | JWT exists in localStorage (`discord_bot_jwt`) |
| `GuildAccessGuard` | `GET /api/guilds/{id}/access`; `owner` vs `moderation` from route data |
| `AdminGuard` | `GET /api/auth/me` → `isAdmin` |

### Access levels

| Route data `guildAccess` | Required access field |
|--------------------------|----------------------|
| `owner` (default) | `canManageSettings` |
| `moderation` | `canAccessModeration` |

If user fails owner check but has moderation access, redirect to `/guilds/{id}/moderation`.

## Core services

| Service | Responsibility |
|---------|----------------|
| `AuthService` | Login redirect, token exchange, logout, `/api/auth/me` |
| `GuildService` | All `/api/guilds/{id}/*` endpoints (largest service) |
| `GuildAccessService` | Loads and caches guild access DTO |
| `GuildContextService` | Current guild name/icon for layout header |
| `AdminService` | `/api/admin/*` |
| `OnboardingService` | `/api/onboarding/status` |
| `LanguageService` | i18n locale switch |
| `ToastService` | User notifications |

## API communication

- Base URL from `environment.apiUrl`
- Development: `http://localhost:5217`
- Production: Railway URL (set in `environment.production.ts` or Vercel build)
- `AuthInterceptor` adds `Authorization: Bearer {token}`
- 401 responses trigger logout + redirect to `/login`

## Feature pages

| Feature | Component folder | Primary service methods |
|---------|------------------|----------------------|
| Servers | `features/servers/` | `getGuilds()` |
| Overview | `features/overview/` | `getOverview()`, `getModules()` |
| Settings | `features/settings/` | `getSettings()`, `updateSettings()` |
| Modules | `features/modules/` | `getModules()`, `updateModule()` |
| Tickets | `features/tickets/` | `getTicketSummaries()`, `getTicketConversation()`, `getTicketTranscript()`, `closeTicket()`, `sendTicketMessage()` |
| Moderation | `features/moderation/` | `getWarnings()`, `getModerationCases()` |
| Moderation Settings | `features/moderation-settings/` | Adapts to `permission-roles` API |
| Logs | `features/logs/` | `getLogs()`, `clearLogs()` |
| Reaction Roles | `features/reaction-roles/` | CRUD reaction role panels |
| Subscription | `features/subscription/` | `getSubscription()`, upgrade requests |
| Staff | `features/staff/` | `getStaff()` → permission-roles |
| Profile | `features/profile/` | `getProfile()`, `updateProfile()` |
| Admin | `features/admin/` | `AdminService` |

## Layout and navigation

**File:** `features/layout/dashboard-layout.component.ts`

Sidebar items visibility driven by `GuildAccessService`:

- Owner sections: overview, settings, modules, subscription, staff, profile, reaction-roles, moderation settings
- Moderation sections: tickets, moderation, logs

Platform admin link shown when `isAdmin` from auth/me.

## Internationalization

All user-visible strings use `| translate` pipe.

Keys organized by feature in `en.json` / `ar.json`. **Both files must be updated together.**

## State management

No NgRx. Component-local state + RxJS observables. `GuildContextService` holds minimal shared guild metadata.

## Build and deploy

```bash
npm run build -- --configuration production
```

Output: `dist/discord-bot.dashboard/`

**Vercel:** `vercel.json` configures SPA rewrites and cache headers for `index.html` and i18n JSON.

## Assumptions

- **JWT in localStorage** — XSS risk accepted for beta
- **No SSR** — pure client-side SPA
- **No WebSocket** — polling/refresh on navigation
- Permission UI lists are **hardcoded in TypeScript** (`staff.models.ts`) — must update when backend adds permissions

## Related docs

- `authorization.md`, `permission-system.md`
- `authentication.md`
- `deployment.md`
