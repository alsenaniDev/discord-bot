#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

dotnet test tests/DiscordBot.Activities.IntegrationTests/DiscordBot.Activities.IntegrationTests.csproj --no-restore --filter "RouletteRuntimeFlowTests|RouletteAuthorizationTests" -v:minimal
npm test --prefix activity/DiscordBot.Activity
env VITE_DISCORD_CLIENT_ID="${VITE_DISCORD_CLIENT_ID:-1521505440003919922}" \
  VITE_API_BASE_URL= \
  VITE_PLATFORM_API_BASE_URL= \
  VITE_ACTIVITIES_API_BASE_URL=/activities-api \
  VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS="${VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS:-1521518056852029440}" \
  VITE_ENVIRONMENT=production \
  npm run build --prefix activity/DiscordBot.Activity

echo "Roulette local verification passed."
