# Technical Debt

Known technical debt prioritized by risk. Sourced from step-30 audit, permission scalability review, and implementation work.

---

## P0 — High risk

| Debt | Impact | Suggested solution | Effort |
|------|--------|-------------------|--------|
| **Permission int bitmask (20/32 bits used)** | Blocks 100+ permissions and plugins | Phase 2: PermissionDefinitions + GuildRolePermissions | Large |
| **Bot HTTP call per command for permissions** | Latency + API load at scale | Cache resolved keys in bot (TTL 60s) | Medium |
| **No CI/CD** | Regressions ship to production | GitHub Actions build + test + deploy | Medium |
| **JWT in localStorage** | XSS token theft | Evaluate httpOnly cookie + CSRF | Medium |

---

## P1 — Medium risk

| Debt | Impact | Suggested solution | Effort |
|------|--------|-------------------|--------|
| **GuildPermissionMapper cross-grants** | Unexpected access (logs → tickets) | Key-based checks, remove coarse rules | Medium |
| **Coarse dashboard guards** | Granular flags unused | Wire GuildAccessGuard to specific keys | Medium |
| **Dual permission UI (staff + moderation settings)** | Merge races, maintenance | Single editor with module tabs | Medium |
| **No test projects** | Refactoring fear | Add Infrastructure + API integration tests | Large |
| **GuildsController size** | Hard to maintain | Split by feature area | Medium |
| **ManagePermissionRoles not enforced** | Misleading UI flag | Enforce or remove flag | Small |
| **No permission audit log** | Compliance gap | AuditLog entity on role CRUD | Medium |
| **Polling workers (30s)** | Delayed ticket messages/sync | Reduce interval or event-driven queue | Medium |
| **Manual billing only** | Revenue friction | Stripe integration | Large |

---

## P2 — Lower risk

| Debt | Impact | Suggested solution | Effort |
|------|--------|-------------------|--------|
| **No API pagination** | Large guild log/ticket load | Cursor pagination | Medium |
| **No Application layer project** | Infrastructure bloat | Extract DiscordBot.Application | Large |
| **Bot ApiModels duplicate DTOs** | Drift from API | Shared contracts package | Medium |
| **Hardcoded dashboard permission list** | Deploy for each new permission | Dynamic from PermissionDefinitions API | Medium |
| **Step docs overlap handbook** | Doc confusion | Handbook is canonical; step docs historical | Small |
| **EF tools version warning** | Scaffold friction | Update dotnet-ef global tool | Small |
| **Bundle size budget exceeded** | Dashboard perf | Lazy load admin routes | Small |

---

## Resolved debt

| Debt | Resolution | Date |
|------|------------|------|
| Dual permission systems (GuildStaff, ModerationPermissionRoles) | Unified GuildPermissionRoles | 2026-07-02 |
| Dual resolvers (ModerationPermissionResolver) | Single GuildPermissionResolver | 2026-07-02 |
| Overview vs modules status mismatch | Overview uses modules API | 2026-07-02 |
| Moderation settings bad route | Fixed routerLink segments | Beta feedback |

---

## Related docs

- `/docs/architecture/2026-07-02-permissions-scalability-review.md`
- `/docs/step-30-architecture-audit.md`
- `backlog.md`
