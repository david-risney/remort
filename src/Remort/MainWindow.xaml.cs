using System.ComponentModel;
using System.Windows;
using Remort.Devices;
using Remort.Settings;
using Wpf.Ui.Controls;

namespace Remort;

/// <summary>
/// Main application window with NavigationView shell (Favorites, Devices, Settings).
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IDeviceStore _deviceStore;
    private readonly DeviceWindowManager _deviceWindowManager;
    private readonly ISettingsStore _settingsStore;
    private readonly DevicesPageViewModel _devicesPageViewModel;
    private readonly FavoritesPageViewModel _favoritesPageViewModel;
    private readonly SettingsPageViewModel _settingsPageViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _deviceStore = new JsonDeviceStore();
        _settingsStore = new JsonSettingsStore();
        _deviceWindowManager = new DeviceWindowManager(CreateDeviceWindow);

        _devicesPageViewModel = new DevicesPageViewModel(_deviceStore, _deviceWindowManager);
        _favoritesPageViewModel = new FavoritesPageViewModel(_deviceStore, _deviceWindowManager);
        _settingsPageViewModel = new SettingsPageViewModel(_settingsStore);

        NavigationView.Navigated += OnNavigated;

        Loaded += OnLoaded;
    }

    /// <inheritdoc/>
    protected override void OnClosing(CancelEventArgs e)
    {
        _deviceWindowManager.CloseAll();
        base.OnClosing(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Select Devices as the default page
        if (NavigationView.MenuItems.Count > 1)
        {
            NavigationView.Navigate(typeof(DevicesPage));
        }
    }

    private void OnNavigated(NavigationView sender, NavigatedEventArgs args)
    {
        if (args.Page is DevicesPage devicesPage)
        {
            devicesPage.DataContext = _devicesPageViewModel;
            _devicesPageViewModel.ShowAddDeviceDialog = devicesPage.ShowAddDeviceDialogAsync;
        }
        else if (args.Page is FavoritesPage favoritesPage)
        {
            favoritesPage.DataContext = _favoritesPageViewModel;
        }
        else if (args.Page is SettingsPage settingsPage)
        {
            settingsPage.DataContext = _settingsPageViewModel;
        }
    }

    private DeviceWindow.DeviceWindowView CreateDeviceWindow(Device device)
    {
        return new DeviceWindow.DeviceWindowView(device, _deviceStore);
    }
}
