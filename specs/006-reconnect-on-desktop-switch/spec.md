# Feature Specification: Reconnect RDP Session on Virtual Desktop Switch

**Feature Branch**: `006-reconnect-on-desktop-switch`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Automatically reconnect an RDP session when the virtual desktop it is pinned to becomes visible, with a configurable toggle"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Automatically Reconnect When Pinned Desktop Becomes Visible (Priority: P1)

A user has Remort pinned to Virtual Desktop 2 (using the pin-to-desktop feature from spec 005) and was previously connected to a remote host. They switch away to another virtual desktop to do other work, and the RDP session eventually disconnects (due to timeout, idle policy, or the remote host dropping the session). Later, the user switches back to Virtual Desktop 2. Remort detects that its pinned desktop is now visible and automatically initiates a reconnection to the last connected host, so the user arrives at their remote session without having to manually click Connect.

**Why this priority**: This is the core value of the feature — seamlessly restoring the remote session when the user returns to the desktop where they expect to see it. It eliminates the friction of manually reconnecting every time the user switches back to their "remote work" desktop.

**Independent Test**: Can be fully tested by connecting to a host on Virtual Desktop 2 with pin-to-desktop enabled, switching to Virtual Desktop 1, waiting for the session to disconnect, switching back to Virtual Desktop 2, and verifying that Remort automatically begins reconnecting. Delivers the hands-free desktop-switch reconnection experience.

**Acceptance Scenarios**:

1. **Given** reconnect-on-desktop-switch is enabled and pin-to-desktop is enabled and the user was previously connected to a host, **When** the user switches to the virtual desktop where Remort is pinned, **Then** Remort automatically initiates a connection to the last connected host.
2. **Given** reconnect-on-desktop-switch is enabled and the session is still active (not disconnected), **When** the user switches to the pinned desktop, **Then** Remort does not attempt a new connection (the existing session is already live).
3. **Given** reconnect-on-desktop-switch is enabled and the auto-reconnect attempt is initiated, **When** the connection succeeds, **Then** the remote desktop session is displayed as if the user had manually connected.
4. **Given** reconnect-on-desktop-switch is enabled and the auto-reconnect attempt is initiated, **When** the connection fails (e.g., host unreachable), **Then** the application shows a clear status indicating the reconnect attempt failed with a reason and returns to the ready state.
5. **Given** reconnect-on-desktop-switch is enabled but no previous connection exists, **When** the user switches to the pinned desktop, **Then** Remort does not attempt to connect and shows its normal idle state.
6. **Given** reconnect-on-desktop-switch is enabled but pin-to-desktop is disabled, **When** the user switches virtual desktops, **Then** Remort does not attempt auto-reconnect on desktop switch (the feature requires pin-to-desktop to be active).

---

### User Story 2 — Enable or Disable Reconnect on Desktop Switch (Priority: P2)

A user wants to control whether Remort automatically reconnects when they switch to its pinned desktop. They find a toggle in the application settings that lets them turn this behaviour on or off. The setting takes effect immediately — no restart or reconnection required. By default, reconnect-on-desktop-switch is disabled so the feature is opt-in.

**Why this priority**: Without a toggle, the reconnect behaviour is forced on all users who pin their window. Some users may prefer to reconnect manually. The toggle gives users control and is essential for making the feature non-disruptive.

**Independent Test**: Can be tested by toggling the setting off, switching away and back to the pinned desktop, and verifying Remort does not auto-reconnect. Then toggling it on, switching away and back, and verifying it does auto-reconnect.

**Acceptance Scenarios**:

1. **Given** the user opens Remort settings, **When** they view the reconnect-on-desktop-switch option, **Then** they see a clearly labelled toggle (e.g., "Reconnect automatically when switching to this desktop").
2. **Given** reconnect-on-desktop-switch is disabled (default), **When** the user switches to the pinned desktop with a disconnected session, **Then** Remort does not attempt to reconnect.
3. **Given** the user enables reconnect-on-desktop-switch, **When** the setting is saved, **Then** the preference persists between application sessions without requiring a restart.
4. **Given** the user disables reconnect-on-desktop-switch after it was previously enabled, **When** they next switch to the pinned desktop, **Then** Remort does not attempt to reconnect.
5. **Given** pin-to-desktop is disabled, **When** the user views the reconnect-on-desktop-switch toggle, **Then** the toggle is visually disabled or accompanied by a hint that pin-to-desktop must be enabled for this feature to work.

