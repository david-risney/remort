using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Interop;
using Remort.Connection;
using Remort.DevBox;
using Remort.Interop;
using Remort.Settings;
using Remort.Theme;
using Remort.VirtualDesktop;

namespace Remort;

/// <summary>
/// Main application window. Creates and wires the RDP host control and ViewModel.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposable", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in OnClosing, the WPF Window lifecycle equivalent of IDisposable.Dispose.")]
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly DesktopSwitchDetector _desktopSwitchDetector;
    private readonly DesktopSwitcherService _desktopSwitcherService;
    private readonly ThemeService _themeService;
    private readonly JsonSettingsStore _settingsStore;
    private readonly JsonProfileStore _profileStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Theme: apply saved profile before any controls render
        _themeService = new ThemeService(System.Windows.Application.Current.Resources);
        _settingsStore = new JsonSettingsStore();
        _profileStore = new JsonProfileStore();
        RestoreSavedTheme();

        var rdpClient = new RdpClientHost();
        RdpHost.Child = rdpClient;

        var connectionService = new RetryingConnectionService(rdpClient);

        var authService = new MsalAuthenticationService();
        var httpClient = new HttpClient();
        var devBoxResolver = new DevBoxResolver(authService, httpClient);
        var virtualDesktopService = new VirtualDesktopService();

        _desktopSwitcherService = new DesktopSwitcherService();

        _viewModel = new MainWindowViewModel(connectionService, devBoxResolver, _settingsStore, virtualDesktopService, _desktopSwitcherService);
        DataContext = _viewModel;

        _desktopSwitchDetector = new DesktopSwitchDetector(virtualDesktopService);
        _desktopSwitchDetector.SwitchedToDesktop += OnSwitchedToDesktop;

        SourceInitialized += OnSourceInitialized;
        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;

        RdpHost.SizeChanged += (_, _) =>
        {
            rdpClient.DesktopWidth = (int)RdpHost.ActualWidth;
            rdpClient.DesktopHeight = (int)RdpHost.ActualHeight;
        };
    }

    /// <inheritdoc/>
    protected override void OnClosing(CancelEventArgs e)
    {
        SourceInitialized -= OnSourceInitialized;
        Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;

        _desktopSwitchDetector.SwitchedToDesktop -= OnSwitchedToDesktop;
        _desktopSwitchDetector.Dispose();

        _desktopSwitcherService.StopMonitoring();
        _desktopSwitcherService.Dispose();

        if (_viewModel.DisconnectCommand.CanExecute(null))
        {
            _viewModel.DisconnectCommand.Execute(null);
        }

        base.OnClosing(e);
    }

    private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock
            || e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLogon)
        {
            Dispatcher.Invoke(() => _viewModel.TryAutoReconnect());
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _viewModel.SetWindowHandle(hwnd);
        _desktopSwitchDetector.StartMonitoring(hwnd);
        _desktopSwitcherService.StartMonitoring();
    }

    private void OnSwitchedToDesktop(object? sender, EventArgs e)
    {
        _viewModel.TryReconnectOnDesktopSwitch();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = new ThemeSettingsViewModel(_themeService, _settingsStore, _profileStore);
        var window = new ThemeSettingsWindow
        {
            DataContext = viewModel,
            Owner = this,
        };
        window.ShowDialog();
    }

    private void RestoreSavedTheme()
    {
        AppSettings settings = _settingsStore.Load();
        if (string.IsNullOrEmpty(settings.ActiveProfileName))
        {
            return;
        }

        ColorProfile? profile = PresetProfiles.All
            .FirstOrDefault(p => string.Equals(p.Name, settings.ActiveProfileName, StringComparison.OrdinalIgnoreCase));

        profile ??= _profileStore.LoadCustomProfiles()
            .FirstOrDefault(p => string.Equals(p.Name, settings.ActiveProfileName, StringComparison.OrdinalIgnoreCase));

        if (profile is not null)
        {
            _themeService.ApplyProfile(profile);
        }
    }
}
