# Data Model: Pin Window to Virtual Desktop

**Feature**: 005-pin-window-virtual-desktop
**Date**: 2026-03-23

## Entities

### AppSettings (Record — Settings/AppSettings.cs) — Modified

Add `PinToDesktopEnabled` to the existing settings record.

```csharp
namespace Remort.Settings;

/// <summary>
/// Application-wide settings, persisted to disk.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Gets the maximum number of connection retry attempts.</summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>Gets a value indicating whether auto-reconnect on Windows login is enabled.</summary>
    public bool AutoReconnectEnabled { get; init; }

    /// <summary>Gets the hostname of the last connected (or last attempted) host.</summary>
    public string LastConnectedHost { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the window is pinned to the current virtual desktop.</summary>
    public bool PinToDesktopEnabled { get; init; }
}
```

| Field | Type | Default | JSON Key | Constraints |
|-------|------|---------|----------|-------------|
| `PinToDesktopEnabled` | `bool` | `false` | `pinToDesktopEnabled` | None — boolean toggle |

### IVirtualDesktopManager (COM Interface — Interop/IVirtualDesktopManager.cs)

Windows SDK documented COM interface for virtual desktop management.

```csharp
namespace Remort.Interop;

/// <summary>
/// COM interface for querying and managing virtual desktop window assignments.
/// Documented in shobjidl_core.h (Windows 10 1803+).
/// </summary>
[ComImport]
[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IVirtualDesktopManager
{
    [PreserveSig]
    int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrentDesktop);

    [PreserveSig]
    int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

    [PreserveSig]
    int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
```

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `IsWindowOnCurrentVirtualDesktop` | `IntPtr hwnd` | `bool` (via out param) | Whether the window is on the active desktop |
| `GetWindowDesktopId` | `IntPtr hwnd` | `Guid` (via out param) | The ID of the desktop the window is on |
| `MoveWindowToDesktop` | `IntPtr hwnd, ref Guid desktopId` | HRESULT | Moves the window to a specific desktop |

### VirtualDesktopManagerClass (COM CoClass — Interop/IVirtualDesktopManager.cs)

```csharp
namespace Remort.Interop;

/// <summary>
/// CoClass for <see cref="IVirtualDesktopManager"/>.
/// CLSID: AA509086-5CA9-4C25-8F95-589D3C07B48A
/// </summary>
[ComImport]
[Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A")]
public class VirtualDesktopManagerClass
{
}
```

### IVirtualDesktopService (Interface — VirtualDesktop/IVirtualDesktopService.cs)

Service abstraction for ViewModel consumption. Hides COM details (`IntPtr`, `Guid`, HRESULT).

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
    /// <param name="hwnd">The window handle.</param>
    void PinToCurrentDesktop(IntPtr hwnd);

    /// <summary>
    /// Releases the pin, returning the window to default OS behavior.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    void Unpin(IntPtr hwnd);
}
```

| Method | Parameters | Description |
|--------|-----------|-------------|
| `IsSupported` | — | `true` if `IVirtualDesktopManager` COM object can be created |
| `PinToCurrentDesktop` | `IntPtr hwnd` | Gets the window's current desktop ID and moves it there explicitly |
| `Unpin` | `IntPtr hwnd` | No-op in current implementation (window stays on current desktop by default) |

### VirtualDesktopService Internal State

| Field | Type | Description |
|-------|------|-------------|
| `_manager` | `IVirtualDesktopManager?` | COM interface instance. `null` if creation failed. |
| `_pinnedDesktopId` | `Guid` | Desktop ID the window was pinned to. `Guid.Empty` if not pinned. |

### ViewModel Properties (MainWindowViewModel — Modified)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PinToDesktopEnabled` | `bool` | `false` | Bound to UI toggle. Changes invoke `IVirtualDesktopService.PinToCurrentDesktop` or `Unpin`. |

## Relationships

```
MainWindowViewModel
  ├─ IConnectionService (existing)
  ├─ ISettingsStore (existing — AppSettings gains PinToDesktopEnabled)
  └─ IVirtualDesktopService (NEW)
        └─ IVirtualDesktopManager (COM, in Interop/)
              └─ VirtualDesktopManagerClass (COM CoClass)
```

## State Diagram — Pin Toggle

```
                    ┌──────────┐
                    │ Unpinned │ (default)
                    │ (false)  │
                    └────┬─────┘
                         │ User enables toggle
                         │ → PinToCurrentDesktop(hwnd)
                         │ → Save settings
                         ▼
                    ┌──────────┐
                    │  Pinned  │
                    │  (true)  │
                    └────┬─────┘
                         │ User disables toggle
                         │ → Unpin(hwnd)
                         │ → Save settings
                         ▼
                    ┌──────────┐
                    │ Unpinned │
                    │ (false)  │
                    └──────────┘
```

On application startup:
1. Load `AppSettings.PinToDesktopEnabled`
2. If `true` → call `PinToCurrentDesktop(hwnd)` after window is shown
3. If `false` → no action (default OS behavior)
