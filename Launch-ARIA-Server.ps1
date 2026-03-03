$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "AutoPC\AutoPC\AutoPC.csproj"
$pidFile = Join-Path $repoRoot "serverpid.txt"

Write-Host "[ARIA] Stopping existing server processes..." -ForegroundColor Cyan
Get-Process AutoPC -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path $pidFile) {
    $existingPid = (Get-Content $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    if ($existingPid -and ($existingPid -as [int])) {
        Stop-Process -Id ([int]$existingPid) -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[ARIA] Starting server (Release) on LAN-friendly URLs..." -ForegroundColor Green
$arguments = @(
    "run",
    "--project", "`"$projectPath`"",
    "--configuration", "Release",
    "--no-launch-profile",
    "--urls", "https://0.0.0.0:7091;http://0.0.0.0:5180"
)

$process = Start-Process -FilePath "dotnet" -ArgumentList $arguments -WorkingDirectory $repoRoot -PassThru
$process.Id | Set-Content -Path $pidFile -Encoding ascii

Write-Host "[ARIA] Server started. PID: $($process.Id)" -ForegroundColor Green
Write-Host "[ARIA] Local browser URL: https://localhost:7091" -ForegroundColor Yellow
Write-Host "[ARIA] LAN URL format: https://<your-computer-ip>:7091" -ForegroundColor Yellow
Write-Host "[ARIA] Health endpoint: https://localhost:7091/api/test" -ForegroundColor Yellow
