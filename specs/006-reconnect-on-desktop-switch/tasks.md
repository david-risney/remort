# Tasks: Reconnect RDP Session on Virtual Desktop Switch

**Input**: Design documents from `/specs/006-reconnect-on-desktop-switch/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/idesktopswitchdetector.md ✅, quickstart.md ✅

**Tests**: Included — plan.md specifies dedicated test files (`DesktopSwitchDetectorTests.cs`, `ReconnectOnDesktopSwitchTests.cs`).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Extend existing project infrastructure with shared plumbing needed by all user stories

- [X] T001 Add `ReconnectOnDesktopSwitchEnabled` property to `AppSettings` record in src/Remort/Settings/AppSettings.cs
- [X] T002 Add `IsOnCurrentDesktop(IntPtr hwnd)` method to `IVirtualDesktopService` interface in src/Remort/VirtualDesktop/IVirtualDesktopService.cs
- [X] T003 Implement `IsOnCurrentDesktop(IntPtr hwnd)` in `VirtualDesktopService` wrapping `IVirtualDesktopManager.IsWindowOnCurrentVirtualDesktop` with COMException handling and graceful degradation (return `true` if unsupported) in src/Remort/VirtualDesktop/VirtualDesktopService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the `IDesktopSwitchDetector` interface and implementation — required before any user story can wire the reconnect flow

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Create `IDesktopSwitchDetector` interface with `SwitchedToDesktop` event, `StartMonitoring(IntPtr)`, `StopMonitoring()`, and `IDisposable` in src/Remort/VirtualDesktop/IDesktopSwitchDetector.cs per contracts/idesktopswitchdetector.md
- [X] T005 Implement `DesktopSwitchDetector` class using `DispatcherTimer` (500ms interval) that polls `IVirtualDesktopService.IsOnCurrentDesktop` and raises `SwitchedToDesktop` on `false→true` transitions in src/Remort/VirtualDesktop/DesktopSwitchDetector.cs. Initialize `_wasOnCurrentDesktop = true` to prevent false trigger on startup. No-op when `hwnd` is `IntPtr.Zero`. Idempotent `StartMonitoring`/`StopMonitoring`. `Dispose` stops monitoring.
- [X] T006 Write unit tests for `DesktopSwitchDetector` in src/Remort.Tests/VirtualDesktop/DesktopSwitchDetectorTests.cs: verify `SwitchedToDesktop` fires on `false→true` transition, does NOT fire on `true→true` steady state, does NOT fire on `true→false`, does NOT fire after `StopMonitoring`, no-op when `hwnd` is zero, idempotent start/stop. Use mocked `IVirtualDesktopService`.

**Checkpoint**: `IDesktopSwitchDetector` interface and implementation ready with passing tests. User story implementation can now begin.

---

## Phase 3: User Story 1 — Automatically Reconnect When Pinned Desktop Becomes Visible (Priority: P1) 🎯 MVP

**Goal**: When the user switches to the virtual desktop where Remort is pinned and the RDP session is disconnected, Remort automatically initiates a reconnection to the last connected host.

**Independent Test**: Connect to a host on Desktop 2 with pin-to-desktop enabled → switch to Desktop 1 → wait for disconnect → switch back to Desktop 2 → verify Remort auto-reconnects.

### Tests for User Story 1

- [X] T007 [P] [US1] Write unit tests for `TryReconnectOnDesktopSwitch()` in src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs: verify reconnect initiates when all preconditions met (feature enabled, pin enabled, disconnected, last host exists); verify no-op when feature disabled; verify no-op when pin disabled; verify no-op when already connected; verify no-op when already connecting; verify no-op when no last host; verify no-op when `LastConnectedHost` is empty. Use mocked `IConnectionService`, `ISettingsStore`, `IDesktopSwitchDetector`.

### Implementation for User Story 1

- [X] T008 [US1] Add `ReconnectOnDesktopSwitchEnabled` observable property (`[ObservableProperty]`) to `MainWindowViewModel` in src/Remort/Connection/MainWindowViewModel.cs. Load from settings in constructor. Persist in `partial void OnReconnectOnDesktopSwitchEnabledChanged(bool value)` using the same `ISettingsStore` pattern as `AutoReconnectEnabled`.
- [X] T009 [US1] Add `TryReconnectOnDesktopSwitch()` public method to `MainWindowViewModel` in src/Remort/Connection/MainWindowViewModel.cs. Check preconditions: `ReconnectOnDesktopSwitchEnabled`, `PinToDesktopEnabled`, `ConnectionState == Disconnected`, `LastConnectedHost` non-empty. If all pass, set `_isAutoReconnect = true`, load last host from settings, set `Hostname` and `_connectionService.Server`, transition to `Connecting`, and call `_connectionService.Connect()`. Reuse existing `_isAutoReconnect` flag for status text.
- [X] T010 [US1] Extract shared `ReconnectToLastHost()` private helper from the common connect-last-host logic in `TryAutoReconnect()` and `TryReconnectOnDesktopSwitch()` in src/Remort/Connection/MainWindowViewModel.cs to eliminate duplication.
- [X] T011 [US1] Wire `DesktopSwitchDetector` in `MainWindow.xaml.cs`: create `DesktopSwitchDetector` in constructor, subscribe `SwitchedToDesktop` to call `_viewModel.TryReconnectOnDesktopSwitch()`, call `StartMonitoring(hwnd)` in `OnSourceInitialized`, call `StopMonitoring()` and dispose in `OnClosing` in src/Remort/MainWindow.xaml.cs

**Checkpoint**: Auto-reconnect on desktop switch works end-to-end. All US1 tests pass. Feature is gated by both `ReconnectOnDesktopSwitchEnabled` and `PinToDesktopEnabled`.

---

## Phase 4: User Story 2 — Enable or Disable Reconnect on Desktop Switch (Priority: P2)

**Goal**: Users can toggle the reconnect-on-desktop-switch feature on/off via a checkbox in the UI. Default is off. Setting persists across sessions. Toggle is disabled when pin-to-desktop is off.

**Independent Test**: Toggle the setting off → switch desktops → verify no reconnect. Toggle on → switch desktops → verify reconnect. Close and reopen app → verify setting persists.

### Implementation for User Story 2

- [X] T012 [US2] Add "Reconnect on desktop switch" `CheckBox` to the connection bar in src/Remort/MainWindow.xaml. Bind `IsChecked` to `ReconnectOnDesktopSwitchEnabled`. Bind `IsEnabled` to `PinToDesktopEnabled`. Add `ToolTip` "Requires 'Pin to desktop' to be enabled" to indicate the dependency per FR-008.
- [X] T013 [US2] Verify default-off behavior: confirm `AppSettings.ReconnectOnDesktopSwitchEnabled` defaults to `false` and the ViewModel initializes the property from settings (already done in T001/T008 — validate the round-trip in an existing or new test in src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs).

**Checkpoint**: Toggle visible in UI, grayed out when pin is disabled, persists between sessions, takes effect immediately (FR-014).

---

## Phase 5: User Story 3 — See Reconnect-on-Switch Status Feedback (Priority: P3)

**Goal**: When auto-reconnect fires from a desktop switch, the status bar shows clear feedback: "Auto-reconnecting to host…" during attempt, "Connected" on success, failure message on failure.

**Independent Test**: Enable feature → disconnect → switch to pinned desktop → observe status area shows "Auto-reconnecting to {host}…" then "Connected" (or failure message).

### Implementation for User Story 3

- [X] T014 [P] [US3] Add test verifying status text shows "Auto-reconnecting to {host}…" when `TryReconnectOnDesktopSwitch` initiates a connection (verify `_isAutoReconnect` is `true` so existing `OnAttemptStarted` produces the right text) in src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs
- [X] T015 [P] [US3] Add test verifying status text shows failure message with reason when auto-reconnect from desktop switch exhausts retries (verify `OnRetriesExhausted` produces "Auto-reconnect failed…" text) in src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs

**Checkpoint**: Status feedback is consistent with existing auto-reconnect (spec 004). No new status logic needed — the `_isAutoReconnect` flag reuse handles it. US3 tests confirm the integration.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validation, edge cases, cleanup

- [X] T016 [P] Verify coexistence with auto-reconnect-on-login (spec 004): both `TryAutoReconnect()` and `TryReconnectOnDesktopSwitch()` check `ConnectionState == Disconnected` — only one wins when both fire simultaneously. Add a test for this race condition in src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs
- [X] T017 Run `dotnet build Remort.sln` — zero errors and zero warnings required
- [X] T018 Run `dotnet test Remort.sln` — all tests pass
- [ ] T019 Run quickstart.md manual test checklist (scenarios 1–13) for end-to-end validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (T002 for `IsOnCurrentDesktop`) — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 completion (needs `IDesktopSwitchDetector`)
- **US2 (Phase 4)**: Depends on T008 from US1 (needs `ReconnectOnDesktopSwitchEnabled` property on ViewModel)
- **US3 (Phase 5)**: Depends on T009 from US1 (needs `TryReconnectOnDesktopSwitch()` method to exist)
- **Polish (Phase 6)**: Depends on all user stories being complete

### Within Each User Story

- Tests written alongside or before implementation
- Interface/model changes before service implementation
- Service before ViewModel integration
- ViewModel before View wiring

### Parallel Opportunities

- T001, T002 can run in parallel (different files)
- T006, T007 can run in parallel (different test files)
- T014, T015 can run in parallel (same file but independent tests)
- T016 can run in parallel with T017–T019
- US2 (Phase 4) and US3 (Phase 5) can run in parallel once US1 core (T008, T009) is done

---

## Parallel Example: Phase 1 (Setup)

```
# These modify different files and can run in parallel:
T001: AppSettings.cs (add property)
T002: IVirtualDesktopService.cs (add method signature)

