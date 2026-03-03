# ARIA Mobile Release Checklist

Use this for each new mobile update so chat commands `/update` and `/update install` work immediately.

## 1) Build and name the APK

- Build release APK from `AutoPC/AutoPC.Mobile`.
- Ensure the release asset file name is exactly what server config expects (currently `ARIA-v3.0.apk`).
- If you change the file name, also update `MobileUpdate:AssetName` in both:
  - `AutoPC/AutoPC/appsettings.json`
  - `AutoPC/AutoPC/appsettings.Development.json`

## 2) Create GitHub release in the correct repo

- Repo: `https://github.com/HazelwoodB/ARIAMOBLIE`
- Create or update tag (currently `v3.0.0`).
- Upload APK asset to that release.
- Verify the asset is publicly downloadable.

## 3) Update server manifest values

In both config files:

- `AutoPC/AutoPC/appsettings.json`
- `AutoPC/AutoPC/appsettings.Development.json`

Set under `MobileUpdate`:

- `LatestVersion`: visible version string (example `3.1`)
- `LatestBuild`: numeric build greater than previous (example `2`)
- `ReleaseTag`: GitHub tag (example `v3.1.0`)
- `AssetName`: exact APK filename
- `ReleaseNotes`: short user-facing notes
- `Mandatory`: `true` or `false`
- `GitHubOwner`: `HazelwoodB`
- `GitHubRepo`: `ARIAMOBLIE`

Notes:

- Keep `DownloadUrl` empty (`""`) if you want the server to auto-generate the GitHub asset URL.
- Set `DownloadUrl` directly only if you want to override auto-generation.

## 4) Restart server and validate endpoints

- Restart backend server after changing appsettings.
- Verify:
  - `GET /api/mobile/update`
  - `GET /api/ollama/health`

Expected result: `/api/mobile/update` returns correct version/build and a valid `downloadUrl`.

## 5) Validate from the app

In ARIA mobile chat:

- Run `/update` to confirm update detection.
- Run `/update install` and confirm it opens the APK download URL.
- If `Mandatory` is true, confirm chat is locked until update.

## 6) Optional quick terminal check

After server is running, test manifest quickly:

```powershell
Invoke-RestMethod "https://YOUR_SERVER_URL/api/mobile/update"
```

Confirm `latestBuild`, `latestVersion`, and `downloadUrl` match the release.
