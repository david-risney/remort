# Tasks: Auto-Reconnect on Windows Login

**Input**: Design documents from `/specs/004-auto-reconnect-on-login/`
**Prerequisites**: plan.md ✅, spec.md ✅, data-model.md ✅, research.md ✅, quickstart.md ✅, contracts/auto-reconnect-settings.md ✅

**Tests**: Included — the feature specification references testable ViewModel logic and the project uses xUnit + NSubstitute + FluentAssertions.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Extend existing infrastructure with the two new settings fields required by all user stories.

- [ ] T001 Add `AutoReconnectEnabled` and `LastConnectedHost` properties to `AppSettings` record in src/Remort/Settings/AppSettings.cs
- [ ] T002 Verify build succeeds and existing tests pass (`dotnet build Remort.sln && dotnet test Remort.sln`)

**Checkpoint**: Settings record extended, backward-compatible with existing settings.json files (missing fields deserialize to defaults).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Wire ViewModel settings loading and persistence for the new fields. All user stories depend on these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 Load `AutoReconnectEnabled` and `LastConnectedHost` from `ISettingsStore` in `MainWindowViewModel` constructor in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T004 Add `_isAutoReconnect` private bool field to `MainWindowViewModel` in src/Remort/Connection/MainWindowViewModel.cs (used by US1 and US3 for status text)

**Checkpoint**: Foundation ready — ViewModel reads new settings on startup, build is clean.

---

## Phase 3: User Story 1 — Automatically Reconnect on Windows Login (Priority: P1) 🎯 MVP

**Goal**: When auto-reconnect is enabled and a last host is known, Remort automatically re-establishes the RDP session on Windows unlock/logon without manual intervention.

**Independent Test**: Connect to a host, lock the workstation, unlock it, and verify Remort automatically begins reconnecting to the previous host.

### Tests for User Story 1

- [ ] T005 [P] [US1] Test `TryAutoReconnect` connects when enabled, disconnected, and last host is known in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T006 [P] [US1] Test `TryAutoReconnect` does nothing when `ConnectionState` is not `Disconnected` in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T007 [P] [US1] Test `TryAutoReconnect` does nothing when `AutoReconnectEnabled` is false in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T008 [P] [US1] Test `TryAutoReconnect` does nothing when `LastConnectedHost` is empty in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T009 [P] [US1] Test `ConnectDirect` persists hostname to `LastConnectedHost` via `ISettingsStore` in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T010 [P] [US1] Test `ConnectToDevBoxAsync` persists resolved server to `LastConnectedHost` via `ISettingsStore` in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

### Implementation for User Story 1

- [ ] T011 [US1] Implement `TryAutoReconnect()` public method in `MainWindowViewModel` in src/Remort/Connection/MainWindowViewModel.cs — check `ConnectionState == Disconnected`, `AutoReconnectEnabled == true`, `LastConnectedHost` not empty; set `_isAutoReconnect = true`, set `Hostname` and `_connectionService.Server` to last host, set state to `Connecting`, call `_connectionService.Connect()`
- [ ] T012 [US1] Persist `LastConnectedHost` in `ConnectDirect()` — save hostname to settings via `ISettingsStore` in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T013 [US1] Persist `LastConnectedHost` in `ConnectToDevBoxAsync()` — save resolved server hostname to settings after successful resolution in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T014 [US1] Subscribe to `Microsoft.Win32.SystemEvents.SessionSwitch` in `MainWindow` constructor in src/Remort/MainWindow.xaml.cs — on `SessionUnlock` or `SessionLogon`, call `Dispatcher.Invoke(() => _viewModel.TryAutoReconnect())`
- [ ] T015 [US1] Unsubscribe from `SystemEvents.SessionSwitch` in `MainWindow.OnClosing()` in src/Remort/MainWindow.xaml.cs
- [ ] T016 [US1] Reset `_isAutoReconnect = false` in `ConnectAsync()` (manual connect path) in src/Remort/Connection/MainWindowViewModel.cs

**Checkpoint**: Auto-reconnect triggers on session unlock. Manual connect still works. All Story 1 tests pass.

---

## Phase 4: User Story 2 — Enable or Disable Auto-Reconnect (Priority: P2)

**Goal**: Users can toggle auto-reconnect on or off via a checkbox. The setting defaults to disabled (opt-in) and persists between sessions.

**Independent Test**: Toggle the setting off → lock/unlock → verify no auto-reconnect. Toggle on → lock/unlock → verify auto-reconnect occurs.

### Tests for User Story 2

- [ ] T017 [P] [US2] Test `AutoReconnectEnabled` defaults to false when no settings store in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T018 [P] [US2] Test changing `AutoReconnectEnabled` persists to `ISettingsStore` in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

### Implementation for User Story 2

- [ ] T019 [US2] Add `[ObservableProperty] private bool _autoReconnectEnabled` to `MainWindowViewModel` and implement `OnAutoReconnectEnabledChanged` partial method to persist via `ISettingsStore` in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T020 [US2] Add "Reconnect on sign-in" `CheckBox` bound to `AutoReconnectEnabled` in the connection bar `StackPanel` in src/Remort/MainWindow.xaml — enable only when disconnected (use existing `DisconnectedToBoolConverter`)

**Checkpoint**: Toggle is visible, defaults to off, persists across restarts. Story 1 + Story 2 both work.

---

## Phase 5: User Story 3 — See Auto-Reconnect Status on Login (Priority: P3)

