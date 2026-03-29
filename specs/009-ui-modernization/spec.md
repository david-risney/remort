# Feature Specification: Modern Windows UI

**Feature Branch**: `009-ui-modernization`  
**Created**: 2026-03-28  
**Status**: Draft  
**Input**: User description: "Modern Windows UI with main window navigation, device windows, and remote desktop integration"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse and Select a Device (Priority: P1)

A user launches Remort and sees the main window with a navigation sidebar containing Favorites, Devices, and Settings. The Devices view shows all known devices as visual cards with rounded corners. Each card displays the device name and either a screenshot from the last connection or a color gradient. The user clicks a device card to open its dedicated device window.

**Why this priority**: This is the core navigation flow — without seeing and selecting devices, no other functionality is reachable.

**Independent Test**: Can be fully tested by launching the app, verifying the navigation sidebar appears with three items, viewing the Devices list, and clicking a device card to confirm a device window opens.

**Acceptance Scenarios**:

1. **Given** the app is launched for the first time with no saved devices, **When** the user views the Devices page, **Then** an empty list is shown with only the Add Device button visible
2. **Given** the user has previously added devices, **When** the user views the Devices page, **Then** each device appears as a rounded-corner card showing its name
3. **Given** a device was previously connected and a screenshot was captured, **When** the card is displayed, **Then** the card background shows the last-captured screenshot
4. **Given** a device has never been connected, **When** the card is displayed, **Then** the card shows a color gradient background
5. **Given** the Devices list is visible, **When** the user clicks a device card, **Then** a new device window opens for that device

---

### User Story 2 - Add a New Device (Priority: P1)

A user clicks the Add Device button on the Favorites or Devices page. A dialog appears prompting for a device name and hostname. The user fills in both fields and presses OK. The new device appears in the list with default settings.

**Why this priority**: Adding devices is essential — users need to configure at least one device to use the app.

**Independent Test**: Can be fully tested by clicking Add Device, entering name and hostname, pressing OK, and confirming the new device card appears in the list.

**Acceptance Scenarios**:

1. **Given** the user is on the Devices page, **When** the user clicks Add Device, **Then** a dialog appears with Name and Hostname fields and OK/Cancel buttons
2. **Given** the Add Device dialog is open, **When** the user enters a name and hostname and clicks OK, **Then** the dialog closes and a new device card appears in the list
3. **Given** the Add Device dialog is open, **When** the user clicks Cancel, **Then** no device is added and the dialog closes
4. **Given** the Add Device dialog is open, **When** the user leaves the Name or Hostname field empty and clicks OK, **Then** the dialog shows a validation message and does not close
5. **Given** a new device is added, **When** the device window is opened, **Then** all settings are at their default values

---

### User Story 3 - Connect to a Remote Device (Priority: P1)

A user opens a device window and sees the Connection view by default. The view shows a Connect button, status labels, and auto-connect checkboxes. The user clicks Connect. The status labels update to show connection progress. Once connected, the Connect button changes to Disconnect.

**Why this priority**: Connecting to a remote device is the primary function of the application.

**Independent Test**: Can be fully tested by opening a device window, clicking Connect, and observing status label changes and button state transitions.

**Acceptance Scenarios**:

1. **Given** a device window is open and not connected, **When** the Connection view is shown, **Then** a Connect button, Status label, Substatus label, and three checkboxes (autoconnect on start, autoconnect when visible, start on startup) are visible
2. **Given** the device is not connected, **When** the user clicks Connect, **Then** the Status label updates to show connection progress
3. **Given** the device is connected, **When** the user views the Connection page, **Then** the button reads Disconnect and the Status shows connected
4. **Given** the device is connected, **When** the user clicks Disconnect, **Then** the connection closes and the Status updates accordingly

---

### User Story 4 - Switch Between Navigation and Remote Desktop View (Priority: P2)

While in a device window, the user clicks the down-arrow toggle button in the titlebar to switch from the navigation view (showing settings pages) to the remote desktop view (showing the active RDP session). Clicking the toggle again returns to the navigation view.

**Why this priority**: Viewing the remote desktop is the reason for connecting — this bridges the settings UI to the actual remote session.

