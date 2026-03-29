using System.ComponentModel;
using System.Windows;
using Remort.Connection;
using Remort.Devices;
using Remort.Interop;
using Wpf.Ui.Controls;

namespace Remort.DeviceWindow;

/// <summary>
/// Per-device window with NavigationView for settings and RDP host area.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposable", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in OnClosing, the WPF Window lifecycle equivalent of IDisposable.Dispose.")]
public partial class DeviceWindowView : FluentWindow
{
    private readonly Device _device;
    private readonly IDeviceStore _deviceStore;
    private readonly RdpClientHost _rdpClient;
    private readonly ConnectionPageViewModel _connectionPageViewModel;
    private readonly DisplayPageViewModel _displayPageViewModel;
    private readonly GeneralPageViewModel _generalPageViewModel;
    private readonly RedirectionsPageViewModel _redirectionsPageViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceWindowView"/> class.
    /// </summary>
    /// <param name="device">The device to display.</param>
    /// <param name="deviceStore">The device store for persisting changes.</param>
    public DeviceWindowView(Device device, IDeviceStore deviceStore)
    {
        InitializeComponent();

        _device = device;
        _deviceStore = deviceStore;

        _rdpClient = new RdpClientHost();
        RdpHost.Child = _rdpClient;

        var connectionService = new RetryingConnectionService(_rdpClient);
        _connectionPageViewModel = new ConnectionPageViewModel(device, connectionService, deviceStore);
        _displayPageViewModel = new DisplayPageViewModel(device, deviceStore);
        _generalPageViewModel = new GeneralPageViewModel(device, deviceStore);
        _redirectionsPageViewModel = new RedirectionsPageViewModel(device, deviceStore);

        var viewModel = new DeviceWindowViewModel(device);
        DataContext = viewModel;

        _connectionPageViewModel.ConnectRequested += OnConnectRequested;
        DeviceNavigationView.Navigated += OnNavigated;
        Loaded += OnLoaded;
    }

    /// <summary>Gets the connection page ViewModel for this device window.</summary>
    public ConnectionPageViewModel ConnectionPageViewModel => _connectionPageViewModel;

    /// <inheritdoc/>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_connectionPageViewModel.ConnectionState == ConnectionState.Connected
            || _connectionPageViewModel.ConnectionState == ConnectionState.Connecting)
        {
            _connectionPageViewModel.ToggleConnectionCommand.Execute(null);
        }

        _rdpClient.Dispose();
        base.OnClosing(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DeviceNavigationView.Navigate(typeof(ConnectionPage));
    }

    private void OnConnectRequested(object? sender, EventArgs e)
    {
        // Switch to RDP view so the WindowsFormsHost becomes visible (COM control needs a HWND).
        if (DataContext is DeviceWindowViewModel vm)
        {
            vm.IsRdpViewActive = true;
        }

        // Force layout so the WFH gets a window handle before the COM Connect() call.
        UpdateLayout();

        System.Diagnostics.Debug.WriteLine($"[ConnectRequested] RdpHost.Visibility={RdpHost.Visibility}, RdpHost.ActualWidth={RdpHost.ActualWidth}, RdpHost.ActualHeight={RdpHost.ActualHeight}, Child={RdpHost.Child != null}, ChildHandle={_rdpClient.IsHandleCreated}, ChildSize={_rdpClient.Width}x{_rdpClient.Height}");
    }

    private void OnNavigated(NavigationView sender, NavigatedEventArgs args)
    {
        if (args.Page is ConnectionPage connectionPage)
        {
            connectionPage.DataContext = _connectionPageViewModel;
        }
        else if (args.Page is GeneralPage generalPage)
        {
            generalPage.DataContext = _generalPageViewModel;
        }
        else if (args.Page is DisplayPage displayPage)
        {
            displayPage.DataContext = _displayPageViewModel;
        }
        else if (args.Page is RedirectionsPage redirectionsPage)
        {
            redirectionsPage.DataContext = _redirectionsPageViewModel;
        }
    }
}
