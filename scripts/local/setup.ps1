$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot/../.."
Set-Location $root

dotnet tool restore
dotnet restore
npm install --prefix activity/DiscordBot.Activity
npm install --prefix dashboard/DiscordBot.Dashboard

docker compose -f docker-compose.local.yml up -d postgres

Write-Host "Waiting for PostgreSQL..."
do {
  docker exec discordbot-local-postgres pg_isready -U postgres -d discordbot_platform | Out-Null
  if ($LASTEXITCODE -ne 0) { Start-Sleep -Seconds 1 }
} while ($LASTEXITCODE -ne 0)

& "$PSScriptRoot/migrate.ps1"

Write-Host ""
Write-Host "Local setup is ready. See docs/local-development.md for required user-secrets."
