# Data Model: Cap Connection Retries

**Feature**: 003-cap-connection-retries
**Date**: 2026-03-23

## Entities

### ConnectionRetryPolicy (Record — Connection/ConnectionRetryPolicy.cs)

Immutable configuration for retry behavior. Passed to the connection service at construction time or updated before the next connection attempt.

```csharp
namespace Remort.Connection;

/// <summary>
/// Defines the retry policy for connection attempts.
/// </summary>
/// <param name="MaxAttempts">
/// Maximum number of connection attempts (including the initial attempt).
/// A value of 1 means no retries. A value of 0 means one attempt with no retries
/// (equivalent to 1 for legacy compatibility — see spec edge case).
/// </param>
public readonly record struct ConnectionRetryPolicy(int MaxAttempts = 3)
{
    /// <summary>The default retry policy: 3 attempts.</summary>
    public static readonly ConnectionRetryPolicy Default = new(MaxAttempts: 3);
}
```

| Field | Type | Default | Constraints |
|-------|------|---------|-------------|
| `MaxAttempts` | `int` | `3` | ≥ 0. Value of 0 = one attempt, no retries per spec edge case. |

### AppSettings (Record — Settings/AppSettings.cs)

Persistent application settings. Serialized to JSON.

```csharp
namespace Remort.Settings;

/// <summary>
/// Application-wide settings, persisted to disk.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Gets or sets the maximum number of connection retry attempts.</summary>
    public int MaxRetryCount { get; init; } = 3;
}
```

| Field | Type | Default | JSON Key | Constraints |
|-------|------|---------|----------|-------------|
| `MaxRetryCount` | `int` | `3` | `maxRetryCount` | ≥ 0 (validated at ViewModel layer) |

### ConnectionState (Enum — Connection/ConnectionState.cs) — Unchanged

No new values. `Connecting` covers the entire retry sequence (see research R3).

| Value | Description |
|-------|-------------|
| `Disconnected` | No active session |
| `Resolving` | Dev Box name resolution in progress |
| `Connecting` | Connection attempt(s) in progress (including retries) |
| `Connected` | Active RDP session |

### Retry Attempt Tracking (Internal to RetryingConnectionService)

| Field | Type | Description |
|-------|------|-------------|
| `_currentAttempt` | `int` | Current attempt number (1-based). Reset to 0 on disconnect/connect-start. |
| `_maxAttempts` | `int` | Maximum attempts from active `ConnectionRetryPolicy`. |
| `_isUserDisconnect` | `bool` | Set to `true` when `Disconnect()` is called by the user. Prevents retry on the subsequent `Disconnected` event. |
| `_retryPolicy` | `ConnectionRetryPolicy` | Active retry policy. Updated via property setter before connection. |

### State Transitions (with Retry)

```
                      ┌───────────────┐
              ┌──────►│  Disconnected │◄─────────────────────┐
              │       └───────┬───────┘                      │
              │               │                              │
              │          ConnectCommand                      │
              │               │                              │
              │               ▼                              │
              │       ┌───────────────┐    attempt < max     │
              │       │  Connecting   │──────────────┐       │
              │       │  (attempt N)  │              │       │
              │       └───┬───┬───────┘              │       │
              │           │   │                      │       │
              │      OnConnected  OnDisconnected     │       │
              │           │       (error)            │       │
              │           │       │                  │       │
              │           │       ▼                  │       │
              │           │   ┌──────────┐           │       │
              │           │   │  Retry?  │───YES────►┘       │
              │           │   └────┬─────┘                   │
              │           │        │ NO (exhausted           │
              │           │        │  or user cancel)        │
              │           │        ▼                         │
              │           │   RetriesExhausted ─────────────►│
              │           ▼                                  │
              │   ┌───────────────┐                          │
              └───│   Connected   │──────────────────────────┘
         Disconnect└──────────────┘  OnDisconnected
          Command                    (remote drop)
```

**Transition details with retry**:

| From | To | Trigger | Condition |
|------|----|---------|-----------|
| Disconnected | Connecting (attempt 1) | `ConnectCommand` | Valid hostname |
| Connecting | Connecting (attempt N+1) | `IRdpClient.Disconnected` | Not user-initiated AND attempt < max |
| Connecting | Connected | `IRdpClient.Connected` | — |
| Connecting | Disconnected | `IRdpClient.Disconnected` | User-initiated (`_isUserDisconnect`) |
| Connecting | Disconnected | `IRdpClient.Disconnected` | Attempt ≥ max (retries exhausted) |
| Connected | Disconnected | `DisconnectCommand` or remote drop | — |

