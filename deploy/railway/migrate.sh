#!/usr/bin/env bash
# Apply EF Core migrations to Railway PostgreSQL.
#
# Prerequisites:
#   - Railway CLI installed: https://docs.railway.app/develop/cli
#   - Linked to the API service: railway link
#   - ConnectionStrings__DefaultConnection available (link Postgres to API service)
#
# Usage (from repo root):
#   railway run --service YOUR_API_SERVICE ./deploy/railway/migrate.sh
#
# Or run locally with DATABASE_URL / connection string exported:
#   export ConnectionStrings__DefaultConnection="Host=...;Port=...;..."
#   ./deploy/railway/migrate.sh

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Applying EF Core migrations..."
dotnet ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

echo "Migrations applied."
