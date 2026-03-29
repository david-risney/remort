using Remort.Interop;

namespace Remort.Connection;

/// <summary>
/// Wraps <see cref="IRdpClient"/> to provide retry orchestration with configurable limits.
/// Tracks attempt counts, intercepts disconnect events, and re-invokes Connect up to the limit.
/// </summary>
public sealed class RetryingConnectionService : IConnectionService
{
    private readonly IRdpClient _rdpClient;
    private string _server = string.Empty;

    private int _currentAttempt;
    private int _maxAttempts;
    private bool _isUserDisconnect;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryingConnectionService"/> class.
    /// </summary>
    /// <param name="rdpClient">The underlying RDP client.</param>
    public RetryingConnectionService(IRdpClient rdpClient)
    {
        _rdpClient = rdpClient;
        _rdpClient.Connected += OnRdpClientConnected;
        _rdpClient.Disconnected += OnRdpClientDisconnected;
    }

    /// <inheritdoc/>
    public event EventHandler<AttemptStartedEventArgs>? AttemptStarted;

    /// <inheritdoc/>
    public event EventHandler? Connected;

    /// <inheritdoc/>
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <inheritdoc/>
    public event EventHandler<RetriesExhaustedEventArgs>? RetriesExhausted;

    /// <inheritdoc/>
    public string Server
    {
        get => _server;
        set
        {
            _server = value;
            _rdpClient.Server = value;
        }
    }

    /// <inheritdoc/>
    public ConnectionRetryPolicy RetryPolicy { get; set; } = ConnectionRetryPolicy.Default;

    /// <inheritdoc/>
    public void Connect()
    {
        _isUserDisconnect = false;
        _isConnected = false;
        _maxAttempts = Math.Max(RetryPolicy.MaxAttempts, 1);
        _currentAttempt = 1;

        _rdpClient.ApplyDefaultSettings();

        // Re-apply Server right before Connect — the OCX may ignore property sets
        // before siting, so the value set in the Server setter earlier may be lost.
        _rdpClient.Server = Server;

        AttemptStarted?.Invoke(this, new AttemptStartedEventArgs(_currentAttempt, _maxAttempts));
        _rdpClient.Connect();
    }

    /// <inheritdoc/>
    public void Disconnect()
    {
        _isUserDisconnect = true;
        _rdpClient.Disconnect();
    }

    private void OnRdpClientConnected(object? sender, EventArgs e)
    {
        _isConnected = true;
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnRdpClientDisconnected(object? sender, DisconnectedEventArgs e)
    {
        // If the user initiated disconnect, pass through without retry.
        if (_isUserDisconnect)
        {
            _isUserDisconnect = false;
            _currentAttempt = 0;
            Disconnected?.Invoke(this, e);
            return;
        }

        // If the session was established and then dropped, pass through (not a retry scenario).
        if (_isConnected)
        {
            _isConnected = false;
            _currentAttempt = 0;
            Disconnected?.Invoke(this, e);
            return;
        }

        // Connection failed during attempt — decide whether to retry.
        if (_currentAttempt < _maxAttempts)
        {
            _currentAttempt++;
            AttemptStarted?.Invoke(this, new AttemptStartedEventArgs(_currentAttempt, _maxAttempts));
            _rdpClient.Connect();
        }
        else
        {
            // Retries exhausted.
            int totalAttempts = _currentAttempt;
            _currentAttempt = 0;

            int extendedReason = _rdpClient.ExtendedDisconnectReason;
            string description = _rdpClient.GetErrorDescription(e.Reason, extendedReason);

            RetriesExhausted?.Invoke(this, new RetriesExhaustedEventArgs(totalAttempts, description));
        }
    }
}
