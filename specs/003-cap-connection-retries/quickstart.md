# Quickstart: Cap Connection Retries

**Feature**: 003-cap-connection-retries

## Prerequisites

- Windows 10 or 11 (x64)
- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- `mstscax.dll` present (ships with Windows)
- An unreachable IP/hostname for retry testing (e.g., `192.0.2.1` — RFC 5737 TEST-NET)

## Build

```powershell
cd c:\Users\davris\source\repos\remort
dotnet build Remort.sln
```

Build must produce **zero errors and zero warnings** (`TreatWarningsAsErrors` is on).

## Run Tests

```powershell
dotnet test Remort.sln
```

All tests must pass. Retry logic tests use mocked `IRdpClient` — no RDP host or COM control required.

## Run the Application

```powershell
dotnet run --project src/Remort/Remort.csproj
```

## Manual Test Checklist

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Enter unreachable host, click Connect | Status → "Connecting… (attempt 1 of 3)", then "…(attempt 2 of 3)", then "…(attempt 3 of 3)", then "Connection failed after 3 attempts: {reason}" |
| 2 | Unreachable host, succeed on attempt 2 | Status → "Connecting… (attempt 1 of 3)", then "…(attempt 2 of 3)", then "Connected to {host}" |
| 3 | Click Disconnect during retry sequence | Retries stop immediately, status → "Disconnected" |
| 4 | Change max retry count to 5, connect to unreachable host | App retries 5 times before showing failure |
| 5 | Set max retry count to 0, connect to unreachable host | One attempt, immediate failure message (no retries) |
| 6 | Close app, reopen, check retry count | Value persists from previous session |
| 7 | Successful first-attempt connection | Status → "Connecting… (attempt 1 of 3)", then "Connected to {host}" — no retry messages |
| 8 | Normal connect/disconnect cycle | Identical to pre-feature behavior when no retries needed |

## Settings File Location

Settings are stored at `%APPDATA%\Remort\settings.json`. Example content:

```json
{
  "maxRetryCount": 3
}
```

Delete this file to reset to defaults.

## Key Files

| File | Purpose |
|------|---------|
| `src/Remort/Connection/IConnectionService.cs` | Retry-aware connection interface |
| `src/Remort/Connection/RetryingConnectionService.cs` | Retry orchestration implementation |
| `src/Remort/Connection/ConnectionRetryPolicy.cs` | Retry policy configuration |
| `src/Remort/Connection/MainWindowViewModel.cs` | ViewModel — updated for retry events + settings |
| `src/Remort/Settings/ISettingsStore.cs` | Settings persistence interface |
| `src/Remort/Settings/JsonSettingsStore.cs` | JSON file-based settings |
| `src/Remort/Settings/AppSettings.cs` | Settings model |
| `src/Remort.Tests/Connection/RetryingConnectionServiceTests.cs` | Retry logic tests |
