# Research: Cap Connection Retries

**Feature**: 003-cap-connection-retries
**Date**: 2026-03-23

## R1: Retry Orchestration — Service Layer vs. ViewModel Logic

**Context**: The spec requires retry logic on connection failure. Currently, `MainWindowViewModel` calls `IRdpClient.Connect()` directly and handles the `Disconnected` event. Where should retry counting and re-connect logic live?

**Decision**: Introduce `IConnectionService` / `RetryingConnectionService` that wraps `IRdpClient`. The service intercepts `IRdpClient.Disconnected` events, tracks attempt counts, and re-invokes `Connect()` up to the configured limit. It raises its own events (`AttemptStarted`, `RetriesExhausted`, `Connected`, `Disconnected`) that the ViewModel observes.

**Rationale**: Spec 001 research (R2) explicitly anticipated this: _"Can be introduced later when retry/reconnect logic justifies it."_ Retry counting, re-invocation, and cancellation are non-trivial state management that would clutter the ViewModel. The service is synchronous from the ViewModel's perspective — it raises events, just like `IRdpClient`. This keeps the ViewModel declarative (Constitution I — MVVM-First) and the retry logic independently testable (Constitution III — Test-First).

**Alternatives considered**:
- Retry logic directly in ViewModel → Rejected. Mixes UI concerns (status text formatting) with connection policy (when to retry). Harder to test retry edge cases independently.
- Polly or third-party retry library → Rejected. Polly is designed for async Task-based retries. MsRdpClient uses fire-and-forget `Connect()` with asynchronous event callbacks — doesn't fit the Polly pipeline model. A 15-line retry counter is simpler than a Polly dependency. (Constitution VII — Simplicity)
- Decorator pattern on `IRdpClient` → Rejected. The retry-aware wrapper needs to expose additional events (`AttemptStarted`, `RetriesExhausted`) and properties (`CurrentAttempt`, `MaxAttempts`) that aren't part of `IRdpClient`. A separate interface (`IConnectionService`) is cleaner.

## R2: Detecting Retryable vs. Non-Retryable Disconnects

**Context**: `IRdpClient.Disconnected` fires for all disconnects — user-initiated, remote drop, auth cancel, unreachable host. The retry logic should only re-attempt on connection failures, not on user-initiated disconnects or successful authentication cancellations.

