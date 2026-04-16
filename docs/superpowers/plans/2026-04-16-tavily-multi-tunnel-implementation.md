# Tavily Multi-Tunnel Desktop Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add multi-profile parallel tunnel runtime and integrate TavilyProxyAPI directly into the desktop app with a dedicated `Tavily API` tab.

**Architecture:** Replace single-runtime orchestration with a profile-based runtime manager. Each profile runs in an isolated runtime instance (`standard` or `tavily`), with profile-scoped snapshots/logs and independent start/stop lifecycle. Port Tavily proxy behavior to C# and run it in-process.

**Tech Stack:** .NET 8, WPF, ASP.NET Core minimal hosting, HttpClient, CommunityToolkit.Mvvm, xUnit + FluentAssertions

---

## File Structure

### Core configuration/runtime model

- Create: `src/LocalhostTunnel.Core/Configuration/ProfileType.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TunnelProfile.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TavilyConfig.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/ProfilesConfig.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TunnelProfileValidator.cs`
- Modify: `src/LocalhostTunnel.Core/Configuration/SessionState.cs`
- Modify: `src/LocalhostTunnel.Core/Logging/LogEntry.cs`
- Create: `src/LocalhostTunnel.Core/Runtime/ProfileRuntimeSnapshot.cs`

### Application orchestration

- Create: `src/LocalhostTunnel.Application/Interfaces/IProfilesConfigStore.cs`
- Create: `src/LocalhostTunnel.Application/Interfaces/ITavilyProxyHost.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/IRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/StandardRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/TavilyRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/RuntimeManager.cs`
- Modify: `src/LocalhostTunnel.Application/Services/StartupImportService.cs`

### Infrastructure

- Create: `src/LocalhostTunnel.Infrastructure/Storage/JsonProfilesConfigStore.cs`
- Modify: `src/LocalhostTunnel.Infrastructure/Storage/AppDataPaths.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Storage/ProfilesConfigMigrator.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyKeyManager.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyQuotaDetector.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyProxyService.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyProxyHost.cs`

### Desktop UI/ViewModels

- Create: `src/LocalhostTunnel.Desktop/ViewModels/ProfileListItemViewModel.cs`
- Create: `src/LocalhostTunnel.Desktop/ViewModels/TavilyApiViewModel.cs`
- Create: `src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml`
- Create: `src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/ConfigurationViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/Views/ConfigurationView.xaml`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/MainWindowViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/MainWindow.xaml`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/OverviewViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/LogsViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/DiagnosticsViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/AppHost.cs`

### Tests

