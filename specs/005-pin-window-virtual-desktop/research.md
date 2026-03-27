# Research: Pin Window to Virtual Desktop

**Feature**: 005-pin-window-virtual-desktop
**Date**: 2026-03-23

## R1: IVirtualDesktopManager COM API — Pinning Mechanism

**Context**: The spec requires pinning the Remort window to a single virtual desktop so it does not follow the user across desktops. Windows 10/11 provides `IVirtualDesktopManager` as a COM interface for querying which desktop a window is on. However, the *pinning* behavior (making a window appear on all desktops, or restricting it to one) uses a separate, undocumented interface `IVirtualDesktopPinnedApps`. What is the correct API surface and how reliable is it?

**Decision**: Use `IVirtualDesktopManager` (CLSID `AA509086-5CA9-4C25-8F95-589D3C07B48A`) for querying the current desktop of a window (`GetWindowDesktopId`, `IsWindowOnCurrentVirtualDesktop`). For the actual "pin to current desktop" behavior, no explicit pinning API call is needed — by default, WPF windows are already assigned to the desktop where they are created/shown. The "pin to all desktops" behavior is what requires an API call (via undocumented `IVirtualDesktopPinnedApps`). Our feature is the inverse: we want the window to stay on ONE desktop (the default OS behavior for most apps), and to remove the "show on all desktops" flag if the OS has applied it. The key insight is:

1. **Default behavior**: Windows already assigns windows to a single virtual desktop. The window stays there unless the user right-clicks the taskbar thumbnail and selects "Show this window on all desktops."
2. **Our feature**: If the window has been set to "show on all desktops" (via OS or another app), we need to undo that. We also need to ensure state correctness after Task View drag operations.
3. **The approach**: Use `IVirtualDesktopManager::MoveWindowToDesktop` to explicitly re-assign the window to its current desktop when the user enables "pin to desktop." This is a no-op in the common case but corrects the state if "show on all desktops" was previously enabled. When the user disables pinning, no action is needed — the window remains on whatever desktop it's currently on per standard OS behavior.

**Rationale**: `IVirtualDesktopManager` is the only *documented* virtual desktop COM interface in the Windows SDK. It ships in `shobjidl.h` and is stable across Windows 10 1803+ and all Windows 11 builds. The undocumented interfaces (`IVirtualDesktopPinnedApps`, `IVirtualDesktop`, etc.) change GUIDs between Windows builds and break frequently. Using only the documented interface keeps us on stable ground. (Constitution VII — Simplicity; Constitution II — COM Interop Isolation)

**Alternatives considered**:
- Use undocumented `IVirtualDesktopPinnedApps` to pin/unpin → Rejected. Interface GUIDs change between Windows 10 builds (21H1, 21H2, 22H2) and Windows 11 builds. Requires version-specific GUID tables and breaks on every Windows update. Too fragile.
- Use `SetWindowPos` / `SetWindowLong` with extended styles → Rejected. No window style flag controls virtual desktop assignment. The concept is managed at the shell level, not the window manager level.
- P/Invoke to `user32.dll` → No relevant APIs. Virtual desktop management is COM-only.

## R2: COM Interface Definition — IVirtualDesktopManager in C#

**Context**: The project uses manual COM interop (no `<COMReference>`, per ADR-002). How should `IVirtualDesktopManager` be declared in C#?

**Decision**: Define the COM interface manually using `[ComImport]`, `[Guid]`, and `[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]` attributes, following the same pattern as the existing `IMsTscAxEvents` sink in `Interop/`. The interface has three methods:

```csharp
[ComImport]
[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IVirtualDesktopManager
{
    bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
    Guid GetWindowDesktopId(IntPtr topLevelWindow);
    void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}
```

Instantiation: `new VirtualDesktopManagerClass()` where `VirtualDesktopManagerClass` is a `[ComImport]` coclass with CLSID `AA509086-5CA9-4C25-8F95-589D3C07B48A`.

**Rationale**: Matches the project's existing pattern for COM interop — manual declarations, no generated interop assemblies, no `<COMReference>`. The interface is tiny (3 methods) so the maintenance burden is negligible. All three methods are documented in the Windows SDK (`shobjidl_core.h`). (Constitution II — COM Interop Isolation)

