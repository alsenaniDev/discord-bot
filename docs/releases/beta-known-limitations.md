# Beta Known Limitations — Release 0.1

**Audience:** Beta customers and support staff  
**Last updated:** 2026-07-02  
**Honesty policy:** We document what works, what is experimental, and what not to rely on yet.

---

## What Works Well (safe to use daily)

| Area | Status |
|------|--------|
| Discord login to dashboard | ✅ Stable |
| Bot invite + `/setup` | ✅ Stable |
| Module enable/disable with plan limits | ✅ Stable |
| Welcome messages + auto-role | ✅ Stable |
| Logs in dashboard (+ Discord channel if configured) | ✅ Stable |
| Ticket open/close in Discord | ✅ Stable |
| Ticket list, reply, close from dashboard | ✅ Stable |
| Ticket Timeline + transcript (after deploy) | ✅ Stable for text messages |
| Archive digest in Discord (not full history) | ✅ Stable |
| Warn / kick / warnings list | ✅ Stable |
| Reaction role panels | ✅ Stable |
| English and Arabic dashboard | ✅ Stable |
| Platform admin plan assignment | ✅ Stable |

---

## Experimental (use with coaching)

| Feature | Caveat |
|---------|--------|
| **Dashboard ticket replies** | Delivered by background worker; may take **up to ~30 seconds** |
| **Ticket transcript** | Only includes messages recorded on Timeline (after platform update); old tickets may be partial |
| **Archive embed link to transcript** | Requires correct dashboard URL in bot config; otherwise shows text fallback |
| **Staff permission roles** | Unified model recently merged; verify access after role changes |
| **Command panel buttons** | Syncs within ~30 seconds after settings save |
| **Member sync** | Role changes in Discord may lag until next sync |

---

## Do Not Rely On Yet (not production-grade)

| Limitation | Detail |
|------------|--------|
| **Self-serve billing** | Upgrades require manual admin approval; no Stripe |
| **Discord channel access for ticket staff** | Staff with dashboard ticket permissions **do not** automatically get access to ticket Discord channels unless they also have Discord Admin/Manage Server. Use the **dashboard** for ticket work. |
| **`/ban` and `/timeout`** | Not implemented; permission flags exist but commands do not |
| **Attachments in tickets/transcript** | Text only; images/files in Discord are not stored on Timeline |
| **Internal notes** | Not implemented |
| **Ticket assign/claim/reopen** | Not implemented |
| **Multi-category tickets** | Single category only |
| **Real-time dashboard updates** | Refresh manually; no WebSocket push |
| **Notifications bell** | UI placeholder; does not deliver notifications |
| **Log export / retention policies** | Dashboard capped at 200 entries; no CSV export |
| **Uptime SLA** | Best-effort beta; monitor health endpoint only |
| **Automated regression tests** | CI builds code but does not run test suite (none yet) |
| **Dashboard load time on slow networks** | Initial JS bundle ~683 KB (budget warning); acceptable for beta; optimization planned |

---

## Permission & Access Quirks

1. **404 on guild pages** often means “no access” — not necessarily missing data.
2. **Ticket-only staff** may see **Moderation** and **Logs** in navigation due to permission cross-grants; they may not have full moderation powers in Discord.
3. **Settings pages** are effectively **owner-only** for saving changes (`ManageSettings` not fully wired to staff flags).
4. **Bot close vs dashboard close** — Discord close archives immediately; dashboard close uses a worker (archive + delete within ~30s).

---

## Data & Privacy

- Message content for tickets is stored in **your platform database** (Timeline), not indefinitely in Discord after channel deletion.
- Log entries may contain message snippets and user IDs.
- No GDPR export/delete self-service in 0.1.

---

## Planned Improvements (post-0.1)

| Priority | Item |
|----------|------|
| P0 | Staff Discord channel access for ticket roles (CM-008) |
| P1 | CI + integration tests |
| P1 | Uptime monitoring + backup runbook |
| P2 | Granular dashboard navigation guards |
| P2 | `/ban`, `/timeout` |
| Phase 2 | Stripe billing, permission catalog scale, staging environment |
| Phase 3 | Ticket assign, internal notes, reopen, categories |

---

## Reporting Issues

1. Note **exact steps**, **server name**, **time (UTC)**, and **screenshots**
2. Distinguish **dashboard** vs **Discord** behavior
3. For ticket issues, note ticket **number** and whether channel still exists

Contact platform admin via the channel provided in your beta onboarding.

---

## Related

- `release-0.1.md` — full release notes  
- `docs/beta-tester-guide.md` — setup walkthrough  
- `docs/releases/release-0.1-readiness.md` — internal readiness review
