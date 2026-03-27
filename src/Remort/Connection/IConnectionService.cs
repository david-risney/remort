using Remort.Interop;

namespace Remort.Connection;

/// <summary>
/// Orchestrates connection attempts with configurable retry logic.
/// Wraps <see cref="IRdpClient"/> and raises higher-level lifecycle events.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Raised when a connection attempt begins.
    /// Carries the 1-based attempt number and the maximum allowed attempts.
    /// </summary>
    public event EventHandler<AttemptStartedEventArgs>? AttemptStarted;

    /// <summary>
    /// Raised when the connection is successfully established.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// Raised when the session disconnects normally (user-initiated or remote drop).
    /// NOT raised when retries are exhausted — see <see cref="RetriesExhausted"/>.
    /// </summary>
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// Raised when all retry attempts have been exhausted without a successful connection.
    /// Carries the total number of attempts made and the last error description.
    /// </summary>
    public event EventHandler<RetriesExhaustedEventArgs>? RetriesExhausted;

    /// <summary>Gets or sets the target server hostname.</summary>
    public string Server { get; set; }

    /// <summary>
    /// Gets or sets the retry policy for subsequent connection attempts.
    /// Changes take effect on the next <see cref="Connect"/> call.
    /// </summary>
    public ConnectionRetryPolicy RetryPolicy { get; set; }

    /// <summary>
    /// Initiates a connection to <see cref="Server"/> with retry support.
    /// Raises <see cref="AttemptStarted"/> for each attempt.
    /// On success, raises <see cref="Connected"/>.
    /// On exhaustion, raises <see cref="RetriesExhausted"/>.
    /// </summary>
    public void Connect();

    /// <summary>
    /// Disconnects the active session and cancels any pending retries.
    /// Raises <see cref="Disconnected"/> after the underlying client disconnects.
    /// </summary>
    public void Disconnect();
}
