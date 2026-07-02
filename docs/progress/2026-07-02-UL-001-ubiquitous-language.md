# UL-001 — Ubiquitous Language (Final Report)

**Date:** 2026-07-02  
**Task:** UL-001 — Create the Official Ubiquitous Language  
**Type:** Documentation only — no code changes

---

## Summary

Created the **official Ubiquitous Language** specification for the Discord Bot Platform — the canonical business vocabulary document. It defines **58+ terms** with business definition, technical definition, rules, related terms, and examples; plus naming rules, consistency rules, forbidden terminology, and domain language principles.

Updated [Product Blueprint](../blueprint/product-blueprint.md) (v1.1) and [Architecture Glossary](../architecture/glossary.md) to reference UL-001 as the authoritative source for term meaning.

---

## Documents Created / Updated

| Path | Action |
|------|--------|
| [docs/blueprint/ubiquitous-language.md](../blueprint/ubiquitous-language.md) | **Created** — UL-001 full specification |
| [docs/blueprint/product-blueprint.md](../blueprint/product-blueprint.md) | **Updated** — document hierarchy, Appendix B, revision 1.1 |
| [docs/architecture/glossary.md](../architecture/glossary.md) | **Updated** — defers to UL-001; expanded index |
| [docs/progress/2026-07-02-UL-001-ubiquitous-language.md](./2026-07-02-UL-001-ubiquitous-language.md) | **Created** — this report |

---

## Terms Defined (Official Count)

**58 required terms** documented in full specification format, plus supporting terms:

| Category | Terms |
|----------|-------|
| Core platform | Platform, Tenant, Multi-Tenant, Guild, Guild Owner, Guild Member, Guild Staff Member, Dashboard, Dashboard User, Bot, API, Platform Administrator |
| Modules & config | Product Domain, Module, Guild Module, Capability, Permission, Permission Role, Discord Role, Policy, Configuration, Guild Settings, Settings, Server Profile |
| Subscriptions | Subscription, Subscription Plan, Upgrade Request, Feature Flag |
| Tickets | Ticket, Ticket Status, Ticket Priority, Ticket Participant, Ticket Timeline, Timeline Event, Ticket Outbound Message, Ticket Assignment, Claim, Internal Note, Transcript, Archive, Support Team |
| Moderation | Moderation, Warning, Moderation Case, Reaction Role |
| Logging | Log Entry, Activity Log, Audit Log, Domain Event |
| Automation | Command Panel, Auto Reply, Trigger, Action, Automation, Workflow |
| Analytics | Analytics, Report |
| Infrastructure | Integration, Notification, Worker, Background Job, Discord Resource, Synchronization |
| DDD meta | Entity, Value Object, Aggregate |

**Additional supporting terms in glossary index:** Module Guard, LogEventType references, Bot API Key, etc.

---

## Terms Renamed (Official vs Legacy)

| Legacy / informal | Official term | Status |
|-------------------|---------------|--------|
| Feature (product) | **Module** or **Capability** | Deprecated in specs |
| Staff (entity) | **Permission Role** | Removed tables |
| Moderation Permission Role | **Permission Role** | Merged 2026-07-02 |
| GuildStaff | **Permission Role** | Removed |
| AccessTickets | **ViewTickets** | Legacy alias only |
| Conversation (tickets) | **Ticket Timeline** | Forbidden |
| Claim (as state noun) | **Ticket Assignment** | Claim = action only |
| Audit log (Logs module) | **Activity Log** / **Log Entry** | Audit Log = planned compliance |
| Server (entity/API) | **Guild** | UI route `/servers` exception documented |
| Feature flag (guild toggles) | **Module** enablement | Feature Flag reserved for platform ops |
| Guild Profile (UI) | **Server Profile** | Matches code/routes |

---

## Conflicting Terminology Discovered

