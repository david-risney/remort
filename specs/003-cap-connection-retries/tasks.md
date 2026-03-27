# Tasks: Cap Connection Retries

**Input**: Design documents from `/specs/003-cap-connection-retries/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not explicitly requested in the feature specification — test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New types and interfaces that multiple user stories depend on

- [X] T001 [P] Create `ConnectionRetryPolicy` record in `src/Remort/Connection/ConnectionRetryPolicy.cs` — readonly record struct with `MaxAttempts` (default 3) and static `Default` field, per data-model.md
- [X] T002 [P] Create `AttemptStartedEventArgs` class in `src/Remort/Connection/AttemptStartedEventArgs.cs` — properties: `Attempt` (int, 1-based), `MaxAttempts` (int), per data-model.md
- [X] T003 [P] Create `RetriesExhaustedEventArgs` class in `src/Remort/Connection/RetriesExhaustedEventArgs.cs` — properties: `TotalAttempts` (int), `LastErrorDescription` (string), per data-model.md
- [X] T004 [P] Create `IConnectionService` interface in `src/Remort/Connection/IConnectionService.cs` — events: `AttemptStarted`, `Connected`, `Disconnected`, `RetriesExhausted`; properties: `Server`, `RetryPolicy`; methods: `Connect()`, `Disconnect()`, per contracts/iconnectionservice.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Settings infrastructure used by US1 (default policy) and US3 (configurable policy). Must be complete before user stories.

**⚠️ CRITICAL**: User Story 3 depends on settings persistence; US1/US2 depend on the service which reads `RetryPolicy`. Build settings layer first.

- [X] T005 [P] Create `AppSettings` record in `src/Remort/Settings/AppSettings.cs` — property: `MaxRetryCount` (int, default 3), per data-model.md
- [X] T006 [P] Create `ISettingsStore` interface in `src/Remort/Settings/ISettingsStore.cs` — methods: `Load()` returns `AppSettings`, `Save(AppSettings)`, per data-model.md
- [X] T007 Create `JsonSettingsStore` class in `src/Remort/Settings/JsonSettingsStore.cs` — implements `ISettingsStore` using `System.Text.Json` to `%APPDATA%/Remort/settings.json`; creates file with defaults on first run; malformed JSON falls back to defaults silently, per research R4

**Checkpoint**: Settings infrastructure ready — user story implementation can begin

---

## Phase 3: User Story 1 — Stop Retrying After Maximum Attempts (Priority: P1) 🎯 MVP

**Goal**: Cap connection retries to a configurable maximum (default 3). When retries are exhausted, show a failure message and return to the ready state.

**Independent Test**: Enter an unreachable hostname (e.g., `192.0.2.1`), wait for 3 attempts, verify the app stops retrying and shows "Connection failed after 3 attempts: {reason}", then returns to Disconnected state.

### Implementation for User Story 1

- [X] T008 [US1] Implement `RetryingConnectionService` in `src/Remort/Connection/RetryingConnectionService.cs` — wraps `IRdpClient`; tracks `_currentAttempt`, `_maxAttempts`, `_isUserDisconnect`; intercepts `IRdpClient.Disconnected` to retry up to `RetryPolicy.MaxAttempts`; raises `AttemptStarted` before each attempt, `Connected` on success, `RetriesExhausted` when exhausted, `Disconnected` on user-cancel or remote drop; per contracts/iconnectionservice.md event sequences and research R1/R2/R5
- [X] T009 [US1] Modify `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — replace direct `IRdpClient` lifecycle event subscriptions with `IConnectionService` events; accept `IConnectionService` (constructed with `IRdpClient` + `ConnectionRetryPolicy.Default`); handle `RetriesExhausted` to set `ConnectionState = Disconnected` and `StatusText` to failure message with attempt count and last error; per research R6
- [X] T010 [US1] Modify `MainWindow.xaml.cs` in `src/Remort/MainWindow.xaml.cs` — wire up `RetryingConnectionService` wrapping the existing `IRdpClient`; pass service to `MainWindowViewModel` instead of `IRdpClient` directly
- [X] T011 [US1] Verify build compiles with zero warnings — `dotnet build Remort.sln`

**Checkpoint**: Retry cap works with default policy (3 attempts). App stops retrying and shows failure after max attempts. Disconnect during retries cancels immediately. Successful mid-retry connection works normally.

---

## Phase 4: User Story 2 — See Retry Progress During Connection Attempts (Priority: P2)

**Goal**: Surface "Connecting… (attempt N of M)" in the status area during retries so the user knows the app is working, not stuck.

**Independent Test**: Connect to an unreachable host and observe the status area updating to show "Connecting… (attempt 1 of 3)", then "…(attempt 2 of 3)", etc.

### Implementation for User Story 2

