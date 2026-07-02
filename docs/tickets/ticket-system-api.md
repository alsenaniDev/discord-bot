# Ticket System — API Reference & Gaps

**Base URLs:** Dashboard JWT `/api/guilds/{guildId}/...` · Bot `/api/bot/...`  
**Auth:** JWT (dashboard) · `X-Bot-Api-Key` (bot)

---

## Dashboard Endpoints (Implemented)

### `GET /api/guilds/{id}/tickets`

**Auth:** JWT — Discord user id from token  
**Permission today:** `CanAccessModerationPagesAsync` (coarse)  
**Permission v1:** `ViewTickets` or guild owner/platform admin

**Response:** `200 TicketDto[]`

```json
{
  "id": "uuid",
  "guildId": "uuid",
  "ticketNumber": 1,
  "ownerDiscordUserId": "123",
  "ownerDisplayName": "User",
  "channelDiscordId": "456",
  "channelName": "ticket-1",
  "status": "Open",
  "createdAt": "2026-07-02T12:00:00Z",
  "closedAt": null
}
```

**Edge case (bug):** If result is empty **and** user lacks moderation access → `404`. Authorized users with zero tickets should get `200 []`.

**Controller:** `GuildsController.GetTickets`  
**Service:** `TicketService.GetGuildTicketsAsync`

---

### `PATCH /api/guilds/{id}/tickets/{ticketId}/close`

**Auth:** JWT  
**Permission today:** `CanAccessModerationPagesAsync`  
**Permission v1:** `CloseTickets`

**Behavior:**
- Sets `Status = Closed`, `ClosedAt = now`, `ChannelCleanupRequested = true`
- Writes `LogEventType.TicketClosed` with `source = dashboard`
- Bot worker archives + deletes channel

**Response:** `200 TicketDto` · `404` if not found / already closed / denied

---

### `POST /api/guilds/{id}/tickets/{ticketId}/messages`

**Auth:** JWT  
**Permission today:** `CanAccessModerationPagesAsync`  
**Permission v1:** `ReplyToTickets`

**Body:** `SendTicketMessageRequest`

```json
{ "content": "Staff reply text" }
```

**Validation:**
- Required non-whitespace content
- Max 2000 characters

**Behavior:** Creates `StaffReplyQueued` Timeline Event, then `TicketOutboundMessage` (`IsDelivered = false`, linked via `StaffReplyQueuedTimelineEventId`); bot delivers within ~30s and ack creates `StaffReplyDelivered` or `StaffReplyFailed`.

**Response:** `200 TicketOutboundMessageDto`

---

### `GET /api/guilds/{id}/tickets/{ticketId}/timeline`

**Auth:** JWT  
**Permission today:** `CanAccessModerationPagesAsync`

**Response:** `200 TicketTimelineEventDto[]` ordered by `OccurredAt` ascending

**DTO fields:** `id`, `ticketId`, `eventType`, `occurredAt`, `actorDiscordUserId`, `actorDisplayName`, `content`, `relatedTimelineEventId`, `metadataJson`

**Traceability:** D-001 §8

## Bot Endpoints (Implemented)

**Controller:** `BotTicketsController`, `BotTicketSetupController`  
**Route prefix:** `/api/bot/tickets`

### `POST /api/bot/tickets`

Create ticket record after Discord channel exists.

**Body:** `CreateTicketRequest`

| Field | Required |
|-------|----------|
| `discordGuildId` | Yes |
| `ownerDiscordUserId` | Yes |
| `channelDiscordId` | Yes |
| `ownerDisplayName` | No |
| `channelDisplayName` | No |

**Errors:**
- `400` tickets disabled / duplicate open ticket / validation
- `400` "You already have an open ticket."

**Side effects:** `LogEventType.TicketOpened`, member/channel display name cache

---

### `GET /api/bot/tickets/by-channel/{channelDiscordId}`

Lookup ticket by channel (any status).

**Used by:** Close flow, auto-reply ticket channel detection

---

### `PATCH /api/bot/tickets/{id}/close`

Mark closed from Discord (does **not** set `ChannelCleanupRequested`).

**Body:** `CloseTicketRequest` (optional actor display fields)

**Side effects:** `LogEventType.TicketClosed` — bot deletes channel in handler after archive

---

### `GET /api/bot/tickets/pending-cleanups`

Returns tickets where `Status = Closed AND ChannelCleanupRequested = true`.

**DTO:** `TicketChannelCleanupDto` — includes archive channel id, closed message template, owner/closed-by display names

---

### `POST /api/bot/tickets/{ticketId}/ack-cleanup`

Clears `ChannelCleanupRequested` after bot processed channel deletion.

---

### `GET /api/bot/tickets/pending-messages`

Undelivered `TicketOutboundMessages` globally ordered by `CreatedAt`.

**DTO:** `PendingTicketMessageDto` — content, channel id, staff reply prefix

---

### `POST /api/bot/tickets/messages/{messageId}/ack`

Marks outbound message delivered or failed.

**Body:** `AcknowledgeTicketMessageDeliveryRequest`

```json
{ "delivered": true, "failureReason": null }
```

**Side effects:**
- `delivered: true` → `IsDelivered = true`, appends `StaffReplyDelivered` (BR-T02)
- `delivered: false` → `DeliveryFailed = true`, appends `StaffReplyFailed`

---

### `POST /api/bot/tickets/timeline/message-sent`

Records a Discord ticket channel message as `MessageSent` (BR-T01).

**Body:** `RecordTicketMessageSentRequest` — channel id, Discord message id, author, content, optional `occurredAt`

**Response:** `200 TicketTimelineEventDto` · `404` if no open ticket or duplicate message

---

