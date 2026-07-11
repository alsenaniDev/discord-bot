#!/usr/bin/env bash
set -euo pipefail

PLATFORM_API_URL="${PLATFORM_API_URL:-}"
ACTIVITIES_API_URL="${ACTIVITIES_API_URL:-}"
DIAGNOSTICS_TOKEN="${ACTIVITIES_DIAGNOSTICS_TOKEN:-}"

if [ -z "$PLATFORM_API_URL" ] || [ -z "$ACTIVITIES_API_URL" ]; then
  echo "Set PLATFORM_API_URL and ACTIVITIES_API_URL before running smoke tests." >&2
  exit 2
fi

curl -fsS "$PLATFORM_API_URL/health"
curl -fsS "$ACTIVITIES_API_URL/health"
curl -fsS "$ACTIVITIES_API_URL/health/live"
curl -fsS "$ACTIVITIES_API_URL/health/ready"

if [ -n "$DIAGNOSTICS_TOKEN" ]; then
  echo "Diagnostics token provided; add protected diagnostics endpoint checks here when enabled."
fi

echo "Production smoke tests passed."