**Independent Test**: Can be fully tested by connecting to a device, clicking the toggle button, verifying the remote desktop view appears, and toggling back to navigation.

**Acceptance Scenarios**:

1. **Given** a device window is open, **When** the user clicks the down-arrow toggle in the titlebar, **Then** the navigation view is replaced by the remote desktop view
2. **Given** the remote desktop view is showing, **When** the user clicks the toggle again, **Then** the navigation view reappears
3. **Given** the device is not connected, **When** the user toggles to the remote desktop view, **Then** a placeholder or empty state is shown

---

### User Story 5 - Configure Display Settings (Priority: P2)

A user opens the Display page of a device window. They can enable "Pin to virtual desktop", "Fit session to window", and "Use all monitors when fullscreen". These settings affect how the remote desktop session is rendered and how the window behaves when maximized.

**Why this priority**: Display settings are important for power users managing multiple desktops and monitors, but the app is usable without them.

**Independent Test**: Can be fully tested by toggling each display checkbox and maximizing the window to verify behavior.

**Acceptance Scenarios**:

1. **Given** a device window Display page is open, **When** the user views the page, **Then** checkboxes for pin to virtual desktop, fit session to window, and use all monitors when fullscreen are visible
2. **Given** "Use all monitors when fullscreen" is checked, **When** the device window is maximized, **Then** the window spans all monitors
3. **Given** "Use all monitors when fullscreen" is unchecked, **When** the device window is maximized, **Then** the window fills only the current monitor
4. **Given** "Fit session to window" is checked, **When** the window is resized, **Then** the remote desktop session scales to fit the window

---

### User Story 6 - Manage Favorites (Priority: P2)

A user navigates to the Favorites page in the main window. It shows a curated list of devices the user has marked as favorites. The Favorites page also has an Add Device button.

**Why this priority**: Favorites provide quick access for users with many devices, improving daily workflow.

**Independent Test**: Can be fully tested by marking a device as a favorite and verifying it appears on the Favorites page.

**Acceptance Scenarios**:

1. **Given** the user navigates to Favorites, **When** the page loads, **Then** only devices marked as favorites are shown
2. **Given** no devices are marked as favorites, **When** the Favorites page is viewed, **Then** an empty state is shown with the Add Device button

---

### User Story 7 - Configure Redirections (Priority: P3)

A user opens the Redirections page of a device window. It shows the same set of local resource redirect checkboxes as the standard Windows Remote Desktop client (printers, clipboard, drives, audio, etc.).

**Why this priority**: Redirections enhance the remote session experience but are not required for basic connectivity.

**Independent Test**: Can be fully tested by toggling redirection checkboxes and verifying the settings are saved per device.

**Acceptance Scenarios**:

1. **Given** a device window Redirections page is open, **When** the page loads, **Then** checkboxes for clipboard, printers, drives, audio playback, audio recording, smart cards, and other standard redirections are visible
2. **Given** a redirection checkbox is toggled, **When** the device reconnects, **Then** the redirection setting is applied to the RDP session

---

### User Story 8 - Autohide Titlebar (Priority: P3)

When the "Autohide title bar" option is enabled in the General settings of a device window, the titlebar hides automatically. Moving the cursor to the top of the window reveals it.

**Why this priority**: A polish feature for immersive remote desktop experience — nice to have but not essential.

**Independent Test**: Can be fully tested by enabling the setting, verifying the titlebar hides, and moving the cursor to the top edge to reveal it.

**Acceptance Scenarios**:

1. **Given** autohide titlebar is enabled, **When** the cursor moves away from the titlebar area, **Then** the titlebar hides
2. **Given** autohide titlebar is enabled, **When** the cursor hovers over the area where the titlebar would be, **Then** the titlebar reveals
3. **Given** autohide titlebar is disabled, **When** the window is displayed, **Then** the titlebar is always visible

---

### User Story 9 - Task View Button (Priority: P3)

The device window titlebar includes a Task View button. Clicking it invokes the Windows Task View on the host machine (not the remote machine).

**Why this priority**: Convenience feature for virtual desktop users — useful but not critical.

