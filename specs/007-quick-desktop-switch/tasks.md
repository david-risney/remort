# Tasks: Quick Virtual Desktop Switching

**Input**: Design documents from `/specs/007-quick-desktop-switch/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/idesktopswitcherservice.md ✅, quickstart.md ✅

**Tests**: Included — the plan and data model specify dedicated test files.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create new files and P/Invoke foundation required by all user stories

- [X] T001 Create `VirtualDesktopInfo` record in `src/Remort/VirtualDesktop/VirtualDesktopInfo.cs` — sealed record with `Guid Id`, `string Name`, `int Index` per data-model.md
- [X] T002 Create `IDesktopSwitcherService` interface in `src/Remort/VirtualDesktop/IDesktopSwitcherService.cs` — `IsSupported`, `GetDesktops()`, `GetCurrentDesktopIndex()`, `SwitchToDesktop()`, `StartMonitoring()`/`StopMonitoring()`, `DesktopsChanged` event, per contracts/idesktopswitcherservice.md
- [X] T003 Create `NativeMethods` static partial class in `src/Remort/Interop/NativeMethods.cs` — `SendInput` P/Invoke declaration, `INPUT`/`INPUTUNION`/`KEYBDINPUT` structs, virtual-key constants (`VK_LWIN`, `VK_LCONTROL`, `VK_LEFT`, `VK_RIGHT`), `INPUT_KEYBOARD`, `KEYEVENTF_KEYUP` per data-model.md

**Checkpoint**: Interface, data record, and P/Invoke declarations compile with `dotnet build Remort.sln`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement `DesktopSwitcherService` — the core service all user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Implement `DesktopSwitcherService` in `src/Remort/VirtualDesktop/DesktopSwitcherService.cs` — constructor checks registry `IsSupported`; `GetDesktops()` reads `VirtualDesktopIDs` binary value, splits into 16-byte GUIDs, resolves names from `Desktops\{GUID}\Name` (fallback "Desktop N"); `GetCurrentDesktopIndex()` reads `CurrentVirtualDesktop` and matches against desktop list; `SwitchToDesktop()` computes delta and sends `Win+Ctrl+Arrow` via `NativeMethods.SendInput` with 50ms inter-step delay; `StartMonitoring()`/`StopMonitoring()` manage 500ms `DispatcherTimer` that polls and raises `DesktopsChanged` when list or current index changes; implements `IDisposable`
- [X] T005 Create `DesktopSwitcherServiceTests` in `src/Remort.Tests/VirtualDesktop/DesktopSwitcherServiceTests.cs` — unit tests covering: `IsSupported` returns false when registry key missing; `GetDesktops()` returns empty list when not supported; `GetCurrentDesktopIndex()` returns -1 when not supported; `SwitchToDesktop()` is no-op when target equals current; verify `VirtualDesktopInfo` record equality and property values

**Checkpoint**: Foundation ready — `DesktopSwitcherService` compiles, tests pass with `dotnet test Remort.sln`

---

## Phase 3: User Story 1 — Switch to a Different Virtual Desktop from Remort (Priority: P1) 🎯 MVP

**Goal**: User selects a virtual desktop from an in-app ComboBox and Windows switches to that desktop

**Independent Test**: Create 2+ virtual desktops, open Remort on Desktop 1, select Desktop 2 from ComboBox, verify Windows switches to Desktop 2

### Tests for User Story 1

- [X] T006 [P] [US1] Create `DesktopSwitchViewModelTests` in `src/Remort.Tests/VirtualDesktop/DesktopSwitchViewModelTests.cs` — tests with mocked `IDesktopSwitcherService`: verify `DesktopList` is populated from `GetDesktops()` on initialization; verify `SwitchToDesktopCommand` calls `SwitchToDesktop()` with correct indices; verify selecting current desktop is no-op; verify `IsDesktopSwitcherSupported` reflects `IsSupported`; verify `DesktopList` is empty when `IsSupported` is false

### Implementation for User Story 1

- [X] T007 [US1] Add desktop switcher properties and commands to `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — add `IDesktopSwitcherService?` field; add `[ObservableProperty]` for `ObservableCollection<VirtualDesktopInfo> DesktopList`, `VirtualDesktopInfo? CurrentDesktop`, `bool IsDesktopSwitcherSupported`; add `[RelayCommand] SwitchToDesktop(VirtualDesktopInfo?)` that calls service with target and current index; add initialization method that checks `IsSupported`, populates `DesktopList` and `CurrentDesktop` from service; add guard flag to prevent re-entrant switching during programmatic `CurrentDesktop` updates
- [X] T008 [US1] Add desktop switcher ComboBox to `MainWindow.xaml` in `src/Remort/MainWindow.xaml` — add `ComboBox` in the connection bar `StackPanel` after the existing reconnect-on-desktop-switch controls; bind `ItemsSource` to `DesktopList`, `SelectedItem` to `CurrentDesktop` (TwoWay), `DisplayMemberPath` to `Name`; bind `Visibility` to `IsDesktopSwitcherSupported` via `BooleanToVisibilityConverter` (FR-010); add label "Desktop:" before the ComboBox
- [X] T009 [US1] Wire `DesktopSwitcherService` in `MainWindow.xaml.cs` in `src/Remort/MainWindow.xaml.cs` — create `DesktopSwitcherService` instance; pass to `MainWindowViewModel`; call `StartMonitoring()` after initialization; call `StopMonitoring()` and `Dispose()` on window `Closing` event

