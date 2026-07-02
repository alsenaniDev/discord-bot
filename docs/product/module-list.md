# Module List

Official module catalog as implemented in code.

**Source:** `ModuleKeys.cs`, `ModuleSeeder.cs`

| Key | Name | Plans (default seed) | Bot | Dashboard | Status |
|-----|------|----------------------|-----|-----------|--------|
| `welcome` | Welcome | Free+ | Join message | Settings | ✅ Live |
| `logs` | Logs | Free+ | Channel delivery | Logs page | ✅ Live |
| `reaction-roles` | Reaction Roles | Basic+ | `/reaction-role create`, buttons | Reaction roles page | ✅ Live |
| `tickets` | Tickets | Pro+ | `/ticket`, panels | Tickets page | ✅ Live (beta) |
| `moderation` | Moderation | Pro+ | `/warn`, `/kick`, `/clear`, `/warnings` | Moderation pages | ⚠️ Partial |
| `auto-role` | Auto Role | Premium (`*`) | Join handler | Settings | ✅ Live |

**Note:** Premium plan uses `"*"` (all modules) — includes auto-role. Pro plan includes tickets + moderation but **not** auto-role per current seeder.

## Module dependencies

| Module | Requires |
|--------|----------|
| All | Guild registered, bot in server |
| tickets | Resource sync (channels, categories) |
| moderation | Permission roles configured for staff |
| logs | Log channel configured in settings |
| reaction-roles | Bot ManageRoles permission |
| welcome | Welcome channel in settings |
| auto-role | Valid role ID in settings |

## Enabling a module

1. Plan must allow module (`SubscriptionService`)
2. Owner toggles on Modules page (`GuildModule.IsEnabled`)
3. Bot checks via `ModuleGuard` at runtime

## Adding a module (developer checklist)

See `/docs/architecture/module-system.md`.

## Related docs

- `pricing.md`
- `/docs/architecture/module-system.md`