| Conflict | Where it appears | Resolution in UL-001 |
|----------|------------------|----------------------|
| **Staff** vs **Permission Role** | Dashboard "Staff" page, `getStaff()` API methods, `GuildPermissionRole` entity | UI label "Staff" OK; persisted model and specs use **Permission Role** |
| **Server** vs **Guild** | Route `/servers`, embed `{server}` placeholders vs `Guild` entity | **Guild** official for entities/API; Server allowed in Discord-facing copy |
| **Activity Log** vs **Audit Log** | Logs module name vs enterprise expectation | **Activity Log** = product today; **Audit Log** = planned compliance export |
| **Archive** vs **Transcript** | `TicketArchiveService` copy claims "full ticket in dashboard" | **Archive** = Discord embed preview; **Transcript** = persisted full record (planned) |
| **Log** overload | Application logging, Logs module, LogEntry, Discord audit log | Qualify: Log Entry, Activity Log, Application Log, Discord Audit Log (external) |
| **Role** overload | Discord Role, Permission Role, colloquial "staff role" | Never bare **Role** — always qualified |
| **Trigger** overload | `AutoReplyRule.Trigger` vs future Workflow Trigger | Qualify: Auto Reply Trigger vs Workflow Trigger |
| **Feature** in code/comments | Various | Product docs use Module/Capability; code rename not in scope |
| **Notification** stub | Layout bell UI with no backend | Marked **Planned** — do not document as shipped |
| **Ticket Timeline** not in code | CM-001 roadmap | Official **Planned** term — do not use Conversation in new specs |
| **Permission** vs **Capability** | Enum named `GuildPermissions` | Interchangeable in docs; **Capability** preferred in product/ADR prose |

---

## Recommendations

### Immediate (documentation)

1. **Reference UL-001 in ADR template** (`/docs/adr/README.md`) — require official terms in Decision and Context sections.
2. **Update ticket docs** (`/docs/tickets/*`) on next edit to replace "conversation" / "full history" with **Ticket Timeline** / **Transcript** language per UL-001.
3. **Add UL-001 to architecture README** reading order for new developers (alongside Product Blueprint).

### Code alignment (future tasks — not UL-001 scope)

4. Consider renaming dashboard API methods `getStaff` → `getPermissionRoles` in a dedicated refactor task (breaking change — needs ADR).
5. Fix ticket archive embed copy when CM-014 ships — align with **Archive** vs **Transcript** definitions.
6. When CM-002 introduces `TicketMessage`, map entity to **Timeline Event** in domain spec.

### Governance

7. **Term additions** require UL-001 revision history entry; **term renames** require ADR if they affect API or database.
8. AI agents and contributors: read UL-001 before generating domain specs or user-facing strings.

---

## Potential Future Terminology

Terms likely needed in Phase 2–4 — not yet official beyond brief mentions:

| Term | When needed |
|------|-------------|
| **Permission Definition** | Phase 2 permission catalog (string keys) |
| **Ticket Category** | Multi-category tickets (CM-015) |
| **Ticket Team** | Queue-scoped support (Phase 3) |
| **Workflow Run** | Automation engine execution record |
| **Webhook Subscription** | Outbound integration delivery |
| **Platform Feature Flag** | Operator kill switches (if introduced) |
| **Data Export** | GDPR / enterprise compliance |
| **Service Level Agreement (SLA)** | Enterprise tier |
| **Plugin** | Marketplace module (Phase 4) |
| **Shard** | Bot horizontal scaling |

Add these to UL-001 when corresponding ADR or domain spec is approved — do not pre-define in depth without implementation commitment.

---

## Suggested Next Task

**DB-001 — Domain Blueprint: Tickets** (or continue **CM-002** implementation planning)

Rationale:
- UL-001 establishes **Ticket Timeline**, **Timeline Event**, **Ticket Assignment**, **Transcript**, and **Archive** with clear rules.
- Ticket domain blueprint should reference UL-001 terms verbatim and map each to entities/API routes.
- CM-002 (message persistence) is the first implementation task that materializes **Timeline Event** in code — domain blueprint should precede or accompany that work.

Alternative documentation task:

**ADR-0001 — Retroactive record: Unified Permission Roles** (referenced as missing in TASK-000 report) using official **Permission Role** and **Capability** vocabulary from UL-001.

---

## Constraints Observed

- No code modified
- No migrations created
- No features implemented

---

## Document hierarchy (updated)

```
Product Blueprint (PB-001)     — What & why
Ubiquitous Language (UL-001)   — Official names & meanings  ← NEW
Architecture Handbook          — How
Domain specs (/docs/tickets/)  — Feature depth
ADRs                           — Decisions
Progress reports               — Task completion
```

When naming conflicts arise: **UL-001** → **Product Blueprint** → **Architecture Handbook** → code.
