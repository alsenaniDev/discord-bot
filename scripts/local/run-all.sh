#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

docker compose -f docker-compose.local.yml up -d postgres
./scripts/local/migrate.sh

mkdir -p .local/logs

./scripts/local/run-platform-api.sh > .local/logs/platform-api.log 2>&1 &
echo $! > .local/platform-api.pid

./scripts/local/run-activities-api.sh > .local/logs/activities-api.log 2>&1 &
echo $! > .local/activities-api.pid

./scripts/local/run-bot.sh > .local/logs/bot.log 2>&1 &
echo $! > .local/bot.pid

./scripts/local/run-activity.sh > .local/logs/activity.log 2>&1 &
echo $! > .local/activity.pid

echo "Local services are starting."
echo "Logs: .local/logs"
echo "Activity: http://localhost:5173"
echo "Activity Player A: http://localhost:5173/?localProfile=PlayerA"
echo "Activity Player B: http://localhost:5173/?localProfile=PlayerB"
echo "Platform API: https://localhost:5001"
echo "Activities API: https://localhost:7001"
