# D-001 — Ticket Management Domain Blueprint (Final Report)

**Date:** 2026-07-02  
**Task:** D-001 — Ticket Management Domain Blueprint  
**Type:** Documentation only — no code changes

---

## Summary

Created the **official Ticket Management Domain Blueprint** — the business architecture foundation for all ticket-related work (persistence, API, Dashboard, Bot, workers, analytics, automation, enterprise).

The document models the domain from **business consistency**, not implementation. The **Ticket** aggregate root owns case identity and lifecycle; the **Ticket Timeline** is defined as the heart of the domain — the authoritative record from which Transcript, Analytics, SLA, and Automation derive.

The blueprint aligns with PB-001, UL-001, CM-001, and the Live MVP (~52% toward v1). It distinguishes **Live** behavior, **v1** requirements, and **Future** concepts without redesigning the product.

**Deliverable:** [docs/domains/ticket-management/ticket-domain-blueprint.md](../domains/ticket-management/ticket-domain-blueprint.md)

---

## Business Concepts Defined

**58 domain concepts** documented across §4 and aggregate design, including:

| Core | Supporting | Future |
|------|------------|--------|
| Ticket, Ticket Number, Owner | Ticket Outbound Message (delivery artifact) | Queue, Ticket Category |
| Ticket Timeline, Timeline Event | Archive, Discord Channel (artifact) | Priority, SLA, Escalation |
| Ticket Status, Ticket Participant | Support Team (organizational) | Observer, Watcher |
| Transcript | Command Panel (entry — external) | Merge, Split |
| Ticket Assignment | | Attachment (persisted) |
| Claim (action), Internal Note | | Automation Event |
| System Event | | Waiting Customer/Staff, Resolved |

**Key modeling decisions:**

1. **Ticket** is the aggregate root — not channel, not message.
2. **Timeline Event** append-only — corrections append; no mutable history.
3. **Archive ≠ Transcript** — notification vs source of truth (UL-001 enforced).
4. **Claim** is action; **Ticket Assignment** is state.
5. Discord channel is **execution artifact**, replaceable on reopen.

---

## Rules Established

**40+ business rules** codified in §7 with IDs (BR-C, BR-P, BR-T, BR-A, BR-S, BR-R, BR-X, BR-Z, BR-D, etc.):

| Category | Example rules |
|----------|---------------|
| Creation | One Guild; one Owner; monotonic Ticket Number; one Open per Owner (default) |
| Timeline | Append-only; every Discord + Dashboard message → event; System never impersonates human |
| Close | Closed tickets retain Timeline; Archive failure must not block close |
| Transcript | Reconstructable after channel deletion; Archive must not lie about completeness |
| Permissions | Module before Capability; View/Reply/Close/Manage separation |
| Reopen | Same Ticket Number; new channel optional; staff-authorized |

**8 policies** defined with defaults: Assignment, Close, Archive, Transcript, Reopen, Retention, Escalation (future), Notification (future), One-open-ticket, Permission.

**State machine:** v1 (Open ↔ Closed + Reopen) and future (Unassigned, Waiting, Resolved) documented with allowed, forbidden, and recovery transitions.

---

## Major Architecture Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Ticket aggregate root** | Case invariants belong to ticket identity, not messages |
| 2 | **Timeline-centric domain** | Transcript, analytics, SLA, AI all derive from Timeline |
| 3 | **Separate Domain Events from Timeline Events** | Timeline = case narrative; Domain Events = cross-domain notification |
| 4 | **v1 lifecycle minimal** | Open/Closed/Reopen ships first; Waiting/Resolved/SLA extend without breaking aggregate |
| 5 | **Policies configurable; invariants fixed** | Guild Owner tunes behavior; integrity rules non-negotiable |
| 6 | **Bot is actor, not rule owner** | Business rules enforced at aggregate boundary — Bot is adapter |
| 7 | **Logging domain consumes events** | TicketOpened/Closed/Archived not replaced by Timeline |
| 8 | **Moderation boundary strict** | Kick/warn outside ticket domain |
| 9 | **Merge/Split as multi-aggregate orchestration** | Explicit events on each ticket; not hidden mutation |
| 10 | **Future extensions attach via events/projections** | AI, CRM, marketplace do not redefine Ticket |

---

## Potential Risks

