# Ticket System — API Reference & Gaps

**Base URLs:** Dashboard JWT `/api/guilds/{guildId}/...` · Bot `/api/bot/...`  
**Auth:** JWT (dashboard) · `X-Bot-Api-Key` (bot)

---

## Dashboard Endpoints (Implemented)

### `GET /api/guilds/{id}/tickets`

**Auth:** JWT  
**Permission:** `ViewTickets` (or owner/platform admin) — enforced server-side via `ITicketReadService`

**Query:** `status` (`Open`|`Closed`), `page` (default 1), `pageSize` (default 20, max 100), `sort` (`lastActivity`|`created`|`number`)

**Response:** `200 PaginatedTicketSummaryReadModel`

```json
{
  "items": [
    {
      "ticketId": "uuid",
      "guildId": "uuid",
      "ticketNumber": 1,
      "ownerDiscordId": "123",
      "ownerUsername": "User",
      "status": "Open",
      "discordChannelId": "456",
      "createdAt": "2026-07-02T12:00:00Z",
      "closedAt": null,
      "lastActivityAt": "2026-07-02T13:00:00Z",
      "lastMessagePreview": "Hello, I need help…",
      "messageCount": 3,
      "staffReplyCount": 1,
      "failedDeliveryCount": 0
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

**Read model:** Ticket Summary (AR-001, CM-003) — query projection over `Tickets` + aggregated `TicketTimelineEvents`

**Replaces:** legacy `GetGuildTicketsAsync` / non-paginated `TicketDto[]` list (removed R-002). Dashboard uses `GuildService.getTicketSummaries()`.

**Controller:** `GuildsController.GetTicketSummaries`  
**Service:** `TicketReadService.GetTicketSummariesAsync`

---

## Legacy & Transitional Endpoints (R-002)

| Endpoint | Status | Use instead |
|----------|--------|-------------|
| `GET /api/guilds/{id}/tickets` (paginated summaries) | ✅ **Current** | — |
| `ITicketService.GetGuildTicketsAsync` | ❌ **Removed** (R-002) | `ITicketReadService.GetTicketSummariesAsync` |
| Dashboard `getTickets()` | ⚠️ **Deprecated wrapper** | `getTicketSummaries()` — maps summaries to legacy `Ticket` shape; no callers in app |
| `GET .../tickets/{ticketId}/timeline` (dashboard JWT) | ⚠️ **Transitional** | `/conversation` or `/transcript` |
| `GET /api/bot/tickets/{ticketId}/timeline` | ⚠️ **Transitional** | `/api/bot/tickets/{ticketId}/conversation` |
| `PATCH .../close`, `POST .../messages` | ✅ **Current** write paths | — |

No conflict between Ticket Summary Read Model and removed legacy list method — single list path via `ITicketReadService`.

---

### `GET /api/guilds/{id}/tickets/{ticketId}/conversation`

**Auth:** JWT  
**Permission:** `ViewTickets`

**Query:** `limit` (default 50, max 200), cursor pagination via `cursorOccurredAt` + `cursorEventId`

**Response:** `200 PaginatedTicketConversationReadModel`

Entry fields: `eventId`, `ticketId`, `eventType`, `actorType`, `actorDiscordId`, `actorUsername`, `content`, `isInternal`, `deliveryStatus`, `occurredAt`, `createdAt`

**Read model:** Ticket Conversation (AR-001, CM-003) — presentation projection over Timeline Events (not raw aggregate exposure)

**Works for closed tickets** without Discord channel.

---

### `GET /api/guilds/{id}/tickets/{ticketId}/transcript`

**Auth:** JWT  
**Permission:** `ViewTickets`

**Query:** Same cursor pagination as `/conversation` — `limit` (default 50, max 200), `cursorOccurredAt` + `cursorEventId`

**Response:** `200 TicketTranscriptReadModel`

```json
{
  "metadata": {
    "ticketId": "uuid",
    "guildId": "uuid",
    "ticketNumber": 1,
    "ownerDiscordId": "123",
    "ownerUsername": "User",
    "status": "Closed",
    "createdAt": "2026-07-02T12:00:00Z",
    "closedAt": "2026-07-02T14:00:00Z",
    "source": "Timeline",
    "discordArchiveIsDigestOnly": true
  },
  "entries": [ /* TicketConversationEntryReadModel[] */ ],
  "hasMore": false,
  "nextCursorOccurredAt": null,
  "nextCursorEventId": null
}
```

**Read model:** Ticket Transcript (AR-001, CM-004) — metadata + Conversation projection over Timeline. **Archive is not Transcript:** Discord archive channel posts a digest only; this endpoint is the durable full record.

**Internal notes:** Entries with `isInternal = true` are omitted unless the caller also has `ReplyToTickets` (staff visibility proxy until Internal Notes ship).

**Controller:** `GuildsController.GetTicketTranscript`  
**Service:** `TicketReadService.GetTicketTranscriptAsync`

**Works for closed tickets** after Discord channel deletion (no channel dependency).

**Distinction from `/conversation`:** Transcript wraps ticket metadata and documents that Discord archive is digest-only. Entry pagination uses the same Timeline projection.

---

### `PATCH /api/guilds/{id}/tickets/{ticketId}/close`

**Auth:** JWT  
**Permission:** `CloseTickets` (CM-003)

**Behavior:**
- Sets `Status = Closed`, `ClosedAt = now`, `ChannelCleanupRequested = true`
- Writes `LogEventType.TicketClosed` with `source = dashboard`
- Bot worker archives + deletes channel

**Response:** `200 TicketDto` · `404` if not found / already closed / denied

---

### `POST /api/guilds/{id}/tickets/{ticketId}/messages`

**Auth:** JWT  
**Permission:** `ReplyToTickets` (CM-003)

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
**Permission:** `ViewTickets`

**Note:** Legacy/raw Timeline DTO. Dashboard should use **`/conversation`** (CM-003). Bot may still use bot timeline route.

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

**DTO:** `TicketChannelCleanupDto` — includes platform `guildId`, archive channel id, closed message template, owner/closed-by display names

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

### `GET /api/bot/tickets/{ticketId}/conversation`

Returns **Ticket Conversation Read Model** for bot consumers (archive preview). Cursor pagination supported. Optional `?limit=` (default 100).

**Used by:** `TicketArchiveService` (CM-003)

---

### `GET /api/bot/tickets/{ticketId}/timeline`

Returns raw timeline events (legacy). Prefer **`/conversation`** for presentation-layer reads.

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
