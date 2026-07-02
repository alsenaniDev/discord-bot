# Release Notes

## Unreleased

### Documentation
- Architecture Handbook (TASK-000) — full `/docs/architecture/` structure
- Permission scalability architecture review
- Project management docs (backlog, technical debt, milestones)

### Backend
- Unified permission system (`GuildPermissionRoles` only)
- Migration `20260702151245_UnifyGuildPermissions`
- Removed `/api/guilds/{id}/staff` and `/api/guilds/{id}/moderation/permission-roles`
- Clear all logs endpoint (`DELETE /api/guilds/{id}/logs`)
- Admin subscription plan CRUD + MonthlyPrice

### Dashboard
- Expanded staff permission keys (20 flags)
- Moderation settings uses unified permission-roles API
- Vercel cache headers for i18n JSON
- Admin plans page

---

## Beta releases (historical summary)

| Milestone | Highlights |
|-----------|------------|
| Initial platform | OAuth, guild API, bot setup/sync |
| Modules | Tickets, moderation, logs, reaction roles, welcome |
| Subscriptions | Plans, upgrade requests, admin approval |
| Platform admin | Guild/user management, stats |
| i18n | English + Arabic dashboard |
| Railway deploy | Docker production path |

Detailed chronology: `/docs/step-*.md` guides.

---

## Upgrade notes (unified permissions)

1. Run migration `20260702151245_UnifyGuildPermissions` **before** deploying new API
2. Redeploy dashboard (moderation settings API path changed)
3. Bot requires no config change (same evaluate endpoints)
4. External integrations using removed `/staff` or `/moderation/permission-roles` must migrate to `/permission-roles`

See `/docs/progress/2026-07-02-unified-permissions.md`.

---

## Related docs

- `changelog.md`
- `/docs/progress/`
