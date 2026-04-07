# Localhost Tunnel

Windows desktop application for tunnel + forwarder operations (`WPF + .NET 8`).

## Download (End Users)

- Open latest release: [https://github.com/binhunicorps/LocalhostTunnel/releases/latest](https://github.com/binhunicorps/LocalhostTunnel/releases/latest)
- Recommended: download `LocalhostTunnel-Setup-win-x64-v*.exe` and run installer
- Portable mode: download `LocalhostTunnel-Portable-win-x64-v*.zip`, extract, run `LocalhostTunnel.Desktop.exe`

## Desktop (Windows GUI)

### Prerequisites

- .NET SDK 8
- Windows 10/11 (x64)
- Optional: Inno Setup Compiler (`iscc`) for installer generation

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

If `iscc` is available, installer output is generated under:

- `artifacts/installer/LocalhostTunnel-Setup-win-x64-v<version>.exe`
- `artifacts/release/LocalhostTunnel-Setup-win-x64-v<version>.exe`

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

## License

ISC