- [X] T012 [US2] Modify `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — handle `AttemptStarted` event to update `StatusText` to "Connecting… (attempt {N} of {M})"; on `RetriesExhausted` show "Connection failed after {N} attempts: {reason}"; first attempt shows progress too (e.g., "Connecting… (attempt 1 of 3)")
- [X] T013 [US2] Verify build compiles with zero warnings — `dotnet build Remort.sln`

**Checkpoint**: Status text shows attempt progress during retries. Final failure message includes total attempts and error reason. Normal (first-attempt) connections show "Connecting… (attempt 1 of 3)" briefly before connecting.

---

## Phase 5: User Story 3 — Configure the Maximum Retry Count (Priority: P3)

**Goal**: Let users adjust the max retry count via an in-app setting. The value persists between sessions.

**Independent Test**: Change the retry count setting to 5, connect to an unreachable host, verify exactly 5 attempts. Close/reopen the app, verify the setting persisted.

### Implementation for User Story 3

- [X] T014 [US3] Modify `MainWindowViewModel` in `src/Remort/Connection/MainWindowViewModel.cs` — add `MaxRetryCount` observable property backed by `ISettingsStore`; load on construction, save on change; update `IConnectionService.RetryPolicy` when `MaxRetryCount` changes; validate non-negative integer
- [X] T015 [US3] Modify `MainWindow.xaml.cs` in `src/Remort/MainWindow.xaml.cs` — create `JsonSettingsStore` and pass to `MainWindowViewModel`
- [X] T016 [US3] Modify `MainWindow.xaml` in `src/Remort/MainWindow.xaml` — add settings UI control (label + TextBox with integer validation) for "Max retry attempts" in the connection bar area; bind to `MainWindowViewModel.MaxRetryCount`; per research R7
- [X] T017 [US3] Verify build compiles with zero warnings — `dotnet build Remort.sln`

**Checkpoint**: User can change the retry count in the UI. Value persists in `%APPDATA%/Remort/settings.json`. New value takes effect on the next connection attempt. Setting to 0 disables retries (one attempt only).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all stories

- [X] T018 [P] Run full quickstart.md manual test checklist (all 8 scenarios) from `specs/003-cap-connection-retries/quickstart.md`
- [X] T019 [P] Verify settings file location and format — `%APPDATA%\Remort\settings.json` contains `{"maxRetryCount": N}`, deletable to reset defaults
- [X] T020 Validate edge cases from spec.md: disconnect during retries, retry count 0, retry count 1, app close during retries, error message changes between retries

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. All tasks are parallel.
- **Foundational (Phase 2)**: T005, T006 are parallel. T007 depends on T005 + T006. BLOCKS user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 1 + Phase 2 completion. T008 first, then T009, T010 sequentially (same-file integration).
- **User Story 2 (Phase 4)**: Depends on Phase 3 (US1 must exist for retry progress to have meaning). Modifies `MainWindowViewModel` (same file as T009).
- **User Story 3 (Phase 5)**: Depends on Phase 2 (settings infrastructure) and Phase 3 (service must exist). Can run in parallel with US2 if T009 is complete, but safer sequentially since T014 and T012 both modify `MainWindowViewModel`.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no story dependencies
- **User Story 2 (P2)**: Depends on US1 (the `AttemptStarted` event handling needs the service from US1)
- **User Story 3 (P3)**: Depends on US1 (the `RetryPolicy` property needs the service from US1). Independent of US2.

### Within Each User Story

- Service implementation before ViewModel modifications
- ViewModel modifications before XAML/code-behind wiring
- Build verification as final step

### Parallel Opportunities

- **Phase 1**: All four tasks (T001–T004) can run in parallel — independent new files
- **Phase 2**: T005 and T006 can run in parallel — independent new files
- **Phase 6**: T018 and T019 can run in parallel — independent validation

---

## Parallel Example: Phase 1

```
# Launch all Setup tasks together (all new files, no dependencies):
Task T001: "Create ConnectionRetryPolicy record in src/Remort/Connection/ConnectionRetryPolicy.cs"
Task T002: "Create AttemptStartedEventArgs in src/Remort/Connection/AttemptStartedEventArgs.cs"
Task T003: "Create RetriesExhaustedEventArgs in src/Remort/Connection/RetriesExhaustedEventArgs.cs"
Task T004: "Create IConnectionService interface in src/Remort/Connection/IConnectionService.cs"
```

## Parallel Example: Phase 2

```
# Launch settings types together (independent new files):
Task T005: "Create AppSettings record in src/Remort/Settings/AppSettings.cs"
Task T006: "Create ISettingsStore interface in src/Remort/Settings/ISettingsStore.cs"

# Then sequentially:
Task T007: "Create JsonSettingsStore in src/Remort/Settings/JsonSettingsStore.cs" (depends on T005, T006)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (types + interfaces)
2. Complete Phase 2: Foundational (settings infrastructure)
3. Complete Phase 3: User Story 1 (retry cap with default policy)
4. **STOP and VALIDATE**: Connect to unreachable host → 3 attempts → failure message → ready state
5. Deploy/demo if ready — core problem (infinite retries) is solved

### Incremental Delivery

1. Setup + Foundational → All types and interfaces exist
2. User Story 1 → Retry cap works → **MVP complete**
3. User Story 2 → Status shows attempt progress → Better UX during failures
4. User Story 3 → User can configure retry count → Power user feature
5. Polish → Full validation across all scenarios

### Serial Execution (Recommended for Single Developer)

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6

All user stories modify `MainWindowViewModel`, so serial execution avoids merge conflicts.

---

## Notes

- `ConnectionState` enum is **not modified** — `Connecting` covers the entire retry sequence (research R3)
- No `[P]` on user story implementation tasks that modify the same file (`MainWindowViewModel.cs`)
- No backoff/delay between retries — per spec assumptions
- Retry applies to initial connection only — not reconnection after an established session drops
- `MaxAttempts` of 0 = one attempt with no retries (spec edge case)
