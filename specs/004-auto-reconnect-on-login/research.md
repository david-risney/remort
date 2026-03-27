# Research: Auto-Reconnect on Windows Login

**Feature**: 004-auto-reconnect-on-login
**Date**: 2026-03-23

## R1: Detecting Windows Session Unlock/Login Events

**Context**: The spec requires auto-reconnect when the user logs in to Windows, including unlock, sign-in after lock, and sign-in after restart (spec Assumptions). How does a .NET WPF app detect these events?

**Decision**: Use `Microsoft.Win32.SystemEvents.SessionSwitch` with `SessionSwitchReason.SessionUnlock` and `SessionSwitchReason.SessionLogon`. Subscribe in `MainWindow.xaml.cs` code-behind and delegate to a ViewModel method.

**Rationale**: `SystemEvents.SessionSwitch` is a built-in .NET API (part of `Microsoft.Win32` in the BCL — not a Windows-only NuGet package). It fires for all the scenarios in the spec: workstation unlock, logon after lock screen, and logon after restart. It works reliably in WPF desktop apps because WPF has a message pump. No P/Invoke, no WMI, no Windows Service dependency. The event handler in code-behind is consistent with other platform-event wiring in the app (e.g., `SizeChanged`, `OnClosing`).

**Alternatives considered**:
- `WM_WTSSESSION_CHANGE` via `HwndSource.AddHook` → Rejected. Requires manual Win32 message parsing and `WTSRegisterSessionNotification` P/Invoke. More complex for the same result. `SystemEvents.SessionSwitch` wraps this internally.
- WMI event subscription (`Win32_LogonSession`) → Rejected. WMI adds significant complexity and latency. Designed for service/monitoring scenarios, not desktop apps.
- `Application.SessionEnding` → Wrong event. `SessionEnding` fires when the user is logging OFF, not logging on. It's for shutdown cleanup.
- Background service with named pipe → Rejected. Massively over-engineered. The app must already be running (spec Assumption). A simple event subscription suffices.

## R2: Where to Subscribe to SessionSwitch — Code-Behind vs. ViewModel

**Context**: `SystemEvents.SessionSwitch` is a static .NET event, not a WPF/XAML binding. Where should the subscription live?

**Decision**: Subscribe in `MainWindow.xaml.cs` code-behind. The handler calls `_viewModel.TryAutoReconnect()` — a parameterless method that encapsulates the full reconnect decision (check enabled, check last host, check state, initiate connect).

**Rationale**: Constitution I (MVVM-First) says code-behind may contain "framework-mandated event wiring." `SystemEvents.SessionSwitch` is a platform event that cannot be bound via XAML. This is analogous to the existing `SizeChanged` subscription in `MainWindow.xaml.cs`. The decision logic (should we reconnect?) lives entirely in the ViewModel method, which is independently testable. The code-behind is a one-line delegate.

