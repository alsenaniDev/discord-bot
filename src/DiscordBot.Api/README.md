# DiscordBot.Api

HTTP entry point for the dashboard and health checks.

- `Controllers/` — REST endpoints (auth, guilds, settings, logs)
- `Extensions/` — DI registration helpers
- `appsettings.json` — connection strings and Discord OAuth config

Why Web API? The Angular dashboard and Discord bot both need a shared backend that owns business rules and PostgreSQL data.
