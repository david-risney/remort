using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Appearance;

namespace Remort.Settings;

/// <summary>
/// ViewModel for the Settings page in the main window.
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private bool _devBoxDiscoveryEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageViewModel"/> class.
    /// </summary>
    /// <param name="settingsStore">The settings store.</param>
    public SettingsPageViewModel(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;

        AppSettings settings = _settingsStore.Load();
        _selectedTheme = settings.Theme;
        _devBoxDiscoveryEnabled = settings.DevBoxDiscoveryEnabled;
    }

    /// <summary>Gets the application version from the assembly info.</summary>
    public static string AppVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        ApplicationTheme theme = value switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Dark,
        };

        ApplicationThemeManager.Apply(theme);

        AppSettings current = _settingsStore.Load();
        _settingsStore.Save(current with { Theme = value });
    }

    partial void OnDevBoxDiscoveryEnabledChanged(bool value)
    {
        AppSettings current = _settingsStore.Load();
        _settingsStore.Save(current with { DevBoxDiscoveryEnabled = value });
    }
}
