# AR-001 — Read Model Architecture

**Date:** 2026-07-02  
**Status:** Complete  
**Deliverable:** `/docs/architecture/read-model-architecture.md`

---

## Summary

Defined the **platform-wide Read Model Architecture** — how business aggregates are exposed for reading across Tickets, Moderation, Logging, Analytics, Automation, Dashboard, AI, Search, and Reporting.

The architecture establishes a clear separation:

**Write Model → Domain Events / Facts → Read Models → Consumers**

This applies to every domain. It reconciles with the existing layered monolith (no event bus, no MediatR) while making query-side projections an official, non-optional pattern.

---

## Architecture decisions

| Decision | Rationale |
|----------|-----------|
| **Read Models are mandatory for dashboard/analytics/AI/search** | Prevents aggregate leakage, N+1 Timeline queries, and dishonest UX at scale |
| **Timeline = Write Model; Conversation = Read Model** | CM-002 built authoritative Timeline; staff UI and transcript need a presentation projection (CM-003) |
| **Sync projectors in v1, async for statistics/search v2+** | Matches current polling workers and transaction-based services — no Kafka prerequisite |
| **LogEntry remains audit read surface, not ticket history** | Preserves separation from Ticket Timeline per D-001 and UL-001 |
| **Guild-scoped projections with permission gates unchanged** | Multi-tenant isolation and unified permission model carry forward |
| **Projections are disposable / rebuildable** | Write Model is source of truth; drift recovery via replay |
| **No CQRS rebranding of entire codebase** | Read Models live inside Infrastructure services — incremental adoption per domain |

---

## Trade-offs

| Choice | Benefit | Cost |
|--------|---------|------|
| Same PostgreSQL for write + read (v1) | Operational simplicity, one migration path | OLTP/OLAP contention at very large scale |
| Sync projection in transaction | Strong consistency for Summary after write | Slightly longer write transactions |
| Transitional raw Timeline API (CM-002) | Shipped Timeline foundation quickly | Dashboard sees internal event types until Conversation read model |
| Named projection catalog vs free-form DTOs | Traceability, analytics consistency | More documentation and projector code per feature |
| Cursor pagination for conversations | Stable performance on large tickets | More complex Angular infinite-scroll |

---

## Risks

1. **Incremental adoption drift** — teams may keep ad-hoc DTO queries unless CM tasks enforce Definition of Done.
2. **Over-projection too early** — building separate tables before query pain may add maintenance; AR-001 allows SQL views/query projections for v0.
3. **Confusion with LogEntry** — developers may still use logs as message history without reading AR-001 §2 P12.
4. **Principle §10 "No CQRS"** — requires explicit reading of AR-001 reconciliation section to avoid rejection of valid Read Model work.

---

## Future recommendations

1. **CM-003** — Implement `Ticket Conversation` read model + paginated API; deprecate raw Timeline list for dashboard.
2. **Add `Ticket Summary` projector** — denormalize `LastActivityAt`, delivery status on list rows.
3. **Statistics workers** — async rollups for overview/analytics module.
4. **ADR** — when introducing Redis cache layer or OpenSearch for search projections.
5. **Update `database.md`** — add Read Model tables section as projections land.
6. **UL-001 addendum** — formalize "Read Model" and "Projection" terms if not already sufficient.

---

## Suggested next task

**CM-003 — Ticket Conversation Read Model & Transcript UX**

Implement the first full AR-001 projection: paginated **Ticket Conversation** read model sourced from Timeline Events, dashboard ticket detail page, and bot archive preview consuming the read API — without a second message store.

---

## Files changed

| File | Change |
|------|--------|
| `docs/architecture/read-model-architecture.md` | **Created** — full AR-001 specification (15 sections + diagrams) |
| `docs/architecture/README.md` | Added to handbook index, reading orders, platform snapshot |
| `docs/architecture/architecture-principles.md` | Cross-reference to Read Model Architecture |

**No code, migrations, or runtime changes.**
