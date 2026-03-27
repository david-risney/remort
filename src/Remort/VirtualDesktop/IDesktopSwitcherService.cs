namespace Remort.VirtualDesktop;

/// <summary>
/// Enumerates Windows virtual desktops, detects the active desktop,
/// and switches between desktops using keyboard simulation.
/// </summary>
public interface IDesktopSwitcherService : IDisposable
{
    /// <summary>
    /// Raised when the desktop list changes (add/remove) or the current desktop changes.
    /// </summary>
    public event EventHandler? DesktopsChanged;

    /// <summary>
    /// Gets a value indicating whether virtual desktop enumeration is available
    /// on the current system (registry keys exist).
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>
    /// Returns all virtual desktops in order, with names and indices.
    /// Returns an empty list if not supported.
    /// </summary>
    /// <returns>An ordered list of virtual desktop descriptors.</returns>
    public IReadOnlyList<VirtualDesktopInfo> GetDesktops();

    /// <summary>
    /// Returns the 0-based index of the currently active virtual desktop.
    /// Returns -1 if the current desktop cannot be determined.
    /// </summary>
    /// <returns>The 0-based index, or -1 if unknown.</returns>
    public int GetCurrentDesktopIndex();

    /// <summary>
    /// Switches the active virtual desktop to the target index by simulating
    /// Win+Ctrl+Arrow keyboard shortcuts. No-op if target equals current.
    /// </summary>
    /// <param name="targetIndex">The 0-based index of the desired desktop.</param>
    /// <param name="currentIndex">The 0-based index of the currently active desktop.</param>
    public void SwitchToDesktop(int targetIndex, int currentIndex);

    /// <summary>
    /// Begins polling the registry for desktop list and current-desktop changes.
    /// </summary>
    public void StartMonitoring();

    /// <summary>
    /// Stops polling. No further <see cref="DesktopsChanged"/> events will be raised.
    /// </summary>
    public void StopMonitoring();
}
