# Feature Specification: Quick Virtual Desktop Switching

**Feature Branch**: `007-quick-desktop-switch`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Quickly switch between Windows virtual desktops from the Remort parent window without using the Windows task view"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Switch to a Different Virtual Desktop from Remort (Priority: P1)

A user has multiple Windows virtual desktops set up and is using Remort on one of them. They want to quickly jump to a different virtual desktop without opening Windows Task View or reaching for the global keyboard shortcut (Ctrl+Win+Arrow). The user accesses a desktop switcher control within the Remort window — such as a dropdown, button bar, or compact list — that shows their available virtual desktops. They select the target desktop and are instantly switched there, just as if they had used the Windows keyboard shortcut.

**Why this priority**: This is the core value of the feature — giving users a one-click way to switch virtual desktops without leaving Remort's interface. It reduces context-switching friction for users who work across multiple desktops but want to stay focused in Remort.

**Independent Test**: Can be fully tested by creating two or more virtual desktops, opening Remort on Virtual Desktop 1, using the desktop switcher control to switch to Virtual Desktop 2, and verifying the active desktop changes to Virtual Desktop 2. Delivers the in-app desktop switching experience.

**Acceptance Scenarios**:

1. **Given** the user has two or more virtual desktops and Remort is open, **When** they open the desktop switcher control, **Then** they see a list of all available virtual desktops.
2. **Given** the desktop switcher control is visible and shows available desktops, **When** the user selects a different virtual desktop, **Then** Windows switches to that virtual desktop.
3. **Given** the user selects the desktop they are already on in the switcher, **When** the switch is attempted, **Then** nothing changes and no error occurs.
4. **Given** the user switches to another virtual desktop using the Remort desktop switcher, **When** the switch completes, **Then** the Remort window on the original desktop behaves according to the user's pin-to-desktop setting (spec 005) — if pinned, it stays on the original desktop; if not pinned, it follows the default Windows behaviour.

---

### User Story 2 — See Which Virtual Desktop Is Currently Active (Priority: P2)

A user glances at the desktop switcher control in Remort and can immediately tell which virtual desktop they are currently on. The current desktop is visually highlighted or indicated, so the user always knows their position without having to open Task View.

**Why this priority**: Knowing which desktop is active provides essential orientation. Without it, the switcher is useful but the user must guess or check externally. This story makes the switcher self-contained and informative.

**Independent Test**: Can be tested by opening Remort on Virtual Desktop 1, looking at the switcher, and verifying it highlights Desktop 1 as active. Then switching to Virtual Desktop 2 and verifying the highlight updates to Desktop 2.

**Acceptance Scenarios**:

1. **Given** the user is on Virtual Desktop 1 and the desktop switcher is visible, **When** they look at the switcher, **Then** Virtual Desktop 1 is visually distinguished as the active desktop.
2. **Given** the user switches to Virtual Desktop 2 (via the switcher or any other method), **When** the switcher is next visible, **Then** the active desktop indicator updates to show Virtual Desktop 2.
3. **Given** a new virtual desktop is created while Remort is running, **When** the user opens or refreshes the switcher, **Then** the new desktop appears in the list.
4. **Given** a virtual desktop is removed while Remort is running, **When** the user opens or refreshes the switcher, **Then** the removed desktop no longer appears in the list.

---

### User Story 3 — Keyboard Shortcut for In-App Desktop Switching (Priority: P3)

A power user wants to switch virtual desktops using a keyboard shortcut within Remort, rather than clicking the desktop switcher control. They use a shortcut (e.g., Ctrl+Alt+Left/Right) that cycles through virtual desktops in order, providing a keyboard-driven alternative to the mouse-based switcher.

**Why this priority**: This enhances the feature for keyboard-oriented users who want to avoid the mouse entirely. The core mouse-driven switching (Story 1) works without this, making it a valuable but non-essential enhancement.

**Independent Test**: Can be tested by pressing the keyboard shortcut while Remort is focused and verifying the active virtual desktop changes to the next or previous desktop.

**Acceptance Scenarios**:

1. **Given** the user has Remort focused and there are multiple virtual desktops, **When** they press the "next desktop" shortcut, **Then** Windows switches to the next virtual desktop in order.
2. **Given** the user is on the last virtual desktop, **When** they press the "next desktop" shortcut, **Then** the behaviour wraps around to the first desktop or does nothing (consistent with Windows default behaviour for Ctrl+Win+Right).
3. **Given** the user presses the "previous desktop" shortcut, **When** they are on the first virtual desktop, **Then** the behaviour wraps to the last desktop or does nothing (consistent with how "next" works).

