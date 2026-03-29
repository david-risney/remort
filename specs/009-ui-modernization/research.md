# Research: Modern Windows UI

**Feature**: 009-ui-modernization  
**Date**: 2026-03-28

## 1. WPF UI Library (Wpf.Ui) for Windows 11-Style Controls

**Decision**: Use Wpf.Ui (NuGet: `WPF-UI` v4.2.0)

**Rationale**:
- Provides `NavigationView`, `FluentWindow`, `TitleBar`, `CardControl`, `ContentDialog` — exactly the controls needed for both window types
- Targets .NET 8.0 natively; MIT licensed; 783K NuGet downloads, actively maintained
- Built-in dark/light/system theme support via `ApplicationThemeManager` and `ThemesDictionary`
- Snap Layout support for TitleBar (Windows 11-style window management)
- Works with Visual Studio designer for XAML preview
- `ui:FluentWindow` replaces `Window` base class; compatible with `WindowsFormsHost` for ActiveX hosting

**Alternatives considered**:
- **MaterialDesignInXamlToolkit**: Material Design language, not Windows 11 style; heavier; different design system
- **ModernWpf**: Discontinued; last release 2021; no .NET 8 support
- **HandyControl**: Chinese-language-first documentation; less consistent with Windows 11 design
- **Custom styling**: Would require building NavigationView, CardControl, ContentDialog from scratch — violates Constitution VII (Simplicity)

**Integration notes**:
- `App.xaml` must load `ui:ThemesDictionary` and `ui:ControlsDictionary` as merged resource dictionaries
- The existing custom theme system (ThemeService, ColorProfile, PresetProfiles) will be replaced by Wpf.Ui's theme infrastructure (`ApplicationThemeManager.Apply()` with `ApplicationTheme.Dark`/`Light`/`HighContrast`)
- The existing ThemeSettingsWindow and color profile system can be simplified — Wpf.Ui handles fluent colors automatically; custom accent colors can be set via `ApplicationAccentColorManager`

## 2. Multi-Window Architecture with Wpf.Ui NavigationView

**Decision**: Main window = `FluentWindow` + `NavigationView` (Favorites/Devices/Settings pages). Device window = `FluentWindow` + `NavigationView` (Connection/General/Display/Redirections pages) + nav/RDP toggle.

**Rationale**:
- Wpf.Ui `NavigationView` provides left sidebar navigation with built-in transition animations
- Each "page" is a WPF `Page` loaded inside the `NavigationView.ContentPresenter`
- Per-device windows are independent `FluentWindow` instances — standard WPF multi-window pattern
- Device window lifecycle is managed by a `DeviceWindowManager` service to enforce single-window-per-device constraint (FR-012)

**Alternatives considered**:
- **Single window with tabs**: Loses the ability to place device windows on different virtual desktops; conflicts with existing pin-to-desktop and multi-monitor features
- **MDI (Multiple Document Interface)**: Outdated UX pattern; not supported by Wpf.Ui; complex to implement

## 3. Device Data Model and Persistence

**Decision**: New `Device` record for per-device settings; persisted as JSON alongside existing `AppSettings`

**Rationale**:
- Current `AppSettings` is a single flat record for global preferences. The new UI requires per-device settings (connection, general, display, redirections)
- A `DeviceStore` service (backed by `devices.json` in `%APPDATA%/Remort/`) handles CRUD for devices separately from app settings
- `IDeviceStore` interface keeps it testable
- DevBox-discovered devices and manually-added devices use the same `Device` model, distinguished by an `IsDiscovered` flag (FR-035)

**Alternatives considered**:
- **Extend AppSettings with a device list**: Mixes global and per-device concerns; makes the single-record pattern unwieldy
- **SQLite database**: Over-engineered for 10-50 devices; adds a dependency; violates Constitution VII

## 4. Theme Migration Strategy

**Decision**: Replace custom ThemeService/ColorProfile system with Wpf.Ui's built-in theme management

