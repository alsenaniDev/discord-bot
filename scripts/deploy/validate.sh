#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

dotnet tool restore
dotnet tool run dotnet-ef --version
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --logger "console;verbosity=minimal"

dotnet tool run dotnet-ef migrations list \
  --project src/DiscordBot.Infrastructure \
  --startup-project src/DiscordBot.Api \
  --configuration Release

dotnet tool run dotnet-ef migrations list \
  --project src/DiscordBot.Activities.Infrastructure \
  --startup-project src/DiscordBot.Activities.Api \
  --configuration Release

npm ci --prefix activity/DiscordBot.Activity
npm test --prefix activity/DiscordBot.Activity
npm run build --prefix activity/DiscordBot.Activity

npm ci --prefix dashboard/DiscordBot.Dashboard
npm run build --prefix dashboard/DiscordBot.Dashboard

"$ROOT_DIR/scripts/deploy/build-images.sh"
