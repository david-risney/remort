# Tasks: Pin Window to Virtual Desktop

**Input**: Design documents from `/specs/005-pin-window-virtual-desktop/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ivirtualdesktopservice.md ✅, quickstart.md ✅

**Tests**: Not explicitly requested in the feature specification. Test tasks are omitted unless otherwise directed.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: New domain folder and COM interop declarations

- [X] T001 [P] Declare `IVirtualDesktopManager` COM interface and `VirtualDesktopManagerClass` coclass in `src/Remort/Interop/IVirtualDesktopManager.cs`
- [X] T002 [P] Create `IVirtualDesktopService` interface in `src/Remort/VirtualDesktop/IVirtualDesktopService.cs`
- [X] T003 [P] Add `PinToDesktopEnabled` property to `AppSettings` record in `src/Remort/Settings/AppSettings.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Service implementation that all user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Implement `VirtualDesktopService` (wraps `IVirtualDesktopManager` COM calls, graceful degradation) in `src/Remort/VirtualDesktop/VirtualDesktopService.cs`

**Checkpoint**: Foundation ready — `IVirtualDesktopService` is implemented and can be injected into the ViewModel

---

## Phase 3: User Story 1 — Pin Connection Window to Current Virtual Desktop (Priority: P1) 🎯 MVP

**Goal**: When pin-to-desktop is enabled, the Remort window stays on its current virtual desktop and does not follow the user when switching desktops.

**Independent Test**: Open Remort on Virtual Desktop 1, enable pin, switch to Virtual Desktop 2 — Remort window does not appear there. Switch back — window is visible where it was left.

### Implementation for User Story 1

- [X] T005 [US1] Add `IVirtualDesktopService` dependency to `MainWindowViewModel` constructor and load `PinToDesktopEnabled` from settings on startup in `src/Remort/Connection/MainWindowViewModel.cs`
- [X] T006 [US1] Add `[ObservableProperty] PinToDesktopEnabled` with `OnPinToDesktopEnabledChanged` partial method that calls `IVirtualDesktopService.PinToCurrentDesktop`/`Unpin` and persists the setting in `src/Remort/Connection/MainWindowViewModel.cs`
- [X] T007 [US1] Wire `VirtualDesktopService` creation and HWND retrieval (via `WindowInteropHelper`) in `MainWindow.xaml.cs`, pass service to ViewModel, and apply initial pin state after `SourceInitialized` in `src/Remort/MainWindow.xaml.cs`

**Checkpoint**: Pinning works end-to-end. Window stays on its desktop when switching. Setting persists across restarts.

---

## Phase 4: User Story 2 — Toggle Pin-to-Desktop On and Off (Priority: P2)

**Goal**: A user-facing toggle (checkbox) in the connection bar lets users enable or disable pinning. Change takes effect immediately. Default is off.

**Independent Test**: Toggle pin off → switch desktops → window follows. Toggle pin on → switch desktops → window stays. Close and reopen → toggle state is remembered.

### Implementation for User Story 2

- [X] T008 [US2] Add "Pin to desktop" `CheckBox` bound to `PinToDesktopEnabled` in the connection bar area of `src/Remort/MainWindow.xaml`

**Checkpoint**: Users can toggle pinning on/off from the UI. Immediate effect, persisted across sessions.

---

## Phase 5: User Story 3 — Visual Indicator of Pinned State (Priority: P3)

**Goal**: A pin icon in the status bar shows when the window is pinned, hidden when unpinned.

**Independent Test**: Enable pin → pin icon visible in status bar. Disable pin → icon disappears.

### Implementation for User Story 3

- [X] T009 [US3] Add pin icon (`TextBlock` with 📌 or `Path` vector) to the `StatusBar` in `src/Remort/MainWindow.xaml`, bound to `PinToDesktopEnabled` with `BooleanToVisibilityConverter`

**Checkpoint**: Visual indicator reflects pin state in real time.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation, edge cases, and cleanup

- [X] T010 [P] Add `VirtualDesktopPinTests` (ViewModel pin toggle with mocked `IVirtualDesktopService`) in `src/Remort.Tests/VirtualDesktop/VirtualDesktopPinTests.cs`
- [X] T011 Build solution with zero warnings (`dotnet build Remort.sln`) and run all tests (`dotnet test Remort.sln`)
- [ ] T012 Run `quickstart.md` manual test checklist to validate end-to-end behavior

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001, T002, T003 can all run in parallel
- **Foundational (Phase 2)**: T004 depends on T001 (COM interface) and T002 (service interface)
- **User Story 1 (Phase 3)**: Depends on Phase 2 (T004) + Phase 1 (T003 for settings)
- **User Story 2 (Phase 4)**: Depends on Phase 3 (T006 provides the `PinToDesktopEnabled` property to bind)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (T006 provides the `PinToDesktopEnabled` property to bind)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational phase — delivers core pinning behavior
- **User Story 2 (P2)**: Depends on US1 (needs the ViewModel property wired) — adds UI control
- **User Story 3 (P3)**: Depends on US1 (needs the ViewModel property wired) — adds visual feedback
- **US2 and US3** can proceed in parallel once US1 is complete (they modify different parts of `MainWindow.xaml`)

### Within Each User Story

- ViewModel changes before View wiring
- Settings changes before ViewModel loading
- Core behavior before UI surface

### Parallel Opportunities

```
Phase 1 (all parallel):
  T001: IVirtualDesktopManager COM interface
  T002: IVirtualDesktopService interface
  T003: AppSettings property

Phase 2 (sequential, depends on T001+T002):
  T004: VirtualDesktopService implementation

Phase 3 (sequential within, depends on Phase 2):
  T005 → T006 → T007

Phase 4 + Phase 5 (parallel after Phase 3):
  T008: UI toggle checkbox     ─┐ (different XAML sections)
  T009: Status bar pin icon    ─┘

Phase 6 (after all stories):
  T010: Unit tests (parallel with T011)
  T011 → T012 (sequential)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003, parallel)
2. Complete Phase 2: Foundational (T004)
3. Complete Phase 3: User Story 1 (T005–T007)
4. **STOP and VALIDATE**: Build, run, test pinning manually on two virtual desktops
5. Core value delivered — window stays on its desktop

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. Add User Story 1 → Test pinning → MVP delivered
3. Add User Story 2 → Test toggle → User control added
4. Add User Story 3 → Test indicator → Visual feedback added
5. Polish → Tests, build validation, quickstart checklist

---

## Notes

- All COM interop is isolated in `Interop/` and `VirtualDesktop/` — ViewModel never touches `IntPtr` or COM types
- `VirtualDesktopService` gracefully degrades (silent no-op) if COM is unavailable (e.g., old Windows, RDP session)
- HWND must be retrieved after `SourceInitialized` — not in the `Window` constructor
- The `Unpin` method is a lightweight reset of internal state (no COM call needed)
- `BooleanToVisibilityConverter` is built into WPF — no custom converter required for the pin icon
