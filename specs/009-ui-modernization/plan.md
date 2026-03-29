# Implementation Plan: Modern Windows UI

**Branch**: `009-ui-modernization` | **Date**: 2026-03-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-ui-modernization/spec.md`

## Summary

Replace the existing single-window layout with a modern Windows 11-style UI using the WPF UI (Wpf.Ui) library. Introduce two window types: a main window with a `NavigationView` (Favorites, Devices, Settings pages) showing device cards, and per-device windows with navigation (Connection, General, Display, Redirections pages) plus a titlebar toggle to switch between settings and the remote desktop view.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0-windows`)
**Primary Dependencies**: WPF, CommunityToolkit.Mvvm, Wpf.Ui (WPF UI library for Windows 11 controls)
**Storage**: JSON file via `ISettingsStore` / `JsonSettingsStore` (existing)
**Testing**: xUnit, FluentAssertions, NSubstitute, Verify
**Target Platform**: Windows 10/11 desktop
**Project Type**: Desktop WPF application
**Performance Goals**: Main window renders device list within 2s; view toggle within 200ms
**Constraints**: Must coexist with `WindowsFormsHost` for ActiveX RDP control hosting; TreatWarningsAsErrors
**Scale/Scope**: ~10-50 devices, 2 window types, ~12 XAML pages/views

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. MVVM-First | ✅ PASS | All UI logic in ViewModels. Views contain only InitializeComponent(). Service interfaces for dialogs/navigation. |
| II. COM Interop Isolation | ✅ PASS | RDP control stays in `Interop/` layer. Device window hosts it via `WindowsFormsHost` — interop code does not leak into ViewModels or views. |
| III. Test-First | ✅ PASS | ViewModels and services are fully testable. UI automation skill available for integration tests. |
| IV. Zero Warnings | ✅ PASS | TreatWarningsAsErrors enforced. Wpf.Ui is a well-maintained NuGet package with no known analyzer conflicts. |
| V. Specification Before Code | ✅ PASS | Spec complete with clarifications. This plan precedes implementation. |
| VI. Layered Dependencies | ✅ PASS | Views → ViewModels → Services → Interop. New UI pages follow the same layered pattern. |
| VII. Simplicity | ✅ PASS | Wpf.Ui provides NavigationView, CardControl, ContentDialog out of the box — no custom control framework. |

**Quality Gates**:
| Gate | Status |
|------|--------|
| Build | Will verify zero warnings after adding Wpf.Ui |
| Tests | All existing 158 tests must continue passing |
| Spec | spec.md + clarifications complete |

**Gate result**: ✅ ALL PASS — proceed to Phase 0.

### Post-Design Re-Check (after Phase 1)

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| I. MVVM-First | ✅ PASS | All pages have dedicated ViewModels. No UI logic in code-behind. Dialog uses ContentDialog VM pattern. |
| II. COM Interop Isolation | ✅ PASS | RdpClientHost stays in Interop/. DeviceWindow hosts it via WindowsFormsHost. Screenshot capture via AxHost.DrawToBitmap() stays in interop layer. |
| III. Test-First | ✅ PASS | All ViewModels and stores have planned test files. Existing 158 tests preserved. |
| IV. Zero Warnings | ✅ PASS | Wpf.Ui well-maintained; no known analyzer conflicts. |
| V. Specification Before Code | ✅ PASS | Full spec → clarify → plan pipeline complete. |
| VI. Layered Dependencies | ✅ PASS | Pages → PageViewModels → Services → Interop. DeviceWindowManager is a service. |
| VII. Simplicity | ✅ PASS | Wpf.Ui provides NavigationView/CardControl/ContentDialog/TitleBar. Device persistence is flat JSON. |

**Post-design gate result**: ✅ ALL PASS

## Project Structure

### Documentation (this feature)