---

### User Story 3 — See Reconnect-on-Switch Status Feedback (Priority: P3)

When Remort auto-reconnects because the user switched to the pinned desktop, the user can see what is happening. The status area shows that the application is reconnecting due to a desktop switch (e.g., "Reconnecting to myserver.contoso.com…") so the user understands the connection was initiated automatically. If the attempt fails, the status clearly indicates it was an automatic attempt that failed.

**Why this priority**: Status feedback ensures the user is not confused about why a connection is starting without their explicit action. It builds on Story 1 and improves trust and transparency, but the core reconnection works without it.

**Independent Test**: Can be tested by enabling the feature, switching to the pinned desktop after a disconnect, and observing the status area to verify it indicates an automatic reconnection is in progress.

**Acceptance Scenarios**:

1. **Given** reconnect-on-desktop-switch triggers on a desktop switch, **When** the connection attempt begins, **Then** the status area displays a message indicating an automatic reconnection is in progress, including the target host name.
2. **Given** reconnect-on-desktop-switch is in progress, **When** the connection succeeds, **Then** the status transitions to the normal "Connected" state.
3. **Given** reconnect-on-desktop-switch is in progress, **When** the connection fails, **Then** the status message indicates the auto-reconnect failed and includes the failure reason.

---

### Edge Cases

- What happens when the user rapidly switches between virtual desktops? Remort debounces desktop switch events to avoid initiating multiple reconnection attempts. Only the final "landing" desktop triggers a reconnect check.
- What happens when the user switches to the pinned desktop while a manual connection or another auto-reconnect (e.g., from spec 004) is already in progress? The desktop switch does not trigger a second connection attempt. Only one connection attempt is active at a time.
- What happens when the session disconnects while the user is still on the pinned desktop (not from switching away)? Reconnect-on-desktop-switch does not trigger because the user never left. The user manually reconnects or relies on other reconnection mechanisms (e.g., spec 004 auto-reconnect on login).
- What happens when Remort is minimized or not the focused window on the pinned desktop? The desktop switch detection still fires as long as the pinned desktop becomes visible. Remort does not need to be in the foreground.
- What happens when the user has multiple Remort instances pinned to different desktops? Each instance independently manages its own reconnect-on-desktop-switch based on its own pinned desktop and last connected host.
- What happens when the remote host requires a credential prompt during auto-reconnect? The credential prompt appears as it would for a manual connection. The user must provide credentials to complete the reconnection.
- What happens when reconnect-on-desktop-switch and auto-reconnect-on-login (spec 004) are both enabled? Both features operate independently. On login, spec 004 triggers. On desktop switch, this feature triggers. If both events occur simultaneously (e.g., logging in directly to the pinned desktop), only one connection attempt is initiated — the first event to fire claims the connection, and the second observes an in-progress connection and does nothing.
- What happens when the user cancels an in-progress auto-reconnect triggered by a desktop switch? The user can disconnect at any time (consistent with spec 001), which cancels the reconnect attempt and returns to the ready state. Switching away and back again triggers a new reconnect attempt.
- What happens when Remort has never been connected to any host? No reconnect is attempted regardless of the toggle state, since there is no host to reconnect to.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When reconnect-on-desktop-switch is enabled and pin-to-desktop is enabled and the session is disconnected, the application MUST automatically initiate an RDP connection to the last connected host when the user switches to the virtual desktop where Remort is pinned.
- **FR-002**: The application MUST NOT attempt to reconnect if the RDP session is already active or a connection attempt is already in progress.
- **FR-003**: The application MUST NOT attempt to reconnect if no previous host is remembered.
- **FR-004**: The application MUST NOT attempt to reconnect on desktop switch if pin-to-desktop (spec 005) is disabled, even if reconnect-on-desktop-switch is enabled.
- **FR-005**: The application MUST provide a user-facing toggle to enable or disable reconnect-on-desktop-switch.
- **FR-006**: The reconnect-on-desktop-switch toggle MUST default to disabled (off) on first use.
- **FR-007**: The reconnect-on-desktop-switch toggle setting MUST persist between application sessions.
- **FR-008**: When pin-to-desktop is disabled, the reconnect-on-desktop-switch toggle MUST be visually disabled or accompanied by a hint indicating the dependency on pin-to-desktop.
- **FR-009**: The reconnect attempt MUST use the same connection flow as a manual connection (including authentication prompts and retry behaviour from spec 003).
- **FR-010**: If the reconnect attempt fails, the application MUST display a clear failure message and return to the ready state.
- **FR-011**: During a reconnect-on-desktop-switch attempt, the status area MUST indicate that an automatic reconnection is in progress, including the target hostname.
- **FR-012**: The user MUST be able to cancel an in-progress reconnect triggered by a desktop switch using the standard disconnect action.
- **FR-013**: The application MUST debounce rapid desktop switch events to prevent multiple simultaneous reconnect attempts.
- **FR-014**: Changing the reconnect-on-desktop-switch setting MUST take effect immediately without requiring an application restart or reconnection.

