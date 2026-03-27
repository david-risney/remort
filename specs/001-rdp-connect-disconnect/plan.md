# Implementation Plan: RDP Connect & Disconnect

**Branch**: `001-rdp-connect-disconnect` | **Date**: 2026-03-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-rdp-connect-disconnect/spec.md`

## Summary

Implement the core RDP connect/disconnect workflow: a hostname text field, Connect/Disconnect buttons, an embedded RDP session via the MsRdpClient ActiveX control, and a connection-state status display. The COM interop layer is adapted from the proven `poc/StickyDesktop/` prototype, restructured into the MVVM architecture defined in `docs/architecture.md`. The ViewModel drives all state and commands; the View is responsible only for hosting the `WindowsFormsHost` + `RdpClientHost` control.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0-windows`)
**Primary Dependencies**: WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), MsRdpClient10 ActiveX (`mstscax.dll`)
**Storage**: N/A
**Testing**: xUnit, FluentAssertions, NSubstitute
**Target Platform**: Windows 10/11 desktop (x64)
**Project Type**: desktop-app (WPF)
**Performance Goals**: Connect-to-visible in <30 s (excl. auth/network), disconnect-to-ready in <3 s
**Constraints**: Single session at a time, Windows-only, standard credential prompt for auth
**Scale/Scope**: Single window, single connection, ~10 new files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. MVVM-First | ✅ PASS | `MainWindowViewModel` owns all state/commands. Code-behind limited to `InitializeComponent()` + `WindowsFormsHost.Child` wiring. ViewModel has no WPF type references. |
| II. COM Interop Isolation | ✅ PASS | All COM types (`RdpClientHost`, `IMsTscAxEvents`, `MsTscAxEventsSink`, `EventSinkCookie`) live in `Interop/`. No `dynamic` or COM calls escape that folder. |
| III. Test-First | ✅ PASS | `IRdpClient` interface allows ViewModel unit tests with NSubstitute mocks. Tests written alongside implementation. |
| IV. Zero Warnings | ✅ PASS | Project already has `TreatWarningsAsErrors`, `AnalysisLevel=latest-all`, StyleCop. One type per file in Interop/. |
| V. Specification Before Code | ✅ PASS | `spec.md` and `checklists/requirements.md` already exist and are approved. |
| VI. Layered Dependencies | ✅ PASS | View → ViewModel → IRdpClient (interface in Connection/) → RdpClientHost (Interop/). No upward or cross-layer refs. |
| VII. Simplicity | ✅ PASS | No DI container, no service layer beyond `IRdpClient`. ViewModel talks directly to the interface. Minimal abstractions. |

**Pre-design gate: PASS — no violations.**
**Post-design gate: PASS — design artifacts (data-model.md, contracts/, research.md) reviewed. IRdpClient interface preserves COM isolation (II). ViewModel has no WPF/COM references (I). One type per file except grouped EventArgs (IV). No speculative abstractions (VII).**

## Project Structure

### Documentation (this feature)

```text
specs/001-rdp-connect-disconnect/
├── plan.md              # This file
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: entities, state machine, interface
├── quickstart.md        # Phase 1: build & run instructions
├── contracts/
│   └── irdpclient.md    # Phase 1: IRdpClient interface contract
└── tasks.md             # Phase 2: task list (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Remort/
│   ├── App.xaml                        # (updated) — unchanged for this feature
│   ├── App.xaml.cs                     # (updated) — unchanged for this feature
│   ├── MainWindow.xaml                 # (updated) — connection bar, status bar, WindowsFormsHost
│   ├── MainWindow.xaml.cs              # (updated) — create RdpClientHost, wire ViewModel
│   ├── Remort.csproj                   # (updated) — add CommunityToolkit.Mvvm package
│   ├── Connection/
│   │   ├── ConnectionState.cs          # Enum: Disconnected, Connecting, Connected
│   │   ├── IRdpClient.cs              # Interface abstracting RdpClientHost
│   │   └── MainWindowViewModel.cs     # ViewModel: hostname, state, commands
│   └── Interop/
│       ├── RdpClientHost.cs           # AxHost subclass (adapted from POC)
│       ├── IMsTscAxEvents.cs          # COM event dispatch interface [ComImport]
│       ├── MsTscAxEventsSink.cs       # [ComVisible] event sink implementation
│       ├── EventSinkCookie.cs         # IConnectionPoint Advise/Unadvise helper
│       └── RdpEventArgs.cs            # DisconnectedEventArgs, WarningEventArgs, FatalErrorEventArgs
└── Remort.Tests/
    └── Connection/
        └── MainWindowViewModelTests.cs # ViewModel unit tests with mocked IRdpClient
```

**Structure Decision**: Single WPF project (`src/Remort/`) with domain folders per `docs/architecture.md`. This feature adds `Connection/` (Connection Management domain) and `Interop/` (COM interop layer). Views and ViewModels co-locate in the same folder per `CODING-STYLE.md`. The test project mirrors the source structure.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
