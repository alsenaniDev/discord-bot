# Release 0.1 — Closed Beta

**Release name:** 0.1 (Closed Beta Foundation)  
**Date:** 2026-07-02  
**Phase:** Product Blueprint Phase 1  
**Audience:** First coached beta customers (5–15 guilds)

---

## Overview

Release 0.1 is the first customer-facing beta of the Discord Bot Platform. It delivers multi-tenant guild management, a Discord OAuth dashboard (EN/AR), six configurable modules, manual subscription administration, unified role-based permissions, and a ticket system with Timeline-backed conversation and transcript.

This is **not** a commercial launch. Self-serve billing, full moderation parity, and enterprise operations are out of scope.

---

## New Features

### Platform & onboarding

- Discord OAuth login with JWT dashboard sessions
- Guild registration via `/setup` and bot join
- Onboarding checklist (invite, sync, plan, modules, welcome, tickets)
- Resource sync (channels, roles, members) via background worker
- Platform admin: guilds, users, plans, upgrade requests

### Modules (six)

| Module | Capability |
|--------|------------|
| Welcome | Join messages with placeholders |
| Auto-role | Role on member join |
| Logs | Event log in dashboard + optional Discord channel |
| Tickets | Create, close, dashboard reply, archive digest, transcript |
| Moderation | Warn, kick, warnings/cases in dashboard |
| Reaction roles | Button-based role panels |

### Tickets (CM-002 – CM-004)

- **Timeline** — append-only event store (`TicketTimelineEvents`)
- **Message capture** — Discord text messages recorded as Timeline events
- **Read models** — paginated ticket summaries and conversation projection
- **Transcript** — full durable record in dashboard (`/guilds/:id/tickets/:ticketId/transcript`)
- **Archive honesty** — Discord archive channel receives digest only, with link to dashboard transcript
- **Delivery tracking** — staff reply queued / delivered / failed states

### Dashboard

- Server overview, settings, modules, subscription, staff permissions
- Tickets list with filters, pagination, inline conversation, transcript page
- Moderation warnings and cases
- Logs with filters and clear
- English and Arabic (RTL-ready i18n)

### Bot

- Slash commands: `/setup`, `/ticket`, `/warn`, `/kick`, `/clear`, `/warnings`, reaction roles, panel buttons
- Background workers: resource sync, ticket cleanup, outbound messages, command panel sync
- Module and plan guards on all feature commands

---

## Architecture Improvements

- **Unified permissions** — single `GuildPermissionRoles` model; legacy `GuildStaff` and `ModerationPermissionRoles` removed
- **Read model architecture (AR-001)** — ticket summaries, conversation, and transcript as query projections over Timeline
- **Separation of Archive vs Transcript (BR-X01)** — honest Discord digest + dashboard truth
- **Production config validation** — startup rejects placeholders and non-HTTPS URLs in Production
- **CI build workflow** — GitHub Actions builds API and dashboard on push/PR

---

## Breaking Changes

### Database

| Migration | Impact |
|-----------|--------|
| `20260702151245_UnifyGuildPermissions` | Drops `GuildStaff` and `ModerationPermissionRoles`; merges moderation role data into `GuildPermissionRoles` |
| `20260702195029_AddTicketTimelineEvents` | Requires new table; ticket features fail if not applied |

**Action:** Run `./deploy/railway/migrate.sh` on production before deploying API code that depends on these migrations.

### API

- Ticket list endpoint returns **paginated read model** (`GET /api/guilds/{id}/tickets?page=&pageSize=&status=`)
- Legacy non-paginated ticket list removed from service layer
- Dashboard should use `/conversation` or `/transcript`, not raw `/timeline` (legacy endpoint retained for compatibility)

### Permissions

- Moderation settings UI writes to unified `GuildPermissionRoles` (same table as Staff page for bot moderation keys)

---

## Known Limitations

See **`beta-known-limitations.md`** for the full customer-facing list.

