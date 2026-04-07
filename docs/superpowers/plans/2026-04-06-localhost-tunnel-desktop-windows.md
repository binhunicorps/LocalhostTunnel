# Localhost Tunnel Desktop Windows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current browser-based Node dashboard with a native Windows WPF application that runs the tunnel and local forwarder from a packaged `.exe`.

**Architecture:** Build a new `desktop/` .NET 8 solution that ports the forwarder and tunnel runtime behavior into C# libraries, then host those services from a WPF shell using MVVM, dependency injection, and a tray-aware lifecycle. Keep the current JavaScript implementation as the behavior reference during migration and leave the existing Node files in place until the desktop path is verified end-to-end.

**Tech Stack:** .NET 8, WPF, Microsoft.Extensions.Hosting, CommunityToolkit.Mvvm, H.NotifyIcon.Wpf, xUnit, FluentAssertions, Inno Setup

---

## Preconditions

- This workspace is currently not a Git repository. If implementation happens in a cloned repository, create a feature branch or worktree first. If implementation happens from this ZIP-style workspace, replace commit steps with named checkpoints until Git is available.
- Run this plan on Windows because WPF, tray behavior, and `cloudflared.exe` packaging are Windows-specific.
- Install Inno Setup 6 and make sure `iscc` is available on `PATH` before Task 10.
- Use the approved spec at `docs/superpowers/specs/2026-04-06-localhost-tunnel-desktop-windows-design.md` as the source of truth.

## Behavior References

Read these files before implementing the matching desktop runtime pieces:

- `src/forwarder.js`
- `src/server.js`
- `src/auth.js`
- `server/config-store.js`
- `server/tunnel-manager.js`
- `server/updater.js`

## Target File Structure

Create the new desktop app under `desktop/` and keep the existing Node app untouched during migration.

```text
desktop/
  LocalhostTunnel.sln
  Directory.Build.props
  src/
    LocalhostTunnel.Core/
      Configuration/AppConfig.cs
      Configuration/SessionState.cs
      Configuration/AppConfigValidator.cs
      Logging/LogEntry.cs
      Runtime/ServiceState.cs
      Runtime/ForwarderSnapshot.cs
      Runtime/TunnelSnapshot.cs
      Runtime/RuntimeSnapshot.cs
      Updates/ReleaseInfo.cs
    LocalhostTunnel.Application/
      Interfaces/IConfigStore.cs
      Interfaces/ISessionStore.cs
      Interfaces/ILogStore.cs
      Interfaces/IForwarderHost.cs
      Interfaces/ITunnelHost.cs
      Interfaces/ICloudflaredInstaller.cs
      Interfaces/IUpdateService.cs
      Services/RuntimeCoordinator.cs
      Services/NavigationService.cs
      Services/StartupImportService.cs
      Services/UpdateCoordinator.cs
    LocalhostTunnel.Infrastructure/
      Storage/AppDataPaths.cs
      Storage/JsonConfigStore.cs
      Storage/JsonSessionStore.cs
      Storage/LegacyConfigImporter.cs
      Logging/ObservableLogStore.cs
      Logging/FileLogWriter.cs
      Forwarding/ForwarderHost.cs
      Forwarding/RequestForwarder.cs
      Forwarding/HeaderSanitizer.cs
      Forwarding/LocalUrlRewriter.cs
      Tunnel/CloudflaredInstaller.cs
      Tunnel/CloudflaredProcessHost.cs
      Tunnel/CloudflaredOutputParser.cs
      Updates/GitHubReleaseService.cs
      Updates/DesktopUpdaterLauncher.cs
    LocalhostTunnel.Desktop/
      App.xaml
      App.xaml.cs
      AppHost.cs
      Resources/Icons/App.ico
      Theme/Colors.xaml
      Theme/Typography.xaml
      Theme/Controls.xaml
      Views/MainWindow.xaml
      Views/OverviewView.xaml
      Views/ConfigurationView.xaml
      Views/LogsView.xaml
      Views/UpdatesView.xaml
      Views/DiagnosticsView.xaml
      Views/Dialogs/CloseBehaviorDialog.xaml
      ViewModels/MainWindowViewModel.cs
      ViewModels/OverviewViewModel.cs
      ViewModels/ConfigurationViewModel.cs
      ViewModels/LogsViewModel.cs
      ViewModels/UpdatesViewModel.cs
      ViewModels/DiagnosticsViewModel.cs
      ViewModels/Dialogs/CloseBehaviorDialogViewModel.cs
      Services/TrayIconService.cs
      Services/WindowLifecycleService.cs
    LocalhostTunnel.Updater/
      Program.cs
  tests/
    LocalhostTunnel.Core.Tests/
      Configuration/AppConfigValidatorTests.cs
      Runtime/RuntimeSnapshotTests.cs
    LocalhostTunnel.Infrastructure.Tests/
      Storage/JsonConfigStoreTests.cs
      Storage/LegacyConfigImporterTests.cs
      Logging/ObservableLogStoreTests.cs
      Forwarding/ForwarderHostTests.cs
      Tunnel/CloudflaredOutputParserTests.cs
      Tunnel/CloudflaredProcessHostTests.cs
      Updates/GitHubReleaseServiceTests.cs
    LocalhostTunnel.Desktop.Tests/
      ViewModels/OverviewViewModelTests.cs
      ViewModels/ConfigurationViewModelTests.cs
      ViewModels/LogsViewModelTests.cs
      ViewModels/Dialogs/CloseBehaviorDialogViewModelTests.cs
  packaging/
    LocalhostTunnel.iss
  scripts/
    publish-desktop.ps1
```

