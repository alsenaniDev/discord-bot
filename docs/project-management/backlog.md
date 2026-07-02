# Backlog

Prioritized work items derived from architecture audit, permission scalability review, beta feedback, and handbook gaps.

**Last updated:** 2026-07-02 (TASK-000)

---

## Critical

| ID | Item | Source | Phase |
|----|------|--------|-------|
| C-01 | Apply `UnifyGuildPermissions` migration to production before deploy | progress/2026-07-02-unified-permissions | 1 |
| C-02 | Verify beta deploy uses correct Vercel URL + API CORS | step-29, audit | 1 |
| C-03 | Permission Phase 2: PermissionDefinitions + junction tables | scalability review | 2 |
| C-04 | Bot/API permission result caching | scalability review | 2 |
| C-05 | No automated payment — manual upgrade only (document until Stripe) | audit | 2 |

---

## High

| ID | Item | Source | Phase |
|----|------|--------|-------|
| H-01 | Implement `/ban` and `/timeout` commands | audit, module gaps | 3 |
| H-02 | Granular dashboard guards (per permission key) | permission unification gaps | 2 |
| H-03 | Single permission editor UI (module-grouped) | scalability review | 2 |
| H-04 | CI/CD pipeline (build + deploy) | audit | 2 |
| H-05 | Structured logging + Sentry | monitoring gap | 2 |
| H-06 | Staging environment | environments.md assumption | 2 |
| H-07 | Stripe billing integration | product/pricing | 3 |
| H-08 | Ticket teams / queue-scoped permissions | scalability review | 3 |
| H-09 | GuildStaffMembers roster entity | scalability review | 3 |
| H-10 | Permission change audit log | security, review | 2 |
| H-11 | Retroactive ADR 0001 unified permissions | adr/README | 2 |

---

## Medium

| ID | Item | Source | Phase |
|----|------|--------|-------|
| M-01 | Auto-moderation (spam, link filter) | competitors gap | 4 |
| M-02 | Log retention policy per plan | logging.md | 3 |
| M-03 | API pagination (tickets, logs, admin guilds) | api-design.md | 2 |
| M-04 | Bot health / heartbeat monitoring | monitoring.md | 2 |
| M-05 | JWT httpOnly cookie evaluation | security.md | 2 |
| M-06 | Rate limiting on auth and bot endpoints | security.md | 2 |
| M-07 | Test projects (Infrastructure unit, API integration) | audit | 2 |
| M-08 | Consolidate moderation-settings into staff page (deep link) | dual UI adapter | 2 |
| M-09 | Enforce ManagePermissionRoles flag for delegated staff mgmt | permission gaps | 2 |
| M-10 | Overview/settings module status consistency | step-29 fixes | 1 ✅ |
| M-11 | Clear logs DELETE confirmation flow | step-29 | 1 ✅ |
| M-12 | Admin plans CRUD UI | git status | 1 ✅ |

---

## Low

| ID | Item | Source | Phase |
|----|------|--------|-------|
| L-01 | API versioning `/api/v1/` | api-design.md | 4 |
| L-02 | SignalR real-time dashboard | roadmap Phase 3 | 3 |
| L-03 | Export logs to CSV | feature requests | 3 |
| L-04 | Correlation IDs bot → API | logging.md | 2 |
| L-05 | Split GuildsController into feature controllers | audit debt | 4 |
| L-06 | Application project layer extraction | audit | 4 |
| L-07 | Bot message i18n | product assumption | 4 |

---

## Future

| ID | Item | Phase |
|----|------|-------|
| F-01 | Analytics module | 4 |
| F-02 | Automation / workflow builder | 4 |
| F-03 | Plugin marketplace + plugin.* permissions | 4 |
| F-04 | Leveling / XP | Future ideas |
| F-05 | White-label dashboard | 5 |
| F-06 | GDPR export/deletion | 5 |
| F-07 | Multi-region deployment | 5 |
| F-08 | SSO beyond Discord OAuth | 5 |

---

## Completed (recent)

| Item | Report |
|------|--------|
| Unified permission system | progress/2026-07-02-unified-permissions.md |
| Architecture handbook TASK-000 | progress/2026-07-02-task-000-architecture-handbook.md |
| Architecture audit doc | step-30-architecture-audit.md |
| Permission scalability review | architecture/2026-07-02-permissions-scalability-review.md |

## Related docs

- `milestones.md`, `technical-debt.md`
- `/docs/architecture/roadmap.md`
