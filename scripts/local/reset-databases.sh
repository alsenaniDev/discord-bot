#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

docker compose -f docker-compose.local.yml down -v
docker compose -f docker-compose.local.yml up -d postgres
./scripts/local/migrate.sh

echo "Local PostgreSQL databases were reset and migrated."
