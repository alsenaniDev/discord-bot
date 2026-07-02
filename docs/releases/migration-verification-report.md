# Migration Verification Report — Release 0.1

**Date:** 2026-07-02  
**Reviewer:** R-002 Beta Launch Execution  
**Database:** PostgreSQL via EF Core 9 / Npgsql

---

## Summary

| Check | Result |
|-------|--------|
| Migration count matches snapshot | ✅ 18 migrations |
| Chronological ordering | ✅ Valid |
| Duplicate migration IDs | ✅ None |
| Model snapshot present | ✅ `AppDbContextModelSnapshot.cs` |
| Breaking migration documented | ✅ `UnifyGuildPermissions` |
| Local database applied | ✅ All 18 (EF Core — see below) |
| Latest migration | ✅ `20260702195029_AddTicketTimelineEvents` |
| Production database | ⚠️ **Verify before deploy** — run `migrate.sh` on Railway |
| `psql` manual SQL verification | ⏭️ **Skipped** — `psql` not installed locally; EF tooling used instead |

---

## EF Core Verification (R-002 — 2026-07-02)

Performed without `psql`:

```bash
dotnet build DiscordBot.sln
dotnet ef migrations list \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

| Step | Result |
|------|--------|
| `dotnet ef migrations list` | ✅ 18 migrations listed; **none marked `(Pending)`** |
| Latest migration in list | ✅ `20260702195029_AddTicketTimelineEvents` |
| `dotnet ef database update` | ✅ Completed (`Done.` — no pending migrations) |
| Build before EF commands | ✅ `DiscordBot.sln` 0 errors |

**Note:** SQL queries against `__EFMigrationsHistory` and `information_schema` were **not run** because `psql` is unavailable on the release engineer machine. EF Core list/update is sufficient for local sign-off. Production should use `migrate.sh` on Railway (or `psql` if available on ops workstation).

---

## Migration Inventory (apply in this order)

| # | Migration ID | Purpose |
|---|--------------|---------|
| 1 | `20260630154720_InitialCreate` | Base schema |
| 2 | `20260630160616_RenameDiscordResources` | Discord resource tables |
| 3 | `20260630163114_AddModeration` | Moderation cases, warnings |
| 4 | `20260630164742_AddModules` | Module catalog |
| 5 | `20260630165829_UpdateLogEntries` | Log entry improvements |
| 6 | `20260630170333_AddReactionRoles` | Reaction role panels |
| 7 | `20260630171155_AddSubscriptionPlans` | Plans + subscriptions |
| 8 | `20260630212001_AddPlatformAdmins` | Platform admin table |
| 9 | `20260630230729_AddUpgradeRequestsAndGuildStaff` | Upgrade requests + legacy GuildStaff |
| 10 | `20260630231054_AddSubscriptionDuration` | Subscription duration |
| 11 | `20260701120000_AddCommandPanelAndTicketCleanup` | Command panels + cleanup flag |
| 12 | `20260701134452_AddDiscordGuildMembers` | Member role sync |
| 13 | `20260701141022_AddGuildPermissionRolesAndMemberRoleIds` | Permission roles |
| 14 | `20260701150442_AddTicketMessagesAndAutoReplies` | Outbound messages + auto-replies |
| 15 | `20260701231527_BetaFeedbackFixes` | Beta feedback schema fixes |
| 16 | `20260701235500_AddSubscriptionPlanMonthlyPrice` | Plan pricing field |
| 17 | `20260702151245_UnifyGuildPermissions` | **Breaking:** merge moderation roles; drop legacy tables |
| 18 | `20260702195029_AddTicketTimelineEvents` | **Required for CM-002+:** Timeline + delivery flags |

---

## Critical Dependencies

### `20260702151245_UnifyGuildPermissions`

- Merges `ModerationPermissionRoles` into `GuildPermissionRoles`
- Drops `GuildStaff` and `ModerationPermissionRoles`
- **Must run before** API code that expects unified permissions model
- See `docs/project-management/release-notes.md` for upgrade notes

### `20260702195029_AddTicketTimelineEvents`

- Creates `TicketTimelineEvents` table
- Adds columns to `TicketOutboundMessages` (`DeliveryFailed`, `DeliveryFailureReason`, `StaffReplyQueuedTimelineEventId`)
- **Required for:** ticket conversation, transcript, archive digest, message capture
- Runtime error if missing: `42P01: relation "TicketTimelineEvents" does not exist`

---

## Verification Commands

### List migrations (local)

```bash
cd /path/to/repo
dotnet ef migrations list \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

When all applied, no `(Pending)` suffix appears on any migration.

### Apply migrations (local)

```bash
./deploy/railway/migrate.sh
```

Or:

```bash
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

### Apply migrations (production via Railway)

```bash
railway run --service discord-bot-api ./deploy/railway/migrate.sh
```

Or set `ConnectionStrings__DefaultConnection` and run `migrate.sh`.

### Verify applied migrations (SQL — optional)

**Skipped in R-002** if `psql` is not installed. Use EF commands above for local verification.

If `psql` is available (recommended on production ops):

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

Expected: 18 rows ending with `20260702195029_AddTicketTimelineEvents`.

### Verify `TicketTimelineEvents` exists (SQL — optional)

**Skipped in R-002** without `psql`. Locally, `dotnet ef database update` succeeding after CM-002 migration implies table creation.

If `psql` is available:

```sql
SELECT EXISTS (
  SELECT FROM information_schema.tables
  WHERE table_name = 'TicketTimelineEvents'
);
```

---

## DbContext Snapshot Check

- File: `src/DiscordBot.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- Contains `TicketTimelineEvents` entity mapping ✅
- Does **not** contain `GuildStaff` or `ModerationPermissionRoles` ✅ (post-unification)

---

## Production Upgrade Path

1. **Backup** Railway Postgres (Railway dashboard → Postgres → Backups, or manual dump)
2. **Apply migrations** using `migrate.sh` **before or with** API deploy that includes CM-002–004
3. **Verify** health endpoint: `GET /api/health` → `"database": "connected"`
4. **Smoke test** ticket endpoints after deploy (see `release-0.1-checklist.md`)

### Rollback

EF Core migrations are **forward-only** in this project. Rollback strategy:

- Restore Postgres backup
- Redeploy previous API/bot image tag

Do not run `dotnet ef database update <PreviousMigration>` on production without explicit DBA review.

---

## Issues Found

| Issue | Severity | Action |
|-------|----------|--------|
| Migrations are manual | High | Document in checklist; add CI migration list step ✅ |
| Production may lag codebase | High | Deploy + migrate before beta customers |
| No automated migration test in CI beyond list | Medium | Phase 2: ephemeral Postgres in CI |

---

## Sign-off

| Environment | Migrations current | Verified by |
|-------------|-------------------|-------------|
| Local dev (`discordbot`) | ✅ All 18 | R-002 EF: `migrations list` + `database update` |
| Production (Railway) | ⚠️ Health OK; run `migrate.sh` + redeploy CM routes | **Required before Go** |

*No schema redesign performed in R-002.*
