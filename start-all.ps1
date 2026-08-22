# ==============================================================================
# Elementum - All-in-One Startup Script (PowerShell)
# Starts: MySQL & Redis (Docker) -> Database Auto-Migration -> API -> Worker -> CLI
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  Starting Elementum Services (Full Stack)            " -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan

$rootDir = $PSScriptRoot
$dockerComposeFile = Join-Path $rootDir "docker\docker-compose.yml"

# 1. Check Docker Daemon
Write-Host "`n[1/5] Checking Docker status..." -ForegroundColor Yellow
try {
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running."
    }
    Write-Host "  -> Docker is running." -ForegroundColor Green
} catch {
    Write-Host "  [!] Docker Desktop is not running or not installed." -ForegroundColor Red
    Write-Host "      Please start Docker Desktop and run this script again." -ForegroundColor Yellow
    exit 1
}

# 2. Start MySQL & Redis Containers
Write-Host "`n[2/5] Starting MySQL & Redis containers via Docker Compose..." -ForegroundColor Yellow
docker compose -f $dockerComposeFile up -d --remove-orphans mysql redis
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [!] Failed to start Docker services." -ForegroundColor Red
    exit 1
}
Write-Host "  -> Containers started." -ForegroundColor Green

# 3. Wait for MySQL to become ready
Write-Host "`n[3/5] Waiting for MySQL database (Port 3307) to become ready..." -ForegroundColor Yellow
$maxRetries = 30
$retryCount = 0
$dbReady = $false

while ($retryCount -lt $maxRetries) {
    $retryCount++
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $asyncResult = $tcpClient.BeginConnect("127.0.0.1", 3307, $null, $null)
        $success = $asyncResult.AsyncWaitHandle.WaitOne(1000, $false)
        if ($success -and $tcpClient.Connected) {
            $tcpClient.EndConnect($asyncResult)
            $tcpClient.Close()
            $dbReady = $true
            break
        }
        $tcpClient.Close()
    } catch { }
    Start-Sleep -Seconds 1
    Write-Host -NoNewline "."
}

if (-not $dbReady) {
    Write-Host "`n  [!] MySQL did not become reachable on port 3307 in time." -ForegroundColor Red
    exit 1
}
Write-Host "`n  -> Database connection established." -ForegroundColor Green

# 4. Start Elementum.Api and Elementum.Worker
Write-Host "`n[4/5] Launching Elementum.Api and Elementum.Worker..." -ForegroundColor Yellow

$apiDir = Join-Path $rootDir "src\Elementum.Api"
$workerDir = Join-Path $rootDir "src\Elementum.Worker"
$cliDir = Join-Path $rootDir "src\Elementum.Cli"

# Start API in separate process window
$apiProcess = Start-Process dotnet -ArgumentList "run --project `"$apiDir`"" -WorkingDirectory $apiDir -PassThru
Write-Host "  -> Elementum.Api starting (PID: $($apiProcess.Id))..." -ForegroundColor Green

# Start Worker in separate process window
$workerProcess = Start-Process dotnet -ArgumentList "run --project `"$workerDir`"" -WorkingDirectory $workerDir -PassThru
Write-Host "  -> Elementum.Worker starting (PID: $($workerProcess.Id))..." -ForegroundColor Green

# Wait for API to become reachable on port 5093
Write-Host "`nWaiting for API (http://localhost:5093) to complete startup..." -ForegroundColor Yellow
$apiReady = $false
$apiRetries = 0
while ($apiRetries -lt 30) {
    $apiRetries++
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $asyncResult = $tcpClient.BeginConnect("127.0.0.1", 5093, $null, $null)
        $success = $asyncResult.AsyncWaitHandle.WaitOne(1000, $false)
        if ($success -and $tcpClient.Connected) {
            $tcpClient.EndConnect($asyncResult)
            $tcpClient.Close()
            $apiReady = $true
            break
        }
        $tcpClient.Close()
    } catch { }
    Start-Sleep -Seconds 1
    Write-Host -NoNewline "."
}

if ($apiReady) {
    Write-Host "`n  -> API is online and listening on http://localhost:5093" -ForegroundColor Green
} else {
    Write-Host "`n  [!] Note: API startup taking longer than expected. Check the API console window for logs." -ForegroundColor Yellow
}

# 5. Start Elementum.Cli
Write-Host "`n[5/5] Launching Elementum.Cli Terminal Dashboard..." -ForegroundColor Yellow
$cliProcess = Start-Process dotnet -ArgumentList "run --project `"$cliDir`"" -WorkingDirectory $cliDir -PassThru
Write-Host "  -> Elementum.Cli started (PID: $($cliProcess.Id))" -ForegroundColor Green

Write-Host "`n======================================================" -ForegroundColor Cyan
Write-Host "  All services are running!                           " -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  * CLI Terminal:       Running in separate window" -ForegroundColor White
Write-Host "  * API Base URL:       http://localhost:5093" -ForegroundColor White
Write-Host "  * API Live Prices:    http://localhost:5093/api/v1/prices/live" -ForegroundColor White
Write-Host "  * API Health:         http://localhost:5093/health/live" -ForegroundColor White
Write-Host "  * Worker Health:      http://localhost:5094/health" -ForegroundColor White
Write-Host "  * MySQL Database:     localhost:3307 (DB: Elementum-Database)" -ForegroundColor White
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "`nAuto-migrations & master data seeding are executed automatically on startup." -ForegroundColor Gray
Write-Host "`nClosing the CLI window or pressing 'Q' in this window will stop all services.`n" -ForegroundColor Yellow

while ($true) {
    # Check if CLI window was closed
    if ($cliProcess.HasExited) {
        Write-Host "`nCLI was closed. Stopping backend services..." -ForegroundColor Yellow
        try { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
        try { Stop-Process -Id $workerProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
        Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
        docker compose -f $dockerComposeFile stop mysql redis
        Write-Host "All services stopped." -ForegroundColor Green
        break
    }

    try {
        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true)
            if ($key.Key -eq [ConsoleKey]::Q) {
                Write-Host "`nStopping services..." -ForegroundColor Yellow
                try { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
                try { Stop-Process -Id $workerProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
                try { Stop-Process -Id $cliProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
                Write-Host "Stopping Docker containers..." -ForegroundColor Yellow
                docker compose -f $dockerComposeFile stop mysql redis
                Write-Host "All services stopped." -ForegroundColor Green
                break
            }
        }
    } catch {
        # Fallback for non-interactive host
    }
    Start-Sleep -Milliseconds 500
}