**Decision**: The service distinguishes between retryable and non-retryable disconnects by tracking whether the disconnect was user-initiated (via the service's own `Disconnect()` method setting an `_isUserDisconnect` flag) vs. an error disconnect during the `Connecting` state. The service only retries when:
1. The disconnect was NOT user-initiated, AND
2. The connection state was `Connecting` (never reached `Connected`)

If the session was `Connected` and drops, that's a remote-drop scenario — out of scope for this feature (spec assumption: "retry applies to initial connection phase only").

**Rationale**: The MsRdpClient `Disconnected` event carries a reason code, but the reason codes for "unreachable host" vs. "user cancelled auth" can overlap or vary by Windows version. Tracking the connection phase (`Connecting` vs. `Connected`) and user intent (`_isUserDisconnect`) is more reliable than parsing reason codes.

**Alternatives considered**:
- Parse disconnect reason codes to decide retryability → Rejected. Reason codes are underdocumented and vary across RDP client versions. Phase-based detection is simpler and deterministic.
- Separate events on `IRdpClient` for different disconnect types → Rejected. Would require changes to the interop layer that leak retry semantics into the COM boundary.

## R3: Connection State Enum — Adding `Retrying`

**Context**: The current `ConnectionState` enum has `Disconnected`, `Resolving`, `Connecting`, `Connected`. The spec requires the status area to show retry progress. Should there be a new state?

**Decision**: No new enum value. The `Connecting` state is sufficient. The service raises `AttemptStarted(attemptNumber, maxAttempts)` events that the ViewModel uses to update `StatusText` with the attempt count. The ViewModel remains in `ConnectionState.Connecting` throughout the retry sequence.

**Rationale**: From the user's perspective, retrying IS connecting — the app is still trying to establish a connection. Adding a `Retrying` state would complicate `CanExecute` logic (both `Connecting` and `Retrying` would disable Connect, enable Disconnect) without adding semantic value. The attempt number in `StatusText` provides all the information the user needs. (Constitution VII — Simplicity)

**Alternatives considered**:
- Add `Retrying` enum value → Rejected. No UI behavior difference from `Connecting`. Would require updating `CanConnect`, `CanDisconnect`, converters, and tests for a state that behaves identically to `Connecting`.
- Use a separate `RetryCount` observable property → Rejected for state machine — but the ViewModel WILL track attempt info via service events for status text formatting.

## R4: Settings Persistence Strategy

**Context**: The spec requires the retry count to persist between sessions (FR-012). There is no existing settings infrastructure in the project.

**Decision**: Introduce `ISettingsStore` interface with `Load()` / `Save()` methods and an `AppSettings` record containing `MaxRetryCount` (default 3). Implement `JsonSettingsStore` using `System.Text.Json` serialization to `%APPDATA%/Remort/settings.json`. The settings file is human-editable JSON. The store creates the file with defaults on first run. Malformed JSON falls back to defaults silently.

**Rationale**: `System.Text.Json` ships with .NET 8 — zero new dependencies. JSON is human-readable and debuggable. `%APPDATA%` is the standard Windows location for per-user app settings. An interface allows unit tests to use an in-memory implementation. This is the lightest approach that satisfies FR-012. (Constitution VII — Simplicity)

**Alternatives considered**:
- .NET `Settings.settings` / `ApplicationSettingsBase` → Rejected. Generates XML configuration files, requires designer support, and is awkward with `net8.0-windows` SDK-style projects. Adds complexity with no benefit.
- Registry → Rejected. Harder to inspect/debug, not portable, requires elevated permissions in some scenarios.
- SQLite / LiteDB → Rejected. Massively over-engineered for a single integer setting.
- No persistence (re-enter each session) → Rejected. Spec FR-012 explicitly requires persistence.

## R5: Cancellation During Retry Sequence

**Context**: The spec requires the user to cancel retries at any time via Disconnect (FR-011). How does cancellation propagate through the retry service?

**Decision**: The `RetryingConnectionService.Disconnect()` method sets `_isUserDisconnect = true`, resets the attempt counter, and calls `IRdpClient.Disconnect()`. When the subsequent `IRdpClient.Disconnected` event fires, the service sees `_isUserDisconnect` is set and does NOT retry — it raises its own `Disconnected` event, which the ViewModel handles normally (transition to `Disconnected` state).

**Rationale**: This piggybacks on the existing `Disconnect()` → `Disconnected` event flow. No cancellation tokens or async machinery needed because `IRdpClient.Connect()` is synchronous (fire-and-forget to the COM control). The flag is the simplest coordination mechanism. (Constitution VII — Simplicity)

**Alternatives considered**:
- `CancellationToken` threading through Connect → Rejected. `IRdpClient.Connect()` is void and synchronous — there's nothing to cancel via token. The COM control handles its own cleanup on `Disconnect()`.
- Timer-based cancel (timeout per attempt) → Rejected. Out of scope — the spec explicitly says no backoff/delay. The COM control has its own internal timeout.

## R6: ViewModel ↔ Service Event Contract

**Context**: The ViewModel currently subscribes to `IRdpClient.Connected` and `IRdpClient.Disconnected`. With the retry service, what events does the ViewModel observe?

**Decision**: The ViewModel subscribes to `IConnectionService` events instead of `IRdpClient` events:

| Event | When Raised | ViewModel Action |
|-------|-------------|------------------|
| `AttemptStarted(attempt, maxAttempts)` | Each connection attempt begins | Update `StatusText` to "Connecting… (attempt N of M)" |
| `Connected` | Session successfully established | Set `ConnectionState = Connected`, update `StatusText` |
| `Disconnected(reason)` | User-initiated, remote drop, or retries exhausted | Set `ConnectionState = Disconnected`, update `StatusText` |
| `RetriesExhausted(attempts, lastReason)` | All attempts failed | Set `ConnectionState = Disconnected`, update `StatusText` to "Connection failed after N attempts: reason" |

**Rationale**: The ViewModel no longer needs to know about individual retry attempts or `IRdpClient` directly (for connection lifecycle). The service is the single source of truth for connection state transitions. Events carry all data the ViewModel needs for `StatusText` formatting.

**Alternatives considered**:
- ViewModel subscribes to both `IConnectionService` and `IRdpClient` → Rejected. Dual-subscription creates ambiguity about which events drive state. Single source of truth is cleaner.
- Property-based polling (e.g., `service.CurrentAttempt`) instead of events → Rejected. No polling trigger — events are the natural notification mechanism for async COM callbacks.

## R7: Settings UI for Retry Count (Story 3)

**Context**: The spec requires users to configure the retry count through a setting in the application (FR-008). There is no settings UI in the app today.

**Decision**: Add a settings flyout or expandable panel in the connection bar area with a numeric input for "Max retry attempts". The setting binds to `MainWindowViewModel.MaxRetryCount`, which reads/writes through `ISettingsStore`. Changes take effect on the next connection attempt (FR-010). The UI is minimal — a label and a `TextBox` with integer validation, or a `NumericUpDown`-style control.

**Rationale**: A full settings dialog/page is premature for a single setting. An inline control in the connection bar keeps the UI simple and discoverable. As more settings are added (future features), this can evolve into a dedicated settings view. (Constitution VII — Simplicity)

**Alternatives considered**:
- Separate Settings window/dialog → Rejected. Over-engineered for one setting. Can be introduced when multiple settings exist.
- Config file only (no UI) → Rejected. Spec FR-008 explicitly requires an in-app setting.
- Slider control → Rejected. Retry count is a small integer (0–~20). A text box with validation is simpler than a slider.
