# CM-002 — Ticket Timeline Foundation

**Date:** 2026-07-02  
**Status:** Complete  
**Blueprint:** D-001 (Ticket Domain Blueprint)

---

## Summary

Implemented **Ticket Timeline v1** as the business source of truth for ticket history. Discord ticket channel messages and dashboard staff replies flow through Timeline Events before (or alongside) delivery mechanics. Logging (`LogEntry`) remains separate and consumes domain events — Timeline is not duplicated in logs.

This is **not** a generic message log table. It implements the seven Timeline Event types defined in D-001 §8 for Sprint 1.

---

## Files Changed

### Domain
- `src/DiscordBot.Domain/Enums/TicketTimelineEventType.cs` — 7 event types
- `src/DiscordBot.Domain/Entities/TicketTimelineEvent.cs` — append-only entity
- `src/DiscordBot.Domain/Entities/Ticket.cs` — `TimelineEvents` navigation
- `src/DiscordBot.Domain/Entities/TicketOutboundMessage.cs` — delivery failure + timeline link

### Infrastructure
- `src/DiscordBot.Infrastructure/Services/TicketTimelineService.cs` — append, read, message/archive recording
- `src/DiscordBot.Infrastructure/Services/TicketService.cs` — timeline integration on create/close/reply/ack
- `src/DiscordBot.Infrastructure/Models/TicketTimelineDtos.cs` — DTOs
- `src/DiscordBot.Infrastructure/Data/Configurations/TicketTimelineEventConfiguration.cs`
- `src/DiscordBot.Infrastructure/Data/AppDbContext.cs`
- `src/DiscordBot.Infrastructure/DependencyInjection.cs`
- `src/DiscordBot.Infrastructure/Migrations/20260702195029_AddTicketTimelineEvents.cs`

### API
- `src/DiscordBot.Api/Controllers/BotTicketsController.cs` — bot timeline + delivery ack body
- `src/DiscordBot.Api/Controllers/GuildsController.cs` — dashboard timeline GET

### Bot
- `src/DiscordBot.Bot/Services/TicketTimelineMessageService.cs` — Discord → `MessageSent`
- `src/DiscordBot.Bot/Services/TicketOutboundMessageService.cs` — delivery failure ack
- `src/DiscordBot.Bot/Services/TicketArchiveService.cs` — timeline-based preview + `ArchivePosted`
- `src/DiscordBot.Bot/Services/DiscordBotHostedService.cs` — message handler wiring
- `src/DiscordBot.Bot/Services/TicketChannelCleanupService.cs` — archive signature
- `src/DiscordBot.Bot/Commands/TicketCommandHandlers.cs` — pass ticket id to archive
- `src/DiscordBot.Bot/Api/BotApiClient.cs`, `ApiModels.cs`
- `src/DiscordBot.Bot/Program.cs`

### Dashboard
- `dashboard/.../core/models/ticket.models.ts`
- `dashboard/.../core/services/guild.service.ts`
- `dashboard/.../features/tickets/tickets.component.{ts,html,css}`
- `dashboard/.../assets/i18n/en.json`, `ar.json`

### Documentation
- `docs/tickets/ticket-system-database.md`
- `docs/tickets/ticket-system-api.md`
- `docs/tickets/ticket-system-dashboard.md`
- `docs/tickets/ticket-system-bot.md`

---

## Database Changes

**Migration:** `20260702195029_AddTicketTimelineEvents`

| Change | Detail |
|--------|--------|
| New table | `TicketTimelineEvents` |
| Outbound columns | `DeliveryFailed`, `DeliveryFailureReason`, `StaffReplyQueuedTimelineEventId` |
| Index | Pending outbound: `(GuildId, IsDelivered, DeliveryFailed, CreatedAt)` |
| Dedup | UNIQUE `(TicketId, DiscordMessageId)` where message id present |

Apply with:

```bash
dotnet ef database update --project src/DiscordBot.Infrastructure --startup-project src/DiscordBot.Api
```

---

## Timeline Event Types Implemented

| Event | Source |
|-------|--------|
| `TicketCreated` | `TicketService.CreateTicketAsync` |
| `MessageSent` | Bot `TicketTimelineMessageService` → API |
| `StaffReplyQueued` | Dashboard reply → `TicketService.SendTicketMessageAsync` |
| `StaffReplyDelivered` | Bot ack success |
| `StaffReplyFailed` | Bot ack failure |
| `StatusChanged` | Ticket close (Discord + dashboard) |
| `ArchivePosted` | Bot archive after embed posted |

---

## D-001 Rules Implemented

| Rule | Implementation |
|------|----------------|
| **BR-C06** | `TicketCreated` on ticket creation |
| **BR-T01** | Discord messages → `MessageSent` (ticket channels only) |
| **BR-T02** | Dashboard reply → `StaffReplyQueued` before outbound queue; delivery updates via Delivered/Failed |
| **BR-T03** | Append-only Timeline Events (`TicketTimelineService.AppendEventAsync`) |
| **BR-T05** | `ArchivePosted` is system notification with metadata, not impersonating message author |
| **BR-S03** | `StatusChanged` on close with metadata (from/to status, source) |
| **BR-X03** | Archive preview built from Timeline API, not Discord history scrape |

---

## Build Status

| Project | Result |
|---------|--------|
| `DiscordBot.Api` | ✅ Build succeeded |
| `DiscordBot.Bot` | ✅ Build succeeded |
| `DiscordBot.Dashboard` (`ng build`) | ✅ Build succeeded |
| EF migration | ✅ Created (`AddTicketTimelineEvents`) |

---

## Manual Tests Performed

| Test | Method |
|------|--------|
| Solution compiles | `dotnet build`, `npm run build` |
| Migration generates | `dotnet ef migrations add` |
| Code review of flows | Create → MessageSent path, reply → Queued → Delivered/Failed, close → StatusChanged, archive → ArchivePosted |

**Note:** End-to-end runtime tests against a live Discord guild + PostgreSQL were not executed in this session. Apply migration and verify in a dev environment before production.

---

## Risks

1. **Pre-CM-002 tickets** — No retroactive Timeline backfill; archive previews for old tickets may be sparse.
2. **Existing outbound rows** — Migration sets `StaffReplyQueuedTimelineEventId` to empty Guid for legacy queue rows; delivered rows are unaffected.
3. **Bot message filter** — Empty-content and bot messages are skipped; attachments not captured (out of scope).
4. **Coarse dashboard permissions** — Timeline uses `CanAccessModerationPagesAsync` until CM-005 permission split.

---

## Remaining Work

- Paginated timeline / ticket detail page (CM-003 transcript UX)
- Permission-aware timeline access (`ViewTickets`)
- Retroactive timeline backfill tool (optional)
- Staff reply content on `StaffReplyDelivered` (optional enrichment)
- Update `ticket-system-future.md` roadmap labels (still references old CM-002 wording)

---

## Suggested Next Task

**CM-003 — Transcript API & ticket detail UX:** Paginated timeline read model, ticket detail page, and richer conversation view built on Timeline (not a second message store).
