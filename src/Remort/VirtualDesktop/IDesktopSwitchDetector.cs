namespace Remort.VirtualDesktop;

/// <summary>
/// Detects when the application window transitions from a non-visible virtual
/// desktop to the currently active virtual desktop.
/// </summary>
public interface IDesktopSwitchDetector : IDisposable
{
    /// <summary>
    /// Raised when the monitored window becomes visible on the current virtual desktop
    /// after previously being on a different (non-visible) desktop.
    /// </summary>
    public event EventHandler? SwitchedToDesktop;

    /// <summary>
    /// Begins monitoring the specified window for desktop switch transitions.
    /// </summary>
    /// <param name="hwnd">The top-level window handle to monitor.</param>
    public void StartMonitoring(IntPtr hwnd);

    /// <summary>
    /// Stops monitoring. No further <see cref="SwitchedToDesktop"/> events will be raised.
    /// </summary>
    public void StopMonitoring();
}
