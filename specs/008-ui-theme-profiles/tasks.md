# Tasks: Modern UI Theme with Color Profiles

**Input**: Design documents from `/specs/008-ui-theme-profiles/`
**Prerequisites**: plan.md (template only — tech context inferred from codebase), spec.md ✅

**Tests**: Not included — not explicitly requested in the specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Data model, resource key constants, and preset profile definitions used by all user stories

- [ ] T001 Create `ColorProfile` sealed record in `src/Remort/Theme/ColorProfile.cs` — properties: `string Name`, `bool IsPreset`, `string PrimaryBackground`, `string SecondaryBackground`, `string Accent`, `string TextPrimary`, `string TextSecondary`, `string Border` (all hex strings, e.g. `"#1E1E1E"`); equality by value (record default); per spec FR-007 color properties
- [ ] T002 [P] Create `ThemeResourceKeys` static class in `src/Remort/Theme/ThemeResourceKeys.cs` — `public const string` fields: `PrimaryBackground`, `SecondaryBackground`, `Accent`, `TextPrimary`, `TextSecondary`, `Border`; these are the `x:Key` values used in XAML `DynamicResource` bindings
- [ ] T003 [P] Create `PresetProfiles` static class in `src/Remort/Theme/PresetProfiles.cs` — `public static IReadOnlyList<ColorProfile> All` containing three profiles with `IsPreset = true`; **Dark** (default): `#1E1E1E` / `#252526` / `#007ACC` / `#CCCCCC` / `#808080` / `#3F3F46`; **Light**: `#F5F5F5` / `#FFFFFF` / `#0078D4` / `#1E1E1E` / `#6E6E6E` / `#CCCCCC`; **Midnight Blue**: `#1B2A4A` / `#243B5E` / `#4FC3F7` / `#E0E0E0` / `#90A4AE` / `#2C4066`; expose `public static ColorProfile Default => All[0]`; all presets must meet WCAG AA 4.5:1 contrast for text on background (SC-006, FR-001)

**Checkpoint**: All three files compile with `dotnet build Remort.sln`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Theme service, resource dictionaries, and XAML updates that enable runtime color switching

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Create `IThemeService` interface in `src/Remort/Theme/IThemeService.cs` — `ColorProfile ActiveProfile { get; }`; `void ApplyProfile(ColorProfile profile)` applies a profile immediately to the UI; `event EventHandler? ProfileChanged` raised after a profile is applied
- [ ] T005 Implement `ThemeService` in `src/Remort/Theme/ThemeService.cs` — constructor takes `ResourceDictionary appResources`; `ApplyProfile` parses each hex color from the `ColorProfile` into a `System.Windows.Media.Color` via `ColorConverter`, creates `SolidColorBrush` resources keyed by `ThemeResourceKeys` constants, builds a new `ResourceDictionary`, removes the previous theme dictionary (if any) from `appResources.MergedDictionaries` and adds the new one; stores `ActiveProfile`; raises `ProfileChanged`; applies `PresetProfiles.Default` in the constructor so the app always has a valid theme
- [ ] T006 [P] Create `src/Remort/Theme/ThemeColors.xaml` ResourceDictionary — define `SolidColorBrush` resources with `x:Key` matching each `ThemeResourceKeys` constant, initialized to the Dark preset colors; this provides design-time defaults before `ThemeService` takes over at runtime
- [ ] T007 [P] Create `src/Remort/Theme/ThemeStyles.xaml` ResourceDictionary — define implicit `Style` entries for `Window` (Background, Foreground), `Button` (Background, Foreground, BorderBrush), `TextBox` (Background, Foreground, BorderBrush, CaretBrush), `ComboBox` (Background, Foreground, BorderBrush), `CheckBox` (Foreground), `StatusBar` (Background, Foreground), `TextBlock` (Foreground), `Separator` (Background); all color references use `DynamicResource` with `ThemeResourceKeys` so they update when the theme dictionary is swapped
- [ ] T008 Update `App.xaml` in `src/Remort/App.xaml` — add `<ResourceDictionary.MergedDictionaries>` containing `ThemeColors.xaml` and `ThemeStyles.xaml`; keep existing converter resources; update `MainWindow.xaml` in `src/Remort/MainWindow.xaml` to set `DockPanel.Background` to `{DynamicResource PrimaryBackground}`, connection bar `StackPanel.Background` to `{DynamicResource SecondaryBackground}`, and `StatusBar.Background` to `{DynamicResource SecondaryBackground}`; ensure all UI chrome uses themed colors (FR-002) while `wf:WindowsFormsHost` (RDP area) has no theme brushes applied (FR-003)

