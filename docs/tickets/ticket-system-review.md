# Ticket System — Technical Review (CM-001)

**Date:** 2026-07-02  
**Scope:** Analysis only — no code changes  
**Purpose:** Baseline for Ticket System v1 implementation planning

---

## Executive Summary

The platform has a **working tickets MVP**: Discord channel–based tickets with SQL persistence, dashboard list/close/reply, command-panel open flow, archive preview on close, and async bot workers for cleanup and outbound messages.

It is **not yet a complete support product**. The largest gaps are **message persistence / transcripts**, **granular permission enforcement**, **staff Discord channel access for non-admin roles**, **ticket detail UX**, and **multi-category / panel workflows**.

**Estimated completion toward Ticket System v1 (defined in this doc set): ~52%**

---

## Files Reviewed

### Domain & database
- `src/DiscordBot.Domain/Entities/Ticket.cs`
- `src/DiscordBot.Domain/Entities/TicketOutboundMessage.cs`
- `src/DiscordBot.Domain/Entities/GuildSettings.cs` (ticket fields)
- `src/DiscordBot.Domain/Enums/TicketStatus.cs`
- `src/DiscordBot.Domain/Enums/LogEventType.cs`
- `src/DiscordBot.Domain/Constants/TicketMessageDefaults.cs`
- `src/DiscordBot.Infrastructure/Data/Configurations/TicketConfiguration.cs`
- `src/DiscordBot.Infrastructure/Data/Configurations/AutoReplyRuleConfiguration.cs` (outbound message config)
- Migrations: `InitialCreate`, `AddCommandPanelAndTicketCleanup`, `AddTicketMessagesAndAutoReplies`, `BetaFeedbackFixes`

### Services & API
- `src/DiscordBot.Infrastructure/Services/TicketService.cs`
- `src/DiscordBot.Infrastructure/Services/CommandPanelService.cs`
- `src/DiscordBot.Infrastructure/Services/GuildService.cs` (settings, overview counts)
- `src/DiscordBot.Infrastructure/Models/TicketDtos.cs`
- `src/DiscordBot.Api/Controllers/BotTicketsController.cs`
- `src/DiscordBot.Api/Controllers/GuildsController.cs` (ticket endpoints)

### Bot
- `src/DiscordBot.Bot/Commands/TicketCommandHandlers.cs`
- `src/DiscordBot.Bot/Commands/TicketInteractionHandlers.cs`
- `src/DiscordBot.Bot/Commands/PanelInteractionHandlers.cs`
- `src/DiscordBot.Bot/Services/TicketArchiveService.cs`
- `src/DiscordBot.Bot/Services/TicketChannelCleanupService.cs`
- `src/DiscordBot.Bot/Services/TicketOutboundMessageService.cs`
- `src/DiscordBot.Bot/Services/GuildMaintenanceWorker.cs`
- `src/DiscordBot.Bot/Services/AutoReplyMessageService.cs` (ticket scope)
- `src/DiscordBot.Bot/Api/BotApiClient.cs` (ticket methods)

### Dashboard
- `dashboard/.../features/tickets/*`
- `dashboard/.../features/settings/*` (tickets tab)
- `dashboard/.../core/services/guild.service.ts`
- `dashboard/.../core/models/ticket.models.ts`
- `dashboard/.../app-routing.module.ts`
- `dashboard/.../features/layout/dashboard-layout.component.*`

### Permissions & docs
- `src/DiscordBot.Domain/Enums/GuildPermissions.cs`
- `src/DiscordBot.Infrastructure/Services/GuildPermissionResolver.cs`
- `docs/step-09-tickets.md`
- `docs/step-29-beta-feedback-fixes.md`
- `docs/step-30-architecture-audit.md`
- `docs/architecture/permission-system.md`, `bot-architecture.md`

---

## Feature Matrix

Status key: **Exists** | **Partial** | **Missing**

