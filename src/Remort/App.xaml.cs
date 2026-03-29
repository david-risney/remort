using System.IO;
using System.Windows;
using System.Windows.Threading;
using Remort.Settings;
using Wpf.Ui.Appearance;

namespace Remort;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ArgumentNullException.ThrowIfNull(e);
        ParseCommandLineArgs(e.Args);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        ApplySavedTheme();
    }

    private static void ParseCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--data-dir", StringComparison.OrdinalIgnoreCase))
            {
                AppDataDirectory.SetOverride(args[i + 1]);
                break;
            }
        }
    }

    private static void ApplySavedTheme()
    {
        var settingsStore = new JsonSettingsStore();
        AppSettings settings = settingsStore.Load();

        ApplicationTheme theme = settings.Theme switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Dark,
        };

        ApplicationThemeManager.Apply(theme);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("DispatcherUnhandledException", e.Exception);
        e.Handled = false; // Let it crash after logging
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogCrash("UnhandledException", e.ExceptionObject as Exception);
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Remort",
                "crash.log");

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            string message = $"""
                [{DateTime.UtcNow:O}] {source}
                {ex}
                ---
                """;

            File.AppendAllText(logPath, message);
        }
#pragma warning disable CA1031 // Logging must not throw
        catch (Exception)
#pragma warning restore CA1031
        {
            // If we can't log, we can't log.
        }
    }
}
