namespace Remort.VirtualDesktop;

/// <summary>
/// Manages pinning the application window to a specific virtual desktop.
/// </summary>
public interface IVirtualDesktopService
{
    /// <summary>
    /// Gets a value indicating whether the virtual desktop API is available
    /// on the current system.
    /// </summary>
    public bool IsSupported { get; }

    /// <summary>
    /// Returns whether the specified window is on the currently active virtual desktop.
    /// Returns <c>true</c> if the API is not supported (graceful degradation).
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    /// <returns><c>true</c> if the window is on the current desktop or the API is unavailable.</returns>
    public bool IsOnCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Pins the specified window to its current virtual desktop,
    /// ensuring it does not appear on other desktops.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    public void PinToCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Releases the pin, returning the window to default OS virtual desktop behavior.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    public void Unpin(IntPtr hwnd);
}
