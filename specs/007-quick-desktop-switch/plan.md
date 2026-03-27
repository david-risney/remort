# Implementation Plan: Quick Virtual Desktop Switching

**Branch**: `007-quick-desktop-switch` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/007-quick-desktop-switch/spec.md`

## Summary

Provide an in-app desktop switcher control that lists all Windows virtual desktops, highlights the active one, and lets users switch desktops without leaving Remort. Desktop enumeration and current-desktop detection use the Windows Registry (`HKCU\...\VirtualDesktops`), which is stable across Windows 10/11 builds. Desktop switching uses `SendInput` P/Invoke to simulate `Win+Ctrl+Arrow` keyboard shortcuts — avoiding undocumented `IVirtualDesktopManagerInternal` COM interfaces that break across Windows updates. In-app keyboard shortcuts (`Ctrl+Alt+Left/Right`) provide a keyboard-driven alternative. The feature coexists with the pin-to-desktop setting from spec 005.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0-windows`)
**Primary Dependencies**: WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), `user32.dll` (`SendInput` P/Invoke), `Microsoft.Win32.Registry`
**Storage**: JSON settings file (`%APPDATA%/Remort/settings.json`) — no new settings fields required for this feature
**Testing**: xUnit, FluentAssertions, NSubstitute
**Target Platform**: Windows 10/11 desktop (x64)
**Project Type**: desktop-app (WPF)
**Performance Goals**: Desktop switch response <500ms from user click/keypress (SC-006); active desktop indicator updates within 1s of any switch (SC-002)
**Constraints**: Windows-only; no undocumented COM interfaces; registry-based enumeration; keyboard-simulation switching; must not interfere with active RDP sessions
**Scale/Scope**: ~8 new/modified files, 1 new service interface + implementation, 1 P/Invoke wrapper, ViewModel extensions, XAML additions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. MVVM-First | ✅ PASS | Desktop list and switching logic live in a service behind `IDesktopSwitcherService`. ViewModel exposes `ObservableCollection<VirtualDesktopInfo>` and `[RelayCommand]` for switching. No WPF types in ViewModel or service. |
| II. COM Interop Isolation | ✅ PASS | The `SendInput` P/Invoke and registry reads are confined to dedicated files in `Interop/` and `VirtualDesktop/`. ViewModel never touches `IntPtr`, P/Invoke, or registry. The existing `IVirtualDesktopManager` COM interface is used only indirectly through `IVirtualDesktopService`. |
| III. Test-First | ✅ PASS | `IDesktopSwitcherService` is mockable — ViewModel tests simulate desktop lists and switch operations without real virtual desktops, P/Invoke, or registry. Registry-reading and input-simulation code are behind interfaces. |
| IV. Zero Warnings | ✅ PASS | P/Invoke declarations will use `LibraryImport` source generation where possible. `#pragma warning disable` only for `CA1031` on COM/P/Invoke boundaries (established pattern). |
| V. Specification Before Code | ✅ PASS | `spec.md` and `checklists/requirements.md` exist and are validated for 007. |
| VI. Layered Dependencies | ✅ PASS | View → ViewModel → `IDesktopSwitcherService` → `Interop/NativeMethods` (P/Invoke) + Registry. Follows the same pattern as `IConnectionService` and `IVirtualDesktopService`. |
| VII. Simplicity | ✅ PASS | Registry reading + keyboard simulation is the simplest approach that avoids undocumented COM. No third-party virtual desktop libraries. No background services — polling on a timer (same pattern as `DesktopSwitchDetector`). |

**Pre-design gate: PASS — no violations.**
**Post-design gate: PASS — design adds one service (`IDesktopSwitcherService`) and one P/Invoke wrapper. No new abstractions beyond what's needed. No speculative features. Keyboard simulation is the simplest reliable switching mechanism. Registry enumeration avoids undocumented COM.**

## Project Structure

### Documentation (this feature)

```text
specs/007-quick-desktop-switch/
├── plan.md              # This file
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: entities, service interface
├── quickstart.md        # Phase 1: build & run instructions
├── contracts/
│   └── idesktopswitcherservice.md  # Phase 1: IDesktopSwitcherService interface contract
└── tasks.md             # Phase 2: task list (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Remort/
│   ├── MainWindow.xaml              # (modified) — add desktop switcher ComboBox to connection bar
│   ├── MainWindow.xaml.cs           # (modified) — wire IDesktopSwitcherService, add InputBindings
│   ├── Connection/
│   │   └── MainWindowViewModel.cs   # (modified) — add desktop list, switch commands, refresh logic
│   ├── Interop/
│   │   └── NativeMethods.cs         # NEW — SendInput P/Invoke declarations
│   └── VirtualDesktop/
│       ├── IDesktopSwitcherService.cs   # NEW — interface for enumeration + switching
│       ├── DesktopSwitcherService.cs    # NEW — registry enumeration + SendInput switching
│       └── VirtualDesktopInfo.cs        # NEW — record: Id, Name, Index, IsCurrent
└── Remort.Tests/
    └── VirtualDesktop/
        ├── DesktopSwitcherServiceTests.cs   # NEW — service logic unit tests
        └── DesktopSwitchViewModelTests.cs   # NEW — ViewModel switching command tests
```

**Structure Decision**: All new code follows the existing `VirtualDesktop/` domain folder pattern. P/Invoke declarations go in `Interop/NativeMethods.cs` (Constitution II). The service interface + implementation live alongside the existing `IVirtualDesktopService`. The `VirtualDesktopInfo` record is the data model for the desktop list — it lives in the domain folder, not in a separate `Models/` folder, consistent with project conventions.

## Complexity Tracking

> No constitution violations to justify — all principles pass.
