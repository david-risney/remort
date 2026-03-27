# Feature Specification: Pin RDP Window to Virtual Desktop

**Feature Branch**: `005-pin-window-virtual-desktop`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Pin an RDP connection window to a specific Windows virtual desktop so it stays on that desktop and does not follow the user when switching desktops"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Pin Connection Window to Current Virtual Desktop (Priority: P1)

A user has multiple Windows virtual desktops and uses Remort on one of them to connect to a remote host. They want the Remort window to stay on that specific virtual desktop so that when they switch to another desktop (e.g., using Ctrl+Win+Arrow or Task View), the Remort window does not follow them. The remote session remains pinned to the desktop where the user placed it, keeping their other desktops clear and organized.

**Why this priority**: This is the core value of the feature — preventing the RDP window from appearing on every virtual desktop. Without this, users who rely on virtual desktops for workspace separation find the Remort window disruptive to their workflow.

**Independent Test**: Can be fully tested by opening Remort on Virtual Desktop 1, connecting to a host, enabling the pin setting, switching to Virtual Desktop 2, and verifying the Remort window does not appear there. Delivers the desktop separation value.

**Acceptance Scenarios**:

1. **Given** a user has the pin-to-desktop feature enabled and is connected to a remote host on Virtual Desktop 1, **When** they switch to Virtual Desktop 2, **Then** the Remort window does not appear on Virtual Desktop 2.
2. **Given** a user has the pin-to-desktop feature enabled, **When** they switch back to Virtual Desktop 1, **Then** the Remort window is visible exactly where they left it.
3. **Given** a user has the pin-to-desktop feature enabled and Remort is not currently connected (idle/ready state), **When** they switch virtual desktops, **Then** the idle Remort window also stays pinned to its current desktop.
4. **Given** a user has the pin-to-desktop feature disabled (default), **When** they switch virtual desktops, **Then** the Remort window follows the default Windows behaviour (appears on all desktops or follows the user, depending on the OS default).

---

### User Story 2 — Toggle Pin-to-Desktop On and Off (Priority: P2)

A user wants to control whether the Remort window is pinned to a single virtual desktop or available across all desktops. They find a toggle in the application (e.g., in settings or a toolbar button) that lets them turn pinning on or off. The change takes effect immediately — no restart or reconnection required. By default, pinning is disabled so existing users are not surprised by changed behaviour.

**Why this priority**: Without a toggle, pinning is forced on all users. Some users may want the window on every desktop (the Windows default for many apps). The toggle provides individual control and is essential for making the feature non-disruptive.

**Independent Test**: Can be tested by toggling pin-to-desktop off, switching desktops, and verifying the window follows (default behaviour). Then toggling it on, switching desktops, and verifying the window stays pinned.

**Acceptance Scenarios**:

1. **Given** the user opens Remort settings, **When** they view the pin-to-desktop option, **Then** they see a clearly labelled toggle (e.g., "Pin window to current virtual desktop").
2. **Given** pin-to-desktop is disabled (default), **When** the user switches virtual desktops, **Then** the Remort window follows the default Windows behaviour.
3. **Given** the user enables pin-to-desktop, **When** the setting change is applied, **Then** the window becomes pinned to the desktop it is currently on, without requiring the user to restart the application or reconnect.
4. **Given** the user disables pin-to-desktop after it was previously enabled, **When** the change is applied, **Then** the window reverts to the default Windows virtual desktop behaviour immediately.
5. **Given** the user enables pin-to-desktop, **When** they close and reopen Remort, **Then** the preference is remembered and the window is pinned to the desktop where it opens.

---

### User Story 3 — Visual Indicator of Pinned State (Priority: P3)

When the window is pinned to the current virtual desktop, the user sees a subtle visual indicator (e.g., an icon in the title bar or status area) confirming that the window is pinned. This helps the user understand why the window does not follow them to other desktops and reassures them the feature is active.

**Why this priority**: The pinning behaviour is invisible unless the user switches desktops. An indicator prevents confusion ("Why can't I see Remort on my other desktop?") and offers discoverability. However, the core pinning works without it.

**Independent Test**: Can be tested by enabling pin-to-desktop and verifying a visual indicator appears, then disabling it and verifying the indicator disappears.

**Acceptance Scenarios**:

