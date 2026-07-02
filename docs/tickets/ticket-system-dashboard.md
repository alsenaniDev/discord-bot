# Ticket System — Dashboard UX & Implementation

**Route:** `/guilds/:id/tickets`  
**Component:** `TicketsComponent`  
**Guard:** `GuildAccessGuard` with `guildAccess: 'moderation'`

---

## Current Pages & Navigation

| Location | Ticket-related UX |
|----------|-------------------|
| **Sidebar** | "Tickets" link when `canAccessModeration` |
| **`/guilds/:id/tickets`** | List table + inline reply/close |
| **`/guilds/:id/settings` → Tickets tab** | Category, archive channel, message templates |
| **`/guilds/:id/settings` → Button panel tab** | Ticket open/help buttons |
| **`/guilds/:id/overview`** | Stats: total/open/closed ticket counts |
| **`/guilds/:id/staff`** | Permission flags: View/Reply/Close/Manage tickets |
| **`/guilds/:id/logs`** | Filter event types: TicketOpened, TicketClosed, TicketArchived |
| **`/servers`** | Quick link to tickets per guild |

---

## Tickets List Page

### Implemented UI

**File:** `tickets.component.html` / `tickets.component.ts`

| Element | Behavior |
|---------|----------|
| Loading state | Spinner with i18n message |
| Error state | Empty state + retry |
| Empty state | Icon + hint when no tickets |
| Data table | Number, status badge, owner, channel, created, closed |
| Actions (open tickets) | Reply toggle, Close button |
| Timeline | Expandable per-ticket timeline panel (load + refresh) |
| Reply box | Textarea + send (inline in row) |
| Close | `window.confirm` → PATCH close API |

**API calls:** `GuildService.getTickets`, `closeTicket`, `sendTicketMessage`, `getTicketTimeline`

**Timeline panel:** Shows event type, timestamp, actor, and content for all Timeline v1 event types. Refreshes after a successful dashboard reply when expanded.

### Missing UI

| Feature | Priority |
|---------|----------|
| Ticket detail / conversation page | P1 |
| Status filter (open/closed/all) | P0 |
| Pagination | P1 |
| Search by number/owner | P1 |
| Assignee column | P2 |
| Last activity timestamp | P1 |
| Reply delivery status | P1 |
| Permission-aware actions (reply vs close) | P0 |
| Link to Discord channel | P2 |
| Transcript download | P2 |
| Mobile-responsive table | P2 |

---

## Settings → Tickets Tab

### Implemented

**Visibility:** `activeTab === 'tickets' && ticketsEnabled` (module flag)

| Field | Control |
|-------|---------|
| Ticket category | Dropdown from synced channels (categories) |
| Archive channel | Dropdown (text channels) |
| Welcome title | Text input, max 256 |
| Welcome message | Textarea |
| Closed message | Textarea |
| Closed from dashboard message | Textarea |
| Staff reply prefix | Textarea, `{staff}` hint |

**Save:** Part of guild settings PUT — triggers command panel refresh if panel fields changed.

### Gaps

- No UI for **staff role channel access** (which Discord roles see tickets).
- No **multi-category** management.
- No **auto-close** settings.
- Archive channel help text implies full transcripts — must update after Phase 1.
- `/ticket setup` still required for initial enable — settings tab does not enable tickets alone (category must exist).

---

## Settings → Button Panel Tab

Default JSON includes:
- `ticket_open` — Create Ticket (Success)
- `ticket_help` — Ticket Help (Secondary)

Panel posts to configured channel; bot syncs via `CommandPanelSyncService`.

**Gap:** No visual editor for mapping buttons → ticket categories (Phase 3).

---

## Staff Permissions Page

**Flags exposed:**
- `ViewTickets`
- `ReplyToTickets`
- `CloseTickets`
- `ManageTickets`

**Legacy mapping:** `AccessTickets` → `ViewTickets`

**Gap:** Dashboard route guard uses `canAccessModeration`, not ticket flags — staff with only ticket permissions may be blocked from nav unless they also have moderation page access via cross-grants.

---

## UX Review Summary

### Server owner journey (current)