## Task 1: Bootstrap The .NET Solution

**Files:**
- Create: `desktop/LocalhostTunnel.sln`
- Create: `desktop/Directory.Build.props`
- Create: `desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj`
- Create: `desktop/src/LocalhostTunnel.Application/LocalhostTunnel.Application.csproj`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj`
- Create: `desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj`
- Create: `desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj`
- Create: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj`
- Create: `desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Storage/AppDataPaths.cs`
- Test: `desktop/tests/LocalhostTunnel.Core.Tests/Configuration/AppDataPathsTests.cs`

- [ ] **Step 1: Scaffold the solution, projects, references, and test packages**

```powershell
New-Item -ItemType Directory -Force -Path desktop/src, desktop/tests, desktop/packaging, desktop/scripts | Out-Null
dotnet new sln -n LocalhostTunnel -o desktop
dotnet new classlib -n LocalhostTunnel.Core -o desktop/src/LocalhostTunnel.Core
dotnet new classlib -n LocalhostTunnel.Application -o desktop/src/LocalhostTunnel.Application
dotnet new classlib -n LocalhostTunnel.Infrastructure -o desktop/src/LocalhostTunnel.Infrastructure
dotnet new wpf -n LocalhostTunnel.Desktop -o desktop/src/LocalhostTunnel.Desktop
dotnet new xunit -n LocalhostTunnel.Core.Tests -o desktop/tests/LocalhostTunnel.Core.Tests
dotnet new xunit -n LocalhostTunnel.Infrastructure.Tests -o desktop/tests/LocalhostTunnel.Infrastructure.Tests
dotnet new xunit -n LocalhostTunnel.Desktop.Tests -o desktop/tests/LocalhostTunnel.Desktop.Tests
dotnet sln desktop/LocalhostTunnel.sln add desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj desktop/src/LocalhostTunnel.Application/LocalhostTunnel.Application.csproj desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj
dotnet add desktop/src/LocalhostTunnel.Application/LocalhostTunnel.Application.csproj reference desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj
dotnet add desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj reference desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj desktop/src/LocalhostTunnel.Application/LocalhostTunnel.Application.csproj
dotnet add desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj reference desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj desktop/src/LocalhostTunnel.Application/LocalhostTunnel.Application.csproj desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj
dotnet add desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj reference desktop/src/LocalhostTunnel.Core/LocalhostTunnel.Core.csproj desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj
dotnet add desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj reference desktop/src/LocalhostTunnel.Infrastructure/LocalhostTunnel.Infrastructure.csproj
dotnet add desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj reference desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj
dotnet add desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj package CommunityToolkit.Mvvm
dotnet add desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj package H.NotifyIcon.Wpf
dotnet add desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj package Microsoft.Extensions.Hosting
dotnet add desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj package FluentAssertions
dotnet add desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj package FluentAssertions
dotnet add desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj package FluentAssertions
```

- [ ] **Step 2: Write the failing path test**

```csharp
public class AppDataPathsTests
{
    [Fact]
    public void Build_Uses_LocalAppData_And_LocalhostTunnel_Subfolder()
    {
        var paths = AppDataPaths.Build();

        paths.RootDirectory.Should().Contain("LocalhostTunnel");
        paths.ConfigFilePath.Should().EndWith(Path.Combine("LocalhostTunnel", "config.json"));
        paths.SessionFilePath.Should().EndWith(Path.Combine("LocalhostTunnel", "session.json"));
        paths.LogDirectory.Should().EndWith(Path.Combine("LocalhostTunnel", "logs"));
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~AppDataPathsTests -v minimal`  
Expected: FAIL because `AppDataPaths` does not exist yet

