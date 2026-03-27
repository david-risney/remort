using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Remort;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
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
