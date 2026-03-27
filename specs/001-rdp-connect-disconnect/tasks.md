# Tasks: RDP Connect & Disconnect

**Input**: Design documents from `/specs/001-rdp-connect-disconnect/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/irdpclient.md ✅, quickstart.md ✅

**Tests**: Included — plan.md Constitution Principle III mandates Test-First, and the project structure explicitly includes `MainWindowViewModelTests.cs`.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet packages required by the feature

- [X] T001 Add CommunityToolkit.Mvvm package reference to src/Remort/Remort.csproj
- [X] T002 [P] Add NSubstitute and FluentAssertions package references to src/Remort.Tests/Remort.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: COM interop layer and shared abstractions that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 [P] Create ConnectionState enum (Disconnected, Connecting, Connected) in src/Remort/Connection/ConnectionState.cs
- [X] T004 [P] Create DisconnectedEventArgs, WarningEventArgs, and FatalErrorEventArgs in src/Remort/Interop/RdpEventArgs.cs (suppress SA1402 with justification for grouped trivial EventArgs — per research.md R5)
- [X] T005 [P] Create IMsTscAxEvents COM dispatch interface with [ComImport] in src/Remort/Interop/IMsTscAxEvents.cs (adapt from poc/StickyDesktop/RdpClientHost.cs)
- [X] T006 [P] Create MsTscAxEventsSink [ComVisible] event sink implementation in src/Remort/Interop/MsTscAxEventsSink.cs (adapt from poc/StickyDesktop/RdpClientHost.cs)
- [X] T007 [P] Create EventSinkCookie IConnectionPoint Advise/Unadvise helper in src/Remort/Interop/EventSinkCookie.cs (adapt from poc/StickyDesktop/RdpClientHost.cs)
- [X] T008 Create IRdpClient interface in src/Remort/Connection/IRdpClient.cs per contracts/irdpclient.md (depends on T004 for DisconnectedEventArgs)
- [X] T009 Create RdpClientHost AxHost subclass implementing IRdpClient in src/Remort/Interop/RdpClientHost.cs (adapt from poc/StickyDesktop/RdpClientHost.cs; implement IRdpClient, add ApplyDefaultSettings, remove Log/UserName/Domain/Ocx per data-model.md — depends on T003–T008)

**Checkpoint**: Foundation ready — IRdpClient contract implemented, COM interop layer complete. User story implementation can now begin.

---

## Phase 3: User Story 1 — Connect to a Remote Host (Priority: P1) 🎯 MVP

**Goal**: User enters a hostname, clicks Connect, authenticates via Windows credential prompt, and sees the remote desktop embedded in the main window.

**Independent Test**: Launch app → enter valid hostname → authenticate → verify remote desktop renders inside window.

### Implementation for User Story 1

- [X] T010 [US1] Create MainWindowViewModel with Hostname property, ConnectionState property, ConnectCommand (CanExecute: Disconnected + non-empty trimmed hostname), and IRdpClient event handlers for Connected/Disconnected state transitions in src/Remort/Connection/MainWindowViewModel.cs. Trim hostname before assigning to IRdpClient.Server. IRdpClient.Connecting event is intentionally unused (state set synchronously before Connect() call).
- [X] T011 [P] [US1] Update MainWindow.xaml with connection bar (hostname TextBox bound to Hostname, Connect button bound to ConnectCommand) and WindowsFormsHost element for hosting the RDP ActiveX control in src/Remort/MainWindow.xaml
- [X] T012 [US1] Update MainWindow.xaml.cs to instantiate RdpClientHost, set as WindowsFormsHost.Child, set IRdpClient.DesktopWidth/DesktopHeight from WindowsFormsHost.ActualWidth/ActualHeight, create MainWindowViewModel with IRdpClient, and set DataContext in src/Remort/MainWindow.xaml.cs (framework wiring per research.md R1)
- [X] T013 [US1] Create MainWindowViewModelTests with Connect scenario tests (valid hostname transitions to Connecting, empty hostname disables ConnectCommand, IRdpClient.Connected event transitions to Connected, IRdpClient.Disconnected event on error returns to Disconnected) in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

**Checkpoint**: User Story 1 fully functional — user can connect to a remote host and see the session embedded in the window.

---

## Phase 4: User Story 2 — Disconnect from an Active Session (Priority: P2)

**Goal**: User clicks Disconnect to end the active session and return the app to the ready state for a new connection.

**Independent Test**: Connect to a host (US1) → click Disconnect → verify session ends, hostname and Connect button become available again.

### Implementation for User Story 2

- [X] T014 [US2] Add DisconnectCommand with CanExecute (ConnectionState == Connected OR Connecting) and handler calling IRdpClient.Disconnect() to MainWindowViewModel in src/Remort/Connection/MainWindowViewModel.cs. This allows cancelling a connection attempt in progress (F1/F2 fix).
- [X] T015 [P] [US2] Add Disconnect button with command binding to DisconnectCommand in MainWindow.xaml in src/Remort/MainWindow.xaml
- [X] T016 [US2] Add Disconnect scenario tests (disconnect when connected returns to Disconnected, DisconnectCommand disabled when not connected, hostname and Connect re-enabled after disconnect) to MainWindowViewModelTests in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

**Checkpoint**: User Stories 1 AND 2 both work — user can connect and disconnect, cycling between sessions.

---

## Phase 5: User Story 3 — View Connection Status (Priority: P3)

**Goal**: User sees the current connection state at all times — Disconnected, Connecting…, Connected to {host}, or Disconnected: {reason}.

**Independent Test**: Observe status area during idle → connecting → connected → disconnected lifecycle and verify it updates at each transition.

### Implementation for User Story 3

- [X] T017 [US3] Add StatusText property and state-driven update logic (Disconnected → "Disconnected", Connecting → "Connecting to {host}…", Connected → "Connected to {host}", Disconnected-with-reason → "Disconnected: {reason}" via IRdpClient.GetErrorDescription) to MainWindowViewModel in src/Remort/Connection/MainWindowViewModel.cs
- [X] T018 [P] [US3] Add status bar to MainWindow.xaml displaying StatusText bound to ViewModel in src/Remort/MainWindow.xaml
- [X] T019 [US3] Add StatusText scenario tests (initial status is Disconnected, connecting shows hostname, connected shows hostname, disconnected with reason shows human-readable message) to MainWindowViewModelTests in src/Remort.Tests/Connection/MainWindowViewModelTests.cs

**Checkpoint**: All three user stories independently functional — full connect/disconnect/status lifecycle works.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases and validation that affect multiple user stories

- [X] T020 Handle Window.Closing to disconnect active session before exit (FR-011, edge case) in src/Remort/MainWindow.xaml.cs (call DisconnectCommand if connected per research.md R7)
- [X] T021 Verify zero-warning build with `dotnet build Remort.sln` (TreatWarningsAsErrors, StyleCop, AnalysisLevel=latest-all)
- [X] T022 Run `dotnet test Remort.sln` and validate all ViewModel tests pass
- [X] T023 Validate quickstart.md manual test scenarios (7 scenarios covering connect, disconnect, empty hostname, unreachable host, window close, credential cancel)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001 for CommunityToolkit.Mvvm used by ViewModel, T002 for test packages) — **BLOCKS all user stories**
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 2 (Phase 4)**: Depends on User Story 1 (DisconnectCommand builds on the ViewModel created in US1)
- **User Story 3 (Phase 5)**: Depends on User Story 2 (StatusText covers all states including disconnect-with-reason)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational — creates the ViewModel, View, and code-behind
- **User Story 2 (P2)**: Depends on US1 — adds DisconnectCommand to the existing ViewModel and Disconnect button to the existing View
- **User Story 3 (P3)**: Depends on US2 — adds StatusText property covering all states (including disconnect reason from US2's flow)

### Within Each User Story

- ViewModel implementation before View updates (ViewModel drives bindings)
- View (.xaml) and code-behind (.xaml.cs) can be parallel within a story
- Tests can be written alongside implementation (shared test file, additive)

### Parallel Opportunities

**Setup phase**:
```
T001 ─┐
      ├─ (both in parallel — different .csproj files)
