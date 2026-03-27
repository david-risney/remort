# Data Model: Quick Virtual Desktop Switching

**Feature**: 007-quick-desktop-switch
**Date**: 2026-03-23

## Entities

### VirtualDesktopInfo (Record — VirtualDesktop/VirtualDesktopInfo.cs) — New

Represents a single virtual desktop in the switcher list.

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Describes a Windows virtual desktop for display in the desktop switcher.
/// </summary>
/// <param name="Id">The virtual desktop's GUID as stored in the Windows Registry.</param>
/// <param name="Name">The display name: custom name on Win11, or "Desktop N" fallback.</param>
/// <param name="Index">The 0-based ordinal position in the desktop list.</param>
public sealed record VirtualDesktopInfo(Guid Id, string Name, int Index);
```

| Field | Type | Description | Source |
|-------|------|-------------|--------|
| `Id` | `Guid` | Desktop identity | Registry `VirtualDesktopIDs` (16-byte chunks) |
| `Name` | `string` | Display name | Registry `Desktops\{GUID}\Name` or "Desktop N" fallback |
| `Index` | `int` | 0-based ordinal | Position in `VirtualDesktopIDs` byte array |

### IDesktopSwitcherService (Interface — VirtualDesktop/IDesktopSwitcherService.cs) — New

Abstracts desktop enumeration, current-desktop detection, and switching behind a testable interface.

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
    /// on the current system (registry keys exist).
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
    /// Win+Ctrl+Arrow keyboard shortcuts. No-op if target equals current.
    /// </summary>
    /// <param name="targetIndex">The 0-based index of the desired desktop.</param>
    /// <param name="currentIndex">The 0-based index of the currently active desktop.</param>
    void SwitchToDesktop(int targetIndex, int currentIndex);

    /// <summary>
    /// Begins polling the registry for desktop list and current-desktop changes.
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

| Member | Type | Description |
|--------|------|-------------|
| `IsSupported` | `bool` | `true` if registry key `HKCU\...\VirtualDesktops\VirtualDesktopIDs` exists |
| `GetDesktops()` | `IReadOnlyList<VirtualDesktopInfo>` | Reads registry, builds ordered list |
| `GetCurrentDesktopIndex()` | `int` | Reads `CurrentVirtualDesktop`, finds index in list |
| `SwitchToDesktop(int, int)` | `void` | Sends `|target - current|` × `Win+Ctrl+Arrow` via `SendInput` |
| `StartMonitoring()` | `void` | Starts 500ms `DispatcherTimer` to poll registry |
| `StopMonitoring()` | `void` | Stops polling timer |
| `DesktopsChanged` | `event` | Fires when desktop count, order, or current index changes |

### DesktopSwitcherService (Class — VirtualDesktop/DesktopSwitcherService.cs) — New

Implementation of `IDesktopSwitcherService`.

```csharp
namespace Remort.VirtualDesktop;

/// <summary>
/// Enumerates virtual desktops from the Windows Registry and switches
/// between them by simulating Win+Ctrl+Arrow keyboard shortcuts via SendInput.
/// </summary>
public sealed class DesktopSwitcherService : IDesktopSwitcherService
{
    private const string VirtualDesktopRegistryPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";
    private const string DesktopsSubkeyPath =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops\Desktops";

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private DispatcherTimer? _timer;
    private IReadOnlyList<VirtualDesktopInfo> _cachedDesktops = [];
    private int _cachedCurrentIndex = -1;

    // ...registry reading, SendInput switching, polling logic...
}
```

**Internal state**:
- `_cachedDesktops`: Last-read desktop list, compared on each poll tick.
- `_cachedCurrentIndex`: Last-read current desktop index, compared on each poll tick.
- `_timer`: 500ms `DispatcherTimer` for change detection.

### NativeMethods (Static Class — Interop/NativeMethods.cs) — New

P/Invoke declarations for `SendInput`. Confined to `Interop/` per Constitution II.

```csharp
using System.Runtime.InteropServices;

namespace Remort.Interop;

/// <summary>
/// P/Invoke declarations for Win32 input simulation.
/// </summary>
internal static partial class NativeMethods
{
    internal const int INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const ushort VK_LWIN = 0x5B;
    internal const ushort VK_LCONTROL = 0xA2;
    internal const ushort VK_LEFT = 0x25;
    internal const ushort VK_RIGHT = 0x27;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public int Type;
        public INPUTUNION Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
```

### MainWindowViewModel (Modified — Connection/MainWindowViewModel.cs)

Add desktop switcher properties and commands to the existing ViewModel.

**New fields**:
```csharp
private readonly IDesktopSwitcherService? _desktopSwitcherService;

[ObservableProperty]
private ObservableCollection<VirtualDesktopInfo> _desktopList = [];

[ObservableProperty]
private VirtualDesktopInfo? _currentDesktop;

[ObservableProperty]
private bool _isDesktopSwitcherSupported;
```

**New commands**:
```csharp
[RelayCommand]
private void SwitchToNextDesktop() { /* increment index, call service */ }

[RelayCommand]
private void SwitchToPreviousDesktop() { /* decrement index, call service */ }
```

**New behavior in `partial void OnCurrentDesktopChanged`**:
When `CurrentDesktop` changes via ComboBox selection, call `_desktopSwitcherService.SwitchToDesktop(newIndex, oldIndex)` and refresh the desktop list. Guard against re-entrant calls during programmatic updates.

| Property | Type | Binding Target |
|----------|------|---------------|
| `DesktopList` | `ObservableCollection<VirtualDesktopInfo>` | ComboBox `ItemsSource` |
| `CurrentDesktop` | `VirtualDesktopInfo?` | ComboBox `SelectedItem` |
| `IsDesktopSwitcherSupported` | `bool` | ComboBox `Visibility` |

## Relationships

```
MainWindowViewModel
    │
    ├── IDesktopSwitcherService (enumeration + switching)
    │       │
    │       ├── Registry (HKCU\...\VirtualDesktops) — read desktop list, names, current
    │       └── NativeMethods.SendInput — keyboard simulation for switching
    │
    └── IVirtualDesktopService (existing — pinning, spec 005)
            │
            └── IVirtualDesktopManager COM — documented API
```

## State Transitions

The desktop switcher has a simple lifecycle:

```
[Unavailable] ──(registry keys exist)──▶ [Idle]
                                            │
                                    StartMonitoring()
                                            │
                                            ▼
                                      [Monitoring]
                                       │       │
                        (poll: no change)   (poll: change detected)
                              │                    │
                              ▼                    ▼
                        [Monitoring]     raise DesktopsChanged → [Monitoring]
                              │
                      StopMonitoring()
                              │
                              ▼
                          [Idle]
```

Desktop switch action flow:
```
User selects Desktop 3 (index 2)
    │
    ▼
ViewModel.OnCurrentDesktopChanged()
    │ (guard: not re-entrant, target ≠ current)
    ▼
IDesktopSwitcherService.SwitchToDesktop(target=2, current=0)
    │ (delta = +2: send Win+Ctrl+Right twice)
    ▼
SendInput(Win+Ctrl+Right) → 50ms delay → SendInput(Win+Ctrl+Right)
    │
    ▼
Next poll tick detects CurrentVirtualDesktop changed
    │
    ▼
DesktopsChanged event → ViewModel refreshes CurrentDesktop indicator
```
