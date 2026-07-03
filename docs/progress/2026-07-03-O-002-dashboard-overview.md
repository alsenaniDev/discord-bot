# O-002 — Dashboard Overview Experience

**Date:** 2026-07-03  
**Status:** Complete  
**Sprint:** O-002  
**Alignment:** O-001 · PB-001 · UX-001

---

## Summary

Replaced the guild **Overview** landing page with an operational dashboard that answers four questions in under 10 seconds:

1. Is my community healthy?
2. What should I do next?
3. What happened recently?
4. What are the fastest actions I can take?

Implementation follows O-001 Phase A/B/C activation model, rule-based Community Health (0–100), prioritized recommendation cards, permission-aware quick actions, and structured analytics events (console in dev).

---

## Files changed

### Backend

| File | Change |
|------|--------|
| `src/DiscordBot.Infrastructure/Models/GuildOverviewExperienceDtos.cs` | **New** — experience DTOs |
| `src/DiscordBot.Infrastructure/Services/GuildOverviewExperienceService.cs` | **New** — health, activation, recommendations, activity |
| `src/DiscordBot.Infrastructure/Models/GuildDtos.cs` | `Experience` on `GuildOverviewDto` |
| `src/DiscordBot.Infrastructure/Services/GuildService.cs` | Compose experience on overview |
| `src/DiscordBot.Infrastructure/DependencyInjection.cs` | Register service |

### Dashboard

| File | Change |
|------|--------|
| `dashboard/.../core/models/guild.models.ts` | Experience types |
| `dashboard/.../core/services/analytics.service.ts` | **New** — structured event logging |
| `dashboard/.../features/overview/overview.component.ts` | Full rewrite |
| `dashboard/.../features/overview/overview.component.html` | Full rewrite |
| `dashboard/.../features/overview/overview.component.css` | Grid layout, skeletons, mobile |
| `dashboard/.../assets/i18n/en.json` | Overview strings |
| `dashboard/.../assets/i18n/ar.json` | Overview strings (RTL) |

### Documentation

| File | Change |
|------|--------|
| `docs/project-management/release-notes.md` | O-002 entry |

---

## New APIs

No new routes. Extended existing endpoint:

```
GET /api/guilds/{id}/overview
```

Response adds `experience`:

| Field | Description |
|-------|-------------|
| `subscription` | Plan name, key, status, expiry |
| `botOnline` | Active + synced within 7 days |
| `activation` | Phase A/B/C steps, progress %, primary CTA |
| `health` | Score 0–100, level, factor breakdown |
| `recommendations` | Top 3 scored cards (id, priority, route) |
| `recentActivity` | Last 8 events (logs, tickets, modules) |

Business logic lives in `GuildOverviewExperienceService` (not duplicated in Angular).

---

## Dashboard sections

| Section | Content |
|---------|---------|
| **Community header** | Icon, name, plan badge, bot online/offline, health badge, activation % |
| **Activation progress** | Progress bar, step list, primary CTA (hidden when activated) |
| **Community health** | Score, level, factor list with ✓/⚠ |
| **Recommendations** | Up to 3 cards with priority badge + CTA |
| **Quick actions** | Grid filtered by `GuildAccess` + module plan gates |
| **Recent activity** | Merged log/ticket/module events |
| **At a glance** | Channels, roles, ticket counts |

Empty states on every widget per O-001. Skeleton loading (no raw text spinner-only page).

---

## Analytics

`AnalyticsService` emits structured events (dev console):

| Event | When |
|-------|------|
| `OverviewViewed` | Page load success |
| `ActivationProgressViewed` | Activation data present |
| `HealthCardViewed` | Health data present |
| `ActivationPrimaryCtaClicked` | Activation CTA |
| `RecommendationClicked` | Recommendation card |
| `QuickActionClicked` | Quick action button |

No third-party analytics SDK in this sprint.

---

## Validation

| Check | Result |
|-------|--------|
| `dotnet build DiscordBot.sln` | Pass |
| `npm run build` | Pass (bundle budget warning pre-existing) |

### Manual verification

1. Open `/guilds/:id/overview` as guild owner
2. Confirm header badges, health card, recommendations
3. Resize to mobile — cards stack vertically
4. Staff user with limited permissions — quick actions filtered
5. Restart API after deploy so `experience` is populated

---

## Screenshots

Not captured in CI. Verify locally after `dotnet run` + dashboard dev server.

---

## Remaining work

- Welcome Wizard modal (O-001 W0–W6) — separate sprint
- `ActivationGoalSelected` / `FirstValueAchieved` persisted events (not inferred from config)
- Server-side analytics pipeline
- Bot heartbeat for accurate online status (today: sync recency heuristic)
- Activity item i18n keys (messages currently English from API)
- Admin activation funnel dashboard (O-001 §11)

---

## Suggested next sprint (O-003)

1. Welcome Wizard UI on overview (resume from `activation.currentStepKey`)
2. Persist activation goal + first value on guild record
3. Owner rejection/expiry banners (UX-001 alternate journeys)
4. Wire analytics to structured backend log table or external tool

---

## Related docs

- [First-Time User Activation (O-001)](../ux/first-time-user-activation.md)
- [O-001 Progress Report](./2026-07-03-O-001-first-time-user-activation.md)
