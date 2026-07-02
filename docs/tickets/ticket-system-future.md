# Ticket System — Future State, Backlog, Risks & Success Criteria

---

## Ticket System v1 — Definition of Done

Ticket System v1 is complete when a support team can **run daily operations entirely from the dashboard + Discord** without misleading gaps, with audit-friendly history and role-appropriate access.

### Checklist

#### Core lifecycle
- [ ] Create ticket via Discord (slash, button, panel)
- [ ] One open ticket per user (configurable limit optional)
- [ ] Close from Discord and dashboard with **consistent** archive behavior
- [ ] Sequential ticket numbers stable under concurrency

#### Message & transcript
- [ ] All Discord text messages in ticket channels persisted
- [ ] Dashboard replies appear in unified timeline
- [x] Ticket detail / transcript page shows full conversation (CM-004 Dashboard transcript route)
- [x] Archive embed accurate — digest only; Dashboard link when configured (CM-004, BR-X02)
- [x] Closed tickets viewable after channel deletion (CM-004 transcript API)

#### Permissions
- [ ] `ViewTickets` gates list/detail
- [ ] `ReplyToTickets` gates dashboard reply API + UI
- [ ] `CloseTickets` gates close API + UI
- [ ] `ManageTickets` gates ticket settings (or documented owner-only exception)
- [ ] Bot close respects same flags via evaluate endpoint
- [ ] Configured staff roles receive Discord channel access

#### Staff workflow
- [ ] Open ticket queue with filter + pagination
- [ ] Claim or assign ticket to staff member
- [ ] Internal notes (staff-only)
- [ ] Reopen closed ticket (documented channel strategy)

#### Configuration
- [ ] Ticket category, archive channel, templates in settings
- [ ] Command panel open flow documented and working
- [ ] Setup path documented (module + category + panel)

#### Reliability
- [ ] Dashboard reply delivery status visible
- [ ] Failed outbound messages retry or surface error
- [ ] Empty ticket list returns 200 for authorized users

#### Logging
- [ ] Open / close / archive events in logs module
- [ ] Optional Discord log channel delivery

#### Documentation
- [ ] Operator guide: setup, staff roles, dashboard workflow
- [ ] API docs for ticket endpoints

**Not required for v1:** multi-category panels, SLA, auto-close, analytics charts, HTML export (Phase 3).

---

## Backlog (CM Items)

| ID | Description | Priority | Complexity | Dependencies |
|----|-------------|----------|------------|--------------|
| **CM-001** | Ticket system technical review & roadmap (this doc set) | P0 | M | — |
| **CM-002** | Add `TicketMessage` entity + Discord message ingestion bot handler | P0 | L | CM-001 |
| **CM-003** | Transcript API: ticket detail + paginated messages | P0 | M | CM-002 |
| **CM-004** | Dashboard ticket detail page with message timeline | P0 | M | CM-003 |
| **CM-005** | Wire granular ticket permissions on API + dashboard guards/UI | P0 | M | CM-001 |
| **CM-006** | Unify close lifecycle + archive from persisted messages | P0 | M | CM-002 |
| **CM-007** | Fix GET tickets 404 vs empty list authorization bug | P1 | S | CM-005 |
| **CM-008** | Staff Discord role channel overwrites on ticket create | P1 | M | CM-005 |
| **CM-009** | Ticket list filters, search, pagination | P1 | M | CM-005 |
| **CM-010** | Claim / assign ticket (DB + API + dashboard) | P1 | M | CM-004 |
| **CM-011** | Internal notes (DB + API + dashboard) | P2 | M | CM-004 |
| **CM-012** | Reopen ticket workflow | P2 | M | CM-006 |
| **CM-013** | Outbound reply delivery status + retry | P2 | M | CM-004 |
| **CM-014** | Remove/fix misleading archive preview copy | P1 | S | CM-004 | **Done (CM-004)** |
| **CM-015** | `TicketCategory` multi-category + panel routing | P2 | L | CM-006 |
| **CM-016** | Open ticket modal forms (custom fields) | P2 | L | CM-015 |
| **CM-017** | Auto-close on inactivity worker | P2 | M | CM-002 |
| **CM-018** | Priority + tags on tickets | P3 | M | CM-010 |
| **CM-019** | Ticket analytics endpoint + overview widgets | P3 | M | CM-002 |
| **CM-020** | Transcript export HTML/TXT | P3 | M | CM-003 |
| **CM-021** | Outbound delivery worker scale (partial index, batching) | P3 | M | CM-013 |
| **CM-022** | DB partial unique index: one open ticket per owner | P2 | S | CM-002 |
| **CM-023** | Ticket operator documentation + setup wizard UX | P2 | M | CM-006 |
| **CM-024** | DM transcript to owner on close (optional setting) | P3 | M | CM-020 |

