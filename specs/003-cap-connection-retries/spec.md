# Feature Specification: Cap Connection Retries

**Feature Branch**: `003-cap-connection-retries`  
**Created**: 2026-03-22  
**Status**: Draft  
**Input**: User description: "Cap connection retries to a configurable maximum instead of retrying forever, and surface clear status to the user when retries are exhausted"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Stop Retrying After a Maximum Number of Attempts (Priority: P1)

A user attempts to connect to a remote host that is unreachable or repeatedly fails. Instead of the application retrying the connection indefinitely (which leaves the user stuck in a "Connecting…" state with no recourse), the application retries a limited number of times and then stops. When retries are exhausted, the application displays a clear message telling the user the connection failed after the maximum number of attempts, and returns to the ready state so they can try again or enter a different host.

**Why this priority**: This is the core problem being solved — infinite retries trap the user in a loading state with no way out. Capping retries and surfacing a final failure is the minimum viable fix.

**Independent Test**: Can be fully tested by entering a hostname that consistently fails (e.g., unreachable IP), waiting for retries to exhaust, and verifying the application stops retrying and shows a clear failure message.

**Acceptance Scenarios**:

1. **Given** a connection attempt fails on the first try, **When** the application retries, **Then** it retries up to the configured maximum number of attempts (default: 3).
2. **Given** all retry attempts have been exhausted, **When** the final attempt fails, **Then** the application stops retrying, displays a message indicating the connection failed after the maximum number of retries, and returns to the ready state.
3. **Given** retries are in progress, **When** one of the retry attempts succeeds, **Then** the application connects normally and no further retries occur.
4. **Given** the user has not changed any settings, **When** a connection fails, **Then** the application uses the default retry limit (3 attempts).

---

### User Story 2 — See Retry Progress During Connection Attempts (Priority: P2)

While the application is retrying a failed connection, the user can see which attempt is currently in progress. The status area shows something like "Connecting… (attempt 2 of 3)" so the user understands that retries are happening and how many remain. This eliminates the guesswork of wondering whether the application is stuck or still working.

**Why this priority**: Without visible retry progress, the user cannot distinguish between "the app is retrying" and "the app is hung." This story builds directly on the retry cap (Story 1) and significantly improves the user's experience during failures.

**Independent Test**: Can be tested by connecting to an unreachable host and observing the status area — it should update to show the current attempt number and the total allowed.

**Acceptance Scenarios**:

1. **Given** a connection attempt fails and a retry begins, **When** the user views the status area, **Then** it displays the current attempt number and the maximum (e.g., "Connecting… (attempt 2 of 3)").
2. **Given** retries are in progress, **When** each retry begins, **Then** the status updates to reflect the new attempt number.
3. **Given** all retries are exhausted, **When** the final failure is shown, **Then** the status message includes the total number of attempts made (e.g., "Connection failed after 3 attempts").

---

### User Story 3 — Configure the Maximum Retry Count (Priority: P3)

A user wants to adjust how many times the application retries before giving up. They access a setting (within the application) that controls the maximum retry count. They can increase it for unreliable networks or decrease it (even to zero for no retries) for fast-fail scenarios. The new value takes effect for the next connection attempt.

**Why this priority**: Configurability is valuable but secondary — the default retry cap (Story 1) and visible progress (Story 2) solve the core problem for most users. Power users benefit from being able to tune this value.

**Independent Test**: Can be tested by changing the retry count setting to a specific value (e.g., 5), attempting a connection to an unreachable host, and verifying the application retries exactly that many times before stopping.

**Acceptance Scenarios**:

1. **Given** the user sets the maximum retry count to 5, **When** a connection fails and retries begin, **Then** the application retries up to 5 times before stopping.
2. **Given** the user sets the maximum retry count to 0, **When** a connection fails, **Then** the application does not retry and immediately shows the failure.
3. **Given** the user changes the retry count while no connection is active, **When** the next connection attempt begins, **Then** the new retry count is used.
4. **Given** the user has not changed the setting, **When** the application starts, **Then** the default value of 3 is used.

---

### Edge Cases

- What happens when the user disconnects manually while retries are in progress? The application cancels all pending retries and returns to the ready state immediately.
- What happens when the retry setting is set to a very high number (e.g., 100)? The application honours the value but the user can always cancel by disconnecting. An upper bound is not enforced — the user is trusted to choose a reasonable value.
- What happens when the underlying error changes between retries (e.g., first attempt times out, second gets "host not found")? The final failure message reflects the most recent error encountered.
- What happens when the application is closed during retries? The retries are cancelled and the session is cleaned up before exit (consistent with the disconnect-on-close behaviour from specification 001).
- What happens when the retry count is set to 1? The application makes exactly one attempt with no retries (1 attempt total, not 1 retry after the first).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST limit connection retries to a configurable maximum number of attempts.
- **FR-002**: The default maximum retry count MUST be 3 attempts.
- **FR-003**: When all retry attempts are exhausted, the application MUST stop retrying and return to the ready state.
- **FR-004**: When all retry attempts are exhausted, the application MUST display a clear message indicating the connection failed and the number of attempts made.
- **FR-005**: The failure message MUST include the reason for the most recent failure (e.g., "Connection timed out after 3 attempts").
- **FR-006**: During retries, the application MUST display the current attempt number and the total allowed (e.g., "Connecting… (attempt 2 of 3)").
- **FR-007**: If a retry attempt succeeds, the application MUST connect normally and cease further retry logic.
- **FR-008**: The user MUST be able to configure the maximum retry count through a setting in the application.
- **FR-009**: Setting the maximum retry count to 0 MUST disable retries — the application makes exactly one connection attempt.
- **FR-010**: The configured retry count MUST take effect for the next connection attempt (not mid-retry).
- **FR-011**: The user MUST be able to cancel retries at any time by disconnecting, which immediately stops all retry attempts and returns to the ready state.
- **FR-012**: The retry count setting MUST persist between application sessions.

### Key Entities

- **Retry Policy**: The configuration that governs how many times the application re-attempts a failed connection. Defined by a maximum attempt count with a default of 3.
- **Connection Attempt**: A single try to establish an RDP session to a target host. Tracked by its ordinal number within the current retry sequence (e.g., attempt 2 of 3).
- **Connection Failure**: The outcome when all retry attempts are exhausted. Includes the reason from the most recent failed attempt and the total number of attempts made.

## Assumptions

- The retry mechanism applies to the initial connection phase only — it does not retry if an established session drops mid-use (that is a reconnection concern, out of scope).
- There is no delay/backoff between retry attempts for this feature. If a backoff strategy is needed in the future, it would be a separate enhancement.
- The retry count setting applies globally to all connection attempts (not per-host).
- The retry count value is a non-negative integer (0 or greater).
- This feature builds on the connection lifecycle defined in specification 001 (connect, disconnect, status display).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When connecting to an unreachable host, the application stops retrying and shows a failure message within a bounded time proportional to the configured retry count — no infinite waiting.
- **SC-002**: 100% of retry exhaustion events display a human-readable message with the attempt count and failure reason (no silent hangs or raw error codes).
- **SC-003**: Users can see which retry attempt is in progress at all times during a failing connection sequence.
- **SC-004**: Users can change the retry count setting, and the new value is reflected in the next connection attempt.
- **SC-005**: Users can cancel an in-progress retry sequence at any time and return to the ready state within 3 seconds.