**Checkpoint**: User Story 1 complete — ComboBox shows desktop list, selecting a desktop triggers `Win+Ctrl+Arrow` switch. All tests pass.

---

## Phase 4: User Story 2 — See Which Virtual Desktop Is Currently Active (Priority: P2)

**Goal**: The ComboBox always highlights the current virtual desktop, updating within 1 second of any switch (including external switches)

**Independent Test**: Open Remort on Desktop 1, verify ComboBox shows Desktop 1 selected; switch to Desktop 2 via Task View or Win+Ctrl+Right, return to Remort's desktop, verify ComboBox now shows Desktop 2 as active

### Tests for User Story 2

- [X] T010 [P] [US2] Add active-desktop indicator tests to `src/Remort.Tests/VirtualDesktop/DesktopSwitchViewModelTests.cs` — verify `CurrentDesktop` updates when `DesktopsChanged` fires; verify new desktops appear in `DesktopList` after `DesktopsChanged`; verify removed desktops disappear from `DesktopList` after `DesktopsChanged`; verify `CurrentDesktop` is set to correct item after refresh

### Implementation for User Story 2

- [X] T011 [US2] Subscribe to `DesktopsChanged` event in `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — on event: re-read `GetDesktops()` and `GetCurrentDesktopIndex()`, update `DesktopList` collection (add/remove differences), update `CurrentDesktop` to match current index, use guard flag to suppress `SwitchToDesktop` during programmatic selection change
- [X] T012 [US2] Handle dynamic desktop list changes in `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — when desktop count changes (add/remove), rebuild `DesktopList` from fresh `GetDesktops()` result; preserve selected item if still present; reset to current if selected was removed

**Checkpoint**: User Story 2 complete — active desktop indicator tracks real-time state. Desktop add/remove reflected within 500ms. All tests pass.

---

## Phase 5: User Story 3 — Keyboard Shortcut for In-App Desktop Switching (Priority: P3)

**Goal**: User presses `Ctrl+Alt+Right` or `Ctrl+Alt+Left` while Remort is focused to cycle through virtual desktops

**Independent Test**: Focus Remort window, press `Ctrl+Alt+Right`, verify Windows switches to next desktop; press `Ctrl+Alt+Left`, verify switch to previous desktop

### Tests for User Story 3

- [X] T013 [P] [US3] Add keyboard shortcut command tests to `src/Remort.Tests/VirtualDesktop/DesktopSwitchViewModelTests.cs` — verify `SwitchToNextDesktopCommand` calls `SwitchToDesktop(current+1, current)` on service; verify `SwitchToPreviousDesktopCommand` calls `SwitchToDesktop(current-1, current)`; verify next-desktop on last desktop is no-op (consistent with Windows behaviour); verify previous-desktop on first desktop is no-op; verify commands are disabled when `IsDesktopSwitcherSupported` is false; verify commands are disabled when only one desktop exists

### Implementation for User Story 3

