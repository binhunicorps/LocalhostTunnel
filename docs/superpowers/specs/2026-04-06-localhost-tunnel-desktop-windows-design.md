# Localhost Tunnel Desktop Windows Design

**Date:** 2026-04-06  
**Status:** Approved in conversation, written for review  
**Audience:** Implementation work for converting the current Node + React web utility into a native Windows desktop application

## Goal

Build a standalone Windows `.exe` desktop application for Localhost Tunnel with a native GUI, optimized for technical users who want fast operational control, clear runtime visibility, and background tray behavior without relying on a browser-based localhost dashboard.

## Approved Product Decisions

- Platform: `.NET 8` + `WPF`
- App form: standalone Windows desktop app, packaged as a native `.exe`
- User persona: technical user
- Close behavior: when the user presses `X`, show a confirmation dialog instead of exiting immediately
- Preferred shell layout: `A. Command Center`
- Technical direction: port core runtime behavior away from the current Node-hosted web app into native desktop services rather than wrapping the current web UI in a shell

## Product Principles

- The app must feel like a Windows utility, not a browser page inside a desktop wrapper.
- The main window must expose operational state quickly: tunnel state, forwarder state, uptime, routing path, and recent logs.
- Common actions must be fast: start, stop, inspect status, open logs, and edit config.
- Advanced configuration stays available because the primary user is technical.
- Runtime services must continue working correctly when the main window is hidden to the system tray.

## Non-Goals

- Do not preserve the current browser-based localhost UI architecture.
- Do not optimize for non-technical users via a full onboarding wizard.
- Do not replace `cloudflared` with a custom tunnel implementation.
- Do not depend on a local HTTP API and browser session for routine operation.

## Current System Context

The current project is a Node/CommonJS application with:

- `src/` containing the forwarder runtime
- `server/` exposing a local Express server for API and WebSocket updates
- `ui/` containing the current React/Vite dashboard
- `config.json` as the current runtime config file

The current experience is web-first: start the backend, open a browser, then manage the forwarder and logs through the web UI. The desktop redesign replaces that model with a native Windows app and native process orchestration.

## Proposed Architecture

The desktop app will be split into four layers.

### 1. Desktop Shell

Responsibilities:

- Single-instance app startup
- Main window creation and restore behavior
- System tray icon and menu
- Close confirmation dialog
- Startup/minimized behavior
- Desktop notifications

Technology direction:

- `WPF`
- `MVVM`
- Avoid business logic in `Window.xaml.cs`

### 2. Application Layer

Responsibilities:

- Coordinate runtime services
- Expose observable state for the UI
- Map user actions to service operations
- Translate infrastructure failures into user-facing states

Primary services:

- `ForwarderService`
- `TunnelService`
- `ConfigService`
- `LogStreamService`
- `UpdateService`
- `AppLifecycleService`

### 3. Core / Domain

Responsibilities:

- Config models and validation rules
- Runtime state models
- State transitions for forwarder and tunnel
- Health evaluation
- Operational policies such as retry/backoff, shutdown rules, and validation outcomes

This layer must stay UI-agnostic.

### 4. Infrastructure

Responsibilities:

- Launch and monitor `cloudflared`
- Run the HTTP forwarding listener
- Read and write config files
- Persist session preferences
- Write rolling log files
- Package and apply updates
- Manage filesystem paths under Windows app-data locations

## Key Technical Decisions

### Native core instead of embedded Node runtime

The current Node-hosted runtime will not remain the main engine. Forwarder behavior will be ported into C# so the application is a native Windows utility rather than a browser stack packaged as desktop.

### `cloudflared` remains an external managed binary

The desktop app will still download, install, and run `cloudflared` as an external executable. This is the practical and low-risk choice because the tunnel behavior already depends on Cloudflare’s official binary.

### Event-driven UI state

The WPF UI will not talk to a localhost REST API or WebSocket server. Instead, runtime services will publish application events and state snapshots directly to view models.

### Persistent storage split

User config, UI/session state, and logs must be separated:

- `config.json`: runtime settings
- `session.json`: UI preferences such as last active screen, window bounds, auto-scroll preference
- rolling log files: operational and crash logs

### Windows-standard storage locations

Runtime data should move to a Windows app-data path such as:

- `%AppData%/LocalhostTunnel/` or `%LocalAppData%/LocalhostTunnel/`

The exact folder choice should be standardized during implementation, but it must no longer depend on the project root being writable.

## UI / UX Design

### Main shell: Command Center

The approved shell is a compact command-center layout:

- Narrow sidebar for top-level navigation
- Status/summary strip near the top
- Main operational panel on the left
- Live log tail on the right

This gives the technical user a fast operational dashboard without turning the app into a cluttered admin console.

### Primary screens

#### 1. Overview

Purpose:

- Fast operational view
- Main control surface

Contents:

- Forwarder state
- Tunnel state
- Uptime
- Active ports / routing path
- Start / stop actions
- Health summary
- Short live log tail

The overview must answer “is it working?” within a glance.

#### 2. Configuration

Purpose:

- Edit and validate runtime settings

Structure:

- Quick setup at the top
- Advanced sections below for networking, security, logging, and update/auth settings

Expected UX:

- Inline validation
- Clear field help
- No mandatory wizard flow
- Optional actions such as “test config”, “open data folder”, or “locate cloudflared”

#### 3. Logs

Purpose:

- Deep runtime inspection

Features:

- Level filter
- Search
- Auto-scroll toggle
- Clear current view
- Copy selected row
- Export logs

The dashboard should show only a useful live tail. Full inspection belongs here.

#### 4. Updates

Purpose:

- Show current version and available updates
- Apply update workflow safely
- Display failures or rollback status

