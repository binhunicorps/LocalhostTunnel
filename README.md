# Localhost Tunnel

Windows desktop application for multi-profile tunnel + forwarder operations (`WPF + .NET 8`) with built-in Tavily API proxy runtime.

## Download (End Users)

- Open latest release: [https://github.com/binhunicorps/LocalhostTunnel/releases/latest](https://github.com/binhunicorps/LocalhostTunnel/releases/latest)
- Download `LocalhostTunnel-Portable-win-x64-v*.zip`, extract, run `LocalhostTunnel.Desktop.exe`

## Desktop (Windows GUI)

### Prerequisites

- .NET SDK 8
- Windows 10/11 (x64)

### Build and test

```powershell
dotnet build LocalhostTunnel.sln -v minimal
dotnet test LocalhostTunnel.sln -v minimal
```

### Publish self-contained binaries

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-desktop.ps1 -Version 1.0.16
```

Expected output:

- `artifacts/publish/LocalhostTunnel.Desktop.exe`
- `artifacts/publish/LocalhostTunnel.Updater.exe`
- `LocalhostTunnel.Desktop.exe` (launcher copy at repository root)
- `artifacts/release/LocalhostTunnel-Portable-win-x64-v<version>.zip`

### GitHub release automation

- Push a tag to trigger full release pipeline:

```powershell
git tag v1.0.16
git push origin v1.0.16
```

- Workflow file: `.github/workflows/release-desktop.yml`
- The workflow builds, tests, packages, and publishes release assets automatically.

### Desktop architecture (high level)

- `LocalhostTunnel.Core`: shared domain models and runtime snapshots
- `LocalhostTunnel.Application`: interfaces and orchestration services
- `LocalhostTunnel.Infrastructure`: forwarder, cloudflared supervision, storage, updates
- `LocalhostTunnel.Desktop`: WPF shell, view models, navigation, screens
- `LocalhostTunnel.Updater`: external updater executable

## Runtime Profiles

- App now stores runtime profiles in `%LOCALAPPDATA%\LocalhostTunnel\config.profiles.json`
- You can create multiple profiles in `Configuration` tab (`Standard` and `Tavily API`)
- `Start Runtime` in Overview starts all enabled profiles
- `Stop Runtime` stops all running profiles
- Each profile can also be started/stopped directly from `Configuration`

## Tavily API (Integrated)

- `Tavily API` tab runs Tavily proxy directly inside desktop app
- Configure:
  - `Tunnel URL`
  - `Tunnel Token`
  - `Tavily Host/Port`
  - `Forwarder Port`
  - `Base URL`
  - `API Key 1/2`
- Use `Start Tavily` to run Tavily runtime for selected Tavily profile
- No external `TavilyProxyAPI` Python project is required

## License

ISC
