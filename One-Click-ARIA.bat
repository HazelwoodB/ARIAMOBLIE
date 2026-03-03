@echo off
setlocal

set SCRIPT_DIR=%~dp0
set PS_SCRIPT=%SCRIPT_DIR%One-Click-ARIA.ps1

if not exist "%PS_SCRIPT%" (
  echo [ARIA] Missing script: %PS_SCRIPT%
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
set EXITCODE=%ERRORLEVEL%

echo.
if %EXITCODE%==0 (
  echo [ARIA] One-click startup complete. All checks passed.
) else (
  echo [ARIA] One-click startup finished with issues. Exit code: %EXITCODE%
  echo [ARIA] Check aria-health-summary.txt for details.
)

echo.
pause
exit /b %EXITCODE%
