using Microsoft.Win32;

namespace Remort.Settings;

/// <summary>
/// Manages application startup registration via the Windows Run registry key.
/// </summary>
public static class StartupRegistration
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Remort";

    /// <summary>
    /// Gets a value indicating whether the app is registered to start on Windows logon.
    /// </summary>
    /// <returns><see langword="true"/> if registered; otherwise <see langword="false"/>.</returns>
    public static bool IsRegistered()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(AppName) is not null;
    }

    /// <summary>
    /// Registers the app to start on Windows logon.
    /// </summary>
    public static void Register()
    {
        string exePath = Environment.ProcessPath ?? string.Empty;
        if (string.IsNullOrEmpty(exePath))
        {
            return;
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.SetValue(AppName, $"\"{exePath}\"");
    }

    /// <summary>
    /// Removes the app from the Windows logon startup list.
    /// </summary>
    public static void Unregister()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }

    /// <summary>
    /// Sets the startup registration based on the specified value.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to register; <see langword="false"/> to unregister.</param>
    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            Register();
        }
        else
        {
            Unregister();
        }
    }
}
