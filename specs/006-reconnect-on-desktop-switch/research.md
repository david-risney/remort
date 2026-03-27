# Research: Reconnect on Virtual Desktop Switch

**Feature**: 006-reconnect-on-desktop-switch
**Date**: 2026-03-23

## R1: Detecting Virtual Desktop Switches — Approach Selection

**Context**: The spec requires that Remort detects when the user switches to the virtual desktop where the window is pinned (FR-001). Windows does not provide a documented event for "virtual desktop changed". How should the app detect this transition?

**Decision**: Poll `IVirtualDesktopManager.IsWindowOnCurrentVirtualDesktop` using a `System.Windows.Threading.DispatcherTimer` at a fixed interval (500ms). Track the previous result. When the result transitions from `false` → `true`, raise a `SwitchedToDesktop` event. Encapsulate this in a `DesktopSwitchDetector` service behind an `IDesktopSwitchDetector` interface.

**Rationale**: This approach uses only the documented `IVirtualDesktopManager` COM interface, which the project already depends on (feature 005). The polling cost is negligible — one COM call every 500ms (in-process, completes in microseconds). The poll interval naturally debounces rapid desktop switches (FR-013): if a user switches through three desktops in under 500ms, only the final landing desktop triggers a check. The `DispatcherTimer` runs on the UI thread, eliminating thread-safety concerns for ViewModel property updates.

**Alternatives considered**:
- **`IVirtualDesktopNotification` COM event interface** → Rejected. This is an undocumented COM interface whose GUIDs change between Windows 10 and 11 builds (and between Windows 11 updates). Feature 005 research (R1) already established the policy of using only documented APIs. Using this would introduce fragile version-specific GUID tables.
- **`SetWinEventHook` with `EVENT_SYSTEM_DESKTOPSWITCH`** → Rejected. This Win32 accessibility event exists but is poorly documented for virtual desktop detection. It fires for RDP desktop switches (session 0 → session N), not necessarily for Windows 10/11 virtual desktop switches. Requires P/Invoke (`SetWinEventHook`, `UnhookWinEvent`) and a callback delegate that must be kept alive. More complex than polling with no reliability advantage.
- **`WM_ACTIVATE` / `WM_ACTIVATEAPP` via `HwndSource.AddHook`** → Rejected. These window messages fire when the window gains or loses focus, not specifically on desktop switches. A window on a desktop that becomes visible but is not focused would not receive `WM_ACTIVATE`. Also fires for unrelated focus changes (e.g., Alt-Tab within the same desktop), producing false positives.
- **`IVirtualDesktopManager.GetWindowDesktopId` timestamp comparison** → Rejected. The desktop ID doesn't change when the user switches desktops — the ID is the desktop's identity, not the "current" desktop. Only `IsWindowOnCurrentVirtualDesktop` answers the right question.
- **No polling — check on `Window.Activated` event** → Partially viable but insufficient. `Window.Activated` fires when the WPF window gains focus, which sometimes coincides with a desktop switch. However, the spec says "Remort does not need to be in the foreground" (Edge Cases). A desktop switch where Remort is not the focused app would be missed.

## R2: Polling Interval and Debounce Strategy

**Context**: FR-013 requires debouncing rapid desktop switch events. What polling interval balances responsiveness with resource efficiency? Does the spec's debounce requirement need additional logic beyond the poll interval?

**Decision**: Use a fixed 500ms polling interval. No additional debounce logic is needed — the poll interval itself is the debounce. The detector only fires when it observes a `false → true` transition, so rapid `false → false → ... → true` sequences (the user switching through multiple desktops) naturally collapse to a single event on the final landing desktop.

**Rationale**: At 500ms, the maximum detection latency is 500ms after the desktop switch completes. This is imperceptible for a reconnection scenario (the RDP connection itself takes seconds). A shorter interval (e.g., 100ms) would detect faster but polls 5x more for marginal benefit. A longer interval (e.g., 2s) would feel sluggish. 500ms is the sweet spot. The transition-based event (`false → true` only) means even if the user stays on the pinned desktop, no duplicate events fire.

**Alternatives considered**:
- **Explicit debounce timer on top of polling** → Rejected. The polling interval already provides debounce. Adding a second timer layer adds complexity for no benefit. (Constitution VII — Simplicity)
- **100ms polling** → Rejected. 5x the COM calls for a scenario where sub-second latency is unnecessary (reconnection takes seconds). Over-optimization.
- **Adaptive polling (faster when disconnected, slower when connected)** → Rejected. Premature optimization. The cost of one COM call per 500ms is negligible regardless of connection state. Can be revisited if profiling shows otherwise.

## R3: Service Design — IDesktopSwitchDetector vs. Extending IVirtualDesktopService

**Context**: The polling/detection logic needs to live somewhere. Should it be added to the existing `IVirtualDesktopService` or be a separate service?

**Decision**: Create a new `IDesktopSwitchDetector` interface and `DesktopSwitchDetector` class in the `VirtualDesktop/` domain folder. The detector depends on `IVirtualDesktopService` for the `IsOnCurrentDesktop()` call. It is a separate service from `IVirtualDesktopService`.

