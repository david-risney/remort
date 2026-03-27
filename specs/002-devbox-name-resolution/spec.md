# Feature Specification: Dev Box Host Name Resolution

**Feature Branch**: `002-devbox-name-resolution`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Support connecting to devbox.microsoft.com hosts by name, resolving them to the correct RDP endpoint"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Connect to a Dev Box by Name (Priority: P1)

A user launches Remort and types a Dev Box name (e.g., `my-devbox`) or a fully qualified Dev Box hostname (e.g., `my-devbox.runnergroup.devbox.microsoft.com`) into the hostname field. When they click Connect, the application recognises the input as a Dev Box identifier, resolves it to the actual RDP endpoint (host and port) behind the scenes, and initiates an RDP connection to that resolved endpoint. The user sees the remote desktop session appear in the main window, exactly as if they had entered a raw IP or hostname.

**Why this priority**: This is the core capability requested — without name resolution, users must manually look up the RDP endpoint for their Dev Box every time, which defeats the purpose of a streamlined Dev Box experience.

**Independent Test**: Can be fully tested by entering a valid Dev Box name, clicking Connect, and verifying the application resolves it and establishes an RDP session. Delivers the primary value of connecting to Dev Boxes by friendly name.

**Acceptance Scenarios**:

1. **Given** the user enters a valid Dev Box short name (e.g., `my-devbox`), **When** they click Connect, **Then** the application resolves the name to an RDP endpoint and initiates a connection.
2. **Given** the user enters a fully qualified Dev Box hostname (e.g., `my-devbox.runnergroup.devbox.microsoft.com`), **When** they click Connect, **Then** the application resolves it to an RDP endpoint and initiates a connection.
3. **Given** the user enters a plain hostname that is not a Dev Box identifier (e.g., `myserver.contoso.com`), **When** they click Connect, **Then** the application connects directly to that hostname as a standard RDP target (no Dev Box resolution attempted).
4. **Given** the user enters a Dev Box name, **When** the resolution is in progress, **Then** the status area indicates the application is resolving the Dev Box name (e.g., "Resolving Dev Box…") before transitioning to "Connecting…".

---

### User Story 2 — Handle Dev Box Resolution Failures (Priority: P2)

A user enters a Dev Box name that cannot be resolved — for example, the Dev Box does not exist, the user does not have access, the resolution service is unreachable, or the user is not authenticated. The application displays a clear, user-friendly error message explaining why the Dev Box could not be resolved, and returns to the ready state.

**Why this priority**: Resolution failures are inevitable (typos, expired Dev Boxes, network issues). Without clear error handling, users cannot diagnose what went wrong and the experience feels broken.

**Independent Test**: Can be tested by entering an invalid or non-existent Dev Box name and verifying the application shows an appropriate error and returns to the ready state.

**Acceptance Scenarios**:

1. **Given** the user enters a Dev Box name that does not exist, **When** they click Connect, **Then** the application displays an error indicating the Dev Box was not found.
2. **Given** the user enters a Dev Box name but the resolution service is unreachable (e.g., no internet), **When** they click Connect, **Then** the application displays an error indicating it could not reach the Dev Box service.
3. **Given** the user is not authenticated with the Dev Box service, **When** they attempt to resolve a Dev Box name, **Then** the application prompts the user to sign in or displays an error indicating authentication is required.
4. **Given** a resolution failure occurs, **When** the error is displayed, **Then** the application returns to the ready state — the hostname field and Connect action are available again.

---

### User Story 3 — Authenticate with the Dev Box Service (Priority: P3)

Before the application can resolve Dev Box names, the user must be authenticated with the Microsoft identity platform. When the user first attempts to connect to a Dev Box by name, the application initiates a sign-in flow if the user is not already authenticated. After successful sign-in, the resolution proceeds automatically. On subsequent connections, cached credentials are reused without prompting again.

**Why this priority**: Authentication is a prerequisite for Dev Box resolution. However, it ranks behind the core connection and error handling stories because the sign-in flow is a supporting mechanism, not the primary user goal.

**Independent Test**: Can be tested by clearing any cached credentials, entering a Dev Box name, verifying the sign-in prompt appears, completing sign-in, and confirming the Dev Box resolves and connects.

**Acceptance Scenarios**:

1. **Given** the user is not signed in to the Dev Box service, **When** they attempt to connect to a Dev Box by name, **Then** the application initiates a sign-in flow (browser-based or system dialog).
2. **Given** the user completes sign-in successfully, **When** the sign-in flow finishes, **Then** the application proceeds to resolve the Dev Box name without requiring the user to click Connect again.
3. **Given** the user has previously signed in and credentials are cached, **When** they enter a Dev Box name and click Connect, **Then** the application resolves the name without an additional sign-in prompt.
4. **Given** the user cancels the sign-in flow, **When** the sign-in dialog is dismissed, **Then** the application displays a message indicating sign-in is required and returns to the ready state.