| Feature | Status | Notes |
|---------|--------|-------|
| Ticket creation (Discord slash) | **Exists** | `/ticket open` → confirm button → private channel |
| Ticket creation (command panel) | **Exists** | Panel button `ticket_open` delegates to same create flow |
| Ticket closing (Discord) | **Exists** | Modal confirmation (`CLOSE`), button, select menu |
| Ticket closing (dashboard) | **Exists** | Sets `ChannelCleanupRequested`; bot worker deletes channel |
| Sequential ticket numbers | **Exists** | Per-guild `Max(TicketNumber)+1`, unique index |
| One open ticket per user | **Exists** | Enforced in `CreateTicketAsync` |
| Private channel permissions | **Partial** | Owner + Discord admin/manage-guild roles only; dashboard staff roles **not** granted channel access |
| Ticket setup | **Exists** | `/ticket setup` creates category + enables in API |
| Single ticket category | **Exists** | `GuildSettings.TicketCategoryId` |
| Multiple ticket categories | **Missing** | One category for all tickets |
| Multiple panels | **Partial** | One command panel per guild; buttons JSON supports ticket actions but not category routing |
| Welcome message templates | **Exists** | Title + body with `{mention}`, `{ticket}`, `{server}` placeholders |
| Closed message templates | **Exists** | Discord close + dashboard close variants |
| Staff reply prefix template | **Exists** | `{staff}` placeholder; dashboard → queue → bot delivery |
| Archive on close | **Partial** | Embed to archive channel; last 8 text messages only |
| Full transcript storage | **Missing** | No `TicketMessage` entity; archive text claims dashboard has full history |
| Transcript viewer (dashboard) | **Missing** | List page only |
| Transcript export (HTML/TXT) | **Missing** | — |
| Dashboard ticket list | **Exists** | Table: number, status, owner, channel, dates |
| Dashboard staff reply | **Exists** | Inline textarea; async delivery via `TicketOutboundMessage` |
| Dashboard close | **Exists** | Confirm dialog |
| Ticket detail page | **Missing** | No `/tickets/:id` route |
| Filters (open/closed/owner) | **Missing** | Full list only |
| Pagination | **Missing** | Loads all tickets |
| Search | **Missing** | — |
| Claim ticket | **Missing** | — |
| Assign ticket | **Missing** | — |
| Rename ticket channel | **Missing** | Renamed once to `ticket-{number}` at creation |
| Reopen ticket | **Missing** | Status enum: Open, Closed only |
| Priority | **Missing** | — |
| Tags / labels | **Missing** | — |
| Internal notes | **Missing** | — |
| Attachments (persisted) | **Missing** | Discord allows attach in channel; not stored in DB |
| Forms on open (modal) | **Missing** | — |
| Custom fields | **Missing** | — |
| Auto-close / inactivity | **Missing** | — |
| SLA / escalation | **Missing** | — |
| Ticket statistics (guild) | **Partial** | Overview counts (`totalTickets`, `openTickets`, `closedTickets`) |
| Ticket analytics (detailed) | **Missing** | No response time, volume trends, staff metrics |
| Logging — opened | **Exists** | `LogEventType.TicketOpened` |
| Logging — closed | **Exists** | `LogEventType.TicketClosed` (bot + dashboard variants) |
| Logging — archived | **Exists** | `LogEventType.TicketArchived` (bot only) |
| Discord log delivery for tickets | **Exists** | Via `DiscordLogDeliveryService` when logs module enabled |
| Module gating | **Exists** | `ModuleKeys.Tickets` checked on bot interactions |
| Settings UI (archive, templates) | **Exists** | Settings → Tickets tab (module-gated) |
| Auto-replies in ticket channels | **Exists** | `AutoReplyScope.TicketChannelsOnly` |
| Granular ticket permissions (enum) | **Exists** | `ViewTickets`, `ReplyToTickets`, `CloseTickets`, `ManageTickets` |
| Granular ticket permissions (enforced) | **Partial** | API list/close/reply use `CanAccessModerationPagesAsync`; bot close uses `CanAccessTickets` |
| Staff Discord access via permission roles | **Missing** | Close allowed via evaluate endpoint; channel view not granted |
| Scheduled cleanup worker | **Exists** | `GuildMaintenanceWorker` 30s poll |
| Outbound message worker | **Exists** | Same worker; ack on delivery or missing channel |
| Orphan channel handling | **Partial** | Missing guild/channel acks cleanup flag; no reconciliation job |

---

## Architecture Review

### Database design

**Strengths**
- Clear `Tickets` table with sensible indexes: `(GuildId, TicketNumber)` unique, `ChannelDiscordId` unique, `(GuildId, Status)`.
- `TicketOutboundMessages` queue with delivery tracking and `(GuildId, IsDelivered, CreatedAt)` index.
- Ticket settings colocated in `GuildSettings` — appropriate for single-tenant-per-row config.

