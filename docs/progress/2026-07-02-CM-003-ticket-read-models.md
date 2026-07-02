# CM-003 — Ticket Read Models & Conversation Projection

**Date:** 2026-07-02  
**Status:** Complete  
**Depends on:** CM-002 (Timeline write model), AR-001 (Read Model Architecture)

---

## Summary

Implemented the first official **Ticket Read Models** per AR-001:

1. **Ticket Summary Read Model** — paginated ticket list with activity stats, preview, and filters
2. **Ticket Conversation Read Model** — paginated presentation projection over Timeline Events with actor types and delivery state

Dashboard and bot archive now consume Read Model APIs. Timeline remains the write-model source of truth. No duplicate message store.

---

## Files Changed

### Infrastructure
- `src/DiscordBot.Infrastructure/Models/TicketReadModelDtos.cs` — read model DTOs + query types
- `src/DiscordBot.Infrastructure/Services/TicketReadService.cs` — **new** query projection service
- `src/DiscordBot.Infrastructure/Services/TicketService.cs` — granular ticket permissions on read/reply/close
- `src/DiscordBot.Infrastructure/Services/GuildAccessService.cs` — `CanViewTickets`, `CanReplyToTickets`, `CanCloseTickets`
- `src/DiscordBot.Infrastructure/Services/GuildPermissionResolver.cs` — permission helpers + `GuildAccessDto` fields
- `src/DiscordBot.Infrastructure/Models/StaffDtos.cs` — access DTO extended
- `src/DiscordBot.Infrastructure/DependencyInjection.cs` — register `ITicketReadService`

### API
- `src/DiscordBot.Api/Controllers/GuildsController.cs` — paginated summaries + conversation endpoints
- `src/DiscordBot.Api/Controllers/BotTicketsController.cs` — bot conversation endpoint

### Bot
- `src/DiscordBot.Bot/Api/Models/ApiModels.cs` — conversation API models
- `src/DiscordBot.Bot/Api/BotApiClient.cs` — `GetTicketConversationAsync`
- `src/DiscordBot.Bot/Services/TicketArchiveService.cs` — archive preview via conversation read model

### Dashboard
- `dashboard/.../core/models/ticket.models.ts` — read model interfaces
- `dashboard/.../core/models/staff.models.ts` — ticket permission flags on access
- `dashboard/.../core/services/guild.service.ts` — `getTicketSummaries`, `getTicketConversation`
- `dashboard/.../features/tickets/tickets.component.{ts,html,css}` — full UI update
- `dashboard/.../assets/i18n/en.json`, `ar.json`

### Documentation
- `docs/tickets/ticket-system-api.md`
- `docs/tickets/ticket-system-dashboard.md`
- `docs/tickets/ticket-system-database.md`
- `docs/architecture/read-model-architecture.md` (transitional status)
- `docs/architecture/api-design.md`

---

## Read Models Implemented

| Read Model | API | Source |
|------------|-----|--------|
| **Ticket Summary** | `GET /api/guilds/{id}/tickets` | `Tickets` + aggregated `TicketTimelineEvents` |
| **Ticket Conversation** | `GET /api/guilds/{id}/tickets/{ticketId}/conversation` | `TicketTimelineEvents` → presentation mapping |

Bot: `GET /api/bot/tickets/{ticketId}/conversation`

---

## API Changes

| Endpoint | Change |
|----------|--------|
| `GET /guilds/{id}/tickets` | Now returns `PaginatedTicketSummaryReadModel` with query params |
| `GET /guilds/{id}/tickets/{id}/conversation` | **New** — cursor-paginated conversation |
| `GET /guilds/{id}/tickets/{id}/timeline` | Legacy raw timeline (dashboard migrated off) |
| `GET /bot/tickets/{id}/conversation` | **New** — bot read model |

---

## Dashboard Changes

- Ticket list from Summary Read Model (stats, preview, last activity)
- Status filter + pagination
- Conversation panel with delivery badges (Queued / Delivered / Failed)
- Load-more cursor pagination for long conversations
- Reply/Close buttons gated by `canReplyToTickets` / `canCloseTickets` from access API

---

## Database Changes

**None.** Read models are query-time projections (AR-001 v0). Existing indexes from CM-002 (`TicketId, OccurredAt`) support list/conversation queries.

---

## Permission Checks

| Action | Server check |
|--------|--------------|
| List summaries | `ViewTickets` |
| Conversation | `ViewTickets` |
| Reply | `ReplyToTickets` |
| Close | `CloseTickets` |
| Owner / platform admin | Full ticket permissions via `GuildPermissionDefaults` |

---

## Validation Performed

| Check | Result |
|-------|--------|
| `dotnet build DiscordBot.sln` | ✅ Pass |
| `npm run build` (dashboard) | ✅ Pass |
| Code review: closed ticket readable without channel | ✅ Conversation queries DB only |
| Code review: pagination on list + conversation | ✅ Implemented |
| Code review: no EF entity exposure | ✅ Read DTOs only |

---

## Risks

1. **Summary query cost** — aggregating timeline stats per guild page may slow at very high ticket volume; materialized summary table may be needed later.
2. **Cursor pagination** — clients must pass both `cursorOccurredAt` and `cursorEventId` together.
3. **Legacy timeline endpoint** — still available; ensure future work does not reintroduce dashboard dependence on raw timeline.

---

## Remaining Work

- Dedicated ticket detail route/page (optional UX)
- Materialized Ticket Summary table if profiling shows need
- Ticket Statistics read model (analytics — out of CM-003 scope)
- Transcript export API (future)
- Remove legacy `GET /timeline` from dashboard docs entirely once bot migrates fully

---

## Suggested Next Task

**CM-004 — Ticket Statistics Read Model** or **permission split cleanup (CM-005)** — depending on product priority. Alternatively **transcript export** built on Conversation read model pagination.