**Checkpoint**: Foundation ready — app launches with dark theme visibly applied to all chrome regions. `dotnet build Remort.sln` passes with zero warnings.

---

## Phase 3: User Story 1 — Apply a Preset Color Profile (Priority: P1) 🎯 MVP

**Goal**: User opens theme settings, sees preset profiles, selects one, and the entire UI updates immediately

**Independent Test**: Launch Remort, click Theme button, select Light profile, verify all UI chrome switches to light colors. Select Midnight Blue, verify it switches again. RDP content area unchanged.

### Implementation for User Story 1

- [ ] T009 [P] [US1] Create `ThemeSettingsViewModel` in `src/Remort/Theme/ThemeSettingsViewModel.cs` — extends `ObservableObject`; constructor takes `IThemeService` and `ISettingsStore?`; `ObservableCollection<ColorProfile> Profiles` populated from `PresetProfiles.All`; `[ObservableProperty] ColorProfile? selectedProfile` initialized to `IThemeService.ActiveProfile`; override `OnSelectedProfileChanged` to call `_themeService.ApplyProfile(value)` for immediate UI update (FR-004); `[RelayCommand] void Close()` — signals the dialog to close
- [ ] T010 [P] [US1] Create `ThemeSettingsWindow.xaml` and code-behind in `src/Remort/Theme/ThemeSettingsWindow.xaml` — modal `Window` (400×500); `ListBox` bound to `Profiles` with a `DataTemplate` showing profile `Name` plus a row of six small colored rectangles previewing each color property; `SelectedItem` bound to `SelectedProfile` (TwoWay); `CloseCommand` bound to a Close button; window title "Theme Settings"; use `DynamicResource` for the dialog's own chrome so it reflects live theme changes
- [ ] T011 [US1] Add "Theme" button to `MainWindow.xaml` in `src/Remort/MainWindow.xaml` — add a `Button` with `Content="Theme"` (or a paintbrush unicode glyph `🎨`) in the `StatusBar` at the right end; give it `x:Name="ThemeButton"` and handle Click in code-behind to open the `ThemeSettingsWindow`
- [ ] T012 [US1] Wire `ThemeService` in `MainWindow.xaml.cs` in `src/Remort/MainWindow.xaml.cs` — create `ThemeService` instance with `Application.Current.Resources` as a field; in the `ThemeButton` Click handler: create `ThemeSettingsViewModel` with `_themeService` and `_settingsStore`, create `ThemeSettingsWindow` with the ViewModel as `DataContext`, call `ShowDialog()`

**Checkpoint**: User Story 1 complete — clicking Theme button opens settings dialog, selecting a preset instantly recolors the entire app. All three presets work. RDP area unaffected.

---

## Phase 4: User Story 2 — Persist a Chosen Color Profile Across Sessions (Priority: P2)

**Goal**: Selected profile (preset or custom) restored automatically on next launch with no visible flash of the wrong theme

**Independent Test**: Select a non-default profile (e.g., Light), close Remort, reopen — app launches with Light theme already active. Delete settings.json, reopen — app launches with Dark default.

### Implementation for User Story 2

