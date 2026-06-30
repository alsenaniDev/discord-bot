# Step 3 — EF Core + PostgreSQL

## Why EF Core?

Entity Framework Core translates C# entities into SQL tables and handles queries. You write C#; EF generates SQL.

## What we added

- `AppDbContext` — gateway to the database (`DbSet<User>`, etc.)
- `Configurations/` — table names, indexes, max lengths (keeps Domain clean)
- `DependencyInjection.cs` — registers DbContext with the connection string
- `docker-compose.yml` — local PostgreSQL without installing it on your Mac

## Connection string

In `appsettings.json`:

```
Host=localhost;Port=5432;Database=discordbot;Username=postgres;Password=postgres
```

## Run PostgreSQL locally

```bash
docker compose up -d
```

Wait until the container is healthy (~5 seconds).

## Create and apply migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api \
  --output-dir Migrations

dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api
```

## Provider

The project uses **Npgsql** (`Npgsql.EntityFrameworkCore.PostgreSQL`) with `UseNpgsql(...)` in `DependencyInjection.cs`.

Next step: **Discord OAuth + JWT** to authenticate dashboard users.