T002 ─┘
```

**Foundational phase**:
```
T003 ─┐
T004 ─┤
T005 ─┼─ (all in parallel — independent files)
T006 ─┤
T007 ─┘
      │
      ▼
T008 ── (depends on T004 for DisconnectedEventArgs)
      │
      ▼
T009 ── (depends on T003–T008 — combines all interop + interface)
```

**User Story 1**:
```
T010 ─┐
      ├─ T011 in parallel (ViewModel .cs vs View .xaml)
      │
      ▼
T012 ── (depends on T010 + T011 — wires ViewModel to View)
T013 ── (can parallel with T012 — test file vs code-behind)
```

**User Story 2**:
```
T014 ─┐
      ├─ T015 in parallel (ViewModel .cs vs View .xaml)
      ▼
T016 ── (after T014)
```

**User Story 3**:
```
T017 ─┐
      ├─ T018 in parallel (ViewModel .cs vs View .xaml)
      ▼
T019 ── (after T017)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (2 tasks)
2. Complete Phase 2: Foundational (7 tasks — COM interop layer)
3. Complete Phase 3: User Story 1 (4 tasks)
4. **STOP and VALIDATE**: Launch app, connect to an RDP host, verify embedded session
5. Deploy/demo if ready — core value proposition works

### Incremental Delivery

1. Setup + Foundational → COM interop layer proven
2. Add User Story 1 → Test independently → **MVP: user can connect** 🎯
3. Add User Story 2 → Test independently → User can connect AND disconnect
4. Add User Story 3 → Test independently → Full status feedback loop
5. Polish → Edge cases, build validation, manual test pass

### Single-Developer Flow (Sequential)

US1 → US2 → US3 → Polish (each story builds on the previous ViewModel/View)

---

## Notes

- All COM interop types adapted from `poc/StickyDesktop/RdpClientHost.cs` — proven in prototype
- ViewModel has zero WPF/COM references — tested entirely via NSubstitute mocks of IRdpClient
- COM events fire on UI thread (research.md R4) — no Dispatcher.Invoke needed
- Single `RdpEventArgs.cs` groups 3 trivial EventArgs classes with SA1402 suppression (research.md R5)
- No DI container — ViewModel receives IRdpClient via constructor, wired in code-behind (research.md R1, R2)