---

### Edge Cases

- What happens when the user has only one virtual desktop? The desktop switcher shows one entry. Selecting it does nothing. No error is displayed.
- What happens when virtual desktops are added or removed while Remort is running? The switcher updates its list to reflect the current desktops. It does not require an application restart to detect changes.
- What happens when the user switches desktops via the Remort switcher while an RDP session is active? The desktop switch proceeds normally. The RDP session is unaffected — it continues running on whichever desktop Remort's window remains on (governed by the pin-to-desktop setting from spec 005).
- What happens when the Remort window is pinned to a specific desktop (spec 005) and the user switches away using the desktop switcher? The Remort window stays on its pinned desktop, as expected. The user may no longer see the Remort window after switching, which is the correct behaviour for a pinned window.
- What happens when Windows virtual desktop names are the default (e.g., "Desktop 1", "Desktop 2") versus custom-named desktops? The switcher displays whatever name Windows has assigned, including custom names if the user has set them.
- What happens if the underlying Windows API for virtual desktop management is unavailable (e.g., running on a very old Windows build)? The desktop switcher control is hidden or disabled, and the user sees no broken UI. The rest of Remort functions normally.
- What happens when a desktop switch is in progress and the user clicks a different desktop in the switcher? The most recent selection wins. No duplicate switch errors or stuck states occur.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST provide an in-app control that lists all currently available Windows virtual desktops.
- **FR-002**: The control MUST display the name of each virtual desktop as reported by Windows (including custom-assigned names).
- **FR-003**: The control MUST visually indicate which virtual desktop is currently active.
- **FR-004**: When the user selects a virtual desktop from the control, the application MUST switch Windows to that virtual desktop.
- **FR-005**: Selecting the currently active virtual desktop MUST have no effect and produce no errors.
- **FR-006**: The list of virtual desktops MUST refresh to reflect desktops added or removed while the application is running.
- **FR-007**: The desktop switcher control MUST be accessible from the main Remort window without navigating to a separate settings or configuration screen.
- **FR-008**: The desktop switcher MUST NOT interfere with the RDP session — switching desktops does not disconnect, pause, or alter the active remote session.
- **FR-009**: The desktop switcher MUST coexist with the pin-to-desktop feature (spec 005). Switching away via the switcher respects the current pin-to-desktop setting.
- **FR-010**: If the Windows virtual desktop API is not available, the desktop switcher control MUST be hidden or disabled gracefully.
- **FR-011**: The application MUST provide keyboard shortcuts to switch to the next and previous virtual desktops while the Remort window is focused.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch to any virtual desktop from the Remort window in under 2 clicks (open switcher, select desktop).
- **SC-002**: The active desktop indicator updates to reflect the current desktop within 1 second of any desktop switch (whether initiated from Remort or externally).
- **SC-003**: The desktop list accurately reflects all available virtual desktops, including custom-named desktops — 100% of the time while the application is running.
- **SC-004**: Switching desktops via the Remort switcher does not disconnect or degrade the active RDP session — 100% of the time.
- **SC-005**: On systems where the virtual desktop API is unavailable, the switcher is hidden with no errors or broken UI visible to the user.
- **SC-006**: Keyboard shortcuts for next/previous desktop respond within 500 milliseconds of keypress while Remort is focused.

## Dependencies

- **Spec 005 — Pin Window to Virtual Desktop**: The desktop switcher interacts with pin-to-desktop behaviour. When pinned, switching away via the switcher leaves the Remort window on its current desktop. This feature does not require pin-to-desktop to be enabled but must coexist with it.

## Assumptions

- Windows 10 or later is required, as virtual desktops were introduced in Windows 10. Remort already targets Windows via WPF.
- Windows provides APIs (or sufficiently documented COM interfaces) to enumerate virtual desktops, detect the active desktop, and programmatically switch between desktops. The publicly supported API surface is limited, but community-documented COM interfaces exist and are widely used.
- Virtual desktop names are available via Windows APIs on Windows 11 and later. On Windows 10 builds where names are not exposed, desktops are identified by ordinal number (e.g., "Desktop 1", "Desktop 2").
- The desktop switcher does not create or destroy virtual desktops — it only navigates between existing ones.
- The keyboard shortcuts for desktop switching apply only when the Remort window is focused. They do not register global hotkeys that would capture input from other applications.
- This feature operates independently of the RDP connection state. The switcher is available whether or not an RDP session is active.