**Alternatives considered**:
- ViewModel subscribes to `SystemEvents.SessionSwitch` directly → Rejected. The ViewModel would reference `Microsoft.Win32.SystemEvents`, which is a platform/BCL type. While not technically a WPF type, it creates a platform coupling in the ViewModel that makes testing harder (static event, can't mock). The code-behind-to-ViewModel delegation pattern is cleaner.
- Introduce an `ISessionMonitor` interface → Rejected. One event subscription with one consumer does not justify an abstraction. The ViewModel method `TryAutoReconnect()` is the testable seam. (Constitution VII — Simplicity)

## R3: Persisting Last Connected Host — Where and When

**Context**: The spec requires remembering the last connected host (FR-001, FR-012). Where should it be stored, and when should it be updated?

**Decision**: Add `LastConnectedHost` (string, default empty) to the existing `AppSettings` record and persist via the existing `JsonSettingsStore`. Update the value in two places in the ViewModel:
1. In `ConnectDirect()` — when a direct hostname connection starts.
2. In `ConnectToDevBoxAsync()` — after Dev Box resolution succeeds, store the resolved server hostname.

The host is saved when the connection is *initiated* (not on successful connect), because the spec says "last attempted host" (FR-001: "last successfully connected or last attempted host").

**Rationale**: Reusing `AppSettings` and `JsonSettingsStore` avoids any new persistence infrastructure. The settings file already exists and the ViewModel already has an `ISettingsStore` reference. Adding one more field to the record and one more `Save()` call is the minimal change. (Constitution VII — Simplicity)

**Alternatives considered**:
- Separate file for last host → Rejected. The app already has a settings.json. Adding a second file for one string is unnecessary.
- Save only on successful connect → Rejected. The spec says "last successfully connected or last attempted host" (FR-001). Saving on initiation covers both cases and is simpler (one save point per connect path).
- Save the `Hostname` field (user input) vs. the resolved server → Decision: save the resolved server (for Dev Box connections, this is the FQDN). The `Hostname` field may contain a Dev Box short name that requires resolution. On auto-reconnect, we want to skip resolution and connect directly to the previously resolved address. However, we also store the display name (the original `Hostname` text) so the UI can show a meaningful status message ("Auto-reconnecting to mydevbox…").

## R4: Auto-Reconnect Status Text — Distinguishing from Manual

**Context**: Story 3 requires that auto-reconnect shows "Auto-reconnecting to hostname…" so the user understands the connection was automatic (FR-010).

**Decision**: Add a boolean flag `_isAutoReconnect` in the ViewModel. Set it to `true` in `TryAutoReconnect()` before calling connect, and reset it to `false` in manual `ConnectAsync()`. The `OnAttemptStarted` event handler checks the flag to format status text as either "Auto-reconnecting to host… (attempt N of M)" or "Connecting… (attempt N of M)".

**Rationale**: A simple boolean flag is sufficient because there's only one connection at a time (spec constraint). No need for a connection-source enum or command pattern. The flag is internal to the ViewModel and doesn't leak into the service layer.

**Alternatives considered**:
- Pass a "source" parameter through `IConnectionService.Connect()` → Rejected. Would change the service interface for a purely UI concern. The service doesn't care why the connection was initiated.
- Separate auto-reconnect status messages in the service → Rejected. The service is connection-lifecycle only. Status text formatting is a ViewModel responsibility (Constitution I — MVVM-First).

## R5: Handling SessionSwitch Thread Safety

**Context**: `SystemEvents.SessionSwitch` can fire on a background thread. WPF ViewModel property changes and `IConnectionService.Connect()` must happen on the UI thread.

**Decision**: In the code-behind handler, dispatch to the UI thread via `Dispatcher.Invoke()` before calling `_viewModel.TryAutoReconnect()`. This guarantees all ViewModel property changes and service calls happen on the UI thread, consistent with the existing connection flow.

**Rationale**: `SystemEvents.SessionSwitch` documentation states the event fires on the thread that registered it IF the registering thread has a message pump, which is true for the WPF UI thread. However, to be defensive, wrapping in `Dispatcher.Invoke()` is a one-line safeguard that costs nothing and prevents subtle threading bugs if the subscription order changes.

**Alternatives considered**:
- Trust that it fires on the UI thread → Risky. Documentation is ambiguous about timing during logon. `Dispatcher.Invoke()` is cheap insurance.
- Use `SynchronizationContext.Post()` → Equivalent but less idiomatic in WPF. `Dispatcher.Invoke()` is the standard WPF pattern.

## R6: Auto-Reconnect Toggle UI Placement

**Context**: The spec requires a toggle to enable/disable auto-reconnect (Story 2). Where should it live in the UI?

**Decision**: Add a `CheckBox` in the connection bar area (same `StackPanel` that holds the hostname, Connect/Disconnect buttons, and max retries input). Label: "Reconnect on sign-in". Binds to `MainWindowViewModel.AutoReconnectEnabled`.

**Rationale**: Consistent with the existing inline settings pattern established by spec 003 (max retry count is inline in the connection bar). A settings page is premature for two settings. The checkbox is discoverable (visible on the main window) and requires one click (SC-003: "under 3 clicks"). (Constitution VII — Simplicity)

**Alternatives considered**:
- Settings flyout/dialog → Rejected. Premature for two settings. Can be introduced when the settings list grows.
- Menu bar option → Rejected. Remort has no menu bar. Adding one for a single toggle is disproportionate.
- System tray right-click option → Rejected. Remort has no system tray icon. Out of scope.

## R7: Settings Field Names — LastConnectedHost vs. LastServer

**Context**: What should the persisted setting be named?

**Decision**: Two new fields on `AppSettings`:
- `AutoReconnectEnabled` (`bool`, default `false`) — matches the spec terminology "auto-reconnect enabled/disabled"
- `LastConnectedHost` (`string`, default `""`) — matches FR-001 "last connected host"

**Rationale**: Names match the spec language directly, making the mapping from requirements to code obvious. `Host` rather than `Server` because the spec uses "host" throughout (and the user-facing text uses "host"). The internal `IConnectionService.Server` property is a different concern (it's the resolved hostname for the RDP control).

**Alternatives considered**:
- `LastServer` → Rejected. The spec says "host" consistently.
- `ReconnectOnLogin` → Rejected. `AutoReconnectEnabled` is more descriptive and matches spec terminology.

## R8: Unsubscribing from SessionSwitch

**Context**: Should the app unsubscribe from `SystemEvents.SessionSwitch` on window close?

**Decision**: Yes. Unsubscribe in `MainWindow.OnClosing()` to prevent the static event from holding a reference to the closed window. This is a standard `SystemEvents` best practice to avoid memory leaks.

**Rationale**: `SystemEvents` uses static event handlers. If the window closes but the handler remains subscribed, the static event keeps the `MainWindow` instance alive (GC root). Unsubscribing in `OnClosing()` is trivial (one line) and prevents the leak.

**Alternatives considered**:
- Don't unsubscribe (app is exiting anyway) → Risky. If the Window closes but the Application continues (edge case), the leak is real. One line of cleanup is worth it.
