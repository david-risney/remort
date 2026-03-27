using System.Runtime.InteropServices;

namespace Remort.Interop;

/// <summary>
/// COM interface for querying and managing virtual desktop window assignments.
/// Documented in shobjidl_core.h (Windows 10 1803+).
/// IID: A5CD92FF-29BE-454C-8D04-D82879FB3F1B.
/// </summary>
[ComImport]
[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IVirtualDesktopManager
{
    /// <summary>Determines whether the specified window is on the current virtual desktop.</summary>
    /// <param name="topLevelWindow">The top-level window handle.</param>
    /// <param name="onCurrentDesktop">Receives a value indicating whether the window is on the current desktop.</param>
    /// <returns>An HRESULT value.</returns>
    [PreserveSig]
    public int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);

    /// <summary>Gets the identifier of the virtual desktop the specified window is on.</summary>
    /// <param name="topLevelWindow">The top-level window handle.</param>
    /// <param name="desktopId">Receives the virtual desktop identifier.</param>
    /// <returns>An HRESULT value.</returns>
    [PreserveSig]
    public int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

    /// <summary>Moves the specified window to a virtual desktop.</summary>
    /// <param name="topLevelWindow">The top-level window handle.</param>
    /// <param name="desktopId">The target virtual desktop identifier.</param>
    /// <returns>An HRESULT value.</returns>
    [PreserveSig]
    public int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}

/// <summary>
/// CoClass for <see cref="IVirtualDesktopManager"/>.
/// CLSID: AA509086-5CA9-4C25-8F95-589D3C07B48A.
/// </summary>
[ComImport]
[Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
public class VirtualDesktopManagerClass
{
}
