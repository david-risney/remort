using System.Runtime.InteropServices;
using Remort.Interop;

namespace Remort.VirtualDesktop;

/// <summary>
/// Wraps <see cref="IVirtualDesktopManager"/> COM calls behind <see cref="IVirtualDesktopService"/>.
/// Gracefully degrades to no-op if the COM object is unavailable.
/// </summary>
public sealed class VirtualDesktopService : IVirtualDesktopService
{
    private readonly IVirtualDesktopManager? _manager;
    private Guid _pinnedDesktopId;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualDesktopService"/> class.
    /// Attempts to create the COM virtual desktop manager. If creation fails,
    /// all operations become silent no-ops.
    /// </summary>
    public VirtualDesktopService()
    {
        try
        {
            _manager = (IVirtualDesktopManager)new VirtualDesktopManagerClass();
        }
#pragma warning disable CA1031 // Do not catch general exception types — COM creation can fail on unsupported systems
        catch (COMException)
#pragma warning restore CA1031
        {
            _manager = null;
        }
    }

    /// <inheritdoc/>
    public bool IsSupported => _manager is not null;

    /// <inheritdoc/>
    public bool IsOnCurrentDesktop(IntPtr hwnd)
    {
        if (_manager is null || hwnd == IntPtr.Zero)
        {
            return true;
        }

        try
        {
            int hr = _manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool onCurrentDesktop);
            return hr < 0 || onCurrentDesktop;
        }
#pragma warning disable CA1031 // Do not catch general exception types — graceful degradation for COM failures
        catch (COMException)
#pragma warning restore CA1031
        {
            return true;
        }
    }

    /// <inheritdoc/>
    public void PinToCurrentDesktop(IntPtr hwnd)
    {
        if (_manager is null || hwnd == IntPtr.Zero)
        {
            return;
        }

        try
        {
            int hr = _manager.GetWindowDesktopId(hwnd, out Guid desktopId);
            if (hr < 0)
            {
                return;
            }

            hr = _manager.MoveWindowToDesktop(hwnd, ref desktopId);
            if (hr >= 0)
            {
                _pinnedDesktopId = desktopId;
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types — graceful degradation for COM failures
        catch (COMException)
#pragma warning restore CA1031
        {
            // Graceful degradation: COM call failed, ignore.
        }
    }

    /// <inheritdoc/>
    public void Unpin(IntPtr hwnd)
    {
        _pinnedDesktopId = Guid.Empty;
    }
}