### IConnectionService (Interface — Connection/IConnectionService.cs)

```csharp
namespace Remort.Connection;

/// <summary>
/// Orchestrates connection attempts with retry logic.
/// </summary>
public interface IConnectionService
{
    /// <summary>Raised when a connection attempt begins.</summary>
    event EventHandler<AttemptStartedEventArgs>? AttemptStarted;

    /// <summary>Raised when the connection is established.</summary>
    event EventHandler? Connected;

    /// <summary>Raised when the session disconnects (user-initiated or remote drop).</summary>
    event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>Raised when all retry attempts are exhausted.</summary>
    event EventHandler<RetriesExhaustedEventArgs>? RetriesExhausted;

    /// <summary>Gets or sets the retry policy for subsequent connections.</summary>
    ConnectionRetryPolicy RetryPolicy { get; set; }

    /// <summary>Gets or sets the target server hostname.</summary>
    string Server { get; set; }

    /// <summary>Initiates a connection with retry support.</summary>
    void Connect();

    /// <summary>Disconnects and cancels any pending retries.</summary>
    void Disconnect();
}
```

### Event Args (Connection/)

```csharp
/// <summary>
/// Event data for the start of a connection attempt.
/// </summary>
public sealed class AttemptStartedEventArgs(int attempt, int maxAttempts) : EventArgs
{
    /// <summary>Gets the 1-based attempt number.</summary>
    public int Attempt { get; } = attempt;

    /// <summary>Gets the maximum number of attempts allowed.</summary>
    public int MaxAttempts { get; } = maxAttempts;
}

/// <summary>
/// Event data when all retry attempts have been exhausted.
/// </summary>
public sealed class RetriesExhaustedEventArgs(int totalAttempts, string lastErrorDescription) : EventArgs
{
    /// <summary>Gets the total number of attempts made.</summary>
    public int TotalAttempts { get; } = totalAttempts;

    /// <summary>Gets the error description from the last failed attempt.</summary>
    public string LastErrorDescription { get; } = lastErrorDescription;
}
```

### ISettingsStore (Interface — Settings/ISettingsStore.cs)

```csharp
namespace Remort.Settings;

/// <summary>
/// Reads and writes application settings.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Loads settings from the backing store.</summary>
    AppSettings Load();

    /// <summary>Saves settings to the backing store.</summary>
    void Save(AppSettings settings);
}
```

### MainWindowViewModel (Modified — Connection/MainWindowViewModel.cs)

**New/changed members**:

| Property | Type | Source Generator | Description |
|----------|------|-----------------|-------------|
| `MaxRetryCount` | `int` | `[ObservableProperty]` | Bound to settings UI. Reads from / writes to `ISettingsStore`. Updates `_connectionService.RetryPolicy` on change. |

**Changed dependencies**:

| Old | New | Reason |
|-----|-----|--------|
| `IRdpClient _rdpClient` (for lifecycle events) | `IConnectionService _connectionService` | Retry orchestration moved to service |
| — | `ISettingsStore _settingsStore` | Settings persistence |

The ViewModel still receives `IRdpClient` for direct property access (e.g., `Server`) but subscribes to `IConnectionService` events for lifecycle state transitions.

**Updated event handlers**:

| Handler | Source | Action |
|---------|--------|--------|
| `OnAttemptStarted` | `IConnectionService.AttemptStarted` | Set `ConnectionState = Connecting`, update `StatusText` to "Connecting… (attempt N of M)" |
| `OnConnected` | `IConnectionService.Connected` | Set `ConnectionState = Connected`, update `StatusText` |
| `OnDisconnected` | `IConnectionService.Disconnected` | Set `ConnectionState = Disconnected`, update `StatusText` |
| `OnRetriesExhausted` | `IConnectionService.RetriesExhausted` | Set `ConnectionState = Disconnected`, update `StatusText` to "Connection failed after N attempts: reason" |

## Relationships

```
MainWindowViewModel
  ├── uses → IConnectionService (retry orchestration + lifecycle events)
  ├── uses → ISettingsStore (load/save retry count)
  └── reads → IRdpClient.Server (for status text)

RetryingConnectionService : IConnectionService
  └── wraps → IRdpClient (delegates Connect/Disconnect, intercepts events)

JsonSettingsStore : ISettingsStore
  └── reads/writes → %APPDATA%/Remort/settings.json

AppSettings
  └── contains → MaxRetryCount : int

ConnectionRetryPolicy
  └── contains → MaxAttempts : int
```