---

### Edge Cases

- What happens when the user enters a Dev Box name that matches multiple Dev Boxes (e.g., same short name in different projects)? The application displays a selection list showing all matches (with enough context — e.g., project name — to distinguish them) and waits for the user to pick one before proceeding.
- What happens when the Dev Box exists but is in a stopped/deallocated state? The application should display a message indicating the Dev Box is not currently running and cannot accept connections.
- What happens when the resolved RDP endpoint becomes unreachable after resolution succeeds? The standard RDP connection error handling (from the base connect/disconnect feature) applies.
- What happens when the user's authentication token expires mid-session? The active RDP session is unaffected (RDP uses its own authentication). Token expiry only affects future resolution attempts, at which point the user is prompted to re-authenticate.
- What happens when the user enters a Dev Box name with a trailing `.devbox.microsoft.com` suffix but misspells part of it? The application should attempt resolution and surface the resulting error from the Dev Box service (e.g., "Dev Box not found").

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST detect whether user input in the hostname field represents a Dev Box identifier or a standard RDP hostname.
- **FR-002**: The application MUST treat input ending in `.devbox.microsoft.com` as a Dev Box identifier.
- **FR-003**: The application MUST treat short names (no dots, or a single unqualified name) as potential Dev Box identifiers when a Dev Box context is available.
- **FR-004**: The application MUST resolve a Dev Box identifier to an RDP endpoint (host and port) before initiating the RDP connection.
- **FR-005**: The application MUST display a "Resolving…" status while Dev Box name resolution is in progress.
- **FR-006**: The application MUST fall back to standard direct RDP connection for hostnames that are not Dev Box identifiers.
- **FR-007**: The application MUST authenticate the user with the Microsoft identity platform before resolving Dev Box names.
- **FR-008**: The application MUST cache authentication credentials so the user is not prompted to sign in on every connection.
- **FR-009**: The application MUST display a user-friendly error message when Dev Box name resolution fails, including the reason (not found, not running, unauthorized, service unreachable).
- **FR-010**: The application MUST return to the ready state after any resolution failure.
- **FR-011**: When a Dev Box identifier could match multiple Dev Boxes, the application MUST display a selection list within the app showing all matching Dev Boxes so the user can choose the intended target.
- **FR-012**: The application MUST inform the user when a Dev Box is in a stopped or deallocated state and cannot accept connections.

### Key Entities

- **Dev Box Identifier**: A user-provided string that names a Dev Box — either a short name (e.g., `my-devbox`) or a fully qualified hostname (e.g., `my-devbox.runnergroup.devbox.microsoft.com`). Used to look up the actual RDP connection endpoint.
- **RDP Endpoint**: The resolved host and port that the RDP client connects to. Obtained by resolving a Dev Box identifier through the Dev Box service.
- **Authentication Context**: The user's signed-in identity used to authorise Dev Box name resolution requests. Cached between sessions to avoid repeated sign-in prompts.
- **Dev Box State**: The runtime status of a Dev Box (Running, Stopped, Deallocated, etc.). Only Running Dev Boxes can accept RDP connections.

## Assumptions

- The Dev Box service exposes an endpoint (or discoverable mechanism) that maps a Dev Box name to an RDP connection target (host and port).
- Users have a Microsoft Entra ID (Azure AD) account with permissions to access their Dev Boxes.
- The application uses standard OAuth2/OIDC flows for authentication with the Microsoft identity platform.
- Only one Dev Box connection is active at a time (consistent with the single-session constraint from specification 001).
- Port configuration for the resolved RDP endpoint is handled by the Dev Box service — the user does not need to specify a port.
- Standard RDP hostnames (IP addresses, FQDNs outside `.devbox.microsoft.com`) continue to work exactly as before — this feature is additive.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can connect to a running Dev Box by entering only its short name — no manual endpoint lookup required.
- **SC-002**: Dev Box name resolution completes (success or failure) within 10 seconds under normal network conditions.
- **SC-003**: 100% of resolution failures display a human-readable explanation to the user (no silent failures, raw error codes, or unhandled exceptions).
- **SC-004**: After initial sign-in, subsequent Dev Box connections within the same application session require no additional authentication prompts.
- **SC-005**: Entering a standard (non-Dev Box) hostname continues to work identically to the existing direct-connect behaviour — no regressions.
