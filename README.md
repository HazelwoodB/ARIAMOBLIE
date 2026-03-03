# AutoPC

- Release/update workflow: see [RELEASE_UPDATE_CHECKLIST.md](RELEASE_UPDATE_CHECKLIST.md)

## Server launch (desktop + LAN)

- Start server: `./Launch-ARIA-Server.ps1`
- Stop server: `./Stop-ARIA-Server.ps1`
- One-click start + system checks: `./One-Click-ARIA.bat`

The start script:

- Stops stale `AutoPC` processes that can lock build outputs.
- Starts the server in Release mode on `https://0.0.0.0:7091` and `http://0.0.0.0:5180`.
- Writes PID to `serverpid.txt` for clean shutdown.

## Client verification

- Browser client build: `dotnet build AutoPC/AutoPC.Client/AutoPC.Client.csproj -c Debug`
- Android client build: `dotnet build AutoPC/AutoPC.Mobile/AutoPC.Mobile.csproj -f net10.0-android -c Release`

## One-click workflow

Double-click `One-Click-ARIA.bat` to:

- Stop stale server processes.
- Start ARIA server on LAN-friendly URLs.
- Validate: `/api/test`, `/api/ollama/health`, `/api/mobile/update`, and `/chat`.
- Open `https://localhost:7091/chat` only when checks pass.
- Write results to `aria-health-summary.txt`.