**Rationale**: Single Responsibility — `IVirtualDesktopService` manages pinning (a one-shot operation). `IDesktopSwitchDetector` manages ongoing monitoring (a lifecycle concern with start/stop semantics). Combining them would bloat the interface and make it harder to test either concern independently. The ViewModel already takes multiple service dependencies via constructor injection — adding one more is consistent with the existing pattern. Constitution III (Test-First) is the primary justification: tests for reconnect-on-switch behavior need to simulate desktop switches by raising events on a mock `IDesktopSwitchDetector`, independent of the pinning logic in `IVirtualDesktopService`.

**Alternatives considered**:
- **Add monitoring to `IVirtualDesktopService`** → Rejected. Mixes two concerns (pin management and ongoing detection). Tests for pinning would need to deal with monitoring methods, and vice versa.
- **Put polling logic directly in code-behind** → Rejected. Too much logic for code-behind (tracking state transitions, managing timer lifecycle). Code-behind should be a thin wire, not a state machine.
- **Put polling logic in ViewModel** → Rejected. The ViewModel would need to reference `DispatcherTimer` (a WPF type), violating Constitution I (MVVM-First: "ViewModels MUST NOT reference WPF types").

## R4: Adding IsOnCurrentDesktop to IVirtualDesktopService

**Context**: The `DesktopSwitchDetector` needs to check whether the window is on the current virtual desktop. The underlying `IVirtualDesktopManager.IsWindowOnCurrentVirtualDesktop` COM call exists but isn't exposed through `IVirtualDesktopService` yet. Should it be added?