- [ ] **Step 4: Implement the minimal path model and shared build settings**

```csharp
public sealed record AppDataPaths(
    string RootDirectory,
    string ConfigFilePath,
    string SessionFilePath,
    string LogDirectory,
    string CloudflaredDirectory)
{
    public static AppDataPaths Build()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalhostTunnel");

        return new(
            root,
            Path.Combine(root, "config.json"),
            Path.Combine(root, "session.json"),
            Path.Combine(root, "logs"),
            Path.Combine(root, "cloudflared"));
    }
}
```

- [ ] **Step 5: Run the targeted tests and a full solution build**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~AppDataPathsTests -v minimal`  
Expected: PASS

Run: `dotnet build desktop/LocalhostTunnel.sln -v minimal`  
Expected: PASS with all projects restoring and compiling

- [ ] **Step 6: Commit**

```bash
git add desktop/LocalhostTunnel.sln desktop/Directory.Build.props desktop/src desktop/tests
git commit -m "chore: scaffold desktop solution"
```

## Task 2: Add Config Models, Validation, And JSON Persistence

**Files:**
- Create: `desktop/src/LocalhostTunnel.Core/Configuration/AppConfig.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Configuration/SessionState.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Configuration/AppConfigValidator.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/IConfigStore.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/ISessionStore.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Storage/JsonConfigStore.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Storage/JsonSessionStore.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Storage/LegacyConfigImporter.cs`
- Test: `desktop/tests/LocalhostTunnel.Core.Tests/Configuration/AppConfigValidatorTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Storage/JsonConfigStoreTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Storage/LegacyConfigImporterTests.cs`

- [ ] **Step 1: Write failing validator and importer tests**

```csharp
[Fact]
public void Validate_Rejects_Empty_TunnelToken_And_Invalid_TargetPort()
{
    var config = new AppConfig
    {
        TunnelUrl = "https://example.trycloudflare.com/",
        TunnelToken = "",
        TargetPort = 0
    };

    var result = AppConfigValidator.Validate(config);

    result.Errors.Should().ContainKey(nameof(AppConfig.TunnelToken));
    result.Errors.Should().ContainKey(nameof(AppConfig.TargetPort));
}
```

```csharp
[Fact]
public async Task ImportAsync_Prefers_Legacy_Project_Config_When_New_Config_Is_Missing()
{
    var importer = new LegacyConfigImporter(paths, legacyProjectRoot);

    var imported = await importer.ImportAsync(CancellationToken.None);

    imported.Should().BeTrue();
    File.Exists(paths.ConfigFilePath).Should().BeTrue();
}
```

- [ ] **Step 2: Run the relevant tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~AppConfigValidatorTests -v minimal`  
Expected: FAIL because `AppConfig` and `AppConfigValidator` are missing

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~LegacyConfigImporterTests -v minimal`  
Expected: FAIL because the storage classes and importer are missing

- [ ] **Step 3: Implement config records, validation, JSON stores, and legacy import**

```csharp
public sealed class AppConfig
{
    public string TunnelUrl { get; init; } = "";
    public string TunnelToken { get; init; } = "";
    public int TargetPort { get; init; } = 8765;
    public int Port { get; init; } = 8788;
    public string Host { get; init; } = "127.0.0.1";
    public string TargetHost { get; init; } = "127.0.0.1";
    public string TargetProtocol { get; init; } = "http";
    public string WebhookSecret { get; init; } = "";
    public int MaxBodySize { get; init; } = 10 * 1024 * 1024;
    public int UpstreamTimeout { get; init; } = 30000;
    public string LogLevel { get; init; } = "info";
    public string GitHubToken { get; init; } = "";
}
```

```csharp
public static class AppConfigValidator
{
    public static ValidationResult Validate(AppConfig config)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(config.TunnelUrl))
            errors[nameof(AppConfig.TunnelUrl)] = "Tunnel URL is required.";
        if (string.IsNullOrWhiteSpace(config.TunnelToken))
            errors[nameof(AppConfig.TunnelToken)] = "Tunnel token is required.";
        if (config.TargetPort is <= 0 or > 65535)
            errors[nameof(AppConfig.TargetPort)] = "Target port must be between 1 and 65535.";

        return new ValidationResult(errors);
    }
}
```

- [ ] **Step 4: Run the targeted storage and validation tests**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~AppConfigValidatorTests -v minimal`  
Expected: PASS

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter "FullyQualifiedName~JsonConfigStoreTests|FullyQualifiedName~LegacyConfigImporterTests" -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Core desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Infrastructure desktop/tests/LocalhostTunnel.Core.Tests desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: add desktop config persistence"
```

## Task 3: Build Runtime State And Log Storage

**Files:**
- Create: `desktop/src/LocalhostTunnel.Core/Logging/LogEntry.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Runtime/ServiceState.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Runtime/ForwarderSnapshot.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Runtime/TunnelSnapshot.cs`
- Create: `desktop/src/LocalhostTunnel.Core/Runtime/RuntimeSnapshot.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/ILogStore.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Logging/ObservableLogStore.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Logging/FileLogWriter.cs`
- Test: `desktop/tests/LocalhostTunnel.Core.Tests/Runtime/RuntimeSnapshotTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Logging/ObservableLogStoreTests.cs`

- [ ] **Step 1: Write failing tests for runtime snapshots and log retention**

```csharp
[Fact]
public void CreateRunning_Sets_Uptime_And_State()
{
    var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

    var snapshot = ForwarderSnapshot.CreateRunning(startedAt, 4321);

    snapshot.State.Should().Be(ServiceState.Running);
    snapshot.ProcessId.Should().Be(4321);
    snapshot.Uptime.Should().BeGreaterThan(TimeSpan.FromMinutes(4));
}
```

```csharp
[Fact]
public void Append_Trims_Buffer_To_Maximum_Count()
{
    var store = new ObservableLogStore(maxEntries: 3);

    store.Append(LogEntry.Info("ui", "1"));
    store.Append(LogEntry.Info("ui", "2"));
    store.Append(LogEntry.Info("ui", "3"));
    store.Append(LogEntry.Info("ui", "4"));

    store.Entries.Select(x => x.Message).Should().Equal("2", "3", "4");
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~RuntimeSnapshotTests -v minimal`  
Expected: FAIL because the snapshot models do not exist yet

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~ObservableLogStoreTests -v minimal`  
Expected: FAIL because the log store does not exist yet

- [ ] **Step 3: Implement snapshot records and log sinks**

```csharp
public enum ServiceState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Degraded,
    Faulted
}
```

```csharp
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message)
{
    public static LogEntry Info(string source, string message) =>
        new(DateTimeOffset.UtcNow, "info", source, message);
}
```

- [ ] **Step 4: Run the snapshot and logging tests**

Run: `dotnet test desktop/tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj --filter FullyQualifiedName~RuntimeSnapshotTests -v minimal`  
Expected: PASS

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~ObservableLogStoreTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Core desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Infrastructure desktop/tests/LocalhostTunnel.Core.Tests desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: add runtime state and log storage"
```

## Task 4: Port The HTTP Forwarder Runtime

**Files:**
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/IForwarderHost.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Forwarding/ForwarderHost.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Forwarding/RequestForwarder.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Forwarding/HeaderSanitizer.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Forwarding/LocalUrlRewriter.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Forwarding/ForwarderHostTests.cs`

- [ ] **Step 1: Write failing integration-style tests for auth, body limits, and upstream forwarding**

```csharp
[Fact]
public async Task HandleAsync_Returns_401_When_Webhook_Secret_Does_Not_Match()
{
    var fixture = await ForwarderHostFixture.StartAsync(webhookSecret: "abc123");

    using var request = new HttpRequestMessage(HttpMethod.Post, fixture.ForwarderUri);
    request.Headers.Add("x-webhook-secret", "wrong");
    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

    var response = await fixture.Client.SendAsync(request);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

```csharp
[Fact]
public async Task HandleAsync_Rewrites_Localhost_Urls_In_Text_Bodies()
{
    var fixture = await ForwarderHostFixture.StartAsync(
        upstreamBody: "visit http://127.0.0.1:8765/test",
        tunnelUrl: "https://public.example.com/");

    var response = await fixture.Client.GetStringAsync(fixture.ForwarderUri);

    response.Should().Contain("https://public.example.com/test");
}
```

- [ ] **Step 2: Run the forwarding tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~ForwarderHostTests -v minimal`  
Expected: FAIL because the forwarding host is not implemented yet

- [ ] **Step 3: Implement the minimal forwarder host and request forwarding pipeline**

```csharp
public async Task<ForwarderResult> ForwardAsync(ForwarderRequest request, CancellationToken cancellationToken)
{
    using var upstream = new HttpRequestMessage(new HttpMethod(request.Method), request.PathAndQuery);
    if (request.Body.Length > 0)
        upstream.Content = new ByteArrayContent(request.Body);

    HeaderSanitizer.CopySafeRequestHeaders(request.Headers, upstream.Headers, upstream.Content);

    using var response = await _client.SendAsync(upstream, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);

    return new ForwarderResult((int)response.StatusCode, response.Headers, response.Content.Headers, _rewriter.Rewrite(body, response.Content.Headers.ContentType?.MediaType, _config.TunnelUrl));
}
```

- [ ] **Step 4: Run the forwarding tests and a focused solution build**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~ForwarderHostTests -v minimal`  
Expected: PASS

Run: `dotnet build desktop/LocalhostTunnel.sln -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Infrastructure desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: port forwarder runtime to dotnet"
```

## Task 5: Add Cloudflared Downloading And Process Supervision

**Files:**
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/ICloudflaredInstaller.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/ITunnelHost.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Tunnel/CloudflaredInstaller.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Tunnel/CloudflaredProcessHost.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Tunnel/CloudflaredOutputParser.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Tunnel/CloudflaredOutputParserTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Tunnel/CloudflaredProcessHostTests.cs`

- [ ] **Step 1: Write failing parser and process lifecycle tests**

```csharp
[Theory]
[InlineData("INF Registered tunnel connection", ServiceState.Running)]
[InlineData("ERR failed to serve quic connection", ServiceState.Faulted)]
public void ParseLine_Maps_Output_To_State(string line, ServiceState expectedState)
{
    var parser = new CloudflaredOutputParser();

    var parsed = parser.Parse(line);

    parsed.State.Should().Be(expectedState);
}
```

```csharp
[Fact]
public async Task StartAsync_Requires_Token_And_Updates_State()
{
    var host = new CloudflaredProcessHost(fakeProcessRunner, installer, logStore, paths);

    var result = await host.StartAsync("", CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    host.Current.State.Should().Be(ServiceState.Stopped);
}
```

- [ ] **Step 2: Run the tunnel tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter "FullyQualifiedName~CloudflaredOutputParserTests|FullyQualifiedName~CloudflaredProcessHostTests" -v minimal`  
Expected: FAIL because the parser and process host are missing

- [ ] **Step 3: Implement cloudflared install and supervision**

```csharp
public async Task EnsureInstalledAsync(CancellationToken cancellationToken)
{
    Directory.CreateDirectory(_paths.CloudflaredDirectory);
    if (File.Exists(_cloudflaredExePath))
        return;

    using var response = await _httpClient.GetAsync(DownloadUrl, cancellationToken);
    response.EnsureSuccessStatusCode();

    await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
    await using var destination = File.Create(_cloudflaredExePath);
    await source.CopyToAsync(destination, cancellationToken);
}
```

- [ ] **Step 4: Run the tunnel tests**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter "FullyQualifiedName~CloudflaredOutputParserTests|FullyQualifiedName~CloudflaredProcessHostTests" -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Infrastructure desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: add cloudflared supervision"
```

## Task 6: Build The Runtime Coordinator And Desktop Host

**Files:**
- Create: `desktop/src/LocalhostTunnel.Application/Services/RuntimeCoordinator.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Services/StartupImportService.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/AppHost.cs`
- Modify: `desktop/src/LocalhostTunnel.Desktop/App.xaml`
- Modify: `desktop/src/LocalhostTunnel.Desktop/App.xaml.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/Services/TrayIconService.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/Services/WindowLifecycleService.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/MainWindowViewModel.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/Dialogs/CloseBehaviorDialogViewModel.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/Dialogs/CloseBehaviorDialog.xaml`
- Test: `desktop/tests/LocalhostTunnel.Desktop.Tests/ViewModels/Dialogs/CloseBehaviorDialogViewModelTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/RuntimeCoordinatorTests.cs`

- [ ] **Step 1: Write failing view model and coordinator tests**

```csharp
[Fact]
public void ConfirmRunInTray_Sets_Result_Without_Closing_Runtime()
{
    var vm = new CloseBehaviorDialogViewModel();

    vm.RunInTrayCommand.Execute(null);

    vm.Result.Should().Be(CloseBehaviorResult.RunInTray);
}
```

```csharp
[Fact]
public async Task StartAsync_Starts_Tunnel_Before_Forwarder()
{
    var coordinator = new RuntimeCoordinator(fakeTunnelHost, fakeForwarderHost, configStore, logStore);

    await coordinator.StartAsync(CancellationToken.None);

    fakeTunnelHost.CallOrder.Should().BeLessThan(fakeForwarderHost.CallOrder);
}
```

- [ ] **Step 2: Run the desktop and application tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter FullyQualifiedName~CloseBehaviorDialogViewModelTests -v minimal`  
Expected: FAIL because the dialog view model does not exist

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~RuntimeCoordinatorTests -v minimal`  
Expected: FAIL because the runtime coordinator does not exist yet

- [ ] **Step 3: Implement the coordinator, WPF host bootstrap, tray service, and close dialog**

```csharp
public async Task StartAsync(CancellationToken cancellationToken)
{
    var config = await _configStore.LoadAsync(cancellationToken);
    var validation = AppConfigValidator.Validate(config);
    if (!validation.IsValid)
        throw new InvalidOperationException("Configuration is invalid.");

    await _cloudflaredInstaller.EnsureInstalledAsync(cancellationToken);
    await _tunnelHost.StartAsync(config.TunnelToken, cancellationToken);
    await _forwarderHost.StartAsync(config, cancellationToken);
    PublishSnapshot();
}
```

```csharp
builder.Services.AddSingleton<AppDataPaths>(_ => AppDataPaths.Build());
builder.Services.AddSingleton<IConfigStore, JsonConfigStore>();
builder.Services.AddSingleton<ISessionStore, JsonSessionStore>();
builder.Services.AddSingleton<ILogStore, ObservableLogStore>();
builder.Services.AddSingleton<IForwarderHost, ForwarderHost>();
builder.Services.AddSingleton<ITunnelHost, CloudflaredProcessHost>();
builder.Services.AddSingleton<RuntimeCoordinator>();
builder.Services.AddSingleton<TrayIconService>();
builder.Services.AddSingleton<MainWindow>();
```

- [ ] **Step 4: Run the coordinator and dialog tests**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter FullyQualifiedName~CloseBehaviorDialogViewModelTests -v minimal`  
Expected: PASS

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~RuntimeCoordinatorTests -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Desktop desktop/tests/LocalhostTunnel.Desktop.Tests desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: add desktop host and runtime coordinator"
```

## Task 7: Build The Command Center Shell And Overview Screen

**Files:**
- Create: `desktop/src/LocalhostTunnel.Desktop/Theme/Colors.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Theme/Typography.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Theme/Controls.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/MainWindow.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/OverviewView.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/OverviewViewModel.cs`
- Test: `desktop/tests/LocalhostTunnel.Desktop.Tests/ViewModels/OverviewViewModelTests.cs`

- [ ] **Step 1: Write a failing overview view model test**

```csharp
[Fact]
public void SnapshotUpdate_Maps_Runtime_Data_To_CommandCenter_Cards()
{
    var vm = new OverviewViewModel();
    var snapshot = new RuntimeSnapshot(
        ForwarderSnapshot.CreateRunning(DateTimeOffset.UtcNow.AddMinutes(-2), 4321),
        TunnelSnapshot.Running(),
        new[] { LogEntry.Info("tunnel", "connected") });

    vm.Apply(snapshot);

    vm.ForwarderLabel.Should().Be("Running");
    vm.TunnelLabel.Should().Be("Connected");
    vm.LiveLogs.Should().ContainSingle(x => x.Message == "connected");
}
```

- [ ] **Step 2: Run the overview test to verify it fails**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter FullyQualifiedName~OverviewViewModelTests -v minimal`  
Expected: FAIL because the overview VM and bindings are missing

- [ ] **Step 3: Implement the shell, theme resources, and overview view model**

```xml
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="72" />
    <ColumnDefinition Width="*" />
  </Grid.ColumnDefinitions>

  <Border Grid.Column="0" Style="{StaticResource SidebarShellStyle}">
    <!-- Navigation icons -->
  </Border>

  <Grid Grid.Column="1">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <!-- summary strip + overview content -->
  </Grid>
</Grid>
```

- [ ] **Step 4: Run the overview test and a full desktop build**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter FullyQualifiedName~OverviewViewModelTests -v minimal`  
Expected: PASS

Run: `dotnet build desktop/LocalhostTunnel.sln -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Desktop desktop/tests/LocalhostTunnel.Desktop.Tests
git commit -m "feat: add command center overview shell"
```

## Task 8: Add Configuration, Logs, And Diagnostics Screens

**Files:**
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/ConfigurationView.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/LogsView.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/DiagnosticsView.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/ConfigurationViewModel.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/LogsViewModel.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/DiagnosticsViewModel.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Services/NavigationService.cs`
- Test: `desktop/tests/LocalhostTunnel.Desktop.Tests/ViewModels/ConfigurationViewModelTests.cs`
- Test: `desktop/tests/LocalhostTunnel.Desktop.Tests/ViewModels/LogsViewModelTests.cs`

- [ ] **Step 1: Write failing tests for config validation feedback and log filtering**

```csharp
[Fact]
public async Task SaveAsync_Rejects_Invalid_Config_And_Exposes_FieldErrors()
{
    var vm = new ConfigurationViewModel(configStore, runtimeCoordinator);
    vm.TunnelToken = "";
    vm.TargetPort = 0;

    await vm.SaveAsync();

    vm.FieldErrors.Should().ContainKey(nameof(vm.TunnelToken));
    vm.FieldErrors.Should().ContainKey(nameof(vm.TargetPort));
}
```

```csharp
[Fact]
public void ApplyFilter_Shows_Only_Selected_Log_Level()
{
    var vm = new LogsViewModel(logStore);
    logStore.Append(LogEntry.Info("app", "ready"));
    logStore.Append(LogEntry.Error("app", "boom"));

    vm.SelectedLevel = "error";
    vm.Refresh();

    vm.FilteredLogs.Should().ContainSingle(x => x.Level == "error");
}
```

- [ ] **Step 2: Run the configuration and logs tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter "FullyQualifiedName~ConfigurationViewModelTests|FullyQualifiedName~LogsViewModelTests" -v minimal`  
Expected: FAIL because the VMs and views do not exist yet

- [ ] **Step 3: Implement the configuration, logs, and diagnostics screens**

```csharp
public async Task SaveAsync()
{
    var config = BuildConfig();
    var validation = AppConfigValidator.Validate(config);
    FieldErrors.Clear();
    foreach (var pair in validation.Errors)
        FieldErrors[pair.Key] = pair.Value;

    if (!validation.IsValid)
        return;

    await _configStore.SaveAsync(config, CancellationToken.None);
    await _runtimeCoordinator.ReloadConfigAsync(CancellationToken.None);
}
```

```csharp
public void Refresh()
{
    var query = _logStore.Entries.AsEnumerable();
    if (!string.Equals(SelectedLevel, "all", StringComparison.OrdinalIgnoreCase))
        query = query.Where(x => x.Level == SelectedLevel);
    if (!string.IsNullOrWhiteSpace(SearchText))
        query = query.Where(x => x.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    FilteredLogs.Clear();
    foreach (var entry in query.TakeLast(500))
        FilteredLogs.Add(entry);
}
```

- [ ] **Step 4: Run the desktop screen tests**

Run: `dotnet test desktop/tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj --filter "FullyQualifiedName~ConfigurationViewModelTests|FullyQualifiedName~LogsViewModelTests" -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Desktop desktop/tests/LocalhostTunnel.Desktop.Tests
git commit -m "feat: add configuration and logs screens"
```

## Task 9: Implement Update Checking And External Updater Handoff

**Files:**
- Create: `desktop/src/LocalhostTunnel.Core/Updates/ReleaseInfo.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Interfaces/IUpdateService.cs`
- Create: `desktop/src/LocalhostTunnel.Application/Services/UpdateCoordinator.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Updates/GitHubReleaseService.cs`
- Create: `desktop/src/LocalhostTunnel.Infrastructure/Updates/DesktopUpdaterLauncher.cs`
- Create: `desktop/src/LocalhostTunnel.Updater/LocalhostTunnel.Updater.csproj`
- Create: `desktop/src/LocalhostTunnel.Updater/Program.cs`
- Create: `desktop/src/LocalhostTunnel.Desktop/Views/UpdatesView.xaml`
- Create: `desktop/src/LocalhostTunnel.Desktop/ViewModels/UpdatesViewModel.cs`
- Test: `desktop/tests/LocalhostTunnel.Infrastructure.Tests/Updates/GitHubReleaseServiceTests.cs`

- [ ] **Step 1: Write failing update service tests**

```csharp
[Fact]
public async Task CheckForUpdatesAsync_Returns_Latest_Release_When_Version_Is_Newer()
{
    var service = new GitHubReleaseService(fakeHttpClient, currentVersion: "1.0.15");

    var release = await service.CheckForUpdatesAsync(CancellationToken.None);

    release.Should().NotBeNull();
    release!.Version.Should().Be("1.0.16");
}
```

- [ ] **Step 2: Run the update tests to verify they fail**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~GitHubReleaseServiceTests -v minimal`  
Expected: FAIL because the update service does not exist yet

- [ ] **Step 3: Implement release checking and the external updater handoff**

```csharp
public async Task<ReleaseInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken)
{
    var response = await _httpClient.GetFromJsonAsync<GitHubReleaseDto>(_releaseUrl, cancellationToken);
    if (response is null)
        return null;

    return Version.Parse(response.TagName.TrimStart('v')) > _currentVersion
        ? new ReleaseInfo(response.TagName.TrimStart('v'), response.ZipballUrl)
        : null;
}
```

```csharp
public static async Task<int> Main(string[] args)
{
    // download release archive, unpack to temp, copy files into install directory,
    // relaunch LocalhostTunnel.Desktop.exe, then exit
}
```

- [ ] **Step 4: Run the update tests and build the updater project**

Run: `dotnet test desktop/tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj --filter FullyQualifiedName~GitHubReleaseServiceTests -v minimal`  
Expected: PASS

Run: `dotnet build desktop/src/LocalhostTunnel.Updater/LocalhostTunnel.Updater.csproj -v minimal`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add desktop/src/LocalhostTunnel.Core desktop/src/LocalhostTunnel.Application desktop/src/LocalhostTunnel.Infrastructure desktop/src/LocalhostTunnel.Updater desktop/src/LocalhostTunnel.Desktop desktop/tests/LocalhostTunnel.Infrastructure.Tests
git commit -m "feat: add desktop update workflow"
```