### Key Entities

- **Reconnect-on-Desktop-Switch Setting**: A boolean preference (enabled/disabled) controlling whether the application automatically reconnects when the user switches to the virtual desktop where Remort is pinned. Defaults to disabled. Stored persistently.
- **Last Connected Host**: The hostname of the most recently connected remote host (shared with spec 004). Used as the target for automatic reconnection.

## Dependencies

- **Spec 005 — Pin Window to Virtual Desktop**: This feature requires pin-to-desktop to be enabled to detect which virtual desktop the Remort window belongs to and when the user switches to it. If pin-to-desktop is disabled, reconnect-on-desktop-switch has no effect.
- **Spec 001 — RDP Connect/Disconnect**: The reconnect attempt uses the same connection and disconnection flow.
- **Spec 003 — Cap Connection Retries**: The retry behaviour from spec 003 applies to reconnect attempts triggered by a desktop switch.
- **Spec 004 — Auto-Reconnect on Login**: Both features can coexist. They share the "last connected host" concept but trigger on different events (login vs. desktop switch). Only one connection attempt proceeds at a time.

## Assumptions

- Pin-to-desktop (spec 005) must be enabled for this feature to function. The feature depends on knowing which virtual desktop Remort is pinned to and detecting when that desktop becomes visible.
- The application can detect when its window's virtual desktop becomes active. Windows provides mechanisms to observe virtual desktop visibility changes.
- "Desktop becomes visible" means the user switches to the virtual desktop where Remort is pinned as their active desktop. It does not include peek/preview actions (e.g., hovering over Task View thumbnails).
- The "last connected host" is the same entity defined in spec 004. If both features are active, they share the same stored hostname.
- Authentication is handled the same way as a manual connection — if the remote host requires credentials, the standard credential prompt appears.
- The retry behaviour from spec 003 applies. Reconnect-on-desktop-switch does not introduce its own separate retry logic.
- This feature does not keep the RDP session alive while the user is on a different desktop. The session may disconnect due to remote host idle policy, network issues, or administrative disconnection — this feature reconnects afterward.
- Debounce duration for rapid desktop switches is an implementation detail. A reasonable default is assumed (e.g., a few hundred milliseconds).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When reconnect-on-desktop-switch is enabled and the user switches to the pinned desktop with a disconnected session, the application begins reconnecting within 3 seconds of the desktop becoming visible.
- **SC-002**: When the session is already active, switching to the pinned desktop does not trigger any redundant connection attempts — 100% of the time.
- **SC-003**: Users can enable or disable reconnect-on-desktop-switch in under 3 clicks from the main window.
- **SC-004**: The reconnect-on-desktop-switch setting persists correctly across application restarts and Windows reboots — no data loss.
- **SC-005**: 100% of auto-reconnect attempts triggered by a desktop switch display a clear status message distinguishing them from manual connections.
- **SC-006**: Users can cancel an in-progress desktop-switch reconnect and return to the ready state within 3 seconds.
- **SC-007**: Rapidly switching between virtual desktops (5+ switches within 2 seconds) results in at most one reconnect attempt.
