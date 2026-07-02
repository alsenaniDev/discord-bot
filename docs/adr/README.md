# Architecture Decision Records (ADR)

## What is an ADR?

An **Architecture Decision Record** documents a significant technical decision: the context, options considered, decision made, and consequences.

ADRs capture **why** the system looks the way it does — the handbook captures **what** it is today.

## When to create an ADR

Create an ADR when a decision:

- Changes data model shape (new tables, removed entities)
- Introduces a new external dependency or service
- Changes authentication or authorization model
- Affects bot ↔ API communication pattern
- Is hard to reverse (migrations, public API contracts)
- Will be debated or questioned later

**Do not** create ADRs for routine bug fixes, UI tweaks, or single-endpoint additions.

### Examples requiring ADRs

| Decision | ADR? |
|----------|------|
| Unified permission system | Yes (retroactive ADR recommended) |
| Phase 2 permission catalog + junction tables | Yes |
| Add Stripe billing | Yes |
| Fix ticket close button label | No |
| Add `/ban` command | No (unless permission model changes) |

## Naming convention

```
docs/adr/NNNN-short-kebab-title.md
```

- `NNNN` — 4-digit sequential number (0001, 0002, …)
- `short-kebab-title` — lowercase, hyphenated

Examples:

- `0001-unified-guild-permissions.md`
- `0002-permission-catalog-junction-tables.md`
- `0003-jwt-httponly-cookie-migration.md`

## Required sections

Every ADR must include:

```markdown
# ADR-NNNN: Title

## Status
Proposed | Accepted | Deprecated | Superseded by ADR-XXXX

## Date
YYYY-MM-DD

## Context
What problem or force motivates this decision?

## Decision
What was decided?

## Alternatives considered
What else was evaluated and why rejected?

## Consequences
Positive and negative outcomes. What becomes easier/harder?

## References
Links to handbook sections, PRs, issues, progress reports.
```

## Approval process

1. **Author** drafts ADR with status `Proposed`
2. **Review** — project lead or senior engineer reviews in PR
3. **Accept** — merge PR, change status to `Accepted`
4. **Implement** — code changes reference ADR number in progress report
5. **Supersede** — if reversed, mark old ADR `Superseded by ADR-XXXX`; do not delete

For solo development: self-review checklist + merge to main constitutes acceptance.

## Index

| ADR | Title | Status |
|-----|-------|--------|
| *(none filed yet)* | | |

### Recommended retroactive ADRs

| Proposed ADR | Topic |
|--------------|-------|
| 0001 | Unified GuildPermissionRoles (July 2026) |
| 0002 | Bot has no direct database access |
| 0003 | Manual upgrade request workflow (no Stripe) |

## Related docs

- `/docs/architecture/README.md`
- `/docs/architecture/architecture-principles.md`