Summary:

- Manual subscription upgrades (no Stripe)
- No `/ban` or `/timeout` commands
- Ticket staff roles do not receive Discord channel access (dashboard-only unless Admin/Manage Server)
- Dashboard replies may take up to ~30 seconds
- Transcript complete only for Timeline-recorded events (post CM-002)
- No attachments in transcript
- No automated tests in CI (build only)
- Dashboard initial bundle ~683 KB (550 KB budget warning — performance debt, not a functional blocker)

---

## Required Manual Steps Before Beta

1. Deploy API + bot + dashboard from current repository
2. Run `./deploy/railway/migrate.sh` on production Postgres
3. Verify conversation/transcript routes return `401` (not `404`) when unauthenticated
4. Complete manual smoke checklist Phase D
5. Send this document + `beta-known-limitations.md` to pilot customers
6. Configure external uptime monitoring on `/api/health`

See **`release-0.1-checklist.md`** for step-by-step instructions.

## Upgrade Notes

### From pre-0.1 deployments

1. Backup Postgres
2. Apply all migrations through `20260702195029_AddTicketTimelineEvents`
3. Deploy API, bot, and dashboard together
4. Restart API after deploy (new routes)
5. Verify `Discord__DashboardUrl`, `Platform__DashboardUrl`, and dashboard `apiUrl` align
6. Run smoke checklist in `release-0.1-checklist.md`

### From local dev missing Timeline

If you see `42P01: relation "TicketTimelineEvents" does not exist`:

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

---

## Beta Scope

**In scope for 0.1 beta customers:**

- Small Discord communities (≤ few thousand members)
- Owner-led setup with team coaching
- Support workflows primarily via **dashboard** for staff without Discord admin
- Manual plan assignment by platform admin

**Out of scope:**

- Public signup / marketing site
- Self-serve payment
- SLA or uptime guarantees
- HTML/PDF/email transcript export
- Ticket assign/claim, internal notes, categories, SLA, AI summary
- Automod, ban/timeout, appeals
- Multi-region / horizontal API scaling

---

## Documentation

| Doc | Purpose |
|-----|---------|
| `release-0.1-checklist.md` | Deploy steps |
| `migration-verification-report.md` | Database upgrade |
| `beta-known-limitations.md` | Customer expectations |
| `docs/beta-tester-guide.md` | Tester walkthrough |
| `docs/step-24-beta-readiness.md` | Extended ops reference |

---

## Contributors & references

Built on PB-001, UL-001, D-001, AR-001, CM-001–004, unified permissions merge.

Readiness review: **R-001** (APPROVED WITH REQUIRED FIXES)  
Launch execution: **R-002** (complete — **GO WITH LIMITATIONS — PENDING REDEPLOY**)

> **Release 0.1 is not deploy-ready yet.** Closed beta approved only after redeploy verification passes.

---

## R-002 Release Validation

| Check | Result |
|-------|--------|
| Production API health | ✅ Passed |
| Production database connection | ✅ Passed (via `/api/health`) |
| Local EF migrations | ✅ 18 applied; latest `AddTicketTimelineEvents` |
| .NET build | ✅ Passed |
| Dashboard build | ✅ Passed (bundle budget warning — not a blocker) |
| Production CM-003/004 routes | ❌ **404** — production deploy missing CM-003/CM-004 |
| Manual E2E smoke | ⚠️ Pending operator (after redeploy) |

**Release decision:** **GO WITH LIMITATIONS — PENDING REDEPLOY**

**Release 0.1 is not deploy-ready yet.** Re-test after redeploy:

- `GET /api/guilds/{id}/tickets/{ticketId}/conversation` → expect **401/403**, not **404**
- `GET /api/guilds/{id}/tickets/{ticketId}/transcript` → expect **401/403**, not **404**

See `release-0.1-checklist.md` and `docs/progress/2026-07-02-R-002-beta-launch-execution.md`.
