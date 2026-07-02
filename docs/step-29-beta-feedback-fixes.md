# Step 29 — Beta Feedback Fixes

Tickets archive, panel images, server profile, Discord log delivery, role-based moderation, log display names, staff vs moderation separation, and i18n.

---

## Database migration

**Migration:** `20260701231527_BetaFeedbackFixes`

| Table / entity | Changes |
|----------------|---------|
| `GuildSettings` | `TicketArchiveChannelId`, `CommandPanelImageUrl` |
| `Guilds` | `DisplayName`, `Description`, `CommunityType`, `SupportMessage`, `RulesUrl`, `WebsiteUrl` |
| `LogEntries` | `ActorUsername`, `TargetUsername`, `RoleDiscordId`, `RoleName`, `ChannelName` |
| `ModerationPermissionRoles` | **New table** — role-based Discord moderation command permissions |
| `LogEventType` | `TicketArchived = 6` |

Apply locally:

```bash
dotnet ef database update --project src/DiscordBot.Infrastructure --startup-project src/DiscordBot.Api
```

Production (Railway):

```bash
railway run --service discord-bot-api ./deploy/railway/migrate.sh
```

---

## API endpoints added/changed

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/guilds/{id}/profile` | Bot-managed server profile |
| PUT | `/api/guilds/{id}/profile` | Update profile (owner / manager) |
| GET | `/api/guilds/{id}/moderation/permission-roles` | List moderation role permissions |
| POST | `/api/guilds/{id}/moderation/permission-roles` | Create moderation role permissions |
| PUT | `/api/guilds/{id}/moderation/permission-roles/{roleId}` | Update |
| DELETE | `/api/guilds/{id}/moderation/permission-roles/{roleId}` | Delete |
| GET | `/api/bot/guilds/{discordGuildId}/profile` | Bot reads profile for `/server` |
| POST | `/api/bot/guilds/{discordGuildId}/dashboard-access/evaluate` | Bot checks dashboard staff access (tickets) |

**Changed:** `PUT /api/guilds/{id}/settings` — includes `ticketArchiveChannelId`, `commandPanelImageUrl`.

**Changed:** `POST /api/bot/guilds/{id}/permissions/evaluate` — now uses `ModerationPermissionRoles` (not dashboard staff roles).

---

## Permission model (important)

| Layer | What it controls | Where configured |
|-------|------------------|------------------|
| **Dashboard staff** (`GuildPermissionRole`) | Dashboard pages: tickets, logs, moderation views, settings | **Staff** page |
| **Moderation roles** (`ModerationPermissionRole`) | Discord slash commands: `/warn`, `/kick`, `/clear`, `/warnings` | **Moderation → Settings** |
| **Bot Discord permissions** | Whether Discord allows the action (Manage Messages, Kick Members) | Discord role/channel permissions |

Guild owner and platform admin always have full access.

---

## Bot behavior changed

| Area | Behavior |
|------|----------|
| **Ticket archive** | On close (Discord or dashboard cleanup), if `TicketArchiveChannelId` is set, posts transcript embed to archive channel + logs `TicketArchived`. Failures are logged, never block close. |
| **Command panel** | Optional `CommandPanelImageUrl` shown as embed image. Invalid URLs are skipped with a warning. |
| **`/server`** | Uses guild profile + settings embed (`BuildServerProfile`). |
| **Discord logs** | After bot-originated API log write, `DiscordLogDeliveryService` posts embed to `LogChannelId` when Logs module is enabled. Dashboard/API-only events (e.g. settings updated) stay dashboard-only. |
| **Moderation commands** | Permission checks use `ModerationPermissionRoles` via API evaluate endpoint. |
| **Ticket close** | Owner, Discord admin, or dashboard staff with **AccessTickets** can close (not native Manage Guild only). |

---

## Dashboard pages changed

| Page | Changes |
|------|---------|
| **Settings → Tickets** | Ticket archive / transcript channel dropdown |
| **Settings → Logs** | Updated help text |
| **Settings → Button panel** | Image URL field |
| **Server profile** (new) | `/guilds/:id/profile` |
| **Moderation settings** (new) | `/guilds/:id/moderation/settings` |
| **Staff** | Dashboard-only permissions + help text |
| **Logs** | Names with muted IDs |

Full **en** / **ar** translations for new labels and help text.

---

## How to test

### A. Ticket archive channel
1. Settings → Tickets → set **Ticket archive channel**.
2. Open a ticket, send messages, close it.
3. Verify archive embed in archive channel.
4. Verify ticket still visible in dashboard.

### B. Action panel image
1. Settings → Button panel → set **Image URL** (https).
2. Save → wait for panel refresh.
3. Confirm image appears in Discord embed.

### C. Server info
1. Open **Server profile**, edit fields, save.
2. Run `/server` in Discord — updated profile should appear.

### D. Logs channel
1. Enable Logs module + set log channel in Settings.
2. Trigger welcome, ticket, warning, reaction role, etc.
3. Confirm embeds appear in Discord log channel.

### E. Role-based moderation
1. Moderation → Settings → assign **Moderator** role `CanWarn` + `CanClearMessages`.
2. Test user with that role: `/warn` works, `/kick` blocked.
3. Add `CanKick`, verify `/kick` works.
4. Remove role, commands blocked.

### F. Logs names
1. Trigger warning / kick / reaction role.
2. Dashboard logs and Discord log embeds show **name + ID**.

### G. i18n
1. Switch EN ↔ AR on new pages.
2. Verify RTL layout and translated help text.

---

## Limitations

- **Ticket transcript** is a short preview from recent Discord messages; full history remains in the dashboard.
- **API/dashboard-originated logs** (e.g. settings updated from dashboard) are **not** pushed to Discord — only bot-originated events listed in `DiscordLogDeliveryService`.
- **Ban / timeout** moderation types are not implemented (UI labels are future-ready).
- **`GuildStaff`** (legacy user-based table) remains unused; staff is role-based via `GuildPermissionRole`.
- **Server profile** does not rename the Discord server — it only affects bot embeds and dashboard.

---

## Key files changed

**Domain:** `GuildSettings.cs`, `Guild.cs`, `LogEntry.cs`, `ModerationPermissionRole.cs`, `LogEventType.cs`

**Infrastructure:** services (`GuildProfileService`, `ModerationPermissionRoleService`, `ModerationPermissionResolver`), DTOs, EF configurations, migration `20260701231527_BetaFeedbackFixes`

**API:** `GuildsController.cs`, `BotGuildsController.cs`

**Bot:** `TicketArchiveService.cs`, `DiscordLogDeliveryService.cs`, `BotLogWriter.cs`, ticket/moderation/slash handlers, `EmbedBuilderService.cs`, `CommandPanelSyncService.cs`

**Dashboard:** settings, profile, moderation-settings, staff, logs, i18n (`en.json`, `ar.json`), routing