```text
specs/009-ui-modernization/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── Remort/
│   ├── App.xaml(.cs)                    # Updated: Wpf.Ui theme, service registration
│   ├── MainWindow.xaml(.cs)             # Rewritten: NavigationView shell
│   ├── Connection/
│   │   ├── MainWindowViewModel.cs       # Refactored → split into per-page VMs
│   │   ├── IConnectionService.cs        # Existing (unchanged)
│   │   ├── RetryingConnectionService.cs # Existing (unchanged)
│   │   ├── ConnectionState.cs           # Existing (unchanged)
│   │   └── ...                          # Existing files unchanged
│   ├── Devices/                         # NEW: Device management domain
│   │   ├── Device.cs                    # Device data model
│   │   ├── JsonDeviceStore.cs           # CRUD + JSON persistence for devices
│   │   ├── IDeviceStore.cs              # Interface for DeviceStore
│   │   ├── DeviceCardViewModel.cs       # Card display logic
│   │   ├── DevicesPageViewModel.cs      # Devices list page VM
│   │   ├── DevicesPage.xaml(.cs)        # Devices page view
│   │   ├── FavoritesPageViewModel.cs    # Favorites list page VM
│   │   ├── FavoritesPage.xaml(.cs)      # Favorites page view
│   │   ├── AddDeviceDialog.xaml(.cs)    # Add device dialog
│   │   └── AddDeviceDialogViewModel.cs  # Dialog VM
│   ├── DeviceWindow/                    # NEW: Per-device window
│   │   ├── DeviceWindow.xaml(.cs)       # Device window shell with nav + titlebar
│   │   ├── DeviceWindowViewModel.cs     # Device window VM (owns toggle, titlebar)
│   │   ├── ConnectionPage.xaml(.cs)     # Connection settings view
│   │   ├── ConnectionPageViewModel.cs   # Connection page VM
│   │   ├── GeneralPage.xaml(.cs)        # General settings view
│   │   ├── GeneralPageViewModel.cs      # General page VM
│   │   ├── DisplayPage.xaml(.cs)        # Display settings view
│   │   ├── DisplayPageViewModel.cs      # Display page VM
│   │   ├── RedirectionsPage.xaml(.cs)   # Redirections view
│   │   └── RedirectionsPageViewModel.cs # Redirections page VM
│   ├── DevBox/                          # Existing (minor: integrate discovered devices into DeviceStore)
│   ├── Interop/                         # Existing (unchanged)
│   ├── Settings/
│   │   ├── AppSettings.cs              # Extended: add global defaults
│   │   ├── ISettingsStore.cs           # Existing (unchanged)
│   │   ├── JsonSettingsStore.cs        # Extended: device list persistence
│   │   ├── SettingsPage.xaml(.cs)      # NEW: Settings page view
│   │   └── SettingsPageViewModel.cs    # NEW: Settings page VM
│   ├── Theme/                          # Existing → migrated to Wpf.Ui theme system
│   ├── VirtualDesktop/                 # Existing (unchanged)
│   └── Converters/                     # Existing (may add new converters)
└── Remort.Tests/
    ├── Devices/                        # NEW: DeviceStore, DeviceCardVM tests
    ├── DeviceWindow/                   # NEW: ConnectionPageVM, GeneralPageVM tests
    ├── Connection/                     # Existing tests (unchanged)
    ├── DevBox/                         # Existing tests (unchanged)
    ├── Theme/                          # Existing tests (may update for Wpf.Ui migration)
    ├── VirtualDesktop/                 # Existing tests (unchanged)
    └── EndToEnd/                       # Existing E2E tests (update for new window structure)
```

**Structure Decision**: Two new domain folders (`Devices/`, `DeviceWindow/`) following the existing pattern where Views and ViewModels live in the same feature folder. The existing `Connection/` services are reused from `DeviceWindow/` ViewModels. No new projects — all code in the single `Remort` project per Constitution VII (Simplicity).

## Complexity Tracking

No constitution violations. No complexity justifications needed.
