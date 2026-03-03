$ErrorActionPreference = "SilentlyContinue"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pidFile = Join-Path $repoRoot "serverpid.txt"

if (Test-Path $pidFile) {
    $pidValue = (Get-Content $pidFile | Select-Object -First 1)
    if ($pidValue -and ($pidValue -as [int])) {
        Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
    }
    Remove-Item $pidFile -Force -ErrorAction SilentlyContinue
}

Get-Process AutoPC -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "[ARIA] Server stopped." -ForegroundColor Cyan