This screen can be lightweight if update operations are also surfaced via banner or notification.

#### 5. About / Diagnostics

Optional supporting screen or panel for:

- App version
- Data path
- Binary path
- Crash-log path
- Environment diagnostics

## Navigation Model

Recommended sidebar items:

- Overview
- Configuration
- Logs
- Updates
- Diagnostics

The sidebar should stay short. Avoid turning it into a settings tree.

## App Lifecycle

### Startup

On launch the app will:

1. Enforce single-instance behavior
2. Load persisted config and session state
3. Initialize log, config, tunnel, forwarder, and update services
4. Open the main window into `Overview`, unless the user has configured start-minimized behavior

### Start runtime

When the user presses `Start`:

1. Validate required configuration
2. Ensure `cloudflared` exists locally, downloading it if required
3. Start the tunnel process
4. Start the forwarder listener
5. Begin streaming state and log events into the UI

### Running state

Forwarder and tunnel status must be tracked independently. The app should distinguish:

- `Starting`
- `Running`
- `Stopping`
- `Stopped`
- `Faulted`
- `Degraded` where appropriate

This avoids flattening all non-healthy states into a vague “stopped” label.

### Close behavior

Pressing the window close button must show a dialog with clear, non-overlapping actions:

- `Run in tray`
- `Cancel`
- `Exit`

Definitions:

- `Run in tray`: hide the window, keep the desktop app and services available in the notification area
- `Cancel`: dismiss the dialog and keep the current window open
- `Exit`: stop app-managed services cleanly and terminate the process

### Tray behavior

The tray icon must support:

- Restore window
- Start / Stop
- Open logs
- Exit

Double-clicking the tray icon should restore the main window.

### Shutdown

On full exit:

1. Stop accepting new forwarded requests
2. Stop the forwarder
3. Stop the tunnel process
4. Flush buffered logs
5. Persist session state

If child processes do not stop within timeout, they may be terminated and the reason must be logged.

## Data Flow

The app uses an internal event-driven model.

High-level flow:

1. Infrastructure components emit state and log events
2. Application-layer services aggregate and normalize them
3. View models expose observable state to WPF views
4. UI actions dispatch commands back to application services

The app does not expose a localhost management API to its own UI.

## Config Model Direction

The desktop app should preserve conceptual compatibility with the current configuration model where useful:

- tunnel URL / token
- local listen host / port
- target host / port / protocol
- webhook secret
- max body size
- upstream timeout
- log level
- update/auth credentials if still needed

Migration helpers may import an existing project-root `config.json` into the new Windows app-data path.

## Logging Design

Log sources:

- Desktop app lifecycle
- Forwarder runtime
- Tunnel process stdout/stderr
- Validation and config changes
- Update workflow
- Crash/unhandled exception events

Log requirements:

- Structured internal representation
- Friendly UI formatting
- Rolling file retention
- Separate crash log when startup or runtime failure is severe

## Error Handling

### Configuration errors

- Block `Start` when required config is missing or invalid
- Highlight the failing fields directly in the configuration screen
- Use actionable wording rather than generic “invalid config” messages

### Port conflicts

- Detect conflicts before startup where possible
- Tell the user which port failed
- Suggest changing the port or stopping the conflicting process

### Tunnel failures

- If `cloudflared` cannot download, start, or authenticate, keep the app alive
- Surface failure clearly on the overview screen
- Record full detail in logs
- Offer retry and diagnostic actions

### Forwarding failures

- If the local target is down or slow, do not crash the app
- Mark the forwarder as degraded or faulted depending on severity
- Keep logging request failures for diagnosis

### Crash handling

- Catch and log unhandled exceptions
- On next launch, show a brief recovery notice with access to the crash log location

### Update failures

- Update flow must fail safely
- If update application fails, rollback or preserve the currently working version

## Testing Strategy

### Unit tests

Cover:

- Config validation
- Runtime state transitions
- Tray close-decision logic
- Logging formatting and retention policy
- Health evaluation rules

### Integration tests

Cover:

- Start/stop forwarder
- Start/stop and monitor `cloudflared`
- Forward request flow to a local target
- Config persistence under app-data paths
- Shutdown behavior across services

### UI tests

Cover:

- Overview state updates
- Config validation feedback
- Close dialog choices
- Tray restore and exit behavior

### Manual acceptance tests

Cover:

- Fresh install
- First run
- Start minimized
- Run in tray
- Restart after config change
- Port conflict
- Target app offline
- Tunnel auth/download failure
- Update workflow

## Migration Scope

### Reuse

- Existing product concepts
- Existing config schema concepts
- Existing operational flows: start, stop, log, configure, update

### Rewrite

- Web dashboard UI
- Local Express management server
- Browser/WebSocket-based control surface
- Node-hosted forwarder runtime as the primary app engine

### Keep external

- `cloudflared` binary and Cloudflare tunnel model

## Delivery Shape

The desktop app should be delivered as a packaged Windows application that:

- launches as a native GUI app
- does not require the user to open a browser
- can run as a tray utility
- can manage its own runtime dependencies and config paths

## Open Implementation Considerations

These are not product blockers, but they should be resolved in the implementation plan:

- exact WPF styling approach and theme system
- installer technology
- update distribution mechanism
- import path for legacy `config.json`
- whether target diagnostics include local health checks

## Success Criteria

The redesign is successful when:

- the app runs as a standalone Windows desktop utility
- the main operational workflow no longer depends on a browser or localhost UI port
- forwarder and tunnel control are stable across repeated start/stop cycles
- tray behavior is reliable and understandable
- config and logs survive restart
- failures are diagnosable from inside the app and from exported logs