**Weaknesses**
- No message history table — transcripts cannot be rebuilt after channel deletion.
- No `AssignedTo`, `ClaimedBy`, `CategoryId`, `Priority`, or metadata JSON on `Ticket`.
- `TicketStatus` is binary; reopen/locked/pending states unsupported without migration.
- `ChannelCleanupRequested` couples dashboard close lifecycle to bot worker; bot-initiated close bypasses same flag (different code paths).
- Outbound queue stores dashboard replies only — inbound Discord messages never persisted.

**Verdict:** Adequate for MVP; **must extend schema for v1** (messages + assignment minimum).

### API

**Strengths**
- Clean separation: bot endpoints (`/api/bot/tickets/*`) vs dashboard JWT endpoints.
- Idempotent-ish cleanup ack; outbound ack prevents duplicate delivery.
- Validation on message length (2000 chars).

**Weaknesses**
- No `GET /tickets/{id}`, no message endpoints, no transcript export.
- Dashboard auth uses coarse `CanAccessModerationPagesAsync` instead of `ViewTickets` / `ReplyToTickets` / `CloseTickets`.
- `GET /guilds/{id}/tickets` returns 404 when empty list **and** user lacks access — ambiguous vs empty authorized list.
- No pagination, filtering, or sorting parameters.
- Bot `GetByChannel` returns ticket regardless of status — used for close validation but also auto-reply detection.

**Verdict:** MVP-complete; needs v1 endpoints for detail, messages, permissions, pagination.

### Bot

**Strengths**
- Solid interaction model: slash commands + buttons + modal confirmation on close.
- Module guard on all ticket interactions.
- Archive failures non-blocking.
- Dashboard close path correctly archives before delete via cleanup worker.

**Weaknesses**
- Two close paths (immediate bot delete vs dashboard cleanup queue) create behavioral inconsistency.
- Channel overwrites ignore configured staff/support roles — only native Discord admin/manage guild.
- Polling delivery (30s) adds latency for dashboard replies; no webhook/push.
- Archive preview misleadingly references dashboard full history.
- No message capture on `MessageReceived` for ticket channels.

**Verdict:** Good Discord UX foundation; needs message ingestion + staff role overwrites.

### Dashboard

**Strengths**
- Simple table usable for small servers.
- Settings tab for templates and archive channel is discoverable.
- i18n (en/ar) for ticket strings.

**Weaknesses**
- No ticket detail view — support teams cannot review conversation history.
- Reply UI embedded in table row — poor at scale.
- No status filters, assignment, or open-ticket queue view.
- Nav tied to `canAccessModeration` not `ViewTickets`.
- Close/reply not gated separately in UI.

**Verdict:** Owner-visible list, not yet a support-team workspace.

### Workers

**Strengths**
- Single `GuildMaintenanceWorker` orchestrates panel sync, cleanup, outbound messages.
- Resilient error handling per item in loops.

**Weaknesses**
- Fixed 30s poll — scales poorly with many pending messages (linear scan of all undelivered globally).
- No dead-letter or retry count on failed outbound messages.
- Cleanup ack on missing channel prevents infinite loop but loses archive if channel existed briefly.

**Verdict:** Acceptable for beta; consider scoped polling and retry policy for v1.

### Permission model

**Strengths**
- Unified `GuildPermissions` flags include ticket-specific capabilities.
- Bot close correctly evaluates `CanAccessTickets` via dashboard-access endpoint.
- Owner/platform admin bypass documented.

**Weaknesses**
- API ticket operations gate on `CanAccessModerationPagesAsync` — cross-grants moderation access to ticket-only staff incorrectly, and ticket-only roles may lack API access if moderation flags absent.
- Dashboard staff with ticket permissions cannot see ticket channels in Discord.
- No `ManageTickets`-specific operations (settings changes still owner-only).

**Verdict:** Enum ready; enforcement incomplete — Phase 1 priority.

### Scalability

| Concern | Severity | Detail |
|---------|----------|--------|
| Full ticket list API | Medium | No pagination; large guilds load all rows |
| Global outbound poll | Medium | `WHERE NOT IsDelivered` across all guilds every 30s |
| Message storage absent | High | Cannot scale support audit/compliance without persistence |
| Discord API on archive | Low | 8 messages per close — bounded |
| Channel-per-ticket | Medium | Discord 500 channel limit per guild — no archival category rotation |

