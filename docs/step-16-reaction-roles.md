# Step 16 — Reaction Roles MVP

Button-based role panels: one message, one role, one toggle button.

## Bot command

`/reaction-role create`

| Option | Description |
|--------|-------------|
| `channel` | Where to post the panel |
| `title` | Embed title |
| `description` | Embed body |
| `role` | Role to toggle |
| `button_label` | Button text (max 80 chars) |

**Permissions:** caller needs **Manage Roles** or **Manage Server**; bot needs **Manage Roles** and a role above the target role.

**Behavior:**
1. Posts an embed + button in the channel.
2. Saves the panel to the API.
3. On button click: assign role if missing, remove if present.
4. Ephemeral success/error embed for the clicker.

## Database

**`ReactionRoles`** — one row per panel:

- `GuildId`, `ChannelDiscordId`, `MessageDiscordId`, `RoleDiscordId`
- `ButtonCustomId` (unique, format `reaction-role:toggle:{guid}`)
- `Title`, `Description`, `ButtonLabel`, `IsActive`

## API

Bot (`X-Bot-Api-Key`):

- `POST /api/bot/reaction-roles`
- `GET /api/bot/reaction-roles/by-button/{customId}`

Dashboard (JWT):

- `GET /api/guilds/{id}/reaction-roles`
- `DELETE /api/guilds/{id}/reaction-roles/{reactionRoleId}` (deactivates)

## Module

Key: `reaction-roles` — seeded as **Reaction Roles**.

When disabled: `/reaction-role create` and button clicks show the standard module-disabled embed.

## Logs

| Type | When |
|------|------|
| `ReactionRoleCreated` | Panel saved after `/reaction-role create` |
| `ReactionRoleAssigned` | Member gets the role via button |
| `ReactionRoleRemoved` | Member loses the role via button |
| `ReactionRoleDeleted` | Panel deactivated from dashboard |

Non-critical — require **Logs** module enabled (same as other routine events).

## Dashboard

Route: `/guilds/:id/reaction-roles`

Lists panels with title, channel, role, status, created date. **Deactivate** sets `IsActive = false` (button clicks then show inactive message).

Panels are created in Discord only (no dashboard create form in MVP).

## Test end-to-end

1. Apply migration, restart API and bot.
2. Open **Modules** → ensure **Reaction Roles** and **Logs** are enabled.
3. Sync Discord data (`/sync` or dashboard) so role/channel names resolve in the dashboard.
4. In Discord (as admin with Manage Roles):
   ```
   /reaction-role create channel:#general title:"Get Member" description:"Click to toggle the Member role." role:@Member button_label:"Toggle Member"
   ```
5. Confirm embed + button appear in the channel.
6. Click the button as a test user → role toggles, ephemeral confirmation appears.
7. Dashboard **Reaction Roles** → panel listed as Active.
8. Dashboard **Logs** → `Reaction role created`, then assigned/removed entries.
9. Dashboard **Deactivate** → click button again → "Panel inactive" message.
10. Disable **Reaction Roles** module → `/reaction-role create` and button show module-disabled embed.
