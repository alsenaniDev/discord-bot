#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

dotnet tool restore

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api

dotnet tool run dotnet-ef database update \
  --project src/DiscordBot.Activities.Infrastructure \
  --startup-project src/DiscordBot.Activities.Api

echo "Local Platform and Activities databases are migrated."