**Alternatives considered**:
- `<COMReference>` in csproj → Rejected per ADR-002 (doesn't work with `dotnet build`).
- Source-generated COM interop (`[GeneratedComInterface]` in .NET 8) → Considered but rejected. Requires `unsafe` context for the generated marshalling code and adds complexity for a 3-method interface. The manual `[ComImport]` pattern is proven in this codebase.
- `dynamic` COM dispatch (`Type.GetTypeFromCLSID` + `Activator.CreateInstance`) → Rejected. No IntelliSense, no compile-time safety, harder to test. Constitution II says "no `dynamic` COM dispatch" outside interop files — and even within interop, typed interfaces are preferred.

## R3: Service Layer — IVirtualDesktopService vs. Direct COM Calls

**Context**: Where should the `IVirtualDesktopManager` COM calls live? The architecture has Views → ViewModels → Services → Interop → Platform APIs. The ViewModel needs to toggle pinning on/off. Should there be a service layer?

**Decision**: Introduce `IVirtualDesktopService` interface in a new `VirtualDesktop/` domain folder and `VirtualDesktopService` implementation. The service wraps `IVirtualDesktopManager` COM calls behind a testable interface with two methods: `PinToCurrentDesktop(IntPtr hwnd)` and `Unpin(IntPtr hwnd)`. The ViewModel calls the service; the service calls COM.

**Rationale**: 
1. **Testability** (Constitution III): Unit tests for the ViewModel can mock `IVirtualDesktopService`. Testing COM calls directly in ViewModel tests would require COM registration and a real virtual desktop environment.
2. **COM isolation** (Constitution II): COM interop stays in the service/interop layer. The ViewModel never touches `IntPtr`, GUIDs, or COM interfaces.
3. **Layered dependencies** (Constitution VI): Follows the same pattern as `IConnectionService` wrapping `IRdpClient`.

The service implementation will catch `COMException` for graceful degradation (e.g., running on a Windows version without virtual desktop support, or in a Remote Desktop session where the API is unavailable).

**Alternatives considered**:
- Direct COM calls in MainWindow.xaml.cs → Rejected. Puts business logic (PIN state management) in code-behind (violates Constitution I — MVVM-First).
- Direct COM calls in ViewModel → Rejected. Leaks COM types (`IntPtr`, `Guid`) into the ViewModel (violates Constitution VI — Layered Dependencies) and makes the ViewModel untestable without COM.
- Put everything in `Interop/` folder → Rejected. The `Interop/` layer is for low-level COM wrappers. The `VirtualDesktop/` domain has its own concerns (service interface, potential future features like desktop-switch detection for features 006/007).

## R4: Window Handle (HWND) Retrieval — WPF to Win32

**Context**: `IVirtualDesktopManager` methods require an `IntPtr topLevelWindow` (HWND). WPF windows don't directly expose their HWND.  How to get it?

**Decision**: Use `System.Windows.Interop.WindowInteropHelper` to get the HWND from the WPF `Window` instance. This is a standard WPF API, no P/Invoke needed:

```csharp
var hwnd = new WindowInteropHelper(window).Handle;
```

The HWND is available after the window's `SourceInitialized` event (i.e., after the Win32 window is created). The code-behind in `MainWindow.xaml.cs` passes the HWND to the service during initialization, after `InitializeComponent()`.

**Rationale**: `WindowInteropHelper` is the framework-provided way to bridge WPF and Win32. It's already used implicitly via `WindowsFormsHost` in the project. No additional P/Invoke or unsafe code needed. (Constitution VII — Simplicity)

**Alternatives considered**:
- P/Invoke `FindWindow` → Rejected. Requires knowing the window class name, fragile, unnecessary when WPF provides the handle directly.
- Pass the `Window` object to the service → Rejected. The service would depend on WPF types (violates Constitution I — MVVM-First, Constitution VI — Layered Dependencies).

## R5: Settings Integration — PinToDesktop Property

**Context**: The pin-to-desktop setting must persist between sessions (FR-006) and the existing `AppSettings` / `ISettingsStore` infrastructure already exists from feature 003.

**Decision**: Add a `PinToDesktopEnabled` property to the existing `AppSettings` record, defaulting to `false`. The `MainWindowViewModel` reads the setting on startup and saves it when toggled, using the same `ISettingsStore` interface. No new settings infrastructure needed.

**Rationale**: Reusing the existing settings system follows Constitution VII — Simplicity. Adding one property to the existing record is the minimal change. The `JsonSettingsStore` already handles serialization/deserialization; new properties with defaults are forward-compatible (missing keys in old JSON files get the default value).

**Alternatives considered**:
- Separate settings file for virtual desktop preferences → Rejected. Over-engineered for a single boolean.
- Windows Registry → Rejected (same rationale as research R4 in feature 003).

## R6: Visual Indicator — Pin Icon in Status Bar

**Context**: The spec requires a visual indicator (FR-008, FR-009) showing when the window is pinned. Where should it go?

**Decision**: Add a pin icon (📌 or a Path-based vector icon) to the status bar area, bound to the `PinToDesktopEnabled` ViewModel property with a `BooleanToVisibilityConverter`. The icon is visible when pinned, collapsed when not. This follows the existing StatusBar pattern in `MainWindow.xaml`.

**Rationale**: The status bar already shows connection state. Adding a pin indicator there is consistent with the existing UI layout. A `BooleanToVisibilityConverter` is a built-in WPF converter — no custom converter needed. (Constitution VII — Simplicity)

**Alternatives considered**:
- Title bar icon → Rejected. WPF doesn't have a clean API for adding icons next to the title. Would require window chrome customization, which is out of scope.
- Separate overlay window → Rejected. Over-engineered for a single icon.
- Toast/notification → Rejected. Spec says a persistent indicator, not a transient notification.
