#!/usr/bin/env bash
# Apply Activities EF Core migrations. Run from an SDK environment, CI job, or dedicated migration service.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

echo "Applying Activities EF Core migrations..."
dotnet tool restore

if [ -z "${ConnectionStrings__ActivitiesDatabase:-}" ] && [ -n "${MIGRATION_CONNECTION:-}" ]; then
  export ConnectionStrings__ActivitiesDatabase="$MIGRATION_CONNECTION"
fi

if [ -n "${ConnectionStrings__ActivitiesDatabase:-}" ]; then
  echo "Using Activities connection string from environment."
fi

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Activities.Infrastructure \
  --startup-project src/DiscordBot.Activities.Api \
  --configuration Release

echo "Activities migrations applied."
