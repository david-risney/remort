# Feature Specification: Auto-Reconnect on Windows Login

**Feature Branch**: `004-auto-reconnect-on-login`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Automatically reconnect an RDP session when the user logs in to Windows, with a configurable enable/disable toggle"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Automatically Reconnect on Windows Login (Priority: P1)

A user was connected to a remote host through Remort, then locked their machine or logged out of Windows. When they log back in to Windows, Remort automatically re-establishes the RDP session to the last connected host without the user needing to open the application and manually click Connect. The user arrives at their desktop and finds their remote session already reconnecting (or reconnected) in the Remort window.

**Why this priority**: This is the core value of the feature — eliminating the repetitive manual step of reconnecting to the same host every time the user logs in. It is the reason users would enable this feature.

**Independent Test**: Can be fully tested by connecting to a host, locking the workstation, unlocking it, and verifying that Remort automatically begins reconnecting to the previous host. Delivers the hands-free reconnection experience.

**Acceptance Scenarios**:

1. **Given** auto-reconnect is enabled and the user was previously connected to a host, **When** the user logs in to Windows, **Then** Remort automatically initiates a connection to the last connected host.
2. **Given** auto-reconnect is enabled and the auto-reconnect attempt is initiated, **When** the connection succeeds, **Then** the remote desktop session is displayed in the Remort window as if the user had manually connected.
3. **Given** auto-reconnect is enabled and the auto-reconnect attempt is initiated, **When** the connection fails (e.g., host unreachable), **Then** the application shows a clear status indicating the auto-reconnect failed with a reason, and returns to the ready state.
4. **Given** auto-reconnect is enabled but no previous connection exists (fresh install or the user has never connected), **When** the user logs in to Windows, **Then** Remort does not attempt to connect and shows its normal idle state.
5. **Given** auto-reconnect is enabled and the previous session was intentionally disconnected by the user (user clicked Disconnect), **When** the user logs in to Windows, **Then** Remort automatically reconnects to the last host, because the feature reconnects based on the last known host, regardless of how the session ended.

---

### User Story 2 — Enable or Disable Auto-Reconnect (Priority: P2)

A user wants to control whether Remort automatically reconnects on login. They find a toggle (e.g., a checkbox or switch) in the application settings that lets them turn auto-reconnect on or off. The setting takes effect for the next Windows login — no application restart is required. By default, auto-reconnect is disabled so the feature is opt-in.

**Why this priority**: Without the toggle, the auto-reconnect behaviour is forced on all users. The toggle gives users control and is essential for making the feature non-disruptive. It is the second most important slice because Story 1 is less useful (and potentially annoying) without user control.

**Independent Test**: Can be tested by toggling the setting off, logging out and back in, and verifying Remort does not auto-reconnect. Then toggling it on, logging out and back in, and verifying it does auto-reconnect.

**Acceptance Scenarios**:

1. **Given** the user opens Remort settings, **When** they view the auto-reconnect option, **Then** they see a toggle that is clearly labelled (e.g., "Reconnect automatically when I sign in to Windows").
2. **Given** auto-reconnect is disabled (default), **When** the user logs in to Windows, **Then** Remort does not attempt to reconnect and shows the normal idle state.
3. **Given** the user enables auto-reconnect, **When** the setting is saved, **Then** the preference persists between application sessions without requiring a restart.
4. **Given** the user disables auto-reconnect after it was previously enabled, **When** they next log in to Windows, **Then** Remort does not attempt to reconnect.

---

### User Story 3 — See Auto-Reconnect Status on Login (Priority: P3)

When Remort auto-reconnects on login, the user can see what is happening. The status area shows that the application is auto-reconnecting (e.g., "Auto-reconnecting to myserver.contoso.com…") so the user understands that the connection was initiated automatically, not manually. If the auto-reconnect fails, the status clearly indicates it was an automatic attempt that failed.

**Why this priority**: Status feedback ensures the user is not confused about why a connection is starting without their explicit action. It builds on Story 1 and improves trust and transparency, but the core reconnection works without it.

**Independent Test**: Can be tested by enabling auto-reconnect, logging in to Windows, and observing the status area to verify it indicates an automatic reconnection is in progress (distinguishable from a manual connection).

**Acceptance Scenarios**:

1. **Given** auto-reconnect triggers on login, **When** the connection attempt begins, **Then** the status area displays a message indicating an automatic reconnection is in progress, including the target host name.
2. **Given** auto-reconnect is in progress, **When** the connection succeeds, **Then** the status transitions to the normal "Connected" state.
3. **Given** auto-reconnect is in progress, **When** the connection fails, **Then** the status message indicates the auto-reconnect failed and includes the failure reason.