1. **Given** pin-to-desktop is enabled, **When** the user looks at the Remort window, **Then** a visual indicator is visible showing the window is pinned to the current desktop.
2. **Given** pin-to-desktop is disabled, **When** the user looks at the Remort window, **Then** no pin indicator is displayed.
3. **Given** the user toggles pin-to-desktop on or off, **When** the change takes effect, **Then** the visual indicator updates immediately to reflect the current state.

---

### Edge Cases

- What happens when the user closes and reopens Remort on a different virtual desktop with pin-to-desktop enabled? The window opens on the current desktop and is pinned there. Pinning is always relative to the desktop the window is currently on, not the desktop it was previously on.
- What happens when the user has only one virtual desktop? The pin setting has no visible effect but causes no errors. The toggle is still available for future use if the user creates additional desktops.
- What happens when the user drags the Remort window between virtual desktops using Task View? The window becomes pinned to the desktop it was moved to. The pin follows the window, not a fixed desktop binding.
- What happens when the pinned desktop is closed (removed) while Remort is on it? Windows moves windows from a closed desktop to an adjacent desktop. Remort should remain pinned to whatever desktop Windows moves it to.
- What happens when multiple Remort instances are open on different desktops? Each instance independently manages its own pinning. Enabling pin-to-desktop in one instance does not affect other instances.
- What happens when Windows restarts and virtual desktops are recreated? The pin setting persists, but pinning is applied to whatever desktop the window appears on after restart. There is no memory of which numbered desktop the window was previously on.
- What happens when the user has "Show this window on all desktops" enabled at the OS level for Remort? The OS-level setting and the application-level pin setting may conflict. The application-level pin should override the OS-level setting when enabled, and release it when disabled, so the user has consistent control from within Remort.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST provide the ability to pin its main window to a single Windows virtual desktop so it does not appear on other virtual desktops.
- **FR-002**: When pin-to-desktop is enabled, the window MUST remain on the virtual desktop it is currently on and MUST NOT follow the user when switching to other desktops.
- **FR-003**: When pin-to-desktop is disabled, the window MUST revert to the default Windows virtual desktop behaviour.
- **FR-004**: The application MUST provide a user-facing toggle to enable or disable pin-to-desktop.
- **FR-005**: The pin-to-desktop toggle MUST default to disabled (off) on first use.
- **FR-006**: The pin-to-desktop setting MUST persist between application sessions.
- **FR-007**: Changing the pin-to-desktop setting MUST take effect immediately without requiring the application to restart or the RDP session to reconnect.
- **FR-008**: When pin-to-desktop is enabled, a visual indicator MUST be shown in the application to communicate that the window is pinned.
- **FR-009**: When pin-to-desktop is disabled, the visual indicator MUST NOT be shown.
- **FR-010**: The pin-to-desktop behaviour MUST apply to the Remort window regardless of connection state (connected, disconnected, connecting, or idle).

### Key Entities

- **Pin-to-Desktop Setting**: A boolean preference (enabled/disabled) controlling whether the Remort window is pinned to a single virtual desktop. Defaults to disabled. Stored persistently.

## Assumptions

- Windows 10 or later is required, as virtual desktops (Task View) were introduced in Windows 10. Remort already targets Windows via WPF.
- The application controls its own window pinning behaviour. It does not modify the global Windows setting for "Show this window on all desktops" for other applications.
- Pinning is relative to the current desktop at the time of activation (or the desktop the window is on), not to a specific numbered desktop. Windows virtual desktops do not have stable identifiers across sessions.
- If the user has never created multiple virtual desktops, the feature has no visible effect but remains harmless and available.
- This feature applies only to the main Remort window. Dialog boxes, pop-ups, and credential prompts follow standard Windows behaviour.
- The feature does not manage or create virtual desktops — it interacts with desktops the user has already created.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When pin-to-desktop is enabled and the user switches virtual desktops, the Remort window is not visible on any desktop other than the one it was pinned to — 100% of the time.
- **SC-002**: Users can enable or disable pin-to-desktop in under 3 clicks from the main window.
- **SC-003**: Toggling pin-to-desktop on or off takes effect within 1 second with no application restart or reconnection required.
- **SC-004**: The pin-to-desktop setting persists correctly across application restarts and Windows reboots — no data loss.
- **SC-005**: A visual indicator reflecting the current pin state is visible whenever pin-to-desktop is enabled, and absent when disabled.
