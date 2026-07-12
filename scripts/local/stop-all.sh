#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

for name in platform-api activities-api bot activity dashboard; do
  pid_file=".local/${name}.pid"
  if [ -f "$pid_file" ]; then
    pid="$(cat "$pid_file")"
    if kill -0 "$pid" >/dev/null 2>&1; then
      kill "$pid" || true
      echo "Stopped $name ($pid)."
    fi
    rm -f "$pid_file"
  fi
done

echo "Local app processes stopped. PostgreSQL is still running."
echo "Use 'docker compose -f docker-compose.local.yml down' to stop Docker services."