**Independent Test**: Can be fully tested by clicking the Task View button and verifying that the host Windows Task View opens.

**Acceptance Scenarios**:

1. **Given** a device window is open, **When** the user clicks the Task View button in the titlebar, **Then** the host machine's Task View is activated

---

### Edge Cases

- What happens when a device is deleted while its device window is open? The device window closes gracefully after a confirmation dialog (FR-044).
- What happens when the same device is opened in two windows? Only one device window per device should be allowed — selecting an already-open device should focus the existing window.
- What happens when the user maximizes on a multi-monitor setup without "Use all monitors" checked? The window fills only the monitor it is on.
- How does the app handle a very long device name? The card and titlebar should truncate with ellipsis.
- What happens when the screenshot for a device card is missing or corrupted? Fall back to the color gradient background.
- What if the DevBox list returns devices with duplicate names? Each device is identified by hostname; duplicate display names are allowed but shown distinctly.

## Clarifications

### Session 2026-03-28

- Q: What should the Settings page in the main window contain? → A: Theme selection, DevBox account/discovery configuration, About info
- Q: How does a user mark a device as a favorite? → A: Star/heart icon on card, right-click context menu, and Favorite checkbox in device window General page
- Q: How does a user delete a device? → A: Right-click context menu on device card with "Remove Device" and a confirmation dialog
- Q: How should the app handle RDP authentication? → A: Delegate to native Windows credential prompt (CredSSP/NLA), no credential storage in the app
- Q: What should the Connection view show when a connection attempt fails? → A: Status label shows error state, Substatus shows error detail, button reverts to Connect (acts as retry)

## Requirements *(mandatory)*

### Functional Requirements

#### Main Window

- **FR-001**: The main window MUST display a navigation sidebar with three items: Favorites, Devices, and Settings
- **FR-002**: The Devices page MUST show all known devices (from DevBox discovery and manually added) as rounded-corner cards
- **FR-003**: Each device card MUST display the device name
- **FR-004**: Each device card MUST show the last-captured screenshot as background when available, otherwise a color gradient
- **FR-005**: The Devices page MUST include an Add Device button
- **FR-006**: The Add Device button MUST open a dialog with Name and Hostname fields and OK/Cancel buttons
- **FR-007**: The Add Device dialog MUST validate that both Name and Hostname are non-empty before accepting
- **FR-008**: A newly added device MUST appear in the device list with all settings at default values
- **FR-009**: The Favorites page MUST show the same card layout but filtered to favorite devices only
- **FR-010**: The Favorites page MUST include an Add Device button
- **FR-011**: Clicking a device card MUST open the device window for that device
- **FR-012**: Only one device window per device MUST be open at a time; re-selecting an open device MUST focus the existing window
- **FR-039**: Each device card MUST display a star/heart icon button that toggles the device's favorite status
- **FR-040**: Right-clicking a device card MUST show a context menu with an option to toggle favorite status
- **FR-042**: The device card context menu MUST include a "Remove Device" option
- **FR-043**: Selecting "Remove Device" MUST show a confirmation dialog before deleting
- **FR-044**: If a device window is open when the device is removed, the device window MUST close gracefully

#### Main Window — Settings Page

- **FR-036**: The Settings page MUST display a theme selector (light, dark, system)
- **FR-037**: The Settings page MUST display a DevBox discovery toggle (enable/disable automatic device discovery)
- **FR-038**: The Settings page MUST display an About section showing the application name and version

#### Device Window — Navigation

- **FR-013**: The device window MUST display a navigation sidebar with four items: Connection, General, Display, Redirections
- **FR-014**: Connection MUST be the default view when the device window opens

#### Device Window — Connection View

- **FR-015**: The Connection view MUST display a Connect/Disconnect button reflecting the current state
- **FR-016**: The Connection view MUST display a Status label and a Substatus label
- **FR-047**: When a connection attempt fails, the Status label MUST show the error state (e.g., "Disconnected") and the Substatus label MUST show the error detail (e.g., reason for failure)
- **FR-048**: After a connection failure, the button MUST revert to "Connect" so the user can retry without additional steps
- **FR-017**: The Connection view MUST display an "Autoconnect on start" checkbox
- **FR-018**: The Connection view MUST display an "Autoconnect when visible" checkbox
- **FR-019**: The Connection view MUST display a "Start on startup" checkbox