**Rationale**:
- Wpf.Ui provides `ApplicationThemeManager` with Dark/Light/HighContrast/System modes out of the box
- Custom accent colors are supported via `ApplicationAccentColorManager.Apply(Color)`
- The existing 6-brush custom color profile system (PrimaryBackground, SecondaryBackground, etc.) becomes redundant — Wpf.Ui manages ~200 fluent theme resources automatically
- Settings page exposes theme selection (light/dark/system) as a simple radio group

**Migration path**:
- Remove: `ThemeColors.xaml`, `ThemeStyles.xaml`, `ThemeResourceKeys.cs`, `ColorProfile.cs`, `PresetProfiles.cs`, `ThemeService.cs`, `IThemeService.cs`, `IProfileStore.cs`, `JsonProfileStore.cs`, `ThemeSettingsWindow.xaml`, `ThemeSettingsViewModel.cs`
- Add: Wpf.Ui `ThemesDictionary` + `ControlsDictionary` in `App.xaml`
- Keep `AppSettings.ActiveProfileName` → repurpose as `Theme` enum (Light, Dark, System)
- Adapt any tests that depend on the old theme service

## 5. Titlebar Toggle (Navigation ↔ Remote Desktop)

**Decision**: Device window uses Wpf.Ui `TitleBar` with custom buttons. A toggle button switches the content area between the `NavigationView` and a `WindowsFormsHost` containing the RDP ActiveX control.

**Rationale**:
- Wpf.Ui `TitleBar` supports custom content (buttons, text) in the title area
- The toggle simply swaps `Visibility` of two content panels (NavigationView grid vs RDP grid) — keeps both alive in the visual tree for instant switching (SC-004: <200ms)
- The RDP control cannot be created/destroyed repeatedly (COM lifecycle issues per POC learnings) — it must stay alive and be shown/hidden

**Alternatives considered**:
- **Recreate NavigationView on toggle**: Slower; loses scroll position and page state
- **Overlay the RDP control**: Z-order issues with WindowsFormsHost airspace problem; ActiveX controls always render on top

## 6. Autohide Titlebar

**Decision**: Use a `MouseMove` handler on the device window to detect cursor proximity to the top edge. Animate `TitleBar` height between 0 and normal height using a `Storyboard`.

**Rationale**:
- Simple proximity detection: if cursor Y is within 8 pixels of the window's top edge, reveal; otherwise, hide after a 1-second delay
- WPF `DoubleAnimation` on `Height` provides smooth reveal/hide
- The titlebar remains in the visual tree (just collapsed height), so all buttons remain functional

## 7. Multi-Monitor Fullscreen (Use All Monitors)

**Decision**: When "Use all monitors when fullscreen" is enabled and the window is maximized, set `WindowStyle=None` and resize to the bounding rectangle of all screens.

**Rationale**:
- WPF `SystemParameters.VirtualScreenLeft/Top/Width/Height` provides the bounding box of all monitors
- Setting `WindowState=Normal` + `WindowStyle=None` + manual positioning avoids the default WPF maximize behavior (which targets a single monitor)
- The existing `RdpClientHost.DesktopWidth/Height` properties can be set to match the full span

## 8. Task View Button

**Decision**: Send `Win+Tab` keypress via `SendInput` P/Invoke (existing `NativeMethods.cs` pattern)

**Rationale**:
- Windows Task View is triggered by `VK_LWIN + VK_TAB`
- The existing `NativeMethods.cs` already contains P/Invoke declarations for keyboard input
- Direct `SendInput` ensures the host machine receives the keypress, not the remote session

## 9. Device Card Screenshot Capture

**Decision**: Capture a screenshot of the RDP control content when the session disconnects, store as PNG in `%APPDATA%/Remort/screenshots/{deviceId}.png`

**Rationale**:
- `RdpClientHost` inherits from `AxHost` which provides `DrawToBitmap()` for capturing the control's visual state
- Capturing on disconnect ensures the screenshot represents the last known state
- PNG file per device is simple to manage (create, overwrite, delete on device removal)
- Cards load the screenshot as `BitmapImage` with file URI; fall back to gradient if file not found