| Risk | Impact | Mitigation in blueprint |
|------|--------|-------------------------|
| Timeline not implemented in v1 scope creep | Blocks transcript, analytics, SLA | §15 Definition of Done lists Timeline as v1 gate |
| Dual close paths (Live) violate Close Policy | Inconsistent business outcome | BR-S05 + recovery transitions; CM-006 |
| Developers treat Outbound Message queue as Timeline | Permanent dual model | Anti-pattern §14; merge at CM-002 |
| Waiting/Resolved states added without policy | State machine explosion | Future extension diagram separate from v1 |
| Archive embed continues false Transcript claim | Trust | BR-X02 + Anti-pattern |
| Assignment without channel access | Support team blocked in Discord | BR-Z + Participation rules; CM-008 |
| Automation bypasses Timeline | Audit holes | Automation Event type; future growth §11 |
| GDPR purge vs immutability | Legal conflict | BR-T03 exception documented in Retention Policy |

---

## Open Questions

| # | Question | Recommendation |
|---|----------|----------------|
| 1 | **Reopen:** new channel vs reuse existing if present? | Blueprint default: new channel (v1); confirm in ADR at CM-012 |
| 2 | **Owner may reopen?** | Default no — staff only; confirm with product owner |
| 3 | **Assignment required before reply?** | Default no for v1; queue-heavy guilds may want yes later |
| 4 | **Include Internal Notes in owner transcript export?** | Default no — confirm for enterprise |
| 5 | **Transcript snapshot immutable JSON at close?** | Recommended optional policy — decide at CM-006 |
| 6 | **Waiting Customer/Staff as Status or tags?** | Future — prefer sub-status or labels before enum explosion |
| 7 | **Auto Reply messages on Timeline?** | Recommend AutoReplySent event for analytics consistency |
| 8 | **Concurrent close race** | Append-only StatusChanged + idempotent close — detail at implementation ADR |
| 9 | **One-open-ticket configurable per guild?** | Reserve in One-open-ticket Policy |
| 10 | **Ticket Category in v1 or v2?** | Blueprint: Future (CM-015) — not Domain v1 |

---

## Recommendations

### Documentation chain

1. Link D-001 from [Product Blueprint](../blueprint/product-blueprint.md) Appendix B and [Ubiquitous Language](../blueprint/ubiquitous-language.md) Ticket section.
2. Update `/docs/tickets/*` technical specs to reference D-001 as business authority (on next edit).
3. CM-002 implementation spec should map each Timeline Event type to §8 catalog.

### Governance

4. No ticket database/API/UI design merges without traceability to a rule in §7 or concept in §4.
5. New Timeline Event types require D-001 revision history entry.
6. File **ADR** for Reopen channel strategy before CM-012.

### Product

7. Position v1 marketing only after §15 Definition of Done met — especially Timeline + honest Archive.
8. Do not implement SLA/Merge/AI until Timeline v1 stable — blueprint dependency order in §11.

---

## Suggested Next Task

**CM-002 — Ticket message persistence (Timeline foundation)**

Implementation planning (not code in D-001 scope) should:

1. Map **MessageSent**, **StaffReplyQueued**, **StaffReplyDelivered**, **StaffReplyFailed** Timeline Events from §8.
2. Satisfy BR-T01, BR-T02, BR-T03, BR-X03.
3. Produce progress report referencing D-001 rule IDs implemented.

Alternative documentation task:

**Update CM-001 ticket-system-database.md** to reframe proposed schema as **projections of Ticket aggregate** — business columns trace to D-001 concepts (still a doc task, not code).

---

## Documents Created

| Path | Description |
|------|-------------|
| [docs/domains/ticket-management/ticket-domain-blueprint.md](../domains/ticket-management/ticket-domain-blueprint.md) | D-001 full domain blueprint (15 sections, Mermaid diagrams) |
| [docs/progress/2026-07-02-D-001-ticket-domain-blueprint.md](./2026-07-02-D-001-ticket-domain-blueprint.md) | This report |

---

## Constraints Observed

- No code modified
- No entities, migrations, API, or database design created
- No product redesign — modeled existing direction from PB-001 + CM-001

---

## Document hierarchy (updated)

```
Product Blueprint (PB-001)           — What & why
Ubiquitous Language (UL-001)         — Official terms
Ticket Domain Blueprint (D-001)      — Ticket business model  ← NEW
Architecture Handbook                — How (system)
/docs/tickets/ technical specs       — Implementation detail (derive from D-001)
Progress reports                     — Task completion
```

When domain rules conflict with technical specs, **D-001 wins** until revised via domain blueprint version bump + ADR.
