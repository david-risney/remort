# Contract: IConnectionService Interface

**Feature**: 003-cap-connection-retries
**Type**: Internal interface (ViewModel → Connection Service boundary)
**File**: `src/Remort/Connection/IConnectionService.cs`

## Purpose

`IConnectionService` is the abstraction boundary between `MainWindowViewModel` and the retry-aware connection logic. It wraps `IRdpClient` to provide retry orchestration, attempt tracking, and cancellation. The ViewModel observes service events to update `ConnectionState` and `StatusText` — it never interacts with `IRdpClient` lifecycle events directly.

This interface is the primary seam for testing retry behavior — tests mock `IRdpClient` and verify the service's event-raising and re-connect behavior.

## Interface Definition

```csharp
namespace Remort.Connection;

using Remort.Interop;

/// <summary>
/// Orchestrates connection attempts with configurable retry logic.
/// Wraps <see cref="IRdpClient"/> and raises higher-level lifecycle events.
/// </summary>
public interface IConnectionService
{
    /// <summary>Gets or sets the target server hostname.</summary>
    string Server { get; set; }

    /// <summary>
    /// Gets or sets the retry policy for subsequent connection attempts.
    /// Changes take effect on the next <see cref="Connect"/> call.
    /// </summary>
    ConnectionRetryPolicy RetryPolicy { get; set; }

    /// <summary>
    /// Initiates a connection to <see cref="Server"/> with retry support.
    /// Raises <see cref="AttemptStarted"/> for each attempt.
    /// On success, raises <see cref="Connected"/>.
    /// On exhaustion, raises <see cref="RetriesExhausted"/>.
    /// </summary>
    void Connect();

    /// <summary>
    /// Disconnects the active session and cancels any pending retries.
    /// Raises <see cref="Disconnected"/> after the underlying client disconnects.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Raised when a connection attempt begins.
    /// Carries the 1-based attempt number and the maximum allowed attempts.
    /// </summary>
    event EventHandler<AttemptStartedEventArgs>? AttemptStarted;

    /// <summary>
    /// Raised when the connection is successfully established.
    /// </summary>
    event EventHandler? Connected;

    /// <summary>
    /// Raised when the session disconnects normally (user-initiated or remote drop).
    /// NOT raised when retries are exhausted — see <see cref="RetriesExhausted"/>.
    /// </summary>
    event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// Raised when all retry attempts have been exhausted without a successful connection.
    /// Carries the total number of attempts made and the last error description.
    /// </summary>
    event EventHandler<RetriesExhaustedEventArgs>? RetriesExhausted;
}
```

## Event Sequence — Successful Connection (First Attempt)

```
ViewModel calls service.Connect()
  → service sets attempt = 1, calls rdpClient.Connect()
  → service raises AttemptStarted(1, 3)
  → rdpClient raises Connected
  → service raises Connected
```

## Event Sequence — Retry then Success

```
ViewModel calls service.Connect()
  → service sets attempt = 1, calls rdpClient.Connect()
  → service raises AttemptStarted(1, 3)
  → rdpClient raises Disconnected(reason=3)       // timeout
  → service increments attempt to 2, calls rdpClient.Connect()
  → service raises AttemptStarted(2, 3)
  → rdpClient raises Connected
  → service raises Connected
```

## Event Sequence — All Retries Exhausted

```
ViewModel calls service.Connect()
  → service sets attempt = 1, calls rdpClient.Connect()
  → service raises AttemptStarted(1, 3)
  → rdpClient raises Disconnected(reason=3)
  → service increments attempt to 2, calls rdpClient.Connect()
  → service raises AttemptStarted(2, 3)
  → rdpClient raises Disconnected(reason=3)
  → service increments attempt to 3, calls rdpClient.Connect()
  → service raises AttemptStarted(3, 3)
  → rdpClient raises Disconnected(reason=3)
  → service sees attempt 3 == max 3
  → service raises RetriesExhausted(3, "Connection timed out")
```

## Event Sequence — User Cancels During Retry

```
ViewModel calls service.Connect()
  → service sets attempt = 1, calls rdpClient.Connect()
  → service raises AttemptStarted(1, 3)
  → rdpClient raises Disconnected(reason=3)
  → service increments attempt to 2, calls rdpClient.Connect()
  → service raises AttemptStarted(2, 3)
  [User clicks Disconnect]
  → ViewModel calls service.Disconnect()
  → service sets _isUserDisconnect = true, calls rdpClient.Disconnect()
  → rdpClient raises Disconnected(reason=1)        // user disconnect
  → service sees _isUserDisconnect, does NOT retry
  → service raises Disconnected(reason=1)
```

## Consumers

| Consumer | Usage |
|----------|-------|
| `MainWindowViewModel` | Subscribes to all four events. Calls `Connect()` / `Disconnect()`. Sets `Server` and `RetryPolicy`. |

## Implementors

| Implementation | Location |
|----------------|----------|
| `RetryingConnectionService` | `src/Remort/Connection/RetryingConnectionService.cs` |

## Constraints

- `Connect()` must raise `AttemptStarted` before each attempt (including the first).
- `RetriesExhausted` and `Disconnected` are mutually exclusive for a given disconnect event — the service raises one or the other, never both.
- `RetryPolicy` changes take effect on the next `Connect()` call, not mid-sequence.
- The service does NOT own `IRdpClient.Server` — the ViewModel (or caller) sets `Server` before calling `Connect()`.
