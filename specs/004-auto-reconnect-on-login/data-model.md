# Data Model: Auto-Reconnect on Windows Login

**Feature**: 004-auto-reconnect-on-login
**Date**: 2026-03-23

## Entities

### AppSettings (Record — Settings/AppSettings.cs) — Modified

Extended with two new fields for auto-reconnect. Existing `MaxRetryCount` field unchanged.

```csharp
namespace Remort.Settings;

/// <summary>
/// Application-wide settings, persisted to disk.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Gets the maximum number of connection retry attempts.</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Gets a value indicating whether auto-reconnect on Windows login is enabled.</summary>
    public bool AutoReconnectEnabled { get; init; }

    /// <summary>Gets the hostname of the last connected (or last attempted) host.</summary>
    public string LastConnectedHost { get; init; } = string.Empty;
}
```

| Field | Type | Default | JSON Key | Source Requirement |
|-------|------|---------|----------|--------------------|
| `MaxRetryCount` | `int` | `3` | `maxRetryCount` | Spec 003 FR-012 (unchanged) |
| `AutoReconnectEnabled` | `bool` | `false` | `autoReconnectEnabled` | FR-005, FR-006 |
| `LastConnectedHost` | `string` | `""` | `lastConnectedHost` | FR-001 |

### Settings JSON Schema

File: `%APPDATA%/Remort/settings.json`

```json
{
  "maxRetryCount": 3,
  "autoReconnectEnabled": false,
  "lastConnectedHost": ""
}
```

After a user enables auto-reconnect and connects to `myserver.contoso.com`:

```json
{
  "maxRetryCount": 3,
  "autoReconnectEnabled": true,
  "lastConnectedHost": "myserver.contoso.com"
}
```

**Backward compatibility**: Existing settings files from spec 003 that lack the new fields will deserialize with defaults (`false`, `""`) because `System.Text.Json` ignores missing properties and `AppSettings` uses `init` with default values.

### ConnectionState (Enum — Connection/ConnectionState.cs) — Unchanged

No new values. `Connecting` covers auto-reconnect attempts, consistent with spec 003 research R3.

| Value | Description |
|-------|-------------|
| `Disconnected` | No active session |
| `Resolving` | Dev Box name resolution in progress |
| `Connecting` | Connection attempt(s) in progress (manual or auto-reconnect) |
| `Connected` | Active RDP session |

### ViewModel State (MainWindowViewModel — Connection/MainWindowViewModel.cs) — Modified

New observable properties and internal state for auto-reconnect:

| Property/Field | Type | Observable | Description |
|----------------|------|------------|-------------|
| `AutoReconnectEnabled` | `bool` | Yes (`[ObservableProperty]`) | Bound to UI toggle. Persisted via `ISettingsStore` on change. |
| `_isAutoReconnect` | `bool` | No (private field) | Tracks whether current connection was initiated automatically. Used for status text formatting. |

### State Transitions (with Auto-Reconnect)

```
                    ┌───────────────────┐
                    │   Disconnected    │◄──────────────────────────┐
                    └───────┬───────────┘                           │
                            │                                       │
             ┌──────────────┼──────────────┐                       │
             │              │              │                        │
        ConnectCommand  TryAutoReconnect   │                        │
        (manual)        (session unlock)   │                        │
             │              │              │                        │
             ▼              ▼              │                        │
        ┌──────────────────────┐          │                        │
        │     Connecting       │          │                        │
        │  (attempt N of M)    │          │                        │
        └──────┬───────┬───────┘          │                        │
               │       │                  │                        │
          Connected  Failed               │                        │
               │       │                  │                        │
               │       ├── Retry ────────►│  (back to Connecting)  │
               │       │                                           │
               │       └── Exhausted ─────────────────────────────►│
               │                                                   │
               └── Disconnected (user/remote) ────────────────────►│
```

**Key difference from manual flow**: `TryAutoReconnect()` sets `_isAutoReconnect = true` before entering `Connecting` state, which causes `StatusText` to display "Auto-reconnecting to host…" instead of "Connecting…".

### TryAutoReconnect Decision Logic

```
TryAutoReconnect():
  IF ConnectionState != Disconnected → return (already connecting/connected)
  IF AutoReconnectEnabled == false → return
  IF LastConnectedHost is empty → return (FR-009)
  
  Set _isAutoReconnect = true
  Set Hostname = LastConnectedHost (display)
  Set IConnectionService.Server = LastConnectedHost
  Set ConnectionState = Connecting
  Call IConnectionService.Connect()
```

### Event Flow — Auto-Reconnect on Session Unlock

```
Windows raises SessionSwitch(SessionUnlock)
  → MainWindow.xaml.cs handler fires
  → Dispatcher.Invoke(() => _viewModel.TryAutoReconnect())
  → ViewModel checks: Disconnected? Enabled? Host exists?
  → ViewModel sets _isAutoReconnect = true
  → ViewModel sets Server = LastConnectedHost
  → ViewModel calls _connectionService.Connect()
  → Service raises AttemptStarted(1, 3)
  → ViewModel updates StatusText: "Auto-reconnecting to host… (attempt 1 of 3)"
  → [standard IConnectionService flow from spec 003]
  → On Connected → StatusText: "Connected to host"
     On RetriesExhausted → StatusText: "Auto-reconnect failed after N attempts: reason"
```

### LastConnectedHost Update Points

| Trigger | Value Stored | Code Location |
|---------|-------------|---------------|
| `ConnectDirect(hostname)` called | `hostname` parameter | `MainWindowViewModel.ConnectDirect()` |
| `ConnectToDevBoxAsync()` resolves successfully | Resolved server FQDN (`info.Endpoint.Host`) | `MainWindowViewModel.ConnectToDevBoxAsync()` |
| Auto-reconnect initiates | No update (uses existing value) | N/A |

The host is saved when connection is *initiated*, not on success, per FR-001 ("last successfully connected or last attempted host").
