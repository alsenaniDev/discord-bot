# Technical Debt

Known technical debt prioritized by risk. Sourced from step-30 audit, permission scalability review, and implementation work.

---

## P0 — High risk

| Debt | Impact | Suggested solution | Effort |
|------|--------|-------------------|--------|
| **Permission int bitmask (20/32 bits used)** | Blocks 100+ permissions and plugins | Phase 2: PermissionDefinitions + GuildRolePermissions | Large |
| **Bot HTTP call per command for permissions** | Latency + API load at scale | Cache resolved keys in bot (TTL 60s) | Medium |
| **JWT in localStorage** | XSS token theft | Evaluate httpOnly cookie + CSRF | Medium |
| **Production deploy lag vs codebase** | CM-003/004 routes missing on prod until redeploy | Deploy + migrate per release-0.1-checklist | Small |

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
| **No CI deploy/test pipeline** | Build-only CI; no tests or deploy automation | Extend GitHub Actions | Medium |
| **Dashboard bundle size budget exceeded** | Initial bundle ~683 KB vs 550 KB warning threshold; slower first load on mobile | Lazy-load admin routes; audit imports (R-002: **not a release blocker**) | Medium |

---

## P2 — Lower risk

| Debt | Impact | Suggested solution | Effort |
|------|--------|-------------------|--------|
| **Logs API pagination missing** | Large guild log load (200 cap) | Cursor pagination for logs | Medium |
| **No Application layer project** | Infrastructure bloat | Extract DiscordBot.Application | Large |
| **Bot ApiModels duplicate DTOs** | Drift from API | Shared contracts package | Medium |
| **Hardcoded dashboard permission list** | Deploy for each new permission | Dynamic from PermissionDefinitions API | Medium |
| **Step docs overlap handbook** | Doc confusion | Handbook is canonical; step docs historical | Small |
| **EF tools version warning** | Scaffold friction | Update dotnet-ef global tool | Small |

---

## Resolved debt

| Debt | Resolution | Date |
|------|------------|------|
| Dual permission systems (GuildStaff, ModerationPermissionRoles) | Unified GuildPermissionRoles | 2026-07-02 |
| Dual resolvers (ModerationPermissionResolver) | Single GuildPermissionResolver | 2026-07-02 |
| Overview vs modules status mismatch | Overview uses modules API | 2026-07-02 |
| Moderation settings bad route | Fixed routerLink segments | Beta feedback |
| No CI build gate | GitHub Actions `build.yml` (dotnet + dashboard) | 2026-07-02 R-002 |
| Dead `GetGuildTicketsAsync` service method | Removed; use `ITicketReadService` | 2026-07-02 R-002 |
| `environment.ts` default pointed at production API | Base file now localhost; prod via fileReplacements | 2026-07-02 R-002 |
| Misleading archive/transcript copy | CM-004 honest digest + transcript page | 2026-07-02 CM-004 |

---

## Related docs

- `/docs/architecture/2026-07-02-permissions-scalability-review.md`
- `/docs/step-30-architecture-audit.md`
- `backlog.md`
