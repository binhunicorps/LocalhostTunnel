# Localhost Tunnel

Windows desktop application for tunnel + forwarder operations (`WPF + .NET 8`).

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
powershell -ExecutionPolicy Bypass -File scripts/publish-desktop.ps1
```

Expected output:

- `artifacts/publish/LocalhostTunnel.Desktop.exe`
- `artifacts/publish/LocalhostTunnel.Updater.exe`
- `LocalhostTunnel.Desktop.exe` (launcher copy at repository root)

If `iscc` is available, installer output is generated under:

- `artifacts/installer`

### Desktop architecture (high level)

- `LocalhostTunnel.Core`: shared domain models and runtime snapshots
- `LocalhostTunnel.Application`: interfaces and orchestration services
- `LocalhostTunnel.Infrastructure`: forwarder, cloudflared supervision, storage, updates
- `LocalhostTunnel.Desktop`: WPF shell, view models, navigation, screens
- `LocalhostTunnel.Updater`: external updater executable

## License

ISC
