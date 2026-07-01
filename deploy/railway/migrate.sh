#!/usr/bin/env bash
# Apply EF Core migrations to PostgreSQL (local or production).
#
# Local (uses appsettings.Development.json):
#   ./deploy/railway/migrate.sh
#
# Production via Railway (uses linked service env vars):
#   railway run --service discord-bot-api ./deploy/railway/migrate.sh
#
# Production via connection string (bypasses local appsettings):
#   export ConnectionStrings__DefaultConnection='Host=...;Port=...;Database=railway;...;SSL Mode=Require;Trust Server Certificate=true'
#   ./deploy/railway/migrate.sh

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

CONNECTION="${ConnectionStrings__DefaultConnection:-${MIGRATION_CONNECTION:-}}"

echo "Applying EF Core migrations..."

if [ -n "$CONNECTION" ]; then
  echo "Using connection from environment (not local appsettings)."
  dotnet ef database update \
    --project src/DiscordBot.Infrastructure \
    --startup-project src/DiscordBot.Api \
    --connection "$CONNECTION"
else
  dotnet ef database update \
    --project src/DiscordBot.Infrastructure \
    --startup-project src/DiscordBot.Api
fi

echo "Migrations applied."