```mermaid
flowchart LR
    A[Enable Tickets module] --> B["/ticket setup in Discord"]
    B --> C[Configure settings tab]
    C --> D[Optional archive channel]
    D --> E[Enable command panel]
    E --> F[Members open tickets]
    F --> G[Owner sees list in dashboard]
```

**Friction points:**
1. Setup split between Discord command and dashboard settings.
2. Archive/transcript expectations not met in dashboard.
3. Staff onboarding requires understanding Discord admin roles vs dashboard staff roles.

### Support team journey (current)

```mermaid
flowchart LR
    A[Open dashboard tickets] --> B[See table only]
    B --> C[Switch to Discord for context]
    C --> D[Reply in dashboard OR Discord]
    D --> E[Wait up to 30s for dashboard reply]
```

**Blockers for daily use:** No conversation view, no queue, no assignment.

---

## Recommended v1 Dashboard Structure

```
/guilds/:id/tickets              → Queue list (open default, filters, pagination)
/guilds/:id/tickets/:ticketId    → Detail: timeline, reply, close, notes, metadata
/guilds/:id/settings (tickets)   → Templates, archive, staff roles, auto-close
/guilds/:id/settings (panel)     → Open buttons / category routing
```

### Detail page wireframe (conceptual)

```
┌─────────────────────────────────────────────────┐
│ Ticket #42 · Open · Owner @User · #ticket-42    │
│ [Claim] [Assign ▼] [Close]                      │
├─────────────────────────────────────────────────┤
│ Message timeline (scroll)                       │
│  User: help with billing                        │
│  Staff (Discord): checking now                  │
│  Staff (Dashboard): sent invoice link ✓ delivered│
├─────────────────────────────────────────────────┤
│ [Internal note tab]                             │
│ Reply: [textarea                    ] [Send]    │
└─────────────────────────────────────────────────┘
```

---

## i18n

**Files:** `src/assets/i18n/en.json`, `ar.json`

**Existing keys:** `tickets.*`, `settings.ticket*`, `logs.eventTypes.ticket*`, `staff.permissions.*Tickets*`

**Needed for v1:** detail page, filters, delivery status, claim/assign, empty history notice for legacy tickets.

---

## Frontend Models

**`ticket.models.ts`**

```typescript
interface Ticket {
  id, guildId, ticketNumber,
  ownerDiscordUserId, ownerDisplayName?,
  channelDiscordId, channelName?,
  status, createdAt, closedAt?
}
```

**v1 extensions:** `assignedTo`, `lastMessageAt`, `priority`, `tags`

**New:** `TicketMessage`, `TicketNote` interfaces matching API.

---

## Service Layer

**`guild.service.ts`**

| Method | HTTP |
|--------|------|
| `getTickets(guildId)` | GET tickets |
| `closeTicket(guildId, ticketId)` | PATCH close |
| `sendTicketMessage(guildId, ticketId, content)` | POST messages |

**v1 additions:** `getTicket`, `getTicketMessages`, `claimTicket`, `assignTicket`, `reopenTicket`, `addTicketNote`

---

## Component Architecture (Proposed)

| Component | Responsibility |
|-----------|----------------|
| `TicketsComponent` | List / queue |
| `TicketDetailComponent` | Timeline + actions |
| `TicketMessageListComponent` | Reusable message thread |
| `TicketReplyFormComponent` | Reply + delivery state |
| `TicketSettingsSectionComponent` | Extract from settings (optional) |

---

## Accessibility & UX Polish

- Replace `window.confirm` with accessible modal for close confirm.
- Show toast on reply **queued** vs **delivered** (after poll/refetch).
- Status badges: color + text (already partially done).
- Keyboard: focus reply textarea when expanding row.

---

## Files Reference

| Path | Role |
|------|------|
| `features/tickets/tickets.component.*` | List page |
| `features/settings/settings.component.*` | Ticket settings tab |
| `core/services/guild.service.ts` | API client |
| `core/models/ticket.models.ts` | Types |
| `app-routing.module.ts` | Route + guard |
| `features/layout/dashboard-layout.component.*` | Nav link |
