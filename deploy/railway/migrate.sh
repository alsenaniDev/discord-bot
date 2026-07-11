#!/usr/bin/env bash
# Apply Platform EF Core migrations to PostgreSQL (local or production).
#
# Local (uses appsettings.Development.json):
#   ./deploy/railway/migrate.sh
#
# Production via Railway (uses linked service env vars):
#   railway run --service discord-bot-api ./deploy/railway/migrate-platform.sh
#
# Production via connection string (bypasses local appsettings):
#   export ConnectionStrings__DefaultConnection='Host=...;Port=...;Database=railway;...;SSL Mode=Require;Trust Server Certificate=true'
#   ./deploy/railway/migrate.sh

set -euo pipefail

"$(cd "$(dirname "$0")" && pwd)/migrate-platform.sh"
