#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

docker build -f deploy/railway/Dockerfile.api -t discordbot-api:local .
docker build -f deploy/railway/Dockerfile.activities-api -t discordbot-activities-api:local .
docker build -f deploy/railway/Dockerfile.bot -t discordbot-bot:local .

echo "Production Docker images built locally."