#### Device Window — General View

- **FR-020**: The General view MUST display a Name text field
- **FR-021**: The General view MUST display a Hostname text field
- **FR-022**: The General view MUST display an "Autohide title bar" checkbox
- **FR-041**: The General view MUST display a "Favorite" checkbox that toggles the device's favorite status

#### Device Window — Display View

- **FR-023**: The Display view MUST display a "Pin to virtual desktop" checkbox
- **FR-024**: The Display view MUST display a "Fit session to window" checkbox
- **FR-025**: The Display view MUST display a "Use all monitors when fullscreen" checkbox
- **FR-026**: When "Use all monitors when fullscreen" is checked and the window is maximized, the window MUST span all monitors
- **FR-027**: When "Fit session to window" is checked, the remote desktop session MUST scale to fit the window dimensions

#### Device Window — Redirections View

- **FR-028**: The Redirections view MUST display checkboxes for standard RDP local resource redirections: clipboard, printers, drives, audio playback, audio recording, smart cards, serial ports, and USB devices

#### Device Window — Titlebar

- **FR-029**: The device window titlebar MUST show the device name
- **FR-030**: The titlebar MUST include a down-arrow toggle button that switches between the navigation view and the remote desktop view
- **FR-031**: The titlebar MUST include a Task View button that activates the host machine's Windows Task View
- **FR-032**: When "Autohide title bar" is enabled, the titlebar MUST hide when the cursor is not near the top edge of the window
- **FR-033**: When "Autohide title bar" is enabled, the titlebar MUST reveal when the cursor hovers over the top edge of the window

#### Security & Authentication

- **FR-045**: The app MUST delegate RDP authentication to the native Windows credential prompt (CredSSP/NLA)
- **FR-046**: The app MUST NOT store user credentials; credential management is handled by Windows Credential Manager

#### Data Persistence

- **FR-034**: All device settings MUST be persisted and restored across application restarts
- **FR-035**: Manually added devices MUST be persisted separately from DevBox-discovered devices

### Key Entities

- **Device**: Represents a remote machine the user can connect to. Key attributes: name, hostname, favorite status, connection settings, display settings, redirection settings, last screenshot
- **DevBox Device**: A device discovered automatically through the DevBox resolution service. Merged into the device list alongside manually added devices
- **Device Settings**: Per-device configuration including connection preferences (autoconnect on start, autoconnect when visible, start on startup), general settings (autohide titlebar), display options (pin to virtual desktop, fit session to window, use all monitors), and redirection toggles

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can navigate from app launch to initiating a remote connection in 3 clicks or fewer (launch → select device → click Connect)
- **SC-002**: Users can add a new device and connect to it in under 60 seconds
- **SC-003**: The main window loads and displays the device list within 2 seconds of launch
- **SC-004**: Switching between navigation and remote desktop views in the device window occurs within 200 milliseconds
- **SC-005**: All device settings persist correctly across application restarts with no data loss
- **SC-006**: The app correctly spans all monitors when "Use all monitors when fullscreen" is enabled and the window is maximized

## Assumptions

- The application uses a modern WPF UI library (e.g., WPF UI / Wpf.Ui) that provides Windows 11-style navigation views, cards, and dialog components
- The standard RDP redirection options match those available in mstsc.exe (clipboard, printers, drives, audio playback, audio recording, smart cards, serial ports, USB devices)
- Device screenshots are captured automatically during active remote sessions; the capture mechanism is handled by the existing RDP control infrastructure
- DevBox-discovered devices and manually-added devices coexist in the same list with no special precedence rules
- "Start on startup" means the application registers to launch at Windows logon, not just when the device window opens
- "Autoconnect when visible" means the device auto-connects when its virtual desktop becomes the active desktop
- The Task View button sends the standard Windows Task View keyboard shortcut to the host OS
- RDP authentication is handled by the native Windows credential prompt (CredSSP/NLA); the app does not store or manage credentials
- Default color gradients for device cards without screenshots are assigned from a predefined palette (no user customization of gradients)
