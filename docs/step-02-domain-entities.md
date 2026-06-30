# Step 2 — Domain entities

## Why start with Domain?

Before Discord or SQL Server, we define **what data exists**. Entities are plain C# classes — no attributes required (EF config lives in Infrastructure).

## Entities

| Entity | Purpose |
|--------|---------|
| `User` | Someone who logged in with Discord OAuth |
| `Guild` | A Discord server where the bot is installed |
| `GuildSettings` | Welcome message, auto-role, log channel — edited from dashboard |
| `LogEntry` | Moderation actions and events (kick, ban, member joined, etc.) |
| `BaseEntity` | Shared `Id`, `CreatedAt`, `UpdatedAt` |

## Important Discord detail

Discord IDs are **64-bit snowflakes**. JavaScript loses precision on large integers, so we store them as **strings** everywhere (`DiscordGuildId`, `DiscordUserId`, channel/role IDs).

## Guild = tenant boundary

In v1, each Discord server is isolated by `GuildId`. Settings and logs always belong to one guild — we never mix server data.

## Placeholders in messages

Welcome text supports `{user}` and `{server}` — the bot replaces these when a member joins.

Next step: **EF Core** maps these entities to SQL Server tables.
