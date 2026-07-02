# Changelog

All notable changes to the Discord Bot Platform documentation and product.

Format based on [Keep a Changelog](https://keepachangelog.com/).

---

## [Unreleased]

### Added
- Complete Architecture Handbook under `/docs/architecture/`
- Product docs under `/docs/product/`
- Project management docs under `/docs/project-management/`
- ADR process documentation under `/docs/adr/`
- Permission scalability review (`docs/architecture/2026-07-02-permissions-scalability-review.md`)
- Unified permissions progress report

### Changed
- Permission model unified to `GuildPermissionRoles` + `GuildPermissions` enum
- Bot permission evaluate uses single `GuildPermissionResolver`
- Dashboard moderation settings reads/writes `/permission-roles`

### Removed
- `GuildStaff` entity and API
- `ModerationPermissionRoles` entity and API

### Fixed
- Moderation settings route (`moderation/settings` segments)
- Overview page module status alignment with modules page
- Clear logs DELETE method (requires API restart locally)

---

## [Beta] — 2026-06 / 2026-07

### Added
- 6 feature modules with subscription gating
- Platform admin panel
- Plan upgrade request workflow
- Discord resource sync (channels, roles, members)
- Guild permission roles (initial)
- Auto-replies, ticket outbound messages, command panels
- EN/AR dashboard i18n
- Railway deployment configuration

See step guides `step-09` through `step-30` for detailed history.

---

## Related docs

- `release-notes.md`
- `/docs/progress/`
