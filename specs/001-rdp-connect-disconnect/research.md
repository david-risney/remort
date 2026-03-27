# Research: RDP Connect & Disconnect

**Feature**: 001-rdp-connect-disconnect
**Date**: 2026-03-22

## R1: MVVM Pattern for WindowsFormsHost Hosting

**Context**: The MsRdpClient ActiveX control requires `AxHost` (WinForms), hosted in WPF via `WindowsFormsHost`. This is inherently a view-layer concern — the ViewModel cannot create or own UI controls.

**Decision**: The `MainWindow.xaml.cs` code-behind creates the `RdpClientHost` instance, sets it as `WindowsFormsHost.Child`, wraps it in `IRdpClient`, and passes it to the ViewModel. This is framework wiring, not business logic — consistent with Constitution Principle I (MVVM-First).

**Rationale**: `WindowsFormsHost.Child` is a WinForms `Control` — it must be set in the view layer. The ViewModel sees only `IRdpClient` (a plain interface). No WPF or WinForms types leak into the ViewModel.

**Alternatives considered**:
- Service locator / DI container for IRdpClient → Rejected. Adds unnecessary infrastructure for a single-window app. Violates Principle VII (Simplicity).
- Attached behavior to set WindowsFormsHost.Child from XAML → Rejected. Over-engineered for a single usage. The control is not data-bound; it's created once at startup.

## R2: IRdpClient Interface vs. Separate Service Layer

**Context**: The spec requires Connect, Disconnect, and connection state observation. Should the ViewModel call an `IRdpClient` interface directly, or should there be an intermediate `IConnectionService`?

**Decision**: ViewModel consumes `IRdpClient` directly. No intermediate service for this feature.

**Rationale**: `IRdpClient` already abstracts a single RDP connection — it exposes Connect/Disconnect, properties (Server, DesktopWidth/Height), and CLR events (Connecting, Connected, Disconnected). Adding a `ConnectionService` wrapper would be a pass-through with no added value. Per Principle VII, we defer additional abstraction until a concrete requirement demands it (e.g., retry logic in spec 003, multi-session support).

**Alternatives considered**:
- `IConnectionService` wrapping `IRdpClient` → Rejected. Pure pass-through today. Can be introduced later when retry/reconnect logic justifies it.

## R3: Connection State Management

**Context**: The spec defines states: Disconnected, Connecting, Connected. The RdpClientHost raises events (OnConnecting, OnConnected, OnDisconnected) that drive state transitions.

**Decision**: `ConnectionState` enum in the `Connection/` folder. The ViewModel owns a `ConnectionState` property, updated in response to `IRdpClient` events. State drives UI element enabled/disabled and status text via data binding.

**Rationale**: A simple enum + event-driven transitions is the lightest correct approach. No state machine framework needed — there are only 3 states and transitions are directly mapped from COM events.

**Alternatives considered**:
- Full state machine library (Stateless, etc.) → Rejected. 3 states, linear transitions. Over-engineering.
- Boolean flags (`IsConnecting`, `IsConnected`) → Rejected. Mutually exclusive states are better represented by an enum.

## R4: COM Event Threading

**Context**: MsRdpClient ActiveX events fire on the UI thread (STA COM apartment). The `RdpClientHost` events propagate to ViewModel handlers.

**Decision**: No thread marshaling needed. Events arrive on the UI thread because the `AxHost` control lives in the WinForms message loop, which runs on the WPF dispatcher thread (shared via `WindowsFormsHost`). ViewModel property updates from event handlers will naturally notify the UI thread.

**Rationale**: Verified in the POC — all events fire on the UI thread. `CommunityToolkit.Mvvm` `[ObservableProperty]` raises `PropertyChanged` synchronously, which is correct for UI-thread updates.

**Alternatives considered**:
- Dispatcher.Invoke in event handlers → Rejected. Unnecessary since events are already on the UI thread. Would add latency and complexity.

## R5: Splitting COM Interop Types Across Files

**Context**: The POC keeps all COM types (`RdpClientHost`, `IMsTscAxEvents`, `MsTscAxEventsSink`, `EventSinkCookie`, event args) in a single file. The production project has StyleCop SA1402 (one type per file) with `TreatWarningsAsErrors`.

**Decision**: Split into 5 files in `Interop/`: `RdpClientHost.cs`, `IMsTscAxEvents.cs`, `MsTscAxEventsSink.cs`, `EventSinkCookie.cs`, `RdpEventArgs.cs`. The event args file contains the 3 small `EventArgs` subclasses (suppress SA1402 for this file with justification comment, as they are trivial related types).

**Rationale**: Compliance with StyleCop and Zero Warnings principle. The types are in the same `Interop/` folder and `Remort.Interop` namespace, preserving logical cohesion.

**Alternatives considered**:
- Single file with SA1402 suppression → Rejected. Suppressing for 5+ types is excessive. The constitution requires justification for every suppression.
- One file per EventArgs class → Rejected. Three 1-line classes in 3 files is noise. Grouping small related types with a single suppression is pragmatic.

## R6: CommunityToolkit.Mvvm Package Version

**Context**: The project needs CommunityToolkit.Mvvm for source generators (`[ObservableProperty]`, `[RelayCommand]`).

**Decision**: Use `CommunityToolkit.Mvvm` version 8.* (latest stable). Add as `<PackageReference>` in `Remort.csproj`.

**Rationale**: Version 8.x is the current stable release for .NET 8. Source generators work at compile time with zero runtime overhead. No other MVVM toolkit packages needed.

## R7: Application Shutdown with Active Session

**Context**: FR-011 requires disconnecting the session when the user closes the window while connected.

**Decision**: Override `MainWindow.OnClosing` → if ViewModel reports connected state, call `DisconnectCommand.Execute(null)`. This is a view-layer concern (window lifecycle event). The actual disconnect logic is in the ViewModel/IRdpClient chain.

**Rationale**: `Window.Closing` is a framework event that must be handled in code-behind. Calling the ViewModel's disconnect command keeps the logic consistent. The COM control's `Disconnect()` is synchronous, so this works without async complexity.

**Alternatives considered**:
- `Application.Exit` event in App.xaml.cs → Rejected. Window-level handling is more targeted and testable.
- Behaviors/triggers for Window.Closing → Rejected. Over-engineered for a single handler. Principle VII.
