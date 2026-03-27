# Quickstart: Auto-Reconnect on Windows Login

**Feature**: 004-auto-reconnect-on-login
**Date**: 2026-03-23

## Prerequisites

- Windows 10/11 (x64)
- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# Dev Kit

## Build

```bash
dotnet build Remort.sln
```

All warnings are errors. The build must be clean (zero errors, zero warnings).

## Test

```bash
dotnet test Remort.sln
```

All tests must pass, including new auto-reconnect tests in `MainWindowViewModelTests.cs`.

## Run

```bash
dotnet run --project src/Remort/Remort.csproj
```

## Manual Verification

### Story 1 — Auto-Reconnect on Login

1. Launch Remort
2. Check the "Reconnect on sign-in" checkbox in the connection bar
3. Enter a hostname and click Connect
4. After the connection is established (or at least attempted), disconnect
5. Lock the workstation (Win+L)
6. Unlock the workstation
7. **Expected**: Remort automatically begins connecting to the last host. Status shows "Auto-reconnecting to [host]… (attempt 1 of 3)"

### Story 2 — Toggle Enable/Disable

1. Launch Remort
2. Verify the "Reconnect on sign-in" checkbox is unchecked (default)
3. Connect to a host, then disconnect
4. Lock and unlock the workstation
5. **Expected**: No auto-reconnect occurs
6. Check the "Reconnect on sign-in" checkbox
7. Lock and unlock the workstation
8. **Expected**: Auto-reconnect occurs

### Story 3 — Status Feedback

1. Enable auto-reconnect and connect to a host
2. Lock and unlock the workstation
3. **Expected**: Status bar shows "Auto-reconnecting to [host]…" (not just "Connecting…")
4. If connection fails, status shows "Auto-reconnect failed after N attempts: [reason]"

### Edge Case — No Previous Host

1. Delete `%APPDATA%/Remort/settings.json` (fresh state)
2. Launch Remort
3. Check "Reconnect on sign-in"
4. Lock and unlock the workstation
5. **Expected**: No auto-reconnect occurs (no host to reconnect to)

### Edge Case — Cancel Auto-Reconnect

1. Enable auto-reconnect and connect to a host
2. Lock and unlock the workstation
3. While "Auto-reconnecting…" is shown, click Disconnect
4. **Expected**: Auto-reconnect is cancelled, returns to Disconnected state

## Settings File

Location: `%APPDATA%/Remort/settings.json`

```json
{
  "maxRetryCount": 3,
  "autoReconnectEnabled": true,
  "lastConnectedHost": "myserver.contoso.com"
}
```