**Decision**: Add `bool IsOnCurrentDesktop(IntPtr hwnd)` to the existing `IVirtualDesktopService` interface. The implementation wraps the COM call with the same `COMException`-catching pattern as existing methods. Returns `true` if COM is unavailable (graceful degradation — don't block reconnect on unsupported systems).

**Rationale**: This is a natural extension of the existing interface. The method wraps a COM call behind a testable interface, consistent with Constitution II (COM Interop Isolation). The `DesktopSwitchDetector` calls this method instead of reaching through to the COM layer. Future features (spec 007 — quick desktop switch) may also use this method.

**Alternatives considered**:
- **Detector calls `IVirtualDesktopManager` directly** → Rejected. Bypasses the service layer, violating Constitution VI (Layered Dependencies). Also makes detector tests harder (would need to mock COM interfaces instead of the simpler service interface).
- **Create a separate `IDesktopQuery` interface** → Rejected. Over-abstraction for one method. `IsOnCurrentDesktop` fits naturally with the existing pin/unpin methods on `IVirtualDesktopService`. (Constitution VII — Simplicity)

## R5: ViewModel Integration — TryReconnectOnDesktopSwitch Pattern

**Context**: How should the ViewModel handle the desktop switch event? The existing `TryAutoReconnect()` method (from spec 004) handles auto-reconnect on login. Should the desktop switch reconnect reuse the same method?

**Decision**: Add a new `TryReconnectOnDesktopSwitch()` method to `MainWindowViewModel`, separate from `TryAutoReconnect()`. The method checks:
1. `ReconnectOnDesktopSwitchEnabled` is `true`
2. `PinToDesktopEnabled` is `true` (FR-004)
3. `ConnectionState` is `Disconnected` (FR-002)
4. A `LastConnectedHost` exists in settings (FR-003)
5. No connection is already in progress (FR-002)

If all conditions pass, it sets `_isAutoReconnect = true`, loads the last host, and calls `_connectionService.Connect()`. The existing `_isAutoReconnect` flag causes status text to show "Auto-reconnecting to host…" (reusing the spec 004 status pattern for FR-011).

**Rationale**: A separate method from `TryAutoReconnect()` is cleaner because the precondition checks differ (desktop switch requires `PinToDesktopEnabled` and `ReconnectOnDesktopSwitchEnabled`; login reconnect only requires `AutoReconnectEnabled`). However, the common connect-last-host logic is factored into a shared private helper `ReconnectToLastHost()` to avoid duplication. The `_isAutoReconnect` flag is reused because the status text and failure handling are identical — the user sees "Auto-reconnecting…" regardless of what triggered it.

**Alternatives considered**:
- **Reuse `TryAutoReconnect()` with a parameter** → Rejected. The precondition sets are different. A parameter would require the callers to know which conditions to skip, leaking internal logic.
- **Separate `_isDesktopSwitchReconnect` flag for distinct status text** → Considered but deferred. FR-011 says "indicate that an automatic reconnection is in progress" — the existing "Auto-reconnecting to host…" text satisfies this. If distinct text is needed later, it's a trivial change.
- **Dedicated reconnect service** → Rejected. The reconnect logic is ViewModel-level decision-making (which settings are enabled, what state we're in). Pushing it to a service would require the service to know about settings and connection state, duplicating the ViewModel's responsibilities.

## R6: Settings Integration — ReconnectOnDesktopSwitchEnabled

**Context**: The spec requires a persisted toggle (FR-005, FR-006, FR-007, FR-014). How should it integrate with the existing `AppSettings`?

**Decision**: Add `ReconnectOnDesktopSwitchEnabled` (`bool`, default `false`) to the existing `AppSettings` record. The ViewModel reads it on startup and persists changes via `ISettingsStore`, using the same pattern as `AutoReconnectEnabled` and `PinToDesktopEnabled`. The `partial void OnReconnectOnDesktopSwitchEnabledChanged(bool value)` method handles persistence and reacts to changes immediately (FR-014).

**Rationale**: Identical pattern to the existing settings properties. No new infrastructure needed. (Constitution VII — Simplicity)

**Alternatives considered**:
- None — this is the established pattern and the only reasonable approach.

## R7: UI — Toggle Dependency on PinToDesktop

**Context**: FR-008 requires that the reconnect-on-desktop-switch toggle is visually disabled or accompanied by a hint when `PinToDesktopEnabled` is `false`. How should this be implemented?

**Decision**: In the XAML, the reconnect-on-desktop-switch `CheckBox` binds its `IsEnabled` property to `PinToDesktopEnabled`. When pinning is off, the checkbox is grayed out and non-interactive. A `ToolTip` on the checkbox says "Requires 'Pin to desktop' to be enabled". This is the simplest approach — no custom controls or validation logic.

**Rationale**: WPF's built-in `IsEnabled` binding handles the visual state (grayed out) automatically. The ToolTip provides the hint required by FR-008. The ViewModel doesn't need special logic — the XAML binding is sufficient. If the user disables pinning while reconnect-on-switch is enabled, the checkbox becomes disabled but the setting value is preserved (it just won't trigger because `TryReconnectOnDesktopSwitch()` checks `PinToDesktopEnabled` as a precondition).

**Alternatives considered**:
- **Auto-disable the setting value when pin is disabled** → Rejected. The spec says the toggle should be "visually disabled or accompanied by a hint" (FR-008), not that the value should be cleared. Preserving the value means re-enabling pin-to-desktop automatically re-activates reconnect-on-switch without the user having to toggle it again.
- **Hide the checkbox entirely** → Rejected. Users wouldn't discover the feature. A disabled-with-tooltip approach is more discoverable.

## R8: Detector Lifecycle — Start/Stop Monitoring

**Context**: When should the `DesktopSwitchDetector` start and stop polling?

**Decision**: The detector is created during `MainWindow` construction (alongside other services). Monitoring starts in `OnSourceInitialized` (after the HWND is available) and stops in `OnClosing`. The detector implements `IDisposable` to clean up the timer. The code-behind calls `StartMonitoring(_hwnd)` and `StopMonitoring()` / `Dispose()`.

The detector should only actively poll when monitoring is meaningful — i.e., when the window is pinned. The ViewModel can call `StartMonitoring()` / `StopMonitoring()` based on whether `PinToDesktopEnabled` is `true`. However, to keep code-behind simple, the detector itself can internally no-op the timer tick when `IVirtualDesktopService.IsSupported` is false or the hwnd is zero. The simplest approach: always run the timer, but the tick handler returns early when preconditions aren't met.

**Rationale**: Matches the lifecycle pattern of `SystemEvents.SessionSwitch` (subscribe in constructor, unsubscribe in `OnClosing`). The timer's 500ms tick is negligible even when idle. Avoiding conditional start/stop reduces edge cases around enable/disable ordering.

**Alternatives considered**:
- **Start/stop timer based on `ReconnectOnDesktopSwitchEnabled` and `PinToDesktopEnabled`** → Rejected for initial implementation. Adds lifecycle complexity (what if the user toggles rapidly?). The always-running timer with early-return is simpler and the cost is effectively zero. Can be optimized later if profiling shows otherwise.
- **Lazy initialization on first enable** → Rejected. Adds conditional logic for negligible savings.

## R9: Interaction with Auto-Reconnect on Login (Spec 004)

**Context**: The spec edge case states: "If both events occur simultaneously (e.g., logging in directly to the pinned desktop), only one connection attempt is initiated — the first event to fire claims the connection, and the second observes an in-progress connection and does nothing." How is this ensured?

**Decision**: Both `TryAutoReconnect()` and `TryReconnectOnDesktopSwitch()` check `ConnectionState == Disconnected` as their first precondition. Since both methods and `IConnectionService.Connect()` run on the UI thread (ensured by `Dispatcher.Invoke` for SessionSwitch, and `DispatcherTimer` for desktop switch), there is no race condition. The first method to execute transitions the state to `Connecting`, and the second method's precondition check fails.

**Rationale**: The existing single-threaded UI model handles this naturally. No explicit mutex, semaphore, or "connection lock" is needed. This is a non-issue in practice because both triggers run on the same dispatcher thread.

**Alternatives considered**:
- **Explicit `_isReconnecting` flag** → Rejected. Redundant with `ConnectionState != Disconnected` check. Adding a flag would be defensive coding for a scenario that can't happen due to single-threaded execution. (Constitution VII — Simplicity)
