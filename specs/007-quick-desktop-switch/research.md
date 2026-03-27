# Research: Quick Virtual Desktop Switching

**Feature**: 007-quick-desktop-switch
**Date**: 2026-03-23

## R1: Desktop Enumeration — Registry vs. Undocumented COM

**Context**: The spec requires listing all virtual desktops (FR-001) with names (FR-002). `IVirtualDesktopManager` (the only documented COM interface) provides `IsWindowOnCurrentVirtualDesktop`, `GetWindowDesktopId`, and `MoveWindowToDesktop` — but NO enumeration. The undocumented `IVirtualDesktopManagerInternal` provides `GetDesktops()` but its GUID changes across Windows builds. How should we enumerate desktops?

**Decision**: Read the Windows Registry under `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops`. The `VirtualDesktopIDs` binary value contains concatenated 16-byte GUIDs representing all virtual desktops in order. Desktop names are stored under `HKCU\...\VirtualDesktops\Desktops\{GUID}` (Windows 11) in a `Name` string value. The current desktop GUID is in the `CurrentVirtualDesktop` binary value.

Registry layout:
```
HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops
├── VirtualDesktopIDs (REG_BINARY) — concatenated GUIDs, 16 bytes each
├── CurrentVirtualDesktop (REG_BINARY) — 16-byte GUID of active desktop
└── Desktops\
    ├── {GUID-1}\
    │   └── Name (REG_SZ) — user-assigned name (Win11+, optional)
    ├── {GUID-2}\
    │   └── Name (REG_SZ)
    └── ...
```

Parsing: Read `VirtualDesktopIDs` as `byte[]`, split into 16-byte chunks, convert each to `Guid`. Read `CurrentVirtualDesktop` as `byte[]` → `Guid`. For each desktop GUID, check `Desktops\{GUID}\Name` — if missing or empty, use "Desktop N" (1-based ordinal).

