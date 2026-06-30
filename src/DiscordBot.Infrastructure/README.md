# DiscordBot.Infrastructure

External integrations and persistence.

- `Data/` — EF Core DbContext, migrations, repositories
- `Auth/` — Discord OAuth HTTP client, JWT generation
- `Services/` — Auth orchestration (login flow, user upsert)
- `Discord/` — Discord.Net bot client (Step 6)
- `Options/` — Typed configuration (Discord, JWT)

Why here? PostgreSQL and Discord.Net are *implementation details*. The API only talks to interfaces.