- [ ] T013 [US2] Add `ActiveProfileName` property to `AppSettings` in `src/Remort/Settings/AppSettings.cs` — `public string ActiveProfileName { get; init; } = string.Empty;` (empty means use default dark theme, FR-014)
- [ ] T014 [US2] Save and restore active profile in `MainWindow.xaml.cs` and `ThemeSettingsViewModel` — on `MainWindow` construction, after creating `ThemeService`, load `AppSettings`, find matching profile by name in `PresetProfiles.All` (later also custom profiles), and call `ApplyProfile` before window renders (SC-005); in `ThemeSettingsViewModel.OnSelectedProfileChanged`, save profile name via `ISettingsStore.Save` with updated `ActiveProfileName`
- [ ] T015 [US2] Handle missing or corrupted profile data — if `ActiveProfileName` does not match any known profile, fall back to `PresetProfiles.Default` (FR-013); if `JsonSettingsStore.Load()` returns defaults due to malformed JSON, the empty `ActiveProfileName` triggers default dark theme (FR-014); no crash on any settings corruption path

**Checkpoint**: User Story 2 complete — profile selection persists across restarts. Corrupt/missing settings degrade gracefully to dark default.

---

## Phase 5: User Story 3 — Create a Custom Color Profile (Priority: P3)

**Goal**: User creates a custom profile with a chosen name, adjusts colors via hex input, previews live, and saves it to the profile list

**Independent Test**: Open theme settings, click "New Profile", enter name "My Theme", change accent color to `#FF5722`, verify UI previews the change live, save — "My Theme" appears in profile list alongside presets. Switch away and back to confirm it persists.

### Implementation for User Story 3

- [ ] T016 [P] [US3] Create `IProfileStore` interface in `src/Remort/Theme/IProfileStore.cs` — `IReadOnlyList<ColorProfile> LoadCustomProfiles()` returns all saved custom profiles; `void SaveCustomProfiles(IReadOnlyList<ColorProfile> profiles)` persists complete list; `void DeleteAll()` clears all custom profiles
- [ ] T017 [P] [US3] Implement `JsonProfileStore` in `src/Remort/Theme/JsonProfileStore.cs` — persists to `%APPDATA%/Remort/color-profiles.json` via `System.Text.Json`; constructor accepts optional `string filePath` for testing; `LoadCustomProfiles` returns empty list if file missing or malformed (same fallback pattern as `JsonSettingsStore`); `SaveCustomProfiles` serializes with `WriteIndented = true` and `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`; all loaded profiles get `IsPreset = false` enforced
- [ ] T018 [US3] Add custom profile creation to `ThemeSettingsViewModel` — `[RelayCommand] void CreateProfile()` prompts for a name (via a `[ObservableProperty] string newProfileName` bound to a TextBox), clones `PresetProfiles.Default` colors with the new name and `IsPreset = false`, adds to `Profiles` collection, selects it, enters edit mode; `[ObservableProperty] bool isEditing` controls visibility of the color editor panel; `[RelayCommand] void SaveProfile()` persists via `IProfileStore.SaveCustomProfiles`, exits edit mode; `[RelayCommand] void CancelEdit()` reverts `ThemeService` to the previously active profile, removes unsaved new profile from list, exits edit mode
- [ ] T019 [US3] Add color editing panel to `ThemeSettingsWindow.xaml` — below the profile list, add a `StackPanel` or `Grid` visible only when `IsEditing` is true; six rows, each with a color label ("Primary Background", "Accent", etc.), a hex-value `TextBox` bound two-way to a property on the ViewModel, and a small `Rectangle` filled with the corresponding brush for visual feedback; add "Save" and "Cancel" buttons bound to `SaveProfileCommand` and `CancelEditCommand`
- [ ] T020 [US3] Implement live preview for color editing — in `ThemeSettingsViewModel`, expose `[ObservableProperty]` string properties for each editable color (`EditPrimaryBackground`, `EditAccent`, etc.); on any color property change, build a temporary `ColorProfile` from the current edit values and call `_themeService.ApplyProfile` for immediate preview (FR-008); `CancelEdit` restores the previously committed profile
- [ ] T021 [US3] Validate duplicate profile names — in `ThemeSettingsViewModel.CreateProfile`, check that `NewProfileName` does not match any existing profile name (case-insensitive) in `Profiles`; if duplicate, set a `[ObservableProperty] string nameValidationError` displayed in the UI; block creation until name is unique (FR-012)
- [ ] T022 [US3] Wire `JsonProfileStore` into `MainWindow.xaml.cs` and `ThemeSettingsViewModel` — create `JsonProfileStore` in `MainWindow.xaml.cs`, pass to `ThemeSettingsViewModel` constructor; on ViewModel construction, merge `IProfileStore.LoadCustomProfiles()` into `Profiles` (custom profiles listed after presets, visually grouped); update profile restoration in T014 to also search custom profiles by name