## Task 10: Package The App And Update Docs

**Files:**
- Create: `desktop/packaging/LocalhostTunnel.iss`
- Create: `desktop/scripts/publish-desktop.ps1`
- Modify: `README.md`
- Modify: `.gitignore`

- [ ] **Step 1: Write a failing packaging smoke check**

```powershell
$publishDir = Join-Path $PWD 'desktop\artifacts\publish'
if (-not (Test-Path (Join-Path $publishDir 'LocalhostTunnel.Desktop.exe'))) {
    throw 'Desktop publish output missing'
}
```

- [ ] **Step 2: Run the packaging smoke check to verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File desktop/scripts/publish-desktop.ps1 -SkipBuild`  
Expected: FAIL because the publish script and profile do not exist yet

- [ ] **Step 3: Implement publish, installer, and README updates**

```powershell
dotnet publish desktop/src/LocalhostTunnel.Desktop/LocalhostTunnel.Desktop.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -o desktop/artifacts/publish
```

```iss
[Setup]
AppName=Localhost Tunnel
AppVersion=1.0.15
DefaultDirName={autopf}\LocalhostTunnel
OutputDir=desktop\artifacts\installer
OutputBaseFilename=LocalhostTunnel-Setup
```

- [ ] **Step 4: Run publish, installer build, and the full test suite**

Run: `powershell -ExecutionPolicy Bypass -File desktop/scripts/publish-desktop.ps1`  
Expected: PASS and `desktop/artifacts/publish/LocalhostTunnel.Desktop.exe` exists

Run: `iscc desktop/packaging/LocalhostTunnel.iss`  
Expected: PASS and installer output appears in `desktop/artifacts/installer`

Run: `dotnet test desktop/LocalhostTunnel.sln -v minimal`  
Expected: PASS with all desktop tests green

- [ ] **Step 5: Commit**

```bash
git add desktop/packaging desktop/scripts README.md .gitignore
git commit -m "docs: package and document desktop app"
```

## Manual Acceptance Checklist

- [ ] Fresh install creates `%LocalAppData%\LocalhostTunnel`
- [ ] First run imports legacy root `config.json` if it exists
- [ ] Start launches tunnel before forwarder
- [ ] Stop shuts both services down cleanly
- [ ] Invalid config blocks start and highlights fields
- [ ] Closing the window shows `Run in tray`, `Cancel`, and `Exit`
- [ ] Tray menu can restore the window and stop the runtime
- [ ] Overview shows live status without a browser
- [ ] Logs screen filters and searches correctly
- [ ] Publish script produces a self-contained Windows executable

## Notes For The Implementer

- Do not delete the existing Node app until the desktop app passes the acceptance checklist.
- Port behavior, not file structure. The JavaScript files are reference material, not a migration target.
- Keep view models slim. Anything involving IO, process launch, or persistence belongs outside the WPF view layer.
- If Git is still unavailable when executing this plan, record each task checkpoint in the plan file itself and commit later once the repository is initialized.
