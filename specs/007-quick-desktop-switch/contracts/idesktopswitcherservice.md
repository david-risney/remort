# Contract: IDesktopSwitcherService Interface

**Feature**: 007-quick-desktop-switch
**Type**: Internal interface (ViewModel → VirtualDesktop Service boundary)
**File**: `src/Remort/VirtualDesktop/IDesktopSwitcherService.cs`

## Purpose

`IDesktopSwitcherService` is the abstraction boundary between `MainWindowViewModel` and the Windows virtual desktop enumeration/switching mechanism. It hides registry reads and `SendInput` P/Invoke calls behind a simple interface. The ViewModel calls the service to list desktops, detect the current desktop, and switch. Unit tests mock this interface to test ViewModel behavior without real virtual desktops, registry, or P/Invoke.

## Interface Definition

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Enumerates Windows virtual desktops, detects the active desktop,
/// and switches between desktops using keyboard simulation.
/// </summary>
public interface IDesktopSwitcherService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether virtual desktop enumeration is available
    /// on the current system.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Returns all virtual desktops in order, with names and indices.
    /// Returns an empty list if not supported.
    /// </summary>
    IReadOnlyList<VirtualDesktopInfo> GetDesktops();

    /// <summary>
    /// Returns the 0-based index of the currently active virtual desktop.
    /// Returns -1 if the current desktop cannot be determined.
    /// </summary>
    int GetCurrentDesktopIndex();

    /// <summary>
    /// Switches the active virtual desktop to the target index by simulating
    /// Win+Ctrl+Arrow keyboard shortcuts.
    /// </summary>
    /// <param name="targetIndex">The 0-based index of the desired desktop.</param>
    /// <param name="currentIndex">The 0-based index of the currently active desktop.</param>
    void SwitchToDesktop(int targetIndex, int currentIndex);

    /// <summary>
    /// Begins polling for desktop list and current-desktop changes.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops polling. No further <see cref="DesktopsChanged"/> events will be raised.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Raised when the desktop list changes (add/remove) or the current desktop changes.
    /// </summary>
    event EventHandler? DesktopsChanged;
}
```

## Method Contracts

### `IsSupported`

- Returns `true` if the registry key `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops` exists and contains a `VirtualDesktopIDs` value with at least 16 bytes (one desktop).
- Returns `false` if the registry key is missing or malformed. This can occur on very old Windows 10 builds or in environments where virtual desktops are disabled.
- Determined at construction time. Cost: one registry read.

### `GetDesktops()`

- **Precondition**: None. Safe to call regardless of `IsSupported`.
- **Postcondition**: Returns an ordered list of `VirtualDesktopInfo` records, one per virtual desktop. Order matches the Windows desktop order (left to right in Task View).
- **If not supported**: Returns an empty `IReadOnlyList<VirtualDesktopInfo>`.
- **Name resolution**: For each desktop GUID, checks `HKCU\...\VirtualDesktops\Desktops\{GUID}\Name`. If present and non-empty, uses the custom name. Otherwise, uses "Desktop N" (1-based index).
- **Thread safety**: Reads the registry on the calling thread. Expected to be called from the UI thread (via ViewModel).
- **Idempotent**: Can be called multiple times; each call re-reads the registry for fresh data.

### `GetCurrentDesktopIndex()`

- **Precondition**: None. Safe to call regardless of `IsSupported`.
- **Postcondition**: Returns the 0-based index of the desktop whose GUID matches the `CurrentVirtualDesktop` registry value. Returns `-1` if:
  - The service is not supported.
  - The `CurrentVirtualDesktop` value is missing or malformed.
  - The current desktop GUID doesn't match any desktop in the list (transient state during desktop creation/deletion).
- **Cost**: Two registry reads (`CurrentVirtualDesktop` + `VirtualDesktopIDs`).

### `SwitchToDesktop(int targetIndex, int currentIndex)`

- **Precondition**: Both `targetIndex` and `currentIndex` are valid 0-based indices within the desktop list. Caller is responsible for providing correct values (typically from `GetDesktops()` and `GetCurrentDesktopIndex()`).
- **Postcondition**: Sends `|targetIndex - currentIndex|` `Win+Ctrl+Arrow` keyboard shortcuts via `SendInput`. The direction is `Right` if `targetIndex > currentIndex`, `Left` otherwise.
- **If target equals current**: No-op. No keyboard events are sent.
- **If out of range**: No-op. The method does not throw — it silently returns to avoid UI errors.
- **Delay**: A 50ms delay is inserted between consecutive shortcut sequences for multi-step switches, allowing Windows to process each desktop transition.
- **Error handling**: If `SendInput` returns 0 (failure), the method logs the error and stops further key sequences. It does not throw.
- **Non-blocking caveat**: The method sends key events and returns. It does *not* wait for the desktop switch to complete. The next poll tick will detect the new current desktop.

### `StartMonitoring()`

- **Postcondition**: A `DispatcherTimer` begins polling every 500ms. On each tick, `GetDesktops()` and `GetCurrentDesktopIndex()` are called. If the result differs from the cached state, `DesktopsChanged` is raised.
- **Idempotent**: If already monitoring, restarts the timer.
- **Thread**: Timer runs on the UI dispatcher thread.

### `StopMonitoring()`

- **Postcondition**: The timer is stopped and disposed. No further `DesktopsChanged` events fire.
- **Idempotent**: Safe to call if not monitoring.

### `DesktopsChanged` (Event)

- Raised on the UI thread (from the `DispatcherTimer` tick).
- Fires when:
  - The desktop count changes (desktop added or removed).
  - The desktop order or IDs change.
  - The current desktop index changes (switch detected).
- Does NOT fire when nothing has changed (comparison is by GUID list + current index).

## Consumer Responsibilities

- The **View** (`MainWindow.xaml.cs`) creates the `DesktopSwitcherService` and passes it to the ViewModel. It calls `StartMonitoring()` after initialization and `StopMonitoring()`/`Dispose()` on window close.
- The **ViewModel** (`MainWindowViewModel`):
  - Calls `GetDesktops()` and `GetCurrentDesktopIndex()` on initialization to populate the `DesktopList` and `CurrentDesktop`.
  - Subscribes to `DesktopsChanged` to refresh the list and indicator.
  - Calls `SwitchToDesktop()` when the user selects a desktop or presses a keyboard shortcut.
  - Checks `IsSupported` to set `IsDesktopSwitcherSupported` (controls ComboBox visibility).
  - Uses a guard flag to prevent re-entrant switching when updating `CurrentDesktop` programmatically (e.g., during refresh).

## Implementation Notes

- Registry access uses `Microsoft.Win32.Registry` (part of .NET BCL). No external packages.
- `SendInput` P/Invoke is declared in `Interop/NativeMethods.cs`, consistent with Constitution II.
- The 50ms inter-step delay uses `Thread.Sleep(50)` on the UI thread. This is acceptable because desktop switching is user-initiated and the total delay for a 5-desktop hop is 200ms (well under the 500ms target). If this proves problematic, it can be converted to `async` with `Task.Delay`.
- The service does not cache desktop names aggressively — names are re-read on each `GetDesktops()` call, so renames in Windows Settings are picked up within 500ms.