# Then sequentially:
T003: VirtualDesktopService.cs (implements T002)
```

## Parallel Example: US1 Tests + US3 Tests

```
# Once Phase 2 is done, test files can be created in parallel:
T007: ReconnectOnDesktopSwitchTests.cs (US1 tests)
T006: DesktopSwitchDetectorTests.cs (foundation tests — already done in Phase 2)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T006)
3. Complete Phase 3: User Story 1 (T007–T011)
4. **STOP and VALIDATE**: Build, test, manual scenario 1
5. Core reconnect-on-desktop-switch is working end-to-end

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. Add US1 → Test independently → Core feature working (MVP!)
3. Add US2 → Test independently → User toggle and persistence
4. Add US3 → Test independently → Status feedback confirmed
5. Polish → Full validation against quickstart checklist

### File Impact Summary

| File | Action | Phase |
|------|--------|-------|
| src/Remort/Settings/AppSettings.cs | MODIFIED | Phase 1 |
| src/Remort/VirtualDesktop/IVirtualDesktopService.cs | MODIFIED | Phase 1 |
| src/Remort/VirtualDesktop/VirtualDesktopService.cs | MODIFIED | Phase 1 |
| src/Remort/VirtualDesktop/IDesktopSwitchDetector.cs | NEW | Phase 2 |
| src/Remort/VirtualDesktop/DesktopSwitchDetector.cs | NEW | Phase 2 |
| src/Remort.Tests/VirtualDesktop/DesktopSwitchDetectorTests.cs | NEW | Phase 2 |
| src/Remort/Connection/MainWindowViewModel.cs | MODIFIED | Phase 3 |
| src/Remort/MainWindow.xaml.cs | MODIFIED | Phase 3 |
| src/Remort.Tests/VirtualDesktop/ReconnectOnDesktopSwitchTests.cs | NEW | Phase 3 |
| src/Remort/MainWindow.xaml | MODIFIED | Phase 4 |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- The `_isAutoReconnect` flag from spec 004 is reused — no separate "desktop switch reconnect" status text needed (R5)
- The 500ms `DispatcherTimer` provides natural debounce for rapid desktop switches (R2, FR-013)
- `DesktopSwitchDetector` always runs its timer; the ViewModel's precondition checks gate the reconnect (R8)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
