using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Remort.DevBox;
using Remort.Interop;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Connection;

/// <summary>
/// ViewModel for the main window, managing RDP connection state and commands.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IConnectionService _connectionService;
    private readonly IDevBoxResolver? _devBoxResolver;
    private readonly ISettingsStore? _settingsStore;
    private readonly IVirtualDesktopService? _virtualDesktopService;
    private readonly IDesktopSwitcherService? _desktopSwitcherService;
    private bool _isAutoReconnect;
    private bool _isSuppressingDesktopSwitch;
    private IntPtr _hwnd;

    [ObservableProperty]
    private string _hostname = string.Empty;

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private int _maxRetryCount = 3;

    [ObservableProperty]
    private bool _autoReconnectEnabled;

    [ObservableProperty]
    private bool _pinToDesktopEnabled;

    [ObservableProperty]
    private bool _reconnectOnDesktopSwitchEnabled;

    [ObservableProperty]
    private ObservableCollection<VirtualDesktopInfo> _desktopList = [];

    [ObservableProperty]
    private VirtualDesktopInfo? _currentDesktop;

    [ObservableProperty]
    private bool _isDesktopSwitcherSupported;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="connectionService">The retry-aware connection service.</param>
    /// <param name="devBoxResolver">Optional Dev Box resolver for name-based connections.</param>
    /// <param name="settingsStore">Optional settings store for persisting configuration.</param>
    /// <param name="virtualDesktopService">Optional virtual desktop pinning service.</param>
    /// <param name="desktopSwitcherService">Optional desktop switcher service for enumeration and switching.</param>
    public MainWindowViewModel(
        IConnectionService connectionService,
        IDevBoxResolver? devBoxResolver = null,
        ISettingsStore? settingsStore = null,
        IVirtualDesktopService? virtualDesktopService = null,
        IDesktopSwitcherService? desktopSwitcherService = null)
    {
        _connectionService = connectionService;
        _devBoxResolver = devBoxResolver;
        _settingsStore = settingsStore;
        _virtualDesktopService = virtualDesktopService;
        _desktopSwitcherService = desktopSwitcherService;

        if (_settingsStore is not null)
        {
            // Settings loading deferred to per-device ViewModels in 009-ui-modernization.
            _connectionService.RetryPolicy = new ConnectionRetryPolicy(_maxRetryCount);
        }

        _connectionService.AttemptStarted += OnAttemptStarted;
        _connectionService.Connected += OnServiceConnected;
        _connectionService.Disconnected += OnServiceDisconnected;
        _connectionService.RetriesExhausted += OnRetriesExhausted;

        InitializeDesktopSwitcher();
    }

    /// <summary>
    /// Attempts auto-reconnect if enabled, disconnected, and a last host is known.
    /// Called by the View when a Windows session unlock/logon event occurs.
    /// </summary>
    public void TryAutoReconnect()
    {
        if (ConnectionState != ConnectionState.Disconnected
            || !AutoReconnectEnabled)
        {
            return;
        }

        ReconnectToLastHost();
    }

    /// <summary>
    /// Attempts reconnection when the user switches to the virtual desktop where
    /// Remort is pinned and the session is disconnected.
    /// Called by the View when a desktop switch is detected.
    /// </summary>
    public void TryReconnectOnDesktopSwitch()
    {
        if (ConnectionState != ConnectionState.Disconnected
            || !ReconnectOnDesktopSwitchEnabled
            || !PinToDesktopEnabled)
        {
            return;
        }

        ReconnectToLastHost();
    }

    /// <summary>
    /// Sets the window handle used for virtual desktop pinning.
    /// Must be called after the Win32 window is created (after SourceInitialized).
    /// Applies the initial pin state if enabled.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    public void SetWindowHandle(IntPtr hwnd)
    {
        _hwnd = hwnd;

        if (PinToDesktopEnabled && _virtualDesktopService is not null && _hwnd != IntPtr.Zero)
        {
            _virtualDesktopService.PinToCurrentDesktop(_hwnd);
        }
    }

    /// <summary>
    /// Gets the command to initiate an RDP connection.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        _isAutoReconnect = false;

        string trimmedHostname = Hostname.Trim();
        DevBoxIdentifier identifier = DevBoxIdentifier.Parse(trimmedHostname);

        if (identifier.IsDevBox && _devBoxResolver is not null)
        {
            await ConnectToDevBoxAsync(identifier).ConfigureAwait(true);
        }
        else
        {
            ConnectDirect(trimmedHostname);
        }
    }

    /// <summary>
    /// Gets the command to disconnect the active RDP session.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private void Disconnect()
    {
        _connectionService.Disconnect();
    }

    private async Task ConnectToDevBoxAsync(DevBoxIdentifier identifier)
    {
        ConnectionState = ConnectionState.Resolving;
        StatusText = $"Resolving Dev Box \u201c{identifier.ShortName}\u201d\u2026";

        try
        {
            DevBoxInfo info = await _devBoxResolver!.ResolveAsync(identifier, CancellationToken.None).ConfigureAwait(true);

            string server = info.Endpoint!.Host;
            PersistLastConnectedHost(server);
            _connectionService.Server = server;
            ConnectionState = ConnectionState.Connecting;
            _connectionService.Connect();
        }
        catch (DevBoxResolutionException ex)
        {
            ConnectionState = ConnectionState.Disconnected;
            StatusText = ex.Reason switch
            {
                ResolutionFailureReason.NotFound => $"Dev Box \u201c{identifier.ShortName}\u201d was not found",
                ResolutionFailureReason.NotRunning => $"Dev Box \u201c{identifier.ShortName}\u201d is not running",
                ResolutionFailureReason.Unauthorized => "Sign-in is required to resolve Dev Box names",
                ResolutionFailureReason.ServiceUnreachable => "Could not reach the Dev Box service",
                _ => $"Could not resolve Dev Box \u201c{identifier.ShortName}\u201d",
            };
        }
        catch (OperationCanceledException)
        {
            ConnectionState = ConnectionState.Disconnected;
            StatusText = "Sign-in is required to resolve Dev Box names";
        }
#pragma warning disable CA1031 // Do not catch general exception types — boundary for unexpected resolution errors
        catch (Exception)
#pragma warning restore CA1031
        {
            ConnectionState = ConnectionState.Disconnected;
            StatusText = $"Could not resolve Dev Box \u201c{identifier.ShortName}\u201d";
        }
    }

    private void ConnectDirect(string hostname)
    {
        PersistLastConnectedHost(hostname);
        _connectionService.Server = hostname;
        ConnectionState = ConnectionState.Connecting;
        _connectionService.Connect();
    }

    private bool CanConnect() =>
        ConnectionState == ConnectionState.Disconnected
        && !string.IsNullOrWhiteSpace(Hostname);

    private bool CanDisconnect() =>
        ConnectionState == ConnectionState.Connected
        || ConnectionState == ConnectionState.Connecting
        || ConnectionState == ConnectionState.Resolving;

    partial void OnConnectionStateChanged(ConnectionState value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    partial void OnHostnameChanged(string value)
    {
        ConnectCommand.NotifyCanExecuteChanged();
    }

    partial void OnAutoReconnectEnabledChanged(bool value)
    {
        // Persistence deferred to per-device ConnectionPageViewModel.
    }

    partial void OnPinToDesktopEnabledChanged(bool value)
    {
        if (_virtualDesktopService is not null && _hwnd != IntPtr.Zero)
        {
            if (value)
            {
                _virtualDesktopService.PinToCurrentDesktop(_hwnd);
            }
            else
            {
                _virtualDesktopService.Unpin(_hwnd);
            }
        }

        if (_settingsStore is not null)
        {
            // Persistence deferred to per-device DisplayPageViewModel.
        }
    }

    partial void OnReconnectOnDesktopSwitchEnabledChanged(bool value)
    {
        // Persistence deferred to per-device ConnectionPageViewModel.
    }

    partial void OnMaxRetryCountChanged(int value)
    {
        _connectionService.RetryPolicy = new ConnectionRetryPolicy(value);
    }

    private void OnAttemptStarted(object? sender, AttemptStartedEventArgs e)
    {
        if (_isAutoReconnect)
        {
            StatusText = $"Auto-reconnecting to {_connectionService.Server}\u2026 (attempt {e.Attempt} of {e.MaxAttempts})";
        }
        else
        {
            StatusText = $"Connecting\u2026 (attempt {e.Attempt} of {e.MaxAttempts})";
        }
    }

    private void OnServiceConnected(object? sender, EventArgs e)
    {
        _isAutoReconnect = false;
        ConnectionState = ConnectionState.Connected;
        StatusText = $"Connected to {_connectionService.Server}";
    }

    private void OnServiceDisconnected(object? sender, DisconnectedEventArgs e)
    {
        _isAutoReconnect = false;
        ConnectionState = ConnectionState.Disconnected;
        StatusText = "Disconnected";
    }

    private void OnRetriesExhausted(object? sender, RetriesExhaustedEventArgs e)
    {
        ConnectionState = ConnectionState.Disconnected;

        if (_isAutoReconnect)
        {
            if (string.IsNullOrWhiteSpace(e.LastErrorDescription))
            {
                StatusText = $"Auto-reconnect failed after {e.TotalAttempts} attempts";
            }
            else
            {
                StatusText = $"Auto-reconnect failed after {e.TotalAttempts} attempts: {e.LastErrorDescription}";
            }

            _isAutoReconnect = false;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(e.LastErrorDescription))
            {
                StatusText = $"Connection failed after {e.TotalAttempts} attempts";
            }
            else
            {
                StatusText = $"Connection failed after {e.TotalAttempts} attempts: {e.LastErrorDescription}";
            }
        }
    }

    private void ReconnectToLastHost()
    {
        // Last-connected-host tracking deferred to per-device model.
        _ = _isAutoReconnect; // read instance field to satisfy CA1822
    }

    private void PersistLastConnectedHost(string host)
    {
        // Persistence deferred to per-device model.
        _ = host;
        _ = _settingsStore; // read instance field to satisfy CA1822
    }

    /// <summary>
    /// Switches to the specified virtual desktop.
    /// </summary>
    /// <param name="target">The target desktop to switch to, or <see langword="null"/> to do nothing.</param>
    [RelayCommand]
    private void SwitchToDesktop(VirtualDesktopInfo? target)
    {
        if (_desktopSwitcherService is null || target is null || _isSuppressingDesktopSwitch)
        {
            return;
        }

        int currentIndex = _desktopSwitcherService.GetCurrentDesktopIndex();
        if (target.Index == currentIndex)
        {
            return;
        }

        _desktopSwitcherService.SwitchToDesktop(target.Index, currentIndex);
    }

    /// <summary>
    /// Switches to the next virtual desktop (no-op at last desktop).
    /// </summary>
    [RelayCommand]
    private void SwitchToNextDesktop()
    {
        if (_desktopSwitcherService is null || !IsDesktopSwitcherSupported)
        {
            return;
        }

        int currentIndex = _desktopSwitcherService.GetCurrentDesktopIndex();
        if (currentIndex < 0 || currentIndex >= DesktopList.Count - 1)
        {
            return;
        }

        _desktopSwitcherService.SwitchToDesktop(currentIndex + 1, currentIndex);
        RefreshDesktopState();
    }

    /// <summary>
    /// Switches to the previous virtual desktop (no-op at first desktop).
    /// </summary>
    [RelayCommand]
    private void SwitchToPreviousDesktop()
    {
        if (_desktopSwitcherService is null || !IsDesktopSwitcherSupported)
        {
            return;
        }

        int currentIndex = _desktopSwitcherService.GetCurrentDesktopIndex();
        if (currentIndex <= 0)
        {
            return;
        }

        _desktopSwitcherService.SwitchToDesktop(currentIndex - 1, currentIndex);
        RefreshDesktopState();
    }

    private void InitializeDesktopSwitcher()
    {
        if (_desktopSwitcherService is null || !_desktopSwitcherService.IsSupported)
        {
            IsDesktopSwitcherSupported = false;
            return;
        }

        IsDesktopSwitcherSupported = true;
        _desktopSwitcherService.DesktopsChanged += OnDesktopsChanged;

        IReadOnlyList<VirtualDesktopInfo> desktops = _desktopSwitcherService.GetDesktops();
        DesktopList = new ObservableCollection<VirtualDesktopInfo>(desktops);

        int currentIndex = _desktopSwitcherService.GetCurrentDesktopIndex();
        if (currentIndex >= 0 && currentIndex < DesktopList.Count)
        {
            _isSuppressingDesktopSwitch = true;
            CurrentDesktop = DesktopList[currentIndex];
            _isSuppressingDesktopSwitch = false;
        }
    }

    private void OnDesktopsChanged(object? sender, EventArgs e)
    {
        RefreshDesktopState();
    }

    private void RefreshDesktopState()
    {
        if (_desktopSwitcherService is null)
        {
            return;
        }

        _isSuppressingDesktopSwitch = true;

        IReadOnlyList<VirtualDesktopInfo> freshDesktops = _desktopSwitcherService.GetDesktops();
        DesktopList = new ObservableCollection<VirtualDesktopInfo>(freshDesktops);

        int currentIndex = _desktopSwitcherService.GetCurrentDesktopIndex();
        CurrentDesktop = currentIndex >= 0 && currentIndex < DesktopList.Count
            ? DesktopList[currentIndex]
            : null;

        _isSuppressingDesktopSwitch = false;
    }

    partial void OnCurrentDesktopChanged(VirtualDesktopInfo? value)
    {
        if (_isSuppressingDesktopSwitch || value is null)
        {
            return;
        }

        SwitchToDesktop(value);
    }
}