**Goal**: Status area distinguishes automatic reconnections from manual ones, showing "Auto-reconnecting to host…" during auto-reconnect and "Auto-reconnect failed…" on failure.

**Independent Test**: Enable auto-reconnect, lock/unlock, observe status area shows "Auto-reconnecting to [host]…" (not just "Connecting…"). On failure, shows "Auto-reconnect failed after N attempts: [reason]".

### Tests for User Story 3

- [ ] T021 [P] [US3] Test `OnAttemptStarted` shows "Auto-reconnecting to host…" when `_isAutoReconnect` is true in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T022 [P] [US3] Test `OnAttemptStarted` shows "Connecting…" when `_isAutoReconnect` is false in src/Remort.Tests/Connection/MainWindowViewModelTests.cs
- [ ] T023 [P] [US3] Test `OnRetriesExhausted` shows "Auto-reconnect failed…" when `_isAutoReconnect` is true in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

### Implementation for User Story 3

- [ ] T024 [US3] Update `OnAttemptStarted` handler in `MainWindowViewModel` to check `_isAutoReconnect` and format status as "Auto-reconnecting to {host}… (attempt N of M)" in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T025 [US3] Update `OnRetriesExhausted` handler in `MainWindowViewModel` to check `_isAutoReconnect` and format status as "Auto-reconnect failed after N attempts: reason" in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T026 [US3] Reset `_isAutoReconnect = false` in `OnServiceConnected` and `OnServiceDisconnected` handlers in src/Remort/Connection/MainWindowViewModel.cs

**Checkpoint**: All three user stories work. Status text correctly distinguishes auto vs. manual. All tests pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup.

- [ ] T027 [P] Update `OnMaxRetryCountChanged` to preserve `AutoReconnectEnabled` and `LastConnectedHost` when saving settings (currently overwrites with `new AppSettings`) in src/Remort/Connection/MainWindowViewModel.cs
- [ ] T028 Verify full build is clean: `dotnet build Remort.sln` (zero errors, zero warnings)
- [ ] T029 Verify all tests pass: `dotnet test Remort.sln`
- [ ] T030 Run quickstart.md manual validation scenarios (Story 1, Story 2, Story 3, edge cases)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Stories (Phases 3–5)**: All depend on Phase 2 completion
  - US1 (Phase 3): No dependency on other stories
  - US2 (Phase 4): No dependency on other stories (toggle is independent of reconnect logic)
  - US3 (Phase 5): Depends on `_isAutoReconnect` flag from US1 (T011) — implement after US1
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 — no dependency on other stories
- **User Story 2 (P2)**: Can start after Phase 2 — independent of US1 (toggle persists setting; US1 reads it)
- **User Story 3 (P3)**: Depends on US1 completion (uses `_isAutoReconnect` flag set in `TryAutoReconnect()`)

### Within Each User Story

- Tests written first, verify they fail
- Core logic before UI wiring
- ViewModel changes before View changes

### Parallel Opportunities

- **Phase 3 tests** (T005–T010): All [P] — different test methods, no dependencies
- **Phase 4 tests** (T017–T018): All [P] — different test methods
- **Phase 5 tests** (T021–T023): All [P] — different test methods
- **US1 and US2** can be worked on in parallel after Phase 2 (different concerns, minimal file overlap)
- **Phase 6** tasks T027 is [P] relative to T028–T030 sequence

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task T005: Test TryAutoReconnect connects when enabled, disconnected, and host known
Task T006: Test TryAutoReconnect does nothing when not Disconnected
Task T007: Test TryAutoReconnect does nothing when disabled
Task T008: Test TryAutoReconnect does nothing when no host
Task T009: Test ConnectDirect persists LastConnectedHost
Task T010: Test ConnectToDevBoxAsync persists LastConnectedHost
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (extend `AppSettings`)
2. Complete Phase 2: Foundational (ViewModel loads new settings)
3. Complete Phase 3: User Story 1 (auto-reconnect on unlock)
4. **STOP and VALIDATE**: Lock/unlock workstation, verify auto-reconnect works
5. This delivers the core value — hands-free reconnection

### Incremental Delivery

1. Setup + Foundational → Settings infrastructure ready
2. Add User Story 1 → Test independently → Auto-reconnect works (MVP!)
3. Add User Story 2 → Test independently → User has toggle control
4. Add User Story 3 → Test independently → Status feedback distinguishes auto vs. manual
5. Each story adds value without breaking previous stories

### Modified Files Summary

| File | Phase | Changes |
|------|-------|---------|
| `src/Remort/Settings/AppSettings.cs` | Phase 1 | Add 2 properties |
| `src/Remort/Connection/MainWindowViewModel.cs` | Phases 2–6 | Add field, property, method, update handlers |
| `src/Remort/MainWindow.xaml` | Phase 4 | Add checkbox |
| `src/Remort/MainWindow.xaml.cs` | Phase 3 | Add SessionSwitch subscription/unsubscription |
| `src/Remort.Tests/Connection/MainWindowViewModelTests.cs` | Phases 3–5 | Add ~11 test methods |

---

## Notes

- No new files created — all changes modify existing files per plan.md
- `SystemEvents.SessionSwitch` subscription in code-behind is constitution-compliant (framework-mandated event wiring)
- Settings backward compatibility is automatic (`System.Text.Json` + `init` defaults)
- T027 is critical — existing `OnMaxRetryCountChanged` creates `new AppSettings { MaxRetryCount = value }` which would overwrite the new fields with defaults
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
