# Release Notes

## Unreleased

### Documentation
- Architecture Handbook (TASK-000) — full `/docs/architecture/` structure
- Permission scalability architecture review
- Project management docs (backlog, technical debt, milestones)
- **O-001:** First-Time User Activation blueprint (`/docs/ux/first-time-user-activation.md`)
- **O-002:** Dashboard Overview experience — health score, activation progress, recommendations, quick actions, recent activity

### Backend
- Unified permission system (`GuildPermissionRoles` only)
- Migration `20260702151245_UnifyGuildPermissions`
- Removed `/api/guilds/{id}/staff` and `/api/guilds/{id}/moderation/permission-roles`
- Clear all logs endpoint (`DELETE /api/guilds/{id}/logs`)
- Admin subscription plan CRUD + MonthlyPrice
- **O-002:** `GuildOverviewExperienceService` on `GET /guilds/{id}/overview` — health, activation, recommendations, activity
- **SB-003:** Owner subscription change flow — payment reference, stepper, renew CTA
- **SB-004:** Admin subscription change review — filters, payment reference column, approve/reject dialogs, override reason, EN/AR labels

### Dashboard
- Expanded staff permission keys (20 flags)
- Moderation settings uses unified permission-roles API
- Vercel cache headers for i18n JSON
- Admin plans page
- **SB-003:** Owner subscription page — stepper, payment reference form, renew CTA, fixed change history table (EN/AR)
- **SB-004:** Admin subscription changes queue — filters, payment reference, approve/reject dialogs, override reason (EN/AR)
- **O-002:** Guild Overview operational dashboard — health, activation, recommendations, quick actions, activity (EN/AR)

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
