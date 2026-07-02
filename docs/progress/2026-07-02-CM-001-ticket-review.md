# CM-001 — Ticket System Technical Review (Final Report)

**Date:** 2026-07-02  
**Task:** CM-001 — Ticket System Technical Review & Roadmap  
**Status:** Complete (documentation only — no code changes)

---

## Summary

The Discord Bot Platform ticket module is a **functional MVP** suitable for closed beta: members open private channels via Discord, staff see a dashboard list, can close tickets and send async replies, and optional archive embeds post on close.

It is **approximately 52% complete** toward the **Ticket System v1** success criteria defined in this review. The system works for small servers with admin-heavy staff, but **cannot yet serve as a primary support desk** because conversations are not persisted, permissions are coarsely enforced, and the dashboard lacks a ticket detail / transcript view.

The highest-priority gap is **message persistence** — the archive service currently shows an 8-message preview and states that the full ticket is available in the dashboard, which is **not implemented**.

Deliverables are in `/docs/tickets/` (7 documents). Implementation should start with **CM-002 (message persistence)** followed by permissions and detail UX (Phase 1).

---

## Files Reviewed

**Count:** 45+ source files and 4 documentation references

### Backend
- Domain: `Ticket`, `TicketOutboundMessage`, `GuildSettings`, `TicketStatus`, `LogEventType`, `TicketMessageDefaults`, `AutoReplyScope`
- Infrastructure: `TicketService`, `CommandPanelService`, `GuildService`, DTOs, EF configurations, migrations (`InitialCreate`, `AddCommandPanelAndTicketCleanup`, `AddTicketMessagesAndAutoReplies`, `BetaFeedbackFixes`)
- API: `GuildsController`, `BotTicketsController`, `BotTicketSetupController`

### Bot
- `TicketCommandHandlers`, `TicketInteractionHandlers`, `PanelInteractionHandlers`
- `TicketArchiveService`, `TicketChannelCleanupService`, `TicketOutboundMessageService`, `GuildMaintenanceWorker`
- `AutoReplyMessageService`, `EmbedBuilderService`, `BotApiClient`

### Dashboard
- `tickets.component.*`, `settings.component.*` (tickets tab)
- `guild.service.ts`, `ticket.models.ts`, routing, layout nav, guards

### Documentation
- `docs/step-09-tickets.md`, `docs/step-29-beta-feedback-fixes.md`, `docs/step-30-architecture-audit.md`
- `docs/architecture/permission-system.md`, `bot-architecture.md`

---

## Current Completion Percentage

| Area | % | Rationale |
|------|---|-----------|
| Create / close lifecycle | 85% | Works in Discord + dashboard; dual paths |
| Settings & templates | 80% | Archive, templates, panel integration |
| Dashboard list UX | 55% | Table only; no detail/filters |
| Transcripts & archive | 25% | Preview only; misleading copy |
| Permissions | 50% | Flags exist; API uses moderation gate |
| Staff workflow | 15% | No claim/assign/notes/reopen |
| Categories / forms | 10% | Single category; no forms |
| Analytics / automation | 10% | Counts only; no auto-close/SLA |
| **Overall toward Ticket System v1** | **~52%** | Weighted against v1 checklist |

---

## Critical Missing Features

1. **Ticket message persistence** — no `TicketMessage` entity; history lost on channel delete
2. **Dashboard transcript / detail page** — staff cannot read conversations in dashboard
3. **Granular permission enforcement** — API uses `CanAccessModerationPagesAsync`
4. **Staff Discord channel access** — only admin/manage-guild roles see channels
5. **Accurate archive** — 8-message preview + false dashboard full-history claim
6. **Unified close pipeline** — bot immediate delete vs dashboard worker async path
7. **Support workflow** — claim, assign, reopen, notes, filters (Phase 2)

---

## Recommended Implementation Order

```
1. CM-002  Message persistence + bot ingestion
2. CM-003  Transcript API (detail + messages)
3. CM-004  Dashboard ticket detail page
4. CM-005  Granular ticket permissions
5. CM-006  Unified close + DB-backed archive
6. CM-014  Fix archive messaging copy
7. CM-007  Empty list / 403 fix
8. CM-008  Staff role channel overwrites
9. CM-009  List filters + pagination
10. CM-010 Claim / assign
```

Phases 2–3 (notes, reopen, categories, auto-close, analytics) follow per [ticket-system-roadmap.md](../tickets/ticket-system-roadmap.md).

---

## Estimated Implementation Effort

| Milestone | Effort |
|-----------|--------|
| Phase 1 (foundation) | 15–20 dev-days |
| Phase 2 (staff workflow) | 10–12 dev-days |
| **Ticket System v1 (Phase 1 + core Phase 2)** | **~25–30 dev-days** |
| Phase 3 (commercial extras) | +15–20 dev-days |

---

## Recommended Next Task

**CM-002 — Ticket message persistence & Discord ingestion**

Add `TicketMessages` table, bot `MessageReceived` handler for ticket channels, and API ingest endpoint. This unblocks transcript viewer, honest archives, analytics, and auto-close.

---

## Deliverables Created

| Path | Description |
|------|-------------|
| [docs/tickets/ticket-system-review.md](../tickets/ticket-system-review.md) | Full review, feature matrix, architecture/UX/commercial analysis |
| [docs/tickets/ticket-system-roadmap.md](../tickets/ticket-system-roadmap.md) | Phase 1–3 with concrete tasks |
| [docs/tickets/ticket-system-database.md](../tickets/ticket-system-database.md) | Current + proposed schema |
| [docs/tickets/ticket-system-api.md](../tickets/ticket-system-api.md) | Endpoints, auth, gaps |
| [docs/tickets/ticket-system-dashboard.md](../tickets/ticket-system-dashboard.md) | Dashboard UX review + plan |
| [docs/tickets/ticket-system-bot.md](../tickets/ticket-system-bot.md) | Bot commands, workers, flows |
| [docs/tickets/ticket-system-future.md](../tickets/ticket-system-future.md) | v1 checklist, CM backlog, risks, debt |

---

## Constraints Observed

- No code modified
- No migrations created
- No features implemented

---

## Key Architectural Verdict

**Approve MVP for beta** with clear positioning as "Discord tickets + basic dashboard ops."

**Do not market as full support platform** until Phase 1 completes — especially message history and permission correctness.

The existing multi-tenant API/bot/dashboard split is sound; v1 work is primarily **data model extension**, **permission wiring**, and **dashboard depth** rather than greenfield rewrite.