**Complexity:** S = 1–2 days · M = 3–5 days · L = 1–2 weeks (single developer, incl. tests)

---

## Architectural Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| No message persistence | High | Current | CM-002 — blocker for v1 |
| Dual close paths diverge | Medium | Current | CM-006 unify pipeline |
| Discord 500 channel limit | Medium | Medium | Archive/delete policy; category rotation Phase 3 |
| Global outbound poll at scale | Medium | Medium | CM-021 partial indexes + batch |
| Permission cross-grants | High | Current | CM-005 explicit ticket checks |
| Staff cannot see tickets in Discord | High | Current | CM-008 role overwrites |
| Misleading archive copy | Medium | Mitigated | CM-004 shipped honest digest wording + transcript separation |
| Orphan channels if API create succeeds after channel delete | Low | Low | Idempotent create + reconciliation job (future) |
| EF enum flags scalability | Low | Long-term | ADR for permission storage (platform-wide) |

---

## Technical Debt

1. **`TicketOutboundMessages` as dual-purpose queue** — merge into message outbox pattern in CM-002.
2. **`CanAccessModerationPagesAsync` on ticket routes** — wrong abstraction; CM-005.
3. **Live channel scrape for archive** — replace with DB transcript in CM-006.
4. **Inline reply UI in table** — replace with detail page in CM-004.
5. **No concurrency token on ticket close** — race possible on simultaneous close; add row version or status check.
6. **Settings split:** `/ticket setup` vs dashboard — document or add dashboard "enable" that validates category.
7. **GET by channel returns closed tickets** — auto-reply uses same endpoint; acceptable but document.

---

## Future Scalability Concerns

### Data volume
- Message table grows unbounded — plan retention/archival policy per guild tier.
- Closed tickets forever — add purge after N days for free tier (product decision).

### Worker model
- Single bot instance polling all guilds — horizontal bot scaling requires partitioned polls or message bus.

### Multi-tenant API
- Pending outbound query scans all guilds — needs `TOP N` or guild-sharded workers at 10k+ guilds.

### Discord rate limits
- Bulk archive/export on close may hit limits — batch and queue.

### Compliance
- GDPR: message persistence requires data export/delete story for user requests.

---

## Post–v1 Roadmap (v2+ Ideas)

| Theme | Features |
|-------|----------|
| **Automation** | Escalation rules, SLA timers, scheduled reminders, AI suggested replies |
| **Channels** | Email ingestion, web widget, cross-guild support desks |
| **Discord UX** | Add/remove users from ticket, thread mode, ticket transcripts to DM |
| **Commercial** | Ticket limits per plan, premium analytics, branded transcript pages |
| **Integration** | Webhook on ticket events, Zapier, existing logs/moderation correlation |
| **Quality** | CSAT survey on close, ticket rating, QA review queue |

---

## Commercial Positioning (Recap)

**Ship v1 as:** Dashboard-first support for Discord communities already using the platform for moderation/logs.

**Do not claim parity** with Ticket Tool until CM-015 (categories/forms) and CM-019 (analytics) land.

**Differentiator:** Unified guild control plane + permission model + i18n dashboard + self-hosted option.

---

## Estimated Effort to v1 Complete

| Phase | Scope | Estimate |
|-------|-------|----------|
| Phase 1 | Messages, permissions, detail, archive fix | **15–20 dev-days** |
| Phase 2 | Claim, list UX, notes, reopen, delivery status | **10–12 dev-days** |
| **v1 minimum (Phase 1 + subset Phase 2)** | CM-002 through CM-010, CM-014 | **~25–30 dev-days** |
| Phase 3 (commercial parity extras) | Categories, auto-close, analytics | **+15–20 dev-days** |

Assumes one senior full-stack developer familiar with the codebase, including tests and migration deploy.

---

## Recommended Next Task

**CM-002 — Ticket message persistence & Discord ingestion**

Rationale: Unblocks transcript API, honest archive, detail page, analytics, and auto-close. Highest leverage; reduces reputational risk from incorrect archive messaging.

Implementation sketch:
1. Migration `TicketMessages`
2. `POST /api/bot/tickets/messages` (bot key)
3. Bot `MessageReceived` handler filtered to ticket channels
4. Link outbound queue rows to timeline entries

---

## Document Index

| Document | Contents |
|----------|----------|
| [ticket-system-review.md](./ticket-system-review.md) | Feature matrix, architecture/UX/commercial review |
| [ticket-system-roadmap.md](./ticket-system-roadmap.md) | Phases 1–3 tasks |
| [ticket-system-database.md](./ticket-system-database.md) | Schema current + proposed |
| [ticket-system-api.md](./ticket-system-api.md) | Endpoints + gaps |
| [ticket-system-dashboard.md](./ticket-system-dashboard.md) | UX review + UI plan |
| [ticket-system-bot.md](./ticket-system-bot.md) | Commands, workers, flows |
