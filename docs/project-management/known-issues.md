# Known Issues

Active issues as of 2026-07-02 (updated R-002). Verify against latest code before treating as current.

---

## Production / deployment

| Issue | Workaround | Status |
|-------|------------|--------|
| Wrong Vercel domain shows stale/different app | Use correct project URL (`discord-bot-eight-gamma.vercel.app` per audit) | Documented |
| API must restart after rebuild for new routes | Restart API service after deploy | Operational |
| Production code may lag repo (CM-003/004 routes) | Deploy API + run migrations per `release-0.1-checklist.md` | **Action required** (R-002) |
| `TicketTimelineEvents` missing if migrations skipped | Run `./deploy/railway/migrate.sh` | Documented in migration report |

---

## Product / behavior

| Issue | Detail | Status |
|-------|--------|--------|
| `/ban` and `/timeout` not implemented | Flags exist in permission enum | By design (scope) |
| Owner-only effective settings guard | ManageSettings flag not wired to guards | Known gap |
| Moderation page access grants broad access | Mapper cross-grants tickets/logs | Known gap |
| No automated subscription renewal | Manual admin extend | By design (beta) |

---

## Dashboard

| Issue | Detail | Status |
|-------|--------|--------|
| Staff page 20 checkboxes | Usable but will not scale to 100+ permissions | Phase 2 UX |
| Bundle size warning on build | 657 KB vs 550 KB budget | Low priority |

---

## Bot

| Issue | Detail | Status |
|-------|--------|--------|
| Permission evaluate on every command | No cache | Phase 2 |
| Single bot instance | No sharding | Scale limit |

---

## Documentation

| Issue | Detail | Status |
|-------|--------|--------|
| Step guides may supersede handbook sections | Prefer handbook for architecture | Resolved by TASK-000 |
| No ADRs filed yet | Recommended retroactive ADR-0001 | Backlog H-11 |

---

## Reporting new issues

1. Add to this file with date and severity
2. Add backlog item in `backlog.md`
3. Fix → move to `changelog.md` and remove from here

## Related docs

- `technical-debt.md`, `backlog.md`
- `docs/step-29-beta-feedback-fixes.md`
