# DiscordBot.Domain

Pure C# models with **no dependencies** on EF Core, Discord, or ASP.NET.

- `Entities/` — database models (User, Guild, GuildSettings, etc.)
- `Enums/` — shared enums (LogType, ModerationAction, etc.)

Why separate? Domain stays stable while API, database, and Discord code change around it.
