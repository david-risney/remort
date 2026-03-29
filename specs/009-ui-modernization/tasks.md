# Tasks: Modern Windows UI

**Input**: Design documents from `/specs/009-ui-modernization/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add Wpf.Ui dependency, migrate App.xaml to Wpf.Ui theme system, remove old theme files

- [x] T001 Add `WPF-UI` v4.* NuGet package to `src/Remort/Remort.csproj`
- [x] T002 Update `src/Remort/App.xaml` to use `ui:ThemesDictionary` and `ui:ControlsDictionary` replacing old `ThemeColors.xaml` and `ThemeStyles.xaml` merged dictionaries
- [x] T003 Update `src/Remort/App.xaml.cs` to call `ApplicationThemeManager.Apply()` on startup with saved theme preference
- [x] T004 Remove old theme files: `src/Remort/Theme/ThemeColors.xaml`, `src/Remort/Theme/ThemeStyles.xaml`, `src/Remort/Theme/ThemeResourceKeys.cs`, `src/Remort/Theme/ColorProfile.cs`, `src/Remort/Theme/PresetProfiles.cs`, `src/Remort/Theme/ThemeService.cs`, `src/Remort/Theme/IThemeService.cs`, `src/Remort/Theme/IProfileStore.cs`, `src/Remort/Theme/JsonProfileStore.cs`, `src/Remort/Theme/ThemeSettingsWindow.xaml(.cs)`, `src/Remort/Theme/ThemeSettingsViewModel.cs`
- [x] T005 [P] Add `AppTheme` enum (Light, Dark, System) in `src/Remort/Settings/AppTheme.cs`
- [x] T006 Update `src/Remort/Settings/AppSettings.cs` to replace `ActiveProfileName` with `Theme` (AppTheme), add `DevBoxDiscoveryEnabled` and `LastSelectedDeviceId` fields, remove fields migrated to per-device settings. **Also update or stub references in `Connection/MainWindowViewModel.cs`** so the build does not break before Phase 3 replaces it.
- [x] T007 Update existing theme tests in `src/Remort.Tests/Theme/` to work with Wpf.Ui theme system or remove tests for deleted types
- [x] T008 Build solution and verify zero warnings with `dotnet build Remort.sln`

**Checkpoint**: App launches with Wpf.Ui theme, old theme system removed, all existing tests pass

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Device data model, IDeviceStore, IDeviceWindowManager — all user stories depend on these

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T009 [P] Create `Device` record and embedded settings records (`DeviceConnectionSettings`, `DeviceGeneralSettings`, `DeviceDisplaySettings`, `DeviceRedirectionSettings`) in `src/Remort/Devices/Device.cs`
- [x] T010 [P] Create `IDeviceStore` interface in `src/Remort/Devices/IDeviceStore.cs` per contracts/idevicestore.md
- [x] T011 Implement `JsonDeviceStore` in `src/Remort/Devices/JsonDeviceStore.cs` (CRUD, JSON persistence to `%APPDATA%/Remort/devices.json`, events)
- [x] T012 [P] Create `IDeviceWindowManager` interface in `src/Remort/Devices/IDeviceWindowManager.cs` per contracts/idevicewindowmanager.md
- [x] T013 Implement `DeviceWindowManager` in `src/Remort/Devices/DeviceWindowManager.cs` (single-window-per-device tracking, open/focus/close)
- [x] T014 [P] Write `JsonDeviceStore` unit tests in `src/Remort.Tests/Devices/JsonDeviceStoreTests.cs` (Add, Update, Remove, GetAll, GetById, Save/Load round-trip, events, duplicate Id, not-found)
- [x] T015 [P] Write `DeviceWindowManager` unit tests in `src/Remort.Tests/Devices/DeviceWindowManagerTests.cs` (OpenOrFocus creates window, OpenOrFocus focuses existing, CloseForDevice, CloseAll, IsOpen)
- [x] T016 Run all tests and verify zero warnings: `dotnet test Remort.sln`

**Checkpoint**: Foundation ready — Device model, store, and window manager working with tests

---

## Phase 3: User Story 1 — Browse and Select a Device (Priority: P1) 🎯 MVP

**Goal**: Main window shows NavigationView with Devices page listing device cards; clicking a card opens a device window

**Independent Test**: Launch app → see nav sidebar with Favorites/Devices/Settings → Devices page shows cards → click card → device window opens

- [x] T017 Rewrite `src/Remort/MainWindow.xaml` as a `ui:FluentWindow` with `ui:NavigationView` containing three nav items: Favorites, Devices, Settings
- [x] T017a Remove or archive `src/Remort/Connection/MainWindowViewModel.cs` — its responsibilities are now split across `DevicesPageViewModel`, `ConnectionPageViewModel`, and `SettingsPageViewModel`
- [x] T018 Rewrite `src/Remort/MainWindow.xaml.cs` to initialize FluentWindow, create shared services (`IDeviceStore`, `IDeviceWindowManager`, `ISettingsStore`), and set `DataContext`
- [x] T019 [P] [US1] Create `DeviceCardViewModel` in `src/Remort/Devices/DeviceCardViewModel.cs` (Name, screenshot BitmapImage or gradient brush, IsFavorite, OpenCommand, ToggleFavoriteCommand). Load screenshot with try/catch — fall back to gradient on IOException or corrupt file. Assign gradient from a palette based on device Id hash.
- [x] T020 [P] [US1] Create `DevicesPageViewModel` in `src/Remort/Devices/DevicesPageViewModel.cs` (ObservableCollection of DeviceCardViewModel, loads from IDeviceStore, OpenDeviceCommand delegates to IDeviceWindowManager, subscribes to DeviceAdded/Removed/Updated events)
- [x] T021 [US1] Create `DevicesPage.xaml` and `DevicesPage.xaml.cs` in `src/Remort/Devices/` (ItemsControl with CardControl template showing device name + screenshot/gradient background, Add Device button). Use `TextTrimming="CharacterEllipsis"` on the device name TextBlock for long names.
- [x] T022 [US1] Create `DeviceWindow.xaml` and `DeviceWindow.xaml.cs` in `src/Remort/DeviceWindow/` (FluentWindow shell with NavigationView for Connection/General/Display/Redirections, WindowsFormsHost for RDP, TitleBar with device name). Use `TextTrimming="CharacterEllipsis"` on the titlebar device name for long names.
- [x] T023 [US1] Create `DeviceWindowViewModel` in `src/Remort/DeviceWindow/DeviceWindowViewModel.cs` (Device reference, window title binding, placeholder for nav/RDP toggle)
- [x] T024 [P] [US1] Create placeholder pages: `ConnectionPage.xaml(.cs)`, `GeneralPage.xaml(.cs)`, `DisplayPage.xaml(.cs)`, `RedirectionsPage.xaml(.cs)` in `src/Remort/DeviceWindow/` with empty content
- [x] T025 [P] [US1] Write `DevicesPageViewModel` tests in `src/Remort.Tests/Devices/DevicesPageViewModelTests.cs` (loads devices, OpenDeviceCommand calls window manager, reacts to store events)
- [x] T026 [P] [US1] Write `DeviceCardViewModel` tests in `src/Remort.Tests/Devices/DeviceCardViewModelTests.cs` (name binding, screenshot fallback to gradient, favorite toggle)
- [x] T027 [US1] Run all tests and verify zero warnings

**Checkpoint**: User Story 1 complete — app shows device list, clicking a card opens an empty device window

---

## Phase 4: User Story 2 — Add a New Device (Priority: P1)

**Goal**: Add Device button opens dialog → user enters name + hostname → device card appears

**Independent Test**: Click Add Device → enter name/hostname → press OK → new card appears in list

- [x] T028 [P] [US2] Create `AddDeviceDialogViewModel` in `src/Remort/Devices/AddDeviceDialogViewModel.cs` (Name, Hostname properties, validation: both non-empty, OK/Cancel commands)
- [x] T029 [US2] Create `AddDeviceDialog.xaml` and `AddDeviceDialog.xaml.cs` in `src/Remort/Devices/` (Wpf.Ui ContentDialog with Name and Hostname TextBox fields, OK/Cancel buttons, validation error display)
- [x] T030 [US2] Wire Add Device button in `DevicesPageViewModel` to show AddDeviceDialog, create Device on success, call IDeviceStore.Add() + Save()
- [x] T031 [P] [US2] Write `AddDeviceDialogViewModel` tests in `src/Remort.Tests/Devices/AddDeviceDialogViewModelTests.cs` (validation rejects empty name/hostname, OK creates device with defaults, Cancel produces no device)
- [x] T032 [US2] Run all tests and verify zero warnings

**Checkpoint**: User Story 2 complete — users can add devices via dialog

---

## Phase 5: User Story 3 — Connect to a Remote Device (Priority: P1)

**Goal**: Connection page in device window shows Connect/Disconnect button, status labels, auto-connect checkboxes; connection lifecycle works

**Independent Test**: Open device window → Connection page → click Connect → status updates → click Disconnect

- [x] T033 [US3] Create `ConnectionPageViewModel` in `src/Remort/DeviceWindow/ConnectionPageViewModel.cs` (Connect/Disconnect commands, Status/Substatus labels bound to ConnectionState, error detail on failure, button reverts to Connect on failure, three checkboxes bound to DeviceConnectionSettings, creates RetryingConnectionService per device). Enable NLA via `AdvancedSettings.EnableCredSspSupport = true` on the RDP control before Connect().
- [x] T034 [US3] Implement `ConnectionPage.xaml` in `src/Remort/DeviceWindow/` (Connect/Disconnect button, Status/Substatus TextBlocks, Autoconnect on start checkbox, Autoconnect when visible checkbox, Start on startup checkbox)
- [x] T035 [US3] Wire `DeviceWindowViewModel` to create `ConnectionPageViewModel` with the device's hostname and connection settings, pass `IConnectionService` and `IRdpClient` from the WindowsFormsHost-hosted RdpClientHost
- [x] T036 [US3] Persist connection setting changes (checkboxes) back to `IDeviceStore.Update()` + `Save()` when toggled
- [x] T037 [P] [US3] Write `ConnectionPageViewModel` tests in `src/Remort.Tests/DeviceWindow/ConnectionPageViewModelTests.cs` (Connect command sets status to Connecting, Disconnect resets, error shows in Substatus, button reverts on failure, checkbox changes persist)
- [x] T038 [US3] Run all tests and verify zero warnings

**Checkpoint**: User Story 3 complete — users can connect/disconnect with status feedback, P1 MVP complete

---

## Phase 6: User Story 4 — Switch Between Navigation and Remote Desktop View (Priority: P2)

**Goal**: Titlebar toggle button switches device window content between NavigationView and RDP view

**Independent Test**: Open device window → click toggle → RDP view shown → click toggle → navigation returns

- [x] T039 [US4] Add `IsRdpViewActive` observable property and `ToggleViewCommand` to `DeviceWindowViewModel` in `src/Remort/DeviceWindow/DeviceWindowViewModel.cs`
- [x] T040 [US4] Update `DeviceWindow.xaml` to include a down-arrow toggle button in the TitleBar, bind NavigationView Visibility to `!IsRdpViewActive` and WindowsFormsHost grid Visibility to `IsRdpViewActive`
- [x] T041 [US4] Show placeholder text ("Not connected") in the RDP area when `ConnectionState` is `Disconnected`
- [x] T042 [P] [US4] Write toggle tests in `src/Remort.Tests/DeviceWindow/DeviceWindowViewModelTests.cs` (ToggleViewCommand flips IsRdpViewActive, default is false/navigation mode)
- [x] T043 [US4] Run all tests and verify zero warnings

**Checkpoint**: User Story 4 complete — users can switch between settings and RDP view

---

## Phase 7: User Story 5 — Configure Display Settings (Priority: P2)

**Goal**: Display page shows pin-to-desktop, fit-session, all-monitors checkboxes with working behavior

**Independent Test**: Open Display page → toggle checkboxes → maximize window → verify multi-monitor behavior

- [x] T044 [P] [US5] Create `DisplayPageViewModel` in `src/Remort/DeviceWindow/DisplayPageViewModel.cs` (three checkbox properties bound to DeviceDisplaySettings, persist on change via IDeviceStore)
- [x] T045 [US5] Implement `DisplayPage.xaml` in `src/Remort/DeviceWindow/` (Pin to virtual desktop checkbox, Fit session to window checkbox, Use all monitors when fullscreen checkbox)
- [x] T046 [US5] Implement multi-monitor maximize logic in `DeviceWindow.xaml.cs` — when `UseAllMonitors` is true and window is maximized, set `WindowStyle=None` and resize to `SystemParameters.VirtualScreen` bounds
- [x] T047 [US5] Implement fit-session-to-window logic — when `FitSessionToWindow` is true, set `RdpClientHost.DesktopWidth/Height` to match window size on resize
- [x] T048 [US5] Wire `PinToVirtualDesktop` to existing `IVirtualDesktopService.PinWindow()` from `DeviceWindow.xaml.cs`
- [x] T049 [P] [US5] Write `DisplayPageViewModel` tests in `src/Remort.Tests/DeviceWindow/DisplayPageViewModelTests.cs` (checkbox toggles update device settings, changes persist)
- [x] T050 [US5] Run all tests and verify zero warnings

**Checkpoint**: User Story 5 complete — display settings functional

---

## Phase 8: User Story 6 — Manage Favorites (Priority: P2)

**Goal**: Favorites page shows favorite-only devices; favorite toggle via card icon, context menu, and General page checkbox

**Independent Test**: Toggle favorite on a device → verify it appears on Favorites page → unfavorite → verify removal

- [x] T051 [P] [US6] Create `FavoritesPageViewModel` in `src/Remort/Devices/FavoritesPageViewModel.cs` (same as DevicesPageViewModel but filtered to IsFavorite=true, subscribes to store events to refresh)
- [x] T052 [US6] Create `FavoritesPage.xaml` and `FavoritesPage.xaml.cs` in `src/Remort/Devices/` (same card layout as DevicesPage, filtered list, Add Device button)
- [x] T053 [US6] Add star/heart icon button to device card template in `DevicesPage.xaml` — binds to `DeviceCardViewModel.ToggleFavoriteCommand`, updates `IDeviceStore`
- [x] T054 [US6] Add right-click context menu to device card template with "Toggle Favorite" and "Remove Device" options
- [x] T055 [US6] Implement "Remove Device" context menu action: show confirmation ContentDialog, call `IDeviceStore.Remove()` + `Save()`, call `IDeviceWindowManager.CloseForDevice()` if window is open (FR-042, FR-043, FR-044)
- [x] T056 [P] [US6] Write `FavoritesPageViewModel` tests in `src/Remort.Tests/Devices/FavoritesPageViewModelTests.cs` (only shows favorites, reacts to favorite toggle events, reacts to device removal)
- [x] T057 [US6] Run all tests and verify zero warnings

**Checkpoint**: User Story 6 complete — favorites management working

---

## Phase 9: User Story 7 — Configure Redirections (Priority: P3)

**Goal**: Redirections page shows RDP redirection checkboxes matching mstsc.exe options

**Independent Test**: Open Redirections page → toggle checkboxes → verify settings persist and apply on reconnect

- [x] T058 [P] [US7] Create `RedirectionsPageViewModel` in `src/Remort/DeviceWindow/RedirectionsPageViewModel.cs` (8 checkbox properties bound to DeviceRedirectionSettings, persist on change)
- [x] T059 [US7] Implement `RedirectionsPage.xaml` in `src/Remort/DeviceWindow/` (checkboxes for clipboard, printers, drives, audio playback, audio recording, smart cards, serial ports, USB devices)
- [x] T060 [US7] Apply redirection settings to `IRdpClient` before `Connect()` in `ConnectionPageViewModel` — map each redirection flag to the corresponding MsRdpClient COM property
- [x] T061 [P] [US7] Write `RedirectionsPageViewModel` tests in `src/Remort.Tests/DeviceWindow/RedirectionsPageViewModelTests.cs` (checkbox toggles update settings, changes persist)
- [x] T062 [US7] Run all tests and verify zero warnings

**Checkpoint**: User Story 7 complete — redirections configurable

---

## Phase 10: User Story 8 — Autohide Titlebar (Priority: P3)

**Goal**: When "Autohide title bar" is enabled, titlebar hides/reveals on cursor proximity

**Independent Test**: Enable autohide in General page → move cursor away → titlebar hides → hover top edge → titlebar reveals

- [x] T063 [P] [US8] Create `GeneralPageViewModel` in `src/Remort/DeviceWindow/GeneralPageViewModel.cs` (Name, Hostname text fields, AutohideTitleBar checkbox, Favorite checkbox — all bound to Device, persist on change)
- [x] T064 [US8] Implement `GeneralPage.xaml` in `src/Remort/DeviceWindow/` (Name TextBox, Hostname TextBox, Autohide title bar CheckBox, Favorite CheckBox)
- [x] T065 [US8] Implement autohide titlebar behavior in `DeviceWindow.xaml.cs` — MouseMove handler checks cursor Y proximity to top edge, animate TitleBar Height between 0 and normal via DoubleAnimation with 1-second hide delay
- [x] T066 [US8] Bind `DeviceWindowViewModel.IsAutohideEnabled` to `DeviceGeneralSettings.AutohideTitleBar` and trigger autohide logic from `DeviceWindow.xaml.cs`
- [x] T067 [P] [US8] Write `GeneralPageViewModel` tests in `src/Remort.Tests/DeviceWindow/GeneralPageViewModelTests.cs` (name/hostname changes persist, favorite toggle persists, autohide toggle persists)
- [x] T068 [US8] Run all tests and verify zero warnings

**Checkpoint**: User Story 8 complete — autohide titlebar working

---

## Phase 11: User Story 9 — Task View Button (Priority: P3)

**Goal**: Task View button in device window titlebar sends Win+Tab to host OS

**Independent Test**: Click Task View button → host Windows Task View activates

- [x] T069 [US9] Add Task View button to TitleBar in `src/Remort/DeviceWindow/DeviceWindow.xaml` with `InvokeTaskViewCommand` binding
- [x] T070 [US9] Implement `InvokeTaskViewCommand` in `DeviceWindowViewModel` — call `NativeMethods.SendInput()` to send `VK_LWIN + VK_TAB` keypress via P/Invoke in `src/Remort/Interop/NativeMethods.cs`
- [x] T071 [US9] Run all tests and verify zero warnings

**Checkpoint**: User Story 9 complete — Task View button working

---

## Phase 12: Settings Page & Polish

**Purpose**: Settings page (theme, DevBox config, About), screenshot capture, DevBox integration, final cleanup

- [x] T072 [P] Create `SettingsPageViewModel` in `src/Remort/Settings/SettingsPageViewModel.cs` (Theme selector bound to AppTheme enum with ApplicationThemeManager.Apply(), DevBoxDiscoveryEnabled toggle, About section with app name/version from assembly info)
- [x] T073 Implement `SettingsPage.xaml` and `SettingsPage.xaml.cs` in `src/Remort/Settings/` (theme radio buttons for Light/Dark/System, DevBox discovery toggle, About section)
- [x] T074 Implement screenshot capture in `src/Remort/Interop/RdpClientHost.cs` — add `CaptureScreenshot()` method using `DrawToBitmap()`, save PNG to `%APPDATA%/Remort/screenshots/{deviceId}.png`
- [x] T075 Wire screenshot capture on disconnect in `ConnectionPageViewModel` — call `RdpClientHost.CaptureScreenshot()` before disconnect completes, update `Device.LastScreenshotPath` via `IDeviceStore`
- [x] T076 Integrate DevBox-discovered devices into `IDeviceStore` — on app startup (if DevBoxDiscoveryEnabled), resolve DevBox list and merge into device store with `IsDiscovered=true`
- [x] T077 [P] Write `SettingsPageViewModel` tests in `src/Remort.Tests/Settings/SettingsPageViewModelTests.cs` (theme change persists to AppSettings, DevBox toggle persists)
- [x] T078 Update E2E tests in `src/Remort.Tests/EndToEnd/` for new window structure (FluentWindow, NavigationView navigation, new AutomationIds)
- [x] T080 Implement "Start on startup" behavior: when `DeviceConnectionSettings.StartOnStartup` is toggled, add/remove a registry entry in `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` to launch Remort at Windows logon. Wire from `ConnectionPageViewModel` checkbox change. Add helper in `src/Remort/Settings/StartupRegistration.cs`.
- [x] T081 Implement "Autoconnect on start" behavior: on app startup in `App.xaml.cs`, after loading `IDeviceStore`, scan all devices with `AutoconnectOnStart=true` and invoke `IDeviceWindowManager.OpenOrFocus()` + `ConnectionPageViewModel.ConnectCommand` for each.
- [x] T082 Implement "Autoconnect when visible" behavior: in `DeviceWindowViewModel`, subscribe to existing `IVirtualDesktopService` desktop-change events. When the device window's virtual desktop becomes active and `AutoconnectWhenVisible=true` and not already connected, invoke Connect automatically.
- [x] T083 [P] Write tests for startup registration in `src/Remort.Tests/Settings/StartupRegistrationTests.cs` (registry add/remove, idempotent toggle)
- [x] T084 Add basic performance validation: measure main window startup time (SC-003: <2s) and nav/RDP toggle time (SC-004: <200ms) as assertions in E2E tests or as logged diagnostics.
- [x] T085 Final build and full test run: `dotnet build Remort.sln && dotnet test Remort.sln`

**Checkpoint**: All features complete, all tests pass, zero warnings

---

## Dependencies

```
Phase 1 (Setup)
  └→ Phase 2 (Foundational: Device model + stores)
       ├→ Phase 3 (US1: Browse/Select) ──→ Phase 4 (US2: Add Device) ──→ Phase 5 (US3: Connect)
       │                                                                      │
       │    ┌─────────────────────────────────────────────────────────────────┘
       │    ├→ Phase 6  (US4: Nav/RDP Toggle)
       │    ├→ Phase 7  (US5: Display Settings)
       │    ├→ Phase 8  (US6: Favorites)
       │    ├→ Phase 9  (US7: Redirections)
       │    ├→ Phase 10 (US8: Autohide Titlebar)
       │    └→ Phase 11 (US9: Task View)
       │
       └→ Phase 12 (Settings + Polish) — after all story phases
```

## Parallel Execution Opportunities

**Within Phase 2**: T009, T010, T012, T014, T015 can all run in parallel (different files, no dependencies)

**After Phase 5 (US3)**: Phases 6–11 (US4–US9) can all be implemented in parallel since they target different pages/features within the device window, and each has its own ViewModel + XAML files

**Within each story phase**: Tasks marked [P] can run in parallel within that phase

## Implementation Strategy

1. **MVP (Phases 1–5)**: Setup + Foundation + US1 + US2 + US3 = user can add a device and connect to it
2. **Enhanced (Phases 6–8)**: US4 + US5 + US6 = navigation toggle, display settings, favorites
3. **Complete (Phases 9–12)**: US7 + US8 + US9 + Settings/Polish = all features