- [X] T014 [US3] Add `SwitchToNextDesktop` and `SwitchToPreviousDesktop` relay commands to `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — `[RelayCommand] SwitchToNextDesktop()` increments current index by 1 (no-op at last desktop); `[RelayCommand] SwitchToPreviousDesktop()` decrements by 1 (no-op at first desktop); both call `SwitchToDesktop()` on service and refresh `CurrentDesktop`
- [X] T015 [US3] Add `InputBindings` to `MainWindow.xaml` in `src/Remort/MainWindow.xaml` — add `<Window.InputBindings>` with `<KeyBinding Key="Right" Modifiers="Ctrl+Alt" Command="{Binding SwitchToNextDesktopCommand}" />` and `<KeyBinding Key="Left" Modifiers="Ctrl+Alt" Command="{Binding SwitchToPreviousDesktopCommand}" />`

**Checkpoint**: User Story 3 complete — keyboard shortcuts cycle through desktops. All tests pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation, edge cases, and final verification

- [X] T016 [P] Verify graceful degradation when virtual desktop API unavailable — ensure `IsDesktopSwitcherSupported` is false and ComboBox is hidden on systems without virtual desktop registry keys (FR-010, SC-005)
- [X] T017 [P] Verify coexistence with pin-to-desktop (spec 005) — switch via ComboBox when Remort is pinned; confirm Remort stays on pinned desktop (FR-009, edge case)
- [X] T018 Run `quickstart.md` validation — execute all 10 verification steps from `specs/007-quick-desktop-switch/quickstart.md` end-to-end
- [X] T019 Run full build and test suite — `dotnet build Remort.sln` (zero warnings) and `dotnet test Remort.sln` (all pass)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs ComboBox and ViewModel wiring)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs ViewModel commands); independent of Phase 4
- **Polish (Phase 6)**: Depends on Phases 3–5

### User Story Dependencies

- **User Story 1 (P1)**: Unblocked after Foundational. Delivers MVP — in-app desktop switching via ComboBox.
- **User Story 2 (P2)**: Depends on US1 wiring (ViewModel + ComboBox must exist). Adds active-desktop tracking.
- **User Story 3 (P3)**: Depends on US1 wiring (ViewModel must exist). Can be worked in parallel with US2 (different files: XAML InputBindings + new commands vs. event subscription logic).

### Within Each User Story

- Tests are written FIRST and should FAIL before implementation
- ViewModel changes before XAML changes (data layer before presentation)
- Code-behind wiring after both ViewModel and XAML are ready

### Parallel Opportunities

**Phase 1**: T001, T002, T003 can all run in parallel (separate files)

**Phase 2**: T004 and T005 are sequential (tests depend on service)

**Phase 3 (US1)**: T006 (test) can start while T007/T008 are in progress (different files). T007 and T008 are parallel (ViewModel vs. XAML). T009 depends on both T007 and T008.

**Phase 4 + 5**: US2 (T010–T012) and US3 (T013–T015) can be worked in parallel — US2 modifies event subscription in ViewModel while US3 adds new commands and XAML InputBindings.

**Phase 6**: T016 and T017 are parallel. T018 and T019 are sequential (final validation).

---

## Parallel Example: User Story 1

```text
# Launch tests and implementation models in parallel:
Task T006: "DesktopSwitchViewModelTests in src/Remort.Tests/VirtualDesktop/"
Task T007: "ViewModel properties/commands in src/Remort/Connection/MainWindowViewModel.cs"
Task T008: "ComboBox XAML in src/Remort/MainWindow.xaml"

# Then wire up (depends on T007 + T008):
Task T009: "Wire service in src/Remort/MainWindow.xaml.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T005)
3. Complete Phase 3: User Story 1 (T006–T009)
4. **STOP and VALIDATE**: ComboBox shows desktops, selecting one switches Windows desktop
5. Delivers core value — in-app desktop switching

### Incremental Delivery

1. Setup + Foundational → data model, interface, service, P/Invoke ready
2. Add User Story 1 → ComboBox switching works → **MVP complete**
3. Add User Story 2 → Active desktop indicator updates in real time
4. Add User Story 3 → Keyboard shortcuts work → **Feature complete**
5. Polish → Edge cases verified, quickstart validated

### Parallel Strategy

With two agents/developers after Phase 2:
- **Agent A**: User Story 1 → then User Story 2 (same ViewModel event logic)
- **Agent B**: (waits for US1 ViewModel wiring) → User Story 3 (XAML + commands)

---

## Notes

- All new files follow existing `VirtualDesktop/` domain folder pattern
- P/Invoke in `Interop/NativeMethods.cs` per Constitution II
- `DispatcherTimer` polling at 500ms matches `DesktopSwitchDetector` pattern (spec 006)
- `ComboBox` uses `BooleanToVisibilityConverter` already present in `src/Remort/Converters/`
- No new NuGet packages required — `Microsoft.Win32.Registry` is part of .NET BCL
- `Ctrl+Alt+Arrow` chosen to avoid conflict with `Win+Ctrl+Arrow` system shortcut (R7)
- Guard flag in ViewModel prevents re-entrant `SwitchToDesktop` during programmatic selection updates