### `POST /api/bot/tickets/{ticketId}/timeline/archive-posted`

Records `ArchivePosted` after archive embed is sent (BR-T05).

**Body:** `RecordTicketArchivePostedRequest` — archive channel id, optional actor

---

### `GET /api/bot/tickets/{ticketId}/timeline`

Returns timeline events for bot consumers (e.g. archive preview). Optional `?limit=` (most recent N, returned ascending).

---

### `POST /api/bot/guilds/{discordGuildId}/tickets/setup`

**Body:** `{ "ticketCategoryId": "snowflake" }`

Enables tickets + saves category (used by `/ticket setup`).

---

## Related Bot Endpoints (Not under `/tickets`)

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/bot/guilds/{id}/settings` | Ticket templates, category, archive channel |
| POST | `/api/bot/guilds/{id}/dashboard-access/evaluate` | `CanAccessTickets` for close authorization |

---

## Settings API (Ticket fields)

**`PUT /api/guilds/{id}/settings`** includes:

- `ticketsEnabled` (read-only effective via module + setup)
- `ticketCategoryId`
- `ticketArchiveChannelId`
- `ticketWelcomeTitle`, `ticketWelcomeMessage`
- `ticketClosedMessage`, `ticketClosedFromDashboardMessage`
- `ticketStaffReplyPrefix`
- Command panel fields (ticket open button)

**Validation:** `GuildSettingsValidator` — template length and required fields

**Access:** Guild owner / manage settings (not `ManageTickets` today)

---

## DTO Catalog (Current)

| DTO | Location |
|-----|----------|
| `TicketDto` | `TicketDtos.cs` |
| `CreateTicketRequest` | `TicketDtos.cs` |
| `CloseTicketRequest` | `TicketDtos.cs` |
| `SetupTicketsRequest` | `TicketDtos.cs` |
| `SendTicketMessageRequest` | `TicketDtos.cs` (verify file — may be separate) |
| `TicketOutboundMessageDto` | `TicketDtos.cs` |
| `TicketChannelCleanupDto` | Used in service projections |
| `PendingTicketMessageDto` | Used in service projections |

---

## Proposed v1 Endpoints (Not Implemented)

| Method | Route | Purpose | Phase |
|--------|-------|---------|-------|
| GET | `/api/guilds/{id}/tickets/{ticketId}` | Ticket detail | 1 |
| GET | `/api/guilds/{id}/tickets/{ticketId}/messages` | Paginated timeline | 1 |
| GET | `/api/guilds/{id}/tickets/{ticketId}/transcript` | Export HTML/TXT | 3 |
| PATCH | `/api/guilds/{id}/tickets/{ticketId}/claim` | Claim ticket | 2 |
| PATCH | `/api/guilds/{id}/tickets/{ticketId}/assign` | Assign to staff | 2 |
| PATCH | `/api/guilds/{id}/tickets/{ticketId}/reopen` | Reopen closed | 2 |
| POST | `/api/guilds/{id}/tickets/{ticketId}/notes` | Internal note | 2 |
| GET | `/api/guilds/{id}/tickets/stats` | Analytics | 3 |
| CRUD | `/api/guilds/{id}/ticket-categories` | Multi-category | 3 |
| POST | `/api/bot/tickets/messages` | Ingest Discord message | 1 |
| POST | `/api/bot/tickets/{id}/sync-messages` | Bulk backfill channel | 1 optional |

### List endpoint enhancements (Phase 1)

```
GET /api/guilds/{id}/tickets?status=Open&page=1&pageSize=25&sort=createdAt_desc
```

---

## Authorization Matrix (Target v1)

| Operation | Flag | Owner | Platform admin |
|-----------|------|-------|----------------|
| List / detail | `ViewTickets` | Yes | Yes |
| Reply | `ReplyToTickets` | Yes | Yes |
| Close | `CloseTickets` | Yes | Yes |
| Ticket settings | `ManageTickets` | Yes | Yes |
| Categories CRUD | `ManageTickets` | Yes | Yes |

**Implementation:** Extend `IGuildAccessService` with ticket-specific methods; stop using `CanAccessModerationPagesAsync` for ticket routes.

---

## Error Conventions

| Code | When |
|------|------|
| 401 | Missing JWT / bot key |
| 403 | Authenticated but missing ticket permission |
| 404 | Ticket not found OR wrongly used for empty unauthorized list |
| 400 | Business rule (duplicate open ticket, empty message) |

---

## API Design Notes

### Polling vs push

Current outbound model is **pull-based** (bot polls every 30s). v1 acceptable; v2 consider:
- SignalR to bot (complex)
- Shorter poll interval with guild cursor
- Webhook to bot service

### Idempotency

- Outbound ack is idempotent (sets delivered flag).
- Message ingestion should dedupe by `DiscordMessageId`.
- Close is not idempotent beyond returning null for already closed.

### Rate limits

No API rate limiting today — ticket message POST could spam queue; v1 add per-guild throttle.

---

## Testing Checklist (API)

- [ ] Create ticket when module enabled / disabled
- [ ] Duplicate open ticket rejected
- [ ] Close from dashboard sets cleanup flag
- [ ] Close from bot does not leave orphan channel
- [ ] Reply queues and ack delivers once
- [ ] Unauthorized list returns 403 not 404
- [ ] Reply without `ReplyToTickets` returns 403
- [ ] Close without `CloseTickets` returns 403

---

## Files Reference

| File | Role |
|------|------|
| `GuildsController.cs` | Dashboard ticket routes |
| `BotTicketsController.cs` | Bot ticket routes |
| `TicketService.cs` | Business logic |
| `TicketDtos.cs` | Request/response models |
| `GuildSettingsValidator.cs` | Template validation |
