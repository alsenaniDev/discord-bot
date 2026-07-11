#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR/activity/DiscordBot.Activity"

if [ ! -f .env.local ]; then
  cp .env.example .env.local
  echo "Created activity/DiscordBot.Activity/.env.local from .env.example. Fill VITE_DISCORD_CLIENT_ID before testing inside Discord."
fi

npm run dev