**Checkpoint**: User Story 3 complete — custom profiles can be created, named, color-edited with live preview, saved, and appear in the profile list. Cancel reverts cleanly.

---

## Phase 6: User Story 4 — Edit and Delete Custom Color Profiles (Priority: P4)

**Goal**: User can modify or remove custom profiles; presets remain read-only

**Independent Test**: Edit a custom profile's accent color, save, verify change persists. Delete a custom profile, confirm dialog, verify it disappears and does not reappear on restart. Verify preset profiles show no edit/delete options.

### Implementation for User Story 4

- [ ] T023 [P] [US4] Add edit command for custom profiles in `ThemeSettingsViewModel` — `[RelayCommand] void EditProfile()` enabled only when `SelectedProfile?.IsPreset == false`; enters edit mode with edit color properties populated from the selected profile's current values; `SaveProfile` updates the existing profile in `Profiles` and persists via `IProfileStore`; `CancelEdit` reverts to the pre-edit state
- [ ] T024 [P] [US4] Add delete command for custom profiles in `ThemeSettingsViewModel` — `[RelayCommand] void DeleteProfile()` enabled only when `SelectedProfile?.IsPreset == false`; shows a confirmation `MessageBox` "Delete profile '{Name}'?"; on confirm: removes from `Profiles`, persists updated list via `IProfileStore`; if deleted profile was the active profile, switch to `PresetProfiles.Default` and save selection (FR-013)
- [ ] T025 [US4] Update `ThemeSettingsWindow.xaml` for edit/delete controls — add "Edit" and "Delete" buttons next to the profile list (or as context actions per profile item); bind `IsEnabled` or `Visibility` to `SelectedProfile.IsPreset` (inverted) so preset profiles cannot be modified or removed (FR-011); show "Edit" and "Delete" only for custom profiles

**Checkpoint**: User Story 4 complete — custom profiles editable and deletable with confirmation. Presets are fully read-only. Deleting active profile falls back to default.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, accessibility, and final verification

- [ ] T026 [P] Verify all preset profiles meet WCAG AA contrast ratio (4.5:1 minimum for `TextPrimary` on `PrimaryBackground` and `TextPrimary` on `SecondaryBackground`) — adjust preset hex values in `PresetProfiles.cs` if any fail (SC-006)
- [ ] T027 [P] Verify no visible flash of wrong theme on startup — ensure `ThemeService.ApplyProfile` is called with the saved profile before `MainWindow` is rendered; the dark default in `ThemeColors.xaml` combined with early `ApplyProfile` in the constructor should prevent any flash (SC-005)
- [ ] T028 [P] Verify RDP content area (`WindowsFormsHost`) is unaffected when switching color profiles — no theme brushes should be applied to the `wf:WindowsFormsHost` element (FR-003)
- [ ] T029 Run full build and test suite — `dotnet build Remort.sln` (zero warnings) and `dotnet test Remort.sln` (all pass)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs ThemeSettingsViewModel and ThemeService wiring)
- **User Story 3 (Phase 5)**: Depends on Phase 4 (needs settings persistence for profile storage) 
- **User Story 4 (Phase 6)**: Depends on Phase 5 (needs custom profiles to exist for edit/delete)
- **Polish (Phase 7)**: Depends on Phases 3–6

### User Story Dependencies

- **User Story 1 (P1)**: Unblocked after Foundational. Delivers MVP — preset color profiles selectable and applied instantly.
- **User Story 2 (P2)**: Depends on US1 (needs ThemeService + SettingsStore wiring). Adds persistence across sessions.
- **User Story 3 (P3)**: Depends on US2 (needs persistence infrastructure). Adds custom profile creation. Can be developed without US4.
- **User Story 4 (P4)**: Depends on US3 (needs custom profiles in the list). Adds edit/delete management.

