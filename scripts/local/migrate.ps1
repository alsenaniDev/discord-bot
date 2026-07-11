$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot/../.."
Set-Location $root

dotnet tool restore

dotnet tool run dotnet-ef database update `
  --project src/DiscordBot.Infrastructure `
  --startup-project src/DiscordBot.Api

dotnet tool run dotnet-ef database update `
  --project src/DiscordBot.Activities.Infrastructure `
  --startup-project src/DiscordBot.Activities.Api

Write-Host "Local Platform and Activities databases are migrated."
