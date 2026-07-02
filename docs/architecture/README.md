# Discord Bot Platform — Architecture Handbook

This folder is the **Single Source of Truth** for how the Discord Bot Platform is designed, built, and operated.

Every future feature task, refactor, and AI agent session should align with these documents.

---

## What this handbook covers

| Area | Documents |
|------|-----------|
| Product direction | `vision.md`, `mission.md`, `product-overview.md` |
| System design | `system-overview.md`, `architecture-principles.md`, `solution-structure.md` |
| Runtime components | `backend-architecture.md`, `bot-architecture.md`, `dashboard-architecture.md` |
| Domain systems | `module-system.md`, `permission-system.md`, `subscription-system.md` |
| Security | `authentication.md`, `authorization.md`, `security.md` |
| Data & API | `database.md`, `api-design.md` |
| Operations | `deployment.md`, `environments.md`, `logging.md`, `monitoring.md` |
| Engineering rules | `coding-standards.md`, `naming-conventions.md`, `folder-structure.md` |
| Planning | `roadmap.md`, `glossary.md` |

Related folders:

- `/docs/product/` — business-facing product docs (users, pricing, competitors)
- `/docs/adr/` — Architecture Decision Records (significant design choices)
- `/docs/project-management/` — backlog, debt, releases, sprints
- `/docs/progress/` — task completion reports
- `/docs/step-*.md` — chronological implementation guides (historical reference)

---

## Recommended reading order — new developers

1. **`product-overview.md`** — what the platform does
2. **`system-overview.md`** — components and communication
3. **`solution-structure.md`** — repo layout and dependency rules
4. **`backend-architecture.md`** — API + Infrastructure
5. **`bot-architecture.md`** — Discord worker
6. **`dashboard-architecture.md`** — Angular SPA
7. **`authentication.md`** + **`authorization.md`** — who can do what
8. **`module-system.md`** + **`permission-system.md`** + **`subscription-system.md`**
9. **`database.md`** + **`api-design.md`**
10. **`coding-standards.md`** + **`naming-conventions.md`**
11. **`deployment.md`** + **`environments.md`**
12. **`glossary.md`** — project vocabulary

Optional deep dives: `/docs/step-30-architecture-audit.md`, `/docs/architecture/2026-07-02-permissions-scalability-review.md`

---

## Recommended reading order — AI agents

Before implementing any task:

1. Read **`architecture-principles.md`** — non-negotiable boundaries
2. Read the component doc for the area you will touch (backend / bot / dashboard)
3. Read **`permission-system.md`** if touching access control
4. Read **`module-system.md`** if touching feature toggles
5. Read **`subscription-system.md`** if touching plan limits
6. Read **`coding-standards.md`** before writing code
7. Check **`/docs/project-management/technical-debt.md`** for known pitfalls
8. Check **`/docs/project-management/backlog.md`** for priority context

After completing a task: add a report under `/docs/progress/`.

For architectural decisions that change direction: create an ADR in `/docs/adr/`.

---

## Document maintenance rules

1. **Update handbook docs when architecture changes** — not only step guides.
2. **Do not duplicate** — link to canonical docs instead of copying sections.
3. **Mark assumptions** explicitly when code and docs disagree.
4. **Historical step docs** (`step-01` … `step-30`) are preserved but may be superseded by this handbook.
5. **ADRs** capture *why* a decision was made; handbook captures *what* the system is today.

---

## Current platform snapshot (July 2026)

| Component | Technology |
|-----------|------------|
| API | ASP.NET Core 9, JWT + Bot API key |
| Bot | .NET 9 worker, Discord.Net 3.17 |
| Dashboard | Angular 16, ngx-translate (EN/AR) |
| Database | PostgreSQL 16, EF Core 9 |
| Production | Railway (API, Bot, DB), Vercel (dashboard) |
| Architecture style | Layered monolith (Domain → Infrastructure → Api/Bot) |

**Latest structural change:** Unified permission system (`GuildPermissionRoles` + `GuildPermissions` flags enum). Migration: `20260702151245_UnifyGuildPermissions`.
