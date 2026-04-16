# Multi-Tunnel Runtime + Tavily API Integration Design

- Date: 2026-04-16
- Repository: `D:\Software\LocalhostTunel-1.0.15`
- Owner decision summary:
  - Add a dedicated sidebar tab named `Tavily API`.
  - Support multiple tunnel configurations running in parallel.
  - Port `D:\VSCode\TavilyProxyAPI` into this desktop application (no external Python runtime dependency).
  - Runtime control model: each profile has its own `Start/Stop` actions (no `Start All/Stop All` in this scope).

## Objectives

1. Replace single-runtime orchestration with per-profile runtime instances that can run concurrently.
2. Integrate Tavily proxy behavior in C# and run it in-process with the desktop app.
3. Preserve current desktop UX style while expanding configuration and runtime controls for multi-profile operations.
4. Maintain backward compatibility through config migration from legacy single-config storage.

## Non-Goals

1. No plugin architecture or generic service marketplace in this release.
2. No `Start All/Stop All` feature.
3. No external Python process management for Tavily.

## Current-State Constraints

1. Existing `RuntimeCoordinator` assumes a single `AppConfig` and single snapshot/log stream.
2. Existing storage persists one config object in `config.json`.
3. Existing UI and view-models assume one active runtime.

## Proposed Architecture

### 1. New Configuration Model

Introduce a profile-based config document:

- File: `%LOCALAPPDATA%\LocalhostTunnel\config.profiles.json`
- Root object: `ProfilesConfig`
  - `profiles: TunnelProfile[]`
  - `selectedProfileId: string?` (UI default selection)

`TunnelProfile`:
- `id: string` (GUID or deterministic ID)
- `name: string`
- `type: "standard" | "tavily"`
- `enabled: bool`
- `tunnel: TunnelConfig`
- `forwarder: ForwarderConfig`
- `tavily: TavilyConfig?` (required only when type=`tavily`)

`TavilyConfig`:
- `apiKey1`, `apiKey2`
- `host` (default `127.0.0.1`)
- `port` (default `8766`)
- `baseUrl` (default `https://api.tavily.com`)
- `requestTimeoutSeconds` (default `60`)

### 2. Runtime Orchestration Refactor

Replace single coordinator usage in UI with `RuntimeManager`:

- `RuntimeManager`
  - `StartAsync(profileId)`
  - `StopAsync(profileId)`
  - `GetSnapshot(profileId)`
  - `GetAllSnapshots()`
  - `SnapshotUpdated(profileId, snapshot)` event

- `RuntimeInstance` per profile:
  - `StandardRuntimeInstance`: cloudflared + forwarder
  - `TavilyRuntimeInstance`: TavilyProxyHost + cloudflared + forwarder

Isolation rules:
1. Each instance has independent state and lock.
2. Failure in one profile does not stop or degrade others.
3. Logs and diagnostics are tagged by `profileId`.

### 3. Tavily API Port (C# In-Process)

Create `TavilyProxyHost` in infrastructure layer:

- Hosts local HTTP endpoint (`/health`, catch-all proxy route).
- Injects active Tavily key into upstream request.
- Preserves behavior from Python implementation:
  - retries once with fallback key only for quota/credit exhaustion indicators.
  - does not switch keys for generic transport/rate-limit failures.
  - supports normal JSON and `text/event-stream`.

Suggested components:
- `TavilyKeyManager` (active/fallback key promotion and quota detection)
- `TavilyProxyService` (request forwarding and retry/failover logic)
- `TavilyProxyHost` (local web host lifecycle)

### 4. Storage & Migration

Migration flow at startup:

1. If `config.profiles.json` exists -> load directly.
2. Else if legacy `config.json` exists -> convert to one `standard` profile.
3. Seed a default disabled `tavily` profile named `Tavily API` if not present.
4. Save converted profile config atomically.

Legacy file remains readable for migration only; runtime uses profile config afterward.

## UI/UX Changes

### Sidebar

- Add explicit menu item: `Tavily API` (no abbreviation).

### Configuration Screen

- Replace single-form runtime configuration with profile list/cards.
- Per profile card:
  - status
  - uptime
  - `Start` / `Stop`
  - `Edit` / `Delete`
- Add `Add Tunnel Profile`.

### Tavily API Screen

- Dedicated form fields for Tavily config + tunnel config.
- Own status panel.
- Actions:
  - `Save`
  - `Start Tavily Runtime`
  - `Stop Tavily Runtime`

### Overview / Logs / Diagnostics

1. Show active profile runtimes instead of single runtime summary.
2. Add profile filter for logs/diagnostics views.
3. Preserve existing time-zone behavior (+7 Ho Chi Minh display) in timestamps.

## Validation Rules

1. Existing tunnel/forwarder validation applies per profile.
2. `tavily` profile additionally requires `apiKey1`, `apiKey2`, valid host/port/base URL.
3. Duplicate local listen ports across active profiles are rejected with explicit profile-specific error messages.

## Error Handling

1. `Start(profileId)` failures are profile-scoped and surfaced on that profile card + Tavily tab status.
2. For `tavily` profile:
  - if Tavily host start fails, tunnel/forwarder start is aborted.
  - partial start is rolled back (stop started components in reverse order).

## Test Strategy

1. Unit tests:
  - profile config load/save/migration
  - runtime manager concurrent profile handling
  - Tavily key failover behavior parity with Python logic
2. Integration tests:
  - run multiple profiles in parallel
  - stop one profile without affecting others
3. View-model tests:
  - per-profile start/stop state transitions
  - Tavily tab validation and action flows

## Rollout Plan

1. Introduce profile config + migration.
2. Introduce runtime manager and runtime instances.
3. Add Tavily proxy host and Tavily tab.
4. Update existing screens to profile-scoped views.
5. Regression test and publish portable build.

## Risks & Mitigations

1. Risk: large refactor touches runtime + UI simultaneously.
   - Mitigation: keep compatibility shims and migrate screen-by-screen.
2. Risk: port-binding conflicts with multiple profiles.
   - Mitigation: strict pre-start validation across profile ports.
3. Risk: Tavily stream response behavior differs from Python version.
   - Mitigation: dedicated tests for SSE and retry switching logic.