---

### Edge Cases

- What happens when Remort is not running at login time? Auto-reconnect only works if Remort is configured to start with Windows (or is already running). If the application is not running, no reconnection occurs. Configuring Remort to start with Windows is out of scope for this feature.
- What happens when the user logs in to Windows but the network is not yet available? The auto-reconnect attempt fails with a connection error. The existing retry mechanism (from spec 003) applies — if retries are configured, the application retries up to the maximum. If retries are exhausted, the failure is shown and the user can retry manually.
- What happens when the user has multiple instances of Remort open? Each instance independently manages its own auto-reconnect based on its own last connected host. If only one instance was connected, only that instance auto-reconnects.
- What happens when the last connected host is no longer valid (e.g., machine was decommissioned)? The connection attempt fails normally with the appropriate error message. No special handling beyond the standard failure path.
- What happens when the user connects to a new host, then logs out and back in? Remort auto-reconnects to the most recently connected host (the new one), not any previously connected host.
- What happens when the user cancels an auto-reconnect in progress? The user can disconnect at any time (consistent with spec 001), which cancels the auto-reconnect attempt and returns to the ready state.
- What happens when the auto-reconnect setting is changed while an auto-reconnect is already in progress? The in-progress attempt continues. The setting change takes effect for the next login.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST remember the hostname of the last successfully connected or last attempted host between sessions.
- **FR-002**: When auto-reconnect is enabled and the user logs in to Windows, the application MUST automatically initiate an RDP connection to the last remembered host.
- **FR-003**: The auto-reconnect attempt MUST use the same connection flow as a manual connection (including authentication prompts and retry behaviour from spec 003).
- **FR-004**: If the auto-reconnect attempt fails, the application MUST display a clear failure message and return to the ready state.
- **FR-005**: The application MUST provide a user-facing toggle to enable or disable auto-reconnect.
- **FR-006**: The auto-reconnect toggle MUST default to disabled (off) on first use.
- **FR-007**: The auto-reconnect toggle setting MUST persist between application sessions.
- **FR-008**: If auto-reconnect is disabled, the application MUST NOT attempt to reconnect on login.
- **FR-009**: If no previous host is remembered (e.g., first launch), the application MUST NOT attempt auto-reconnect even if the setting is enabled.
- **FR-010**: During an auto-reconnect attempt, the status area MUST indicate that an automatic reconnection is in progress, including the target hostname.
- **FR-011**: The user MUST be able to cancel an in-progress auto-reconnect at any time using the standard disconnect action.
- **FR-012**: The remembered host MUST be updated whenever the user connects to a new host (so auto-reconnect always targets the most recent host).

### Key Entities

- **Last Connected Host**: The hostname of the most recently connected (or most recently attempted) remote host. Stored persistently so it survives application and Windows restarts.
- **Auto-Reconnect Setting**: A boolean preference (enabled/disabled) controlling whether the application automatically reconnects on Windows login. Defaults to disabled. Stored persistently.

## Assumptions

- Remort must already be running (or configured to launch at startup) for auto-reconnect to trigger. Configuring Remort as a startup application is out of scope for this feature.
- Authentication is handled the same way as a manual connection — if the remote host requires credentials, the standard credential prompt appears during auto-reconnect.
- The retry behaviour from spec 003 (cap connection retries) applies to auto-reconnect attempts. Auto-reconnect does not introduce its own separate retry logic.
- "Logging in to Windows" includes unlocking the workstation, signing in after a lock screen, and signing in after a restart — any event that results in the user arriving at their desktop with Remort running.
- Only one host is remembered at a time (the most recent one). Reconnecting to multiple previous hosts simultaneously is out of scope.
- This feature does not cover reconnecting after a mid-session drop (e.g., network interruption during an active session). That is a separate reconnection concern.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When auto-reconnect is enabled and the user logs in to Windows, the application begins reconnecting to the last host within 5 seconds of the user's desktop becoming available.
- **SC-002**: 100% of auto-reconnect attempts display a clear status message distinguishing them from manual connections (e.g., "Auto-reconnecting to…").
- **SC-003**: Users can enable or disable auto-reconnect in under 3 clicks from the main window.
- **SC-004**: The auto-reconnect setting and last connected host persist correctly across application restarts and Windows reboots — no data loss.
- **SC-005**: Users can cancel an in-progress auto-reconnect and return to the ready state within 3 seconds.
