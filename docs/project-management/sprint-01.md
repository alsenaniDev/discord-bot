# Sprint 01

**Theme:** Beta foundation + documentation + permission unification  
**Period:** June – July 2026 (informal — no formal sprint tooling yet)

---

## Sprint goals

1. Stabilize beta feedback issues
2. Unify permission architecture
3. Create Architecture Handbook (TASK-000)
4. Prepare for wider beta deploy

---

## Completed

| Item | Owner | Notes |
|------|-------|-------|
| Beta feedback fixes (routing, overview, logs DELETE) | Dev | step-29 |
| Unified permissions implementation | Dev | progress/2026-07-02-unified-permissions |
| Permission scalability review | Architect | architecture review doc |
| Admin plans CRUD + MonthlyPrice | Dev | Dashboard + API |
| Architecture audit (step-30) | Dev | 664-line audit |
| Architecture Handbook TASK-000 | Architect | This sprint doc |

---

## Carried over

| Item | Reason |
|------|--------|
| Permission Phase 2 (catalog + junction) | Scoped to Phase 2 — too large for sprint |
| Stripe integration | Not started — manual billing continues |
| `/ban`, `/timeout` commands | Out of scope for permission task |
| CI/CD pipeline | Documented in backlog |
| Production migration apply | Waiting deploy window |

---

## Sprint metrics (informal)

| Metric | Value |
|--------|-------|
| EF migrations added | 2 (MonthlyPrice, UnifyGuildPermissions) |
| API controllers | 12 |
| Dashboard feature pages | 15+ |
| Handbook docs created | 40+ files |
| Test coverage | 0% (no test projects) |

---

## Retrospective notes

**What went well**
- Permission unification reduced duplicate code paths
- Step guides provided implementation history for handbook extraction

**What to improve**
- Apply migrations to staging before merge
- Add CI before next sprint
- File retroactive ADR for permission unification

---

## Next sprint focus (proposed)

1. Deploy unified permissions to production + verify
2. Permission Phase 2 planning ADR
3. Bot permission caching spike
4. CI pipeline (build + lint)

See `backlog.md` Critical and High items.

---

## Related docs

- `milestones.md`, `backlog.md`
- `/docs/progress/2026-07-02-task-000-architecture-handbook.md`
