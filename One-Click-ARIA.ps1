$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "AutoPC\AutoPC\AutoPC.csproj"
$pidFile = Join-Path $repoRoot "serverpid.txt"
$healthSummary = Join-Path $repoRoot "aria-health-summary.txt"

function Write-Step($message) {
    Write-Host "[ARIA] $message" -ForegroundColor Cyan
}

function Test-Endpoint {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Url,
        [int] $TimeoutSec = 8
    )

    try {
        $result = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec $TimeoutSec
        return [PSCustomObject]@{ Name = $Name; Url = $Url; Ok = $true; Detail = ($result | ConvertTo-Json -Depth 6 -Compress) }
    }
    catch {
        return [PSCustomObject]@{ Name = $Name; Url = $Url; Ok = $false; Detail = $_.Exception.Message }
    }
}

Write-Step "Stopping stale ARIA processes..."
Get-Process AutoPC -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

if (Test-Path $pidFile) {
    $existingPid = (Get-Content $pidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    if ($existingPid -and ($existingPid -as [int])) {
        Stop-Process -Id ([int]$existingPid) -Force -ErrorAction SilentlyContinue
    }
}

Write-Step "Starting server in Release mode..."
$arguments = @(
    "run",
    "--project", "`"$projectPath`"",
    "--configuration", "Release",
    "--no-launch-profile",
    "--urls", "https://0.0.0.0:7091;http://0.0.0.0:5180"
)

$process = Start-Process -FilePath "dotnet" -ArgumentList $arguments -WorkingDirectory $repoRoot -PassThru
$process.Id | Set-Content -Path $pidFile -Encoding ascii
Write-Step "Server PID: $($process.Id)"

Write-Step "Waiting for server readiness..."
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 1
    try {
        $null = Invoke-RestMethod -Uri "http://localhost:5180/api/test" -TimeoutSec 3
        $ready = $true
        break
    }
    catch {
    }
}

if (-not $ready) {
    $message = "Server did not become ready on http://localhost:5180/api/test"
    Write-Host "[ARIA] $message" -ForegroundColor Red
    "ARIA One-Click FAILED`n$message`n$(Get-Date -Format o)" | Set-Content -Path $healthSummary -Encoding utf8
    exit 1
}

Write-Step "Running system checks..."
$checks = @(
    (Test-Endpoint -Name "Server API" -Url "http://localhost:5180/api/test"),
    (Test-Endpoint -Name "Model Health" -Url "http://localhost:5180/api/ollama/health"),
    (Test-Endpoint -Name "Update Manifest" -Url "http://localhost:5180/api/mobile/update")
)

try {
    $chatPage = Invoke-WebRequest -Uri "http://localhost:5180/chat" -UseBasicParsing -TimeoutSec 8
    $checks += [PSCustomObject]@{ Name = "Browser Route"; Url = "http://localhost:5180/chat"; Ok = ($chatPage.StatusCode -eq 200); Detail = "HTTP $($chatPage.StatusCode)" }
}
catch {
    $checks += [PSCustomObject]@{ Name = "Browser Route"; Url = "http://localhost:5180/chat"; Ok = $false; Detail = $_.Exception.Message }
}

$allOk = $checks | Where-Object { -not $_.Ok } | Measure-Object | Select-Object -ExpandProperty Count
$timestamp = Get-Date -Format o

$summaryLines = @()
$summaryLines += "ARIA One-Click Health Summary"
$summaryLines += "Timestamp: $timestamp"
$summaryLines += "PID: $($process.Id)"
$summaryLines += ""

foreach ($check in $checks) {
    $status = if ($check.Ok) { "PASS" } else { "FAIL" }
    $summaryLines += "[$status] $($check.Name) -> $($check.Url)"
    $summaryLines += "        $($check.Detail)"
}

$summaryLines | Set-Content -Path $healthSummary -Encoding utf8

if ($allOk -eq 0) {
    Write-Host "[ARIA] All systems PASS. Opening browser..." -ForegroundColor Green
    Start-Process "https://localhost:7091/chat"
    Write-Host "[ARIA] Summary file: $healthSummary" -ForegroundColor Yellow
    exit 0
}
else {
    Write-Host "[ARIA] Some checks FAILED. See summary: $healthSummary" -ForegroundColor Red
    exit 2
}