**Rationale**: The registry values are written by `explorer.exe` and have been stable across Windows 10 1803+ and all Windows 11 builds (21H2 through 24H2). While technically undocumented, they are far more stable than the COM interface GUIDs which change between builds. The registry is read-only for our purposes — we never write to it, only read. This avoids loading undocumented COM interfaces or maintaining build-specific GUID tables. The existing project already uses `Microsoft.Win32.Registry` (it's part of .NET BCL), so no new dependency.

**Alternatives considered**:
- **`IVirtualDesktopManagerInternal::GetDesktops()`** → Rejected. The user explicitly noted that this interface's GUIDs change between Windows builds. Maintaining a version-specific GUID table is fragile and requires testing against every Windows update. Community libraries (e.g., `VirtualDesktop` by MzHmO) do this but are frequently broken by Windows updates.
- **WMI / PowerShell** → Rejected. No WMI provider for virtual desktops exists. PowerShell-based solutions also rely on the undocumented COM interfaces.
- **UI Automation of Task View** → Rejected. Extremely fragile, slow, and violates the spirit of the feature (avoiding Task View).
- **No enumeration — just next/prev buttons** → Rejected. The spec explicitly requires listing all desktops (FR-001) with names (FR-002) and selecting a specific one (FR-004). Next/prev alone would satisfy FR-011 (keyboard shortcuts) but not FR-001–FR-004.

## R2: Desktop Switching — Keyboard Simulation via SendInput

**Context**: The spec requires switching to a selected desktop (FR-004). `IVirtualDesktopManager` only moves *windows* between desktops — it does not switch the active desktop. The undocumented `IVirtualDesktopManagerInternal::SwitchDesktop()` changes GUIDs across builds. The user explicitly requested keyboard simulation (`Win+Ctrl+Left/Right`) for stability. How should this be implemented?

**Decision**: Use the Win32 `SendInput` API via P/Invoke to simulate `Win+Ctrl+Left` and `Win+Ctrl+Right` keyboard shortcuts. To switch from desktop index `current` to index `target`:
1. Compute `delta = target - current` (positive = right, negative = left).
2. Send `|delta|` sequences of `Win+Ctrl+Arrow` with appropriate direction.
3. Each sequence is: key-down `VK_LWIN`, key-down `VK_LCONTROL`, key-down `VK_LEFT/RIGHT`, key-up `VK_LEFT/RIGHT`, key-up `VK_LCONTROL`, key-up `VK_LWIN`.
4. All key events for one shortcut are sent in a single `SendInput` call (array of 6 `INPUT` structs) for atomicity.
5. For multi-step switches, insert a brief delay (~50ms) between each shortcut sequence to allow Windows to process the desktop transition.

P/Invoke declarations go in `Interop/NativeMethods.cs`:
```csharp
[DllImport("user32.dll", SetLastError = true)]
static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
```

**Rationale**: `Win+Ctrl+Arrow` has been the standard virtual desktop switching shortcut since Windows 10 1511 and remains stable through all Windows 11 builds. Unlike COM interfaces, keyboard shortcuts are part of the public, documented Windows UX and are extremely unlikely to change. The `SendInput` API is well-documented Win32, used by accessibility tools, test frameworks, and automation software. It does not require elevated privileges. The user explicitly preferred this approach for cross-build stability.

**Alternatives considered**:
- **`IVirtualDesktopManagerInternal::SwitchDesktop()`** → Rejected per user direction. GUIDs change between builds. Would require maintaining a version-specific lookup table.
- **`keybd_event`** → Rejected. Deprecated in favor of `SendInput` since Windows XP. `SendInput` is the modern replacement with better atomicity (sends multiple events in one call, preventing interleaving).
- **WPF `System.Windows.Forms.SendKeys`** → Rejected. `SendKeys` doesn't support the Windows key modifier. It's designed for alphanumeric and simple modifier keys within a forms context.
- **`InputSimulator` NuGet package** → Rejected. Adds an external dependency for something achievable with a single P/Invoke. (Constitution VII — Simplicity)
- **Direct desktop switch to specific GUID** → Not possible with documented APIs. Would require undocumented COM.

## R3: Current Desktop Detection — Combining Registry + Documented COM

**Context**: The spec requires highlighting the active desktop (FR-003, SC-002). We need to know which desktop is current, both initially and after switches (including external switches via Task View or Win+Ctrl+Arrow).

**Decision**: Use a two-pronged approach:
1. **Initial/refresh**: Read `CurrentVirtualDesktop` from the registry, compare against the desktop list to find the index.
2. **Cross-validation**: Use `IVirtualDesktopManager.GetWindowDesktopId(hwnd)` on the Remort window to get its desktop GUID. This is a documented COM call already in the codebase. Compare with the registry desktop list to confirm which desktop Remort is on.
3. **Ongoing updates**: Poll the registry `CurrentVirtualDesktop` value on the same `DispatcherTimer` interval used by `DesktopSwitchDetector` (500ms). When the value changes, refresh the current-desktop indicator.

**Rationale**: The registry `CurrentVirtualDesktop` value updates in real time when desktops change (whether via Remort's own keyboard simulation or external switches). Polling at 500ms satisfies SC-002's 1-second indicator update requirement. Using `GetWindowDesktopId` as a cross-validation ensures consistency — if the Remort window was moved by the OS, we still detect correctly. This reuses the existing `IVirtualDesktopService.IsOnCurrentDesktop` pattern (documented COM only).

**Alternatives considered**:
- **Registry change notification (`RegNotifyChangeKeyValue`)** → Considered but deferred. It's a push-based approach that avoids polling, but requires a dedicated thread or async continuation. The 500ms poll is already in place for `DesktopSwitchDetector` and is sufficient. Can be revisited if polling proves problematic.
- **Only use `GetWindowDesktopId`** → Insufficient. This tells us which desktop *Remort's window* is on, not which desktop is *active*. If the window is pinned (spec 005), Remort stays on one desktop while the user might be viewing another.
- **WMI event subscription** → No WMI provider for virtual desktop state changes.

## R4: Desktop Name Retrieval — Windows 11 vs. Windows 10

**Context**: FR-002 requires displaying desktop names. Windows 11 allows custom desktop names. Windows 10 does not store names in the registry for default desktops. How should names be resolved?

**Decision**: For each desktop GUID, check the registry key `HKCU\...\VirtualDesktops\Desktops\{GUID}` for a `Name` string value:
- If the key and value exist and are non-empty → use the custom name.
- If the key doesn't exist, value is missing, or value is empty → use "Desktop N" where N is the 1-based index in the desktop list.

This handles:
- **Windows 11 with custom names**: Custom names are returned.
- **Windows 11 with default names**: The `Name` value exists but is empty → fallback to "Desktop N".
- **Windows 10**: The `Desktops\{GUID}` subkeys may not exist → fallback to "Desktop N".

**Rationale**: This is the simplest approach that satisfies the spec. The fallback naming matches what Windows Task View shows by default. No version detection is needed — the registry presence/absence naturally handles both OS versions. (Constitution VII — Simplicity)

**Alternatives considered**:
- **Detect Windows version and branch logic** → Rejected. Unnecessary. The registry check naturally handles both versions.
- **Use `IVirtualDesktop::GetName()` undocumented COM** → Rejected. Same instability concerns as other undocumented interfaces.

## R5: Service Design — New IDesktopSwitcherService

**Context**: The feature needs enumeration, name resolution, current desktop detection, and switching. Should this extend the existing `IVirtualDesktopService` or be a new service?

**Decision**: Create a new `IDesktopSwitcherService` interface and `DesktopSwitcherService` implementation in the `VirtualDesktop/` domain folder. The service provides:
- `bool IsSupported { get; }` — whether the registry keys exist (virtual desktops are available)
- `IReadOnlyList<VirtualDesktopInfo> GetDesktops()` — enumerate all desktops with names and current-desktop flag
- `int GetCurrentDesktopIndex()` — returns the 0-based index of the active desktop
- `void SwitchToDesktop(int targetIndex, int currentIndex)` — sends keyboard shortcuts to switch
- `event EventHandler? DesktopsChanged` — raised when polling detects a change in desktop count or current desktop

The service internally manages a `DispatcherTimer` for polling (same 500ms pattern as `DesktopSwitchDetector`).

**Rationale**: Single Responsibility — `IVirtualDesktopService` manages window-to-desktop pinning (spec 005). `IDesktopSwitcherService` manages desktop enumeration and switching (spec 007). They have different data sources (COM vs. registry + P/Invoke) and different lifecycles. Combining them would bloat the pinning interface with unrelated enumeration methods. Tests for switching need to mock enumeration results independently of window pinning. (Constitution III — Test-First, Constitution VII — Simplicity)

**Alternatives considered**:
- **Extend `IVirtualDesktopService`** → Rejected. Mixes two distinct concerns (pinning vs. switching). The interface would grow from 4 methods to 8+, violating Interface Segregation. Tests for pinning would be cluttered with switching concerns.
- **Static helper class** → Rejected. Not testable. Can't be mocked for ViewModel unit tests.
- **Put switching logic in ViewModel** → Rejected. ViewModel would need P/Invoke and registry access, violating Constitution I (MVVM-First) and Constitution VI (Layered Dependencies).

## R6: UI Design — Desktop Switcher Control in Connection Bar

**Context**: FR-007 requires the switcher be accessible from the main window without navigating to a separate screen. SC-001 requires switching in under 2 clicks. What UI control and placement?

**Decision**: Add a `ComboBox` to the existing connection bar (`StackPanel` at the top of `MainWindow.xaml`), positioned after the "Reconnect on desktop switch" checkbox separator. The ComboBox:
- `ItemsSource` binds to `DesktopList` (`ObservableCollection<VirtualDesktopInfo>`) on the ViewModel.
- `SelectedItem` binds to `CurrentDesktop` on the ViewModel, with `TwoWay` binding. Setting the selected item triggers `SwitchToDesktopCommand`.
- `DisplayMemberPath` shows the desktop name.
- `Visibility` binds to `IsDesktopSwitcherSupported` with `BooleanToVisibilityConverter` (FR-010: hidden when API unavailable).

This satisfies SC-001 (1 click to open dropdown + 1 click to select = 2 clicks).

**Rationale**: A `ComboBox` is the simplest WPF control that shows a list and allows selection. It integrates naturally into the existing horizontal `StackPanel` layout. The connection bar already has similar controls (textboxes, checkboxes, buttons). No custom control or separate panel needed. (Constitution VII — Simplicity)

**Alternatives considered**:
- **Separate panel/sidebar** → Rejected. Violates FR-007 (accessible without navigating elsewhere) and the simplicity principle. More clicks than needed.
- **Button bar with one button per desktop** → Considered. Provides a single-click switch (SC-001). However, it doesn't scale well if the user has many desktops — the buttons would overflow the toolbar. A ComboBox collapses to a compact widget regardless of desktop count.
- **ListBox in a flyout/popup** → More complex to implement than a ComboBox for minimal UX benefit. Could be a future refinement.
- **Context menu on a status bar icon** → Requires right-click discovery. Less discoverable than a visible ComboBox. Could complement but shouldn't replace.

## R7: In-App Keyboard Shortcuts — WPF InputBindings

**Context**: FR-011 requires keyboard shortcuts for next/previous desktop while Remort is focused. How should these be registered in WPF?

**Decision**: Use WPF `InputBindings` on the `Window` element in XAML:
```xml
<Window.InputBindings>
    <KeyBinding Key="Right" Modifiers="Ctrl+Alt" Command="{Binding SwitchToNextDesktopCommand}" />
    <KeyBinding Key="Left" Modifiers="Ctrl+Alt" Command="{Binding SwitchToPreviousDesktopCommand}" />
</Window.InputBindings>
```

Use `Ctrl+Alt+Arrow` (not `Ctrl+Win+Arrow`) to avoid conflicting with the Windows system shortcut. The ViewModel exposes `SwitchToNextDesktopCommand` and `SwitchToPreviousDesktopCommand` as `[RelayCommand]` methods.

**Rationale**: WPF `InputBindings` are the framework-provided mechanism for keyboard shortcuts. They only fire when the Remort window is focused (satisfying the spec assumption: "keyboard shortcuts apply only when the Remort window is focused"). `Ctrl+Alt+Arrow` is a natural modifier combination that doesn't conflict with common shortcuts. Using `Ctrl+Win+Arrow` would conflict with the OS shortcut itself. No global hotkey registration needed (`RegisterHotKey`), which simplifies the implementation and avoids cross-application interference.

**Alternatives considered**:
- **`Ctrl+Win+Arrow` (same as OS)** → Rejected. Would conflict with the Windows system shortcut. The OS would handle it before WPF gets the input event, making our handler unreachable.
- **`RegisterHotKey` P/Invoke** → Rejected. Registers a *global* hotkey that captures input from all applications. The spec says shortcuts apply only when Remort is focused. Global hotkeys would steal keystrokes from other apps.
- **`PreviewKeyDown` in code-behind** → Rejected. Puts too much logic in code-behind. `InputBindings` + ViewModel commands follows MVVM properly. (Constitution I — MVVM-First)
- **Configurable shortcuts** → Deferred. The spec doesn't require configurability. Fixed shortcuts suffice. (Constitution VII — Simplicity)

## R8: Desktop Change Detection — Polling for Add/Remove/Switch

**Context**: FR-006 requires the desktop list to refresh when desktops are added or removed while Remort is running. SC-002 requires the active indicator to update within 1 second. How should changes be detected?

**Decision**: Integrate polling into the `DesktopSwitcherService` using a `DispatcherTimer` at 500ms intervals (matching `DesktopSwitchDetector`'s pattern). On each tick:
1. Re-read `VirtualDesktopIDs` and `CurrentVirtualDesktop` from the registry.
2. Compare with the cached desktop list and current index.
3. If the desktop count, order, or current desktop changed → raise `DesktopsChanged` event.

The ViewModel subscribes to `DesktopsChanged` and refreshes `DesktopList` plus `CurrentDesktop`. The 500ms interval means changes are detected within 500ms, well under the 1-second requirement (SC-002).

**Rationale**: Polling reuses the established pattern from `DesktopSwitchDetector` (feature 006). The cost is negligible — two registry reads every 500ms, completing in microseconds. The `DesktopsChanged` event only fires when actual changes are detected, avoiding unnecessary UI updates. Starting and stopping the timer follows the same `StartMonitoring`/`StopMonitoring` lifecycle pattern.

**Alternatives considered**:
- **`RegNotifyChangeKeyValue`** → Considered but rejected for now. Requires either a dedicated thread (complexity) or `WaitForSingleObject` in an async context. The polling approach is proven in this codebase and sufficient for the 1-second requirement. Can be revisited if needed.
- **Event from `DesktopSwitchDetector`** → Partially useful. `DesktopSwitchDetector` detects when Remort's window becomes visible on the current desktop, but doesn't detect desktop additions/removals or switches when Remort isn't involved. A separate poll is needed.
- **Manual refresh button** → Would omit the automatic detection required by FR-006, but could complement polling as a fallback. Deferred — polling should be sufficient.

## R9: Interaction with Pin-to-Desktop (Spec 005)

**Context**: FR-009 requires coexistence with pin-to-desktop. If the user has Remort pinned to Desktop 1 and switches to Desktop 2 via the switcher, Remort should stay on Desktop 1. How does this work with the keyboard simulation approach?

**Decision**: No special handling needed. The `Win+Ctrl+Arrow` shortcut switches the *active desktop*, not Remort's window. If pin-to-desktop is enabled (spec 005), `VirtualDesktopService.PinToCurrentDesktop` has already assigned Remort's window to its desktop via `MoveWindowToDesktop`. When the active desktop changes, Remort's window stays on its pinned desktop — this is standard Windows behavior. After the switch, the user may no longer see the Remort window (it's on a different desktop), which aligns with the spec's edge case definition.

If pin-to-desktop is *not* enabled, Remort follows default Windows behavior: the window stays on whatever desktop it's on, and the active desktop switches away from it.

**Rationale**: The keyboard simulation approach naturally separates "switch active desktop" from "move window." No code needs to check pin state before switching. This is simpler than COM-based switching where we might accidentally move the window.

**Alternatives considered**:
- **Check pin state and move window after switch** → Not needed. Keyboard simulation doesn't affect window placement.