- Create: `tests/LocalhostTunnel.Core.Tests/Configuration/TunnelProfileValidatorTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Storage/JsonProfilesConfigStoreTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Storage/ProfilesConfigMigratorTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Tavily/TavilyKeyManagerTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Tavily/TavilyProxyServiceTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Runtime/RuntimeManagerTests.cs`
- Modify: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/ConfigurationViewModelTests.cs`
- Modify: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/MainWindowViewModelTests.cs`
- Create: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/TavilyApiViewModelTests.cs`

### Docs

- Modify: `README.md`

---

### Task 1: Add profile-based configuration model in Core

**Files:**
- Create: `src/LocalhostTunnel.Core/Configuration/ProfileType.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TunnelProfile.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TavilyConfig.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/ProfilesConfig.cs`
- Create: `src/LocalhostTunnel.Core/Configuration/TunnelProfileValidator.cs`
- Create: `tests/LocalhostTunnel.Core.Tests/Configuration/TunnelProfileValidatorTests.cs`

- [ ] **Step 1: Write failing validation tests for standard+tavily profile rules**

```csharp
[Fact]
public void Validate_TavilyProfile_Requires_BothKeys()
{
    var profile = TestProfiles.CreateTavily(apiKey1: "a", apiKey2: "");
    var result = TunnelProfileValidator.Validate(profile);
    result.IsValid.Should().BeFalse();
    result.Errors.Should().ContainKey("Tavily.ApiKey2");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj -v minimal --filter TunnelProfileValidatorTests`
Expected: FAIL due missing model/validator implementation.

- [ ] **Step 3: Implement profile entities and validator with profile-type specific rules**

```csharp
public enum ProfileType { Standard, Tavily }
public sealed class TunnelProfile { public string Id { get; init; } = ""; /* ... */ }
public sealed class TavilyConfig { public string ApiKey1 { get; init; } = ""; /* ... */ }
```

- [ ] **Step 4: Re-run targeted tests**

Run: `dotnet test tests/LocalhostTunnel.Core.Tests/LocalhostTunnel.Core.Tests.csproj -v minimal --filter TunnelProfileValidatorTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Core/Configuration tests/LocalhostTunnel.Core.Tests/Configuration/TunnelProfileValidatorTests.cs
git commit -m "feat(core): add profile config model and validator for standard/tavily profiles"
```

### Task 2: Add profiles config store and migration from legacy config

**Files:**
- Create: `src/LocalhostTunnel.Application/Interfaces/IProfilesConfigStore.cs`
- Modify: `src/LocalhostTunnel.Infrastructure/Storage/AppDataPaths.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Storage/JsonProfilesConfigStore.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Storage/ProfilesConfigMigrator.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Storage/JsonProfilesConfigStoreTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Storage/ProfilesConfigMigratorTests.cs`

- [ ] **Step 1: Write failing tests for loading default profile config and migration from legacy `config.json`**

```csharp
[Fact]
public async Task MigrateAsync_Creates_StandardProfile_From_Legacy_Config()
{
    // arrange legacy config.json
    var migrated = await migrator.MigrateAsync(CancellationToken.None);
    migrated.Should().BeTrue();
}
```

- [ ] **Step 2: Run storage/migration tests and confirm failure**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter "JsonProfilesConfigStoreTests|ProfilesConfigMigratorTests"`
Expected: FAIL (types/services not implemented).

- [ ] **Step 3: Implement JSON store + atomic writes + migration seeding Tavily profile**

```csharp
public sealed class JsonProfilesConfigStore : IProfilesConfigStore
{
    public Task<ProfilesConfig> LoadAsync(...) { /* config.profiles.json */ }
}
```

- [ ] **Step 4: Re-run tests**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter "JsonProfilesConfigStoreTests|ProfilesConfigMigratorTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Application/Interfaces/IProfilesConfigStore.cs src/LocalhostTunnel.Infrastructure/Storage tests/LocalhostTunnel.Infrastructure.Tests/Storage
git commit -m "feat(storage): add profiles config store and legacy migration"
```

### Task 3: Implement Tavily proxy logic in C#

**Files:**
- Create: `src/LocalhostTunnel.Application/Interfaces/ITavilyProxyHost.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyKeyManager.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyQuotaDetector.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyProxyService.cs`
- Create: `src/LocalhostTunnel.Infrastructure/Tavily/TavilyProxyHost.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Tavily/TavilyKeyManagerTests.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Tavily/TavilyProxyServiceTests.cs`

- [ ] **Step 1: Write failing tests for quota failover behavior + no-retry on non-quota errors**

```csharp
[Fact]
public async Task ForwardAsync_RetriesOnce_With_Fallback_When_QuotaExhausted()
{
    // first response: 432; second response: 200
    // assert second key promoted
}
```

- [ ] **Step 2: Run Tavily tests and verify failure**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter "TavilyKeyManagerTests|TavilyProxyServiceTests"`
Expected: FAIL.

- [ ] **Step 3: Implement in-process Tavily proxy host and proxy service**

```csharp
app.MapGet("/health", () => Results.Json(new { status = "ok" }));
app.Map("/{**path}", async ctx => await _proxyService.ForwardAsync(ctx, cancellationToken));
```

- [ ] **Step 4: Re-run Tavily tests**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter "TavilyKeyManagerTests|TavilyProxyServiceTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Application/Interfaces/ITavilyProxyHost.cs src/LocalhostTunnel.Infrastructure/Tavily tests/LocalhostTunnel.Infrastructure.Tests/Tavily
git commit -m "feat(tavily): port proxy and key failover logic to C# runtime"
```

### Task 4: Implement multi-runtime manager and profile runtime instances

**Files:**
- Create: `src/LocalhostTunnel.Application/Services/Runtime/IRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/StandardRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/TavilyRuntimeInstance.cs`
- Create: `src/LocalhostTunnel.Application/Services/Runtime/RuntimeManager.cs`
- Modify: `src/LocalhostTunnel.Application/Services/StartupImportService.cs`
- Create: `tests/LocalhostTunnel.Infrastructure.Tests/Runtime/RuntimeManagerTests.cs`

- [ ] **Step 1: Write failing runtime manager tests for starting/stopping profiles independently**

```csharp
[Fact]
public async Task StopAsync_Stops_Only_Target_Profile()
{
    await manager.StartAsync(profileA.Id, ct);
    await manager.StartAsync(profileB.Id, ct);
    await manager.StopAsync(profileA.Id, ct);
    manager.GetSnapshot(profileB.Id).Tunnel.State.Should().NotBe(ServiceState.Stopped);
}
```

- [ ] **Step 2: Run runtime manager tests and confirm failure**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter RuntimeManagerTests`
Expected: FAIL.

- [ ] **Step 3: Implement `RuntimeManager` with per-profile instance map and snapshot events**

```csharp
private readonly ConcurrentDictionary<string, IRuntimeInstance> _instances;
public event EventHandler<ProfileRuntimeSnapshot>? SnapshotUpdated;
```

- [ ] **Step 4: Re-run runtime manager tests**

Run: `dotnet test tests/LocalhostTunnel.Infrastructure.Tests/LocalhostTunnel.Infrastructure.Tests.csproj -v minimal --filter RuntimeManagerTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Application/Services/Runtime src/LocalhostTunnel.Application/Services/StartupImportService.cs tests/LocalhostTunnel.Infrastructure.Tests/Runtime/RuntimeManagerTests.cs
git commit -m "feat(runtime): add profile-scoped runtime manager and runtime instances"
```

### Task 5: Wire DI and app bootstrap to profiles runtime stack

**Files:**
- Modify: `src/LocalhostTunnel.Desktop/AppHost.cs`
- Modify: `src/LocalhostTunnel.Desktop/App.xaml.cs`
- Modify: `src/LocalhostTunnel.Infrastructure/Storage/AppDataPaths.cs` (if additional path helpers needed)

- [ ] **Step 1: Add failing desktop test for resolving new services from DI**

```csharp
[Fact]
public void Build_Registers_RuntimeManager_And_TavilyViewModel()
{
    using var host = AppHost.Build();
    host.Services.GetRequiredService<RuntimeManager>().Should().NotBeNull();
}
```

- [ ] **Step 2: Run desktop DI test and verify failure**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter Build_Registers`
Expected: FAIL.

- [ ] **Step 3: Register new store/manager/tavily services and ensure startup migration runs**

```csharp
services.AddSingleton<IProfilesConfigStore, JsonProfilesConfigStore>();
services.AddSingleton<RuntimeManager>();
services.AddSingleton<ITavilyProxyHost, TavilyProxyHost>();
```

- [ ] **Step 4: Re-run desktop DI test**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter Build_Registers`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Desktop/AppHost.cs src/LocalhostTunnel.Desktop/App.xaml.cs
git commit -m "chore(desktop): wire multi-runtime and tavily services into host bootstrap"
```

### Task 6: Replace single configuration screen with profile list controls

**Files:**
- Create: `src/LocalhostTunnel.Desktop/ViewModels/ProfileListItemViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/ConfigurationViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/Views/ConfigurationView.xaml`
- Modify: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/ConfigurationViewModelTests.cs`

- [ ] **Step 1: Write failing tests for profile add/edit/start/stop commands**

```csharp
[Fact]
public async Task StartProfileCommand_Starts_Selected_Profile()
{
    await vm.StartProfileCommand.ExecuteAsync(targetProfileId);
    runtimeManager.StartCalls.Should().Contain(targetProfileId);
}
```

- [ ] **Step 2: Run targeted configuration VM tests**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter ConfigurationViewModelTests`
Expected: FAIL.

- [ ] **Step 3: Implement profile list view-model state + commands and update XAML cards**

```xml
<ItemsControl ItemsSource="{Binding Profiles}">
  <!-- card with Start/Stop/Edit/Delete -->
</ItemsControl>
```

- [ ] **Step 4: Re-run configuration VM tests**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter ConfigurationViewModelTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Desktop/ViewModels/ProfileListItemViewModel.cs src/LocalhostTunnel.Desktop/ViewModels/ConfigurationViewModel.cs src/LocalhostTunnel.Desktop/Views/ConfigurationView.xaml tests/LocalhostTunnel.Desktop.Tests/ViewModels/ConfigurationViewModelTests.cs
git commit -m "feat(ui): convert configuration screen to profile-based runtime management"
```

### Task 7: Add dedicated Tavily API tab and view-model

**Files:**
- Create: `src/LocalhostTunnel.Desktop/ViewModels/TavilyApiViewModel.cs`
- Create: `src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml`
- Create: `src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml.cs`
- Modify: `src/LocalhostTunnel.Desktop/MainWindow.xaml`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/MainWindowViewModel.cs`
- Create: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/TavilyApiViewModelTests.cs`
- Modify: `tests/LocalhostTunnel.Desktop.Tests/ViewModels/MainWindowViewModelTests.cs`

- [ ] **Step 1: Write failing tests for Tavily tab routing and Tavily start/stop actions**

```csharp
[Fact]
public async Task InstallTavilyRuntime_Starts_Tavily_Profile_Instance()
{
    await vm.StartTavilyRuntimeCommand.ExecuteAsync(null);
    vm.StatusMessage.Should().Contain("started");
}
```

- [ ] **Step 2: Run targeted tests and verify failure**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter "TavilyApiViewModelTests|MainWindowViewModelTests"`
Expected: FAIL.

- [ ] **Step 3: Implement Tavily tab UI + menu entry + routing + view-model**

```xml
<Button Command="{Binding ShowTavilyApiCommand}">
  <TextBlock Text="Tavily API" />
</Button>
```

- [ ] **Step 4: Re-run targeted tests**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter "TavilyApiViewModelTests|MainWindowViewModelTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Desktop/MainWindow.xaml src/LocalhostTunnel.Desktop/ViewModels/MainWindowViewModel.cs src/LocalhostTunnel.Desktop/ViewModels/TavilyApiViewModel.cs src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml src/LocalhostTunnel.Desktop/Views/TavilyApiView.xaml.cs tests/LocalhostTunnel.Desktop.Tests/ViewModels/TavilyApiViewModelTests.cs tests/LocalhostTunnel.Desktop.Tests/ViewModels/MainWindowViewModelTests.cs
git commit -m "feat(ui): add Tavily API tab with integrated runtime controls"
```

### Task 8: Scope logs/overview/diagnostics to profile-aware runtime data

**Files:**
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/OverviewViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/LogsViewModel.cs`
- Modify: `src/LocalhostTunnel.Desktop/ViewModels/DiagnosticsViewModel.cs`
- Modify: `src/LocalhostTunnel.Core/Logging/LogEntry.cs`
- Create/Modify tests in:
  - `tests/LocalhostTunnel.Desktop.Tests/ViewModels/OverviewViewModelTests.cs`
  - `tests/LocalhostTunnel.Desktop.Tests/ViewModels/LogsViewModelTests.cs`
  - `tests/LocalhostTunnel.Desktop.Tests/ViewModels/DiagnosticsViewModelTests.cs`

- [ ] **Step 1: Add failing tests for profile filter in logs and overview aggregate state**
- [ ] **Step 2: Run targeted tests and verify failure**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter "OverviewViewModelTests|LogsViewModelTests|DiagnosticsViewModelTests"`
Expected: FAIL.

- [ ] **Step 3: Implement profile-tagged log model and UI filtering**
- [ ] **Step 4: Re-run targeted tests**

Run: `dotnet test tests/LocalhostTunnel.Desktop.Tests/LocalhostTunnel.Desktop.Tests.csproj -v minimal --filter "OverviewViewModelTests|LogsViewModelTests|DiagnosticsViewModelTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/LocalhostTunnel.Core/Logging/LogEntry.cs src/LocalhostTunnel.Desktop/ViewModels/OverviewViewModel.cs src/LocalhostTunnel.Desktop/ViewModels/LogsViewModel.cs src/LocalhostTunnel.Desktop/ViewModels/DiagnosticsViewModel.cs tests/LocalhostTunnel.Desktop.Tests/ViewModels
git commit -m "feat(observability): add profile-scoped snapshots and log filtering"
```

### Task 9: End-to-end verification and docs update

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Update README with profile runtime + Tavily tab usage and migration notes**
- [ ] **Step 2: Run full build and test suite**

Run:
- `dotnet build LocalhostTunnel.sln -v minimal`
- `dotnet test LocalhostTunnel.sln -v minimal`

Expected: all pass.

- [ ] **Step 3: Manual smoke test checklist**

Run:
1. Start two standard profiles concurrently.
2. Start Tavily profile and call `/health` on local port.
3. Verify one profile stop does not stop others.
4. Verify logs and diagnostics can filter by profile.

Expected: all actions succeed with isolated status updates.

- [ ] **Step 4: Commit final integration/docs changes**

```bash
git add README.md
git commit -m "docs: document multi-tunnel and integrated Tavily API runtime workflow"
```

- [ ] **Step 5: Tag and release prep**

```bash
git push origin main
git tag v1.0.19
git push origin v1.0.19
```

Expected: release workflow publishes portable zip.

