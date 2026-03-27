# Contract: IVirtualDesktopService Interface

**Feature**: 005-pin-window-virtual-desktop
**Type**: Internal interface (ViewModel → VirtualDesktop Service boundary)
**File**: `src/Remort/VirtualDesktop/IVirtualDesktopService.cs`

## Purpose

`IVirtualDesktopService` is the abstraction boundary between `MainWindowViewModel` and the Windows virtual desktop COM API. It hides `IVirtualDesktopManager` COM details (`IntPtr`, `Guid`, HRESULT marshalling) behind a simple pin/unpin interface. The ViewModel calls the service; the service calls COM. Unit tests mock this interface to test ViewModel behavior without COM or real virtual desktops.

## Interface Definition

```csharp
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
    bool IsSupported { get; }

    /// <summary>
    /// Pins the specified window to its current virtual desktop,
    /// ensuring it does not appear on other desktops.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    void PinToCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Releases the pin, returning the window to default OS virtual desktop behavior.
    /// </summary>
    /// <param name="hwnd">The top-level window handle.</param>
    void Unpin(IntPtr hwnd);
}
```

## Method Contracts

### `IsSupported`

- Returns `true` if the `IVirtualDesktopManager` COM object was successfully created.
- Returns `false` if COM creation failed (e.g., old Windows version, running inside an RDP session where virtual desktops are unavailable).
- Determined once at construction time; does not change during the lifetime of the service instance.

### `PinToCurrentDesktop(IntPtr hwnd)`

- **Precondition**: `hwnd` is a valid, non-zero top-level window handle.
- **Postcondition**: The window is explicitly assigned to its current virtual desktop via `IVirtualDesktopManager.MoveWindowToDesktop`. This ensures the window is bound to a specific desktop ID, preventing any "show on all desktops" behavior.
- **Error handling**: If `IsSupported` is `false`, this is a silent no-op. If the COM call fails (`COMException`), the exception is caught and logged — the caller is not interrupted.
- **Idempotent**: Calling this multiple times has no additional effect.

### `Unpin(IntPtr hwnd)`

- **Precondition**: `hwnd` is a valid, non-zero top-level window handle.
- **Postcondition**: The internal `_pinnedDesktopId` is cleared. The window remains on whatever desktop it is currently on — it simply stops being actively managed by the service.
- **Error handling**: No COM calls are needed for unpin in the current design. This is a lightweight operation.
- **Idempotent**: Calling this multiple times has no additional effect.

## Consumer Responsibilities

- The **View** (`MainWindow.xaml.cs`) obtains the HWND via `WindowInteropHelper` and passes it to the ViewModel or service during initialization. The HWND must not be passed before `SourceInitialized`.
- The **ViewModel** (`MainWindowViewModel`) calls `PinToCurrentDesktop` when `PinToDesktopEnabled` changes to `true`, and `Unpin` when it changes to `false`.
- The **ViewModel** checks `IsSupported` to optionally hide the toggle if virtual desktops are unavailable (optional — the toggle can remain visible and the service will no-op).

## Implementation Notes

- The service creates the `VirtualDesktopManagerClass` COM object once in its constructor. If creation throws `COMException`, `_manager` is `null` and `IsSupported` returns `false`.
- `PinToCurrentDesktop` calls `GetWindowDesktopId` to get the current desktop GUID, then `MoveWindowToDesktop` with that same GUID. This is effectively a "re-seat" operation that ensures the window is explicitly assigned.
- No background threads. All calls are synchronous and expected to complete in microseconds (in-process COM).
