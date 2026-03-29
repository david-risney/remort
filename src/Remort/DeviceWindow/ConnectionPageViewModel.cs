using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Remort.Connection;
using Remort.Devices;

namespace Remort.DeviceWindow;

/// <summary>
/// ViewModel for the Connection page in a device window.
/// </summary>
public partial class ConnectionPageViewModel : ObservableObject
{
    private readonly IConnectionService _connectionService;
    private readonly IDeviceStore _deviceStore;
    private readonly Guid _deviceId;

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private string _substatusText = string.Empty;

    [ObservableProperty]
    private bool _autoconnectOnStart;

    [ObservableProperty]
    private bool _autoconnectWhenVisible;

    [ObservableProperty]
    private bool _startOnStartup;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPageViewModel"/> class.
    /// </summary>
    /// <param name="device">The device to connect to.</param>
    /// <param name="connectionService">The connection service wrapping the RDP client.</param>
    /// <param name="deviceStore">The device store for persisting setting changes.</param>
    public ConnectionPageViewModel(Device device, IConnectionService connectionService, IDeviceStore deviceStore)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(connectionService);

        _connectionService = connectionService;
        _deviceStore = deviceStore;
        _deviceId = device.Id;

        _connectionService.Server = device.Hostname;
        _connectionService.RetryPolicy = new ConnectionRetryPolicy(device.ConnectionSettings.MaxRetryCount);

        _autoconnectOnStart = device.ConnectionSettings.AutoconnectOnStart;
        _autoconnectWhenVisible = device.ConnectionSettings.AutoconnectWhenVisible;
        _startOnStartup = device.ConnectionSettings.StartOnStartup;

        _connectionService.AttemptStarted += OnAttemptStarted;
        _connectionService.Connected += OnConnected;
        _connectionService.Disconnected += OnDisconnected;
        _connectionService.RetriesExhausted += OnRetriesExhausted;
    }

    /// <summary>Raised when a connection attempt is about to start, before the COM call. The view should ensure the RDP host is visible.</summary>
    public event EventHandler? ConnectRequested;

    /// <summary>Gets the button label reflecting the current connection state.</summary>
    public string ButtonLabel => ConnectionState == ConnectionState.Connected ? "Disconnect" : "Connect";

    /// <summary>
    /// Connects or disconnects based on current state.
    /// </summary>
    [RelayCommand]
    private void ToggleConnection()
    {
        if (ConnectionState == ConnectionState.Connected
            || ConnectionState == ConnectionState.Connecting
            || ConnectionState == ConnectionState.Resolving)
        {
            _connectionService.Disconnect();
        }
        else
        {
            ConnectionState = ConnectionState.Connecting;
            StatusText = "Connecting\u2026";
            SubstatusText = string.Empty;

            // Signal the view to make the RDP host visible before the COM call
            ConnectRequested?.Invoke(this, EventArgs.Empty);

            try
            {
                _connectionService.Connect();
            }
#pragma warning disable CA1031 // Catch COM/interop exceptions at the UI boundary
            catch (Exception ex)
#pragma warning restore CA1031
            {
                ConnectionState = ConnectionState.Disconnected;
                StatusText = "Disconnected";
                SubstatusText = $"Connection failed: {ex.Message}";
            }
        }
    }

    partial void OnConnectionStateChanged(ConnectionState value)
    {
        OnPropertyChanged(nameof(ButtonLabel));
    }

    partial void OnAutoconnectOnStartChanged(bool value)
    {
        PersistConnectionSettings(s => s with { AutoconnectOnStart = value });
    }

    partial void OnAutoconnectWhenVisibleChanged(bool value)
    {
        PersistConnectionSettings(s => s with { AutoconnectWhenVisible = value });
    }

    partial void OnStartOnStartupChanged(bool value)
    {
        PersistConnectionSettings(s => s with { StartOnStartup = value });
    }

    private void PersistConnectionSettings(Func<DeviceConnectionSettings, DeviceConnectionSettings> update)
    {
        Device? device = _deviceStore.GetById(_deviceId);
        if (device is null)
        {
            return;
        }

        DeviceConnectionSettings updated = update(device.ConnectionSettings);
        _deviceStore.Update(device with { ConnectionSettings = updated });
        _deviceStore.Save();
    }

    private void OnAttemptStarted(object? sender, AttemptStartedEventArgs e)
    {
        ConnectionState = ConnectionState.Connecting;
        StatusText = $"Connecting\u2026 (attempt {e.Attempt} of {e.MaxAttempts})";
        SubstatusText = string.Empty;
    }

    private void OnConnected(object? sender, EventArgs e)
    {
        ConnectionState = ConnectionState.Connected;
        StatusText = $"Connected to {_connectionService.Server}";
        SubstatusText = string.Empty;
    }

    private void OnDisconnected(object? sender, Interop.DisconnectedEventArgs e)
    {
        ConnectionState = ConnectionState.Disconnected;
        StatusText = "Disconnected";
        SubstatusText = string.Empty;
    }

    private void OnRetriesExhausted(object? sender, RetriesExhaustedEventArgs e)
    {
        ConnectionState = ConnectionState.Disconnected;
        StatusText = "Disconnected";
        SubstatusText = string.IsNullOrWhiteSpace(e.LastErrorDescription)
            ? $"Connection failed after {e.TotalAttempts} attempts"
            : $"Connection failed after {e.TotalAttempts} attempts: {e.LastErrorDescription}";
    }
}
