# PostgreSQL migration (SQL Server → PostgreSQL)

Development reset: old SQL Server migrations were removed and replaced with a single PostgreSQL `InitialCreate` migration.

## What changed

| Area | Change |
|------|--------|
| EF provider | `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` |
| DI | `UseSqlServer(...)` → `UseNpgsql(...)` |
| Docker | `mcr.microsoft.com/mssql/server` → `postgres:16-alpine` |
| Port | 1433 → 5432 |
| Migrations | Fresh `InitialCreate` for PostgreSQL |

## Connection string

```
Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres
```

## Reset local database

```bash
docker compose down -v
docker compose up -d
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

## Recreate migrations (if you change entities)

```bash
# Remove Migrations folder contents first if starting fresh, then:
dotnet ef migrations add InitialCreate \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

No SQL Server-specific column types remain in entity configurations — GUIDs, string enums, and `DateTimeOffset` map cleanly to PostgreSQL via Npgsql.