### Future maintainability

**Good:** Service boundaries (`ITicketService`), DTO mapping, bot/API split, settings in one place.  
**Risk:** Close logic split across `TicketCommandHandlers`, `TicketService`, `TicketChannelCleanupService` — changes need coordinated updates.  
**Risk:** Misleading archive copy creates support debt until transcript feature ships.

---

## UX Review (Dashboard)

### Would a server owner understand it?

**Mostly yes** for setup:
- Module enable → `/ticket setup` or settings category → optional archive channel → command panel button.
- Settings → Tickets tab explains templates with placeholder hints.

**Gaps for owners:**
- Archive channel described as transcript destination, but dashboard cannot show transcripts — expectation mismatch.
- No in-dashboard explanation of one-ticket-per-user limit or staff Discord access requirements.
- Ticket list shows raw Discord IDs when display names missing.

### Would support teams use it comfortably?

**Not yet** for daily operations:
- No conversation view — staff must switch to Discord for context while using dashboard reply.
- No queue/filter for open tickets, assignment, or "my tickets".
- Inline reply in table does not scale beyond ~10 active tickets.
- 30s+ delay on dashboard replies without status indicator on delivery.
- Cannot distinguish reply vs close permissions for junior staff.

### Recommended UX improvements (v1)

1. Ticket detail page with message timeline (Discord + dashboard replies).
2. Open-tickets default filter + search by number/owner.
3. Separate nav visibility: `ViewTickets` vs moderation bundle.
4. Delivery status on outbound replies (queued / sent / failed).
5. Settings help: who can see ticket channels in Discord and how to configure staff roles.
6. Empty states linking to setup checklist.

---

## Commercial Review (Market Reference)

Compared to established Discord ticket bots (Ticket Tool, TicketsBot, tickety-type products, Support System–style dashboards):

### Where this project is stronger

| Area | Advantage |
|------|-----------|
| **Unified SaaS dashboard** | Single control plane for tickets, moderation, logs, modules, subscriptions — most ticket bots are Discord-only or weak web UI |
| **Permission model foundation** | Role-based dashboard access with flags (once wired) beats many bots' single "support role" setting |
| **Platform integration** | Logs, auto-replies scoped to ticket channels, command panel, guild profile — ecosystem stickiness |
| **Self-hosted / white-label potential** | Full stack ownership (API + bot + dashboard) vs bot-only SaaS |
| **i18n** | Arabic + English dashboard — uncommon in competitor dashboards |
| **Audit logging** | Structured `LogEntry` events with Discord delivery option |

### Where this project is weaker

| Area | Gap |
|------|-----|
| **Transcripts** | Competitors offer HTML/TXT transcripts, DM copies, full archive |
| **Panel/category UX** | Multi-panel, dropdown category select, ticket forms are table stakes |
| **Staff workflow** | Claim, assign, notes, priority, reopen |
| **Automation** | Auto-close, reminders, SLA — largely absent |
| **Analytics** | Competitors expose ticket volume, response times, staff leaderboard |
| **Discord UX polish** | Competitors have rich embed panels, ticket controls, member add/remove |
| **Immediate staff replies** | Dashboard reply latency (poll) vs in-channel native experience |

**Positioning recommendation:** Do not compete on feature parity with Ticket Tool on Discord alone. Compete on **managed dashboard + moderation + tickets control plane** once transcript, permissions, and ticket detail ship.

---

## Critical Issues (Fix in v1)

1. **False transcript promise** — archive embed and preview text claim full dashboard history that does not exist.
2. **Permission mismatch** — staff can close via bot evaluate but may lack channel access and API uses moderation gate.
3. **Dual close paths** — bot close immediate; dashboard close async — different archive timing and UX.
4. **No message persistence** — compliance and support workflow blocker.
5. **GET tickets 404 edge case** — unauthorized vs empty list confusion.

---

## Related Documents

- [Roadmap](./ticket-system-roadmap.md)
- [Database](./ticket-system-database.md)
- [API](./ticket-system-api.md)
- [Dashboard](./ticket-system-dashboard.md)
- [Bot](./ticket-system-bot.md)
- [Future, backlog, risks](./ticket-system-future.md)
