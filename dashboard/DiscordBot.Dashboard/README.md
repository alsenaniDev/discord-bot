# Dashboard (Angular)

Pages we will add:

- `features/auth/` — Discord login
- `features/servers/` — pick a Discord server
- `features/settings/` — welcome, auto-role, log channel
- `features/logs/` — moderation and event history
- `core/` — auth service, API client, route guards

The dashboard never talks to Discord directly for bot actions — it calls the .NET API.
