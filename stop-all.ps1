# ==============================================================================
# Elementum - Stop All Services (PowerShell)
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$rootDir = $PSScriptRoot
$dockerComposeFile = Join-Path $rootDir "docker\docker-compose.yml"

Write-Host "Stopping all Elementum dotnet processes..." -ForegroundColor Yellow
Get-Process -Name "Elementum.Api", "Elementum.Worker", "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*Elementum*" } | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
docker compose -f $dockerComposeFile down

Write-Host "All Elementum services stopped." -ForegroundColor Green
