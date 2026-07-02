# CM-004 — Transcript & Archive Honesty

**Date:** 2026-07-02  
**Status:** Complete  
**Depends on:** CM-002 (Timeline), CM-003 (Read Models), AR-001

---

## Summary

Implemented the first **honest Transcript** experience and separated it from **Archive**:

- **Archive** = Discord notification digest (short summary from Timeline, capped preview)
- **Transcript** = full durable record from Timeline, available in Dashboard via dedicated API + page

Archive embed copy no longer implies full history is stored in Discord. When configured, archive embeds link to the Dashboard transcript route.

---

## Files Changed

### Infrastructure
- `src/DiscordBot.Infrastructure/Models/TicketReadModelDtos.cs` — `TicketTranscriptReadModel`, metadata, query types
- `src/DiscordBot.Infrastructure/Services/TicketReadService.cs` — `GetTicketTranscriptAsync` (ViewTickets + internal note filter)
- `src/DiscordBot.Infrastructure/Models/CommandPanelModels.cs` — `GuildId` on cleanup DTO
- `src/DiscordBot.Infrastructure/Services/TicketService.cs` — populate `GuildId` in pending cleanups

### API
- `src/DiscordBot.Api/Controllers/GuildsController.cs` — `GET .../transcript`

### Bot
- `src/DiscordBot.Bot/Services/TicketArchiveService.cs` — digest wording, transcript URL, honest log message
- `src/DiscordBot.Bot/Services/EmbedBuilderService.cs` — `BuildTicketArchive` digest + transcript link field
- `src/DiscordBot.Bot/Commands/TicketCommandHandlers.cs` — pass platform `GuildId` to archive
- `src/DiscordBot.Bot/Api/Models/ApiModels.cs` — `GuildId` on `TicketCleanupApiResponse`

### Dashboard
- `dashboard/.../core/models/ticket.models.ts` — transcript interfaces
- `dashboard/.../core/services/guild.service.ts` — `getTicketTranscript`
- `dashboard/.../features/tickets/ticket-transcript.component.{ts,html,css}` — **new** transcript page
- `dashboard/.../features/tickets/tickets.component.html` — "View transcript" for closed tickets
- `dashboard/.../app-routing.module.ts` — transcript route
- `dashboard/.../app.module.ts` — register component
- `dashboard/.../features/layout/dashboard-layout.component.ts` — page titles
- `dashboard/.../assets/i18n/en.json`, `ar.json` — transcript UI + archive hint fix

### Documentation
- `docs/tickets/ticket-system-api.md`
- `docs/tickets/ticket-system-dashboard.md`
- `docs/tickets/ticket-system-bot.md`
- `docs/tickets/ticket-system-future.md`

---

## API Changes

### New endpoint

`GET /api/guilds/{guildId}/tickets/{ticketId}/transcript`

- **Permission:** `ViewTickets`
- **Response:** `TicketTranscriptReadModel` (metadata + paginated `entries` from Timeline)
- **Pagination:** Same cursor params as `/conversation`
- **Internal notes:** Filtered unless caller has `ReplyToTickets`
- **No Discord channel dependency** — works after channel deletion

---

## Dashboard Changes

- Route: `/guilds/:id/tickets/:ticketId/transcript`
- Shows ticket metadata, Archive vs Transcript notice, Timeline entries, delivery states
- Closed tickets list includes **View transcript** button
- Settings archive channel hint updated (digest, not full transcript)

---

## Bot Archive Changes

- Embed title: **Ticket #N closed** (not "archived" as full record)
- Description: states archive is a **digest**, not full transcript
- Preview built from Timeline conversation API (last 8 messages)
- **Full transcript** field: Dashboard link when `Platform:DashboardUrl` + `GuildId` available
- Fallback: "Available in the Dashboard for authorized staff"
- Log message: **archive digest posted** (not "transcript archived")

---

## Business Rules Satisfied

| Rule | How |
|------|-----|
| **BR-X01** Archive is notification; Transcript is truth | Separate API/read model, UI notice, embed disclaimer |
| **BR-X02** Archive must not claim full history unless Transcript accessible | Digest-only copy; link only when Dashboard URL configured |
| **BR-X03** Transcript reconstructable from Timeline after channel deletion | Transcript endpoint uses Timeline projection only |
| **BR-T03** Timeline append-only | No write changes; read-only projection |

---

## Validation Performed

- [x] `dotnet build DiscordBot.sln`
- [x] `npm run build` (Dashboard)
- [x] Transcript endpoint enforces `ViewTickets` (returns null → 404 when denied)
- [x] Transcript derived from `BuildTicketConversationAsync` / Timeline events
- [x] Archive embed uses honest digest wording + transcript link/fallback
- [x] Closed ticket transcript route does not reference Discord channel ID

---

## Risks

- **Dashboard transcript links in Discord** require correct `Platform:DashboardUrl` in bot config; misconfiguration shows fallback text only (still honest).
- **Pre-CM-002 messages** are absent from Timeline — transcript is complete for recorded events only.
- **Internal notes** filtering uses `ReplyToTickets` as staff proxy until dedicated Internal Notes ship.

---

## Remaining Work

- HTML/PDF/email/DM transcript export (out of scope)
- Dedicated Internal Notes write model + `isInternal` population
- Open-ticket transcript access from list (currently emphasized for closed tickets)
- Bot archive digest could mention ticket number in transcript URL anchor text only (cosmetic)

---

## Suggested Next Task

**CM-011 — Internal notes** or **CM-008 — Staff Discord role channel overwrites** to complete staff workflow gaps after transcript foundation.
