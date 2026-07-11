#!/usr/bin/env bash
# Apply Platform EF Core migrations. Run from an SDK environment, CI job, or dedicated migration service.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Applying Platform EF Core migrations..."
dotnet tool restore

if [ -z "${ConnectionStrings__DefaultConnection:-}" ] && [ -n "${MIGRATION_CONNECTION:-}" ]; then
  export ConnectionStrings__DefaultConnection="$MIGRATION_CONNECTION"
fi

if [ -n "${ConnectionStrings__DefaultConnection:-}" ]; then
  echo "Using Platform connection string from environment."
fi

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api \
  --configuration Release

echo "Platform migrations applied."
