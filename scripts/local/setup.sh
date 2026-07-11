#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

echo "Restoring .NET tools and packages..."
dotnet tool restore
dotnet restore

echo "Installing React Activity dependencies..."
npm install --prefix activity/DiscordBot.Activity

echo "Installing Angular Dashboard dependencies..."
npm install --prefix dashboard/DiscordBot.Dashboard

echo "Starting local PostgreSQL..."
docker compose -f docker-compose.local.yml up -d postgres

echo "Waiting for PostgreSQL..."
until docker exec discordbot-local-postgres pg_isready -U postgres -d discordbot_platform >/dev/null 2>&1; do
  sleep 1
done

"$ROOT_DIR/scripts/local/migrate.sh"

cat <<'EOF'

Local setup is ready.

Next configure required secrets with dotnet user-secrets:
  - src/DiscordBot.Api: Discord:ClientSecret, Discord:BotToken, Bot:ApiKey, ActivitiesIntegration:ServiceToken, Jwt:Secret
  - src/DiscordBot.Activities.Api: Discord:ClientSecret, PlatformApi:ServiceToken, ActivitiesDiagnostics:ServiceToken, Jwt:SigningKey
  - src/DiscordBot.Bot: Discord:Token, Api:ApiKey

See docs/local-development.md for exact commands.
EOF