### Within Each User Story

- ViewModel changes before XAML changes (data layer before presentation)
- Service wiring after both ViewModel and XAML are ready
- Core implementation before integration/validation

### Parallel Opportunities

**Phase 1**: T002 and T003 can run in parallel (separate files). T001 should complete first (others reference `ColorProfile`).

**Phase 2**: T006 and T007 can run in parallel (separate XAML files). T004 before T005 (interface before implementation). T008 depends on T006 + T007.

**Phase 3 (US1)**: T009 and T010 can run in parallel (ViewModel vs. XAML). T011 depends on T010 (button in MainWindow). T012 depends on T009 + T011.

**Phase 5 (US3)**: T016 and T017 can run in parallel (interface + impl for ProfileStore). T023 and T024 can run in parallel in Phase 6 (edit vs. delete commands).

**Phase 7**: T026, T027, T028 can all run in parallel. T029 runs last.

---

## Parallel Example: User Story 1

```text
# Launch ViewModel and XAML creation in parallel:
Task T009: "ThemeSettingsViewModel in src/Remort/Theme/"
Task T010: "ThemeSettingsWindow.xaml in src/Remort/Theme/"

# Then add button and wire service (depends on T009 + T010):
Task T011: "Theme button in src/Remort/MainWindow.xaml"
Task T012: "Wire ThemeService in src/Remort/MainWindow.xaml.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T008)
3. Complete Phase 3: User Story 1 (T009–T012)
4. **STOP and VALIDATE**: Three preset profiles are selectable, UI recolors everywhere instantly
5. Delivers core value — modern theming with preset profiles

### Incremental Delivery

1. Setup + Foundational → theme infrastructure, ResourceDictionaries, styled controls ready
2. Add User Story 1 → preset profiles selectable → **MVP complete**
3. Add User Story 2 → profile persists across restarts → sessions feel seamless
4. Add User Story 3 → custom profiles creatable → power users served
5. Add User Story 4 → edit/delete custom profiles → profile management complete
6. Polish → accessibility verified, edge cases handled

### Parallel Strategy

With two agents/developers after Phase 2:
- **Agent A**: User Story 1 → User Story 2 → User Story 3 (sequential dependency chain)
- **Agent B**: Phase 7 T026 (WCAG contrast audit) can start early once presets exist

Note: US1 → US2 → US3 → US4 form a sequential dependency chain because each builds on the previous story's infrastructure. Parallelism is primarily within each phase, not across stories.

---

## Notes

- All new files go in `src/Remort/Theme/` domain folder, following existing folder-per-feature convention (`Connection/`, `Settings/`, `VirtualDesktop/`)
- WPF theming uses `DynamicResource` (not `StaticResource`) throughout so brushes update when the ResourceDictionary is swapped at runtime
- `ThemeService` manipulates `Application.Current.Resources.MergedDictionaries` — the standard WPF approach for runtime theme switching
- Custom profiles persist to `%APPDATA%/Remort/color-profiles.json` (separate from `settings.json`) for clean separation of concerns
- Active profile name persists in `AppSettings.ActiveProfileName` via the existing `JsonSettingsStore`
- `ThemeColors.xaml` provides design-time defaults; `ThemeService` overrides them at runtime, ensuring XAML designer still renders correctly
- `ThemeStyles.xaml` implicit styles ensure new dialogs (like `ThemeSettingsWindow`) automatically pick up theme colors
- Color values use hex strings (e.g., `"#1E1E1E"`) parsed via `ColorConverter` — no raw WPF `Color` structs in the model to keep serialization simple
- Live preview (US3) applies a temporary profile via `ThemeService.ApplyProfile`; cancel reverts to the committed profile stored in the ViewModel
- No new NuGet packages required — `System.Text.Json`, `System.Windows.Media`, and WPF ResourceDictionary APIs are all in-box
