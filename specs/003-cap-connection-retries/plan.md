# Implementation Plan: Cap Connection Retries

**Branch**: `003-cap-connection-retries` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/003-cap-connection-retries/spec.md`

## Summary

Cap connection retries to a configurable maximum (default 3) instead of allowing the ActiveX control's default infinite-retry behavior. Introduce a `ConnectionRetryPolicy` record and a `RetryingConnectionService` that wraps `IRdpClient` to intercept `Disconnected` events, track attempt counts, and re-invoke `Connect()` up to the limit. Surface retry progress in `StatusText` (e.g., "Connecting… (attempt 2 of 3)") and display a final failure message when retries are exhausted. The retry count setting persists via `System.Text.Json` to a JSON file in the user's app-data folder. The ViewModel delegates retry orchestration to the service, keeping the ViewModel free of retry bookkeeping.

## Technical Context

**Language/Version**: C# / .NET 8 (`net8.0-windows`)
**Primary Dependencies**: WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), MsRdpClient10 ActiveX (`mstscax.dll`), System.Text.Json
**Storage**: JSON settings file (`%APPDATA%/Remort/settings.json`)
**Testing**: xUnit, FluentAssertions, NSubstitute
**Target Platform**: Windows 10/11 desktop (x64)
**Project Type**: desktop-app (WPF)
**Performance Goals**: Retry-to-retry transition <1 s (no artificial delay), exhaustion-to-ready <3 s
**Constraints**: Single session at a time, Windows-only, no backoff between retries (per spec assumptions), retry applies to initial connection only
**Scale/Scope**: ~6 new/modified files, 1 new service, 1 new settings type, 1 new record

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. MVVM-First | ✅ PASS | Retry orchestration lives in `RetryingConnectionService`, not the ViewModel. ViewModel observes events and updates `StatusText`/`ConnectionState`. No WPF types in ViewModel or service. |
| II. COM Interop Isolation | ✅ PASS | The service wraps `IRdpClient` — it never touches `AxHost`, COM interfaces, or `dynamic`. All COM stays in `Interop/`. |
| III. Test-First | ✅ PASS | `RetryingConnectionService` depends on `IRdpClient` (mockable). Settings persistence uses an `ISettingsStore` interface (mockable). All retry logic is unit-testable. |
| IV. Zero Warnings | ✅ PASS | No new suppression pragmas needed. New files follow one-type-per-file. |
| V. Specification Before Code | ✅ PASS | `spec.md` and `checklists/requirements.md` exist and are approved for 003. |
| VI. Layered Dependencies | ✅ PASS | View → ViewModel → `IConnectionService` → `IRdpClient` → Interop. Service is a new layer between ViewModel and `IRdpClient`, consistent with architecture.md's layer diagram. |
| VII. Simplicity | ✅ PASS | One service (`RetryingConnectionService`), one settings interface, one JSON file. No DI container, no state machine library, no retry frameworks (Polly, etc.). |

**Pre-design gate: PASS — no violations.**
**Post-design gate: PASS — design introduces exactly one service layer between ViewModel and IRdpClient, justified by the retry orchestration requirement (spec 001 R2 anticipated this). Settings use basic file I/O with System.Text.Json — no new dependencies. No speculative abstractions.**

## Project Structure

### Documentation (this feature)

```text
specs/003-cap-connection-retries/
├── plan.md              # This file
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: entities, state machine, retry policy
├── quickstart.md        # Phase 1: build & run instructions
├── contracts/
│   └── iconnectionservice.md  # Phase 1: IConnectionService interface contract
└── tasks.md             # Phase 2: task list (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Remort/
│   ├── MainWindow.xaml.cs              # (modified) — wire RetryingConnectionService
│   ├── Connection/
│   │   ├── ConnectionState.cs          # (modified) — add Retrying state
│   │   ├── IRdpClient.cs              # (unchanged)
│   │   ├── IConnectionService.cs      # NEW — abstraction for retry-aware connection
│   │   ├── RetryingConnectionService.cs # NEW — retry orchestration wrapping IRdpClient
│   │   ├── ConnectionRetryPolicy.cs   # NEW — record: MaxAttempts with default
│   │   └── MainWindowViewModel.cs     # (modified) — use IConnectionService, show retry progress
│   ├── Converters/
│   │   └── DisconnectedToBoolConverter.cs  # (unchanged)
│   ├── Settings/
│   │   ├── ISettingsStore.cs          # NEW — interface for settings persistence
│   │   ├── JsonSettingsStore.cs       # NEW — System.Text.Json file-based implementation
│   │   └── AppSettings.cs            # NEW — settings model (MaxRetryCount)
│   └── Interop/                        # (unchanged)
└── Remort.Tests/
    ├── Connection/
    │   ├── MainWindowViewModelTests.cs     # (modified) — add retry-related tests
    │   └── RetryingConnectionServiceTests.cs # NEW — retry logic unit tests
    └── Settings/
        └── JsonSettingsStoreTests.cs       # NEW — settings persistence tests
```

**Structure Decision**: Existing single-project layout per `docs/architecture.md`. This feature adds `Settings/` domain folder (first feature to need persistent settings) and new files in `Connection/`. The `RetryingConnectionService` sits between the ViewModel layer and `IRdpClient`, consistent with the architecture's Services layer.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
