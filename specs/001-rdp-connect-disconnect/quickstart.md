# Quickstart: RDP Connect & Disconnect

**Feature**: 001-rdp-connect-disconnect

## Prerequisites

- Windows 10 or 11 (x64)
- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- `mstscax.dll` present (ships with Windows — no separate install)
- A reachable RDP host for manual testing

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

All tests must pass. The ViewModel tests use mocked `IRdpClient` — no RDP host or COM control required.

## Run the Application

```powershell
dotnet run --project src/Remort/Remort.csproj
```

1. The main window opens with a hostname text field, Connect button, and status bar showing "Disconnected".
2. Enter a hostname (e.g., `myserver.contoso.com`) and click **Connect**.
3. The Windows credential dialog appears — authenticate.
4. The remote desktop renders inside the main window. Status shows "Connected to myserver.contoso.com".
5. Click **Disconnect** to end the session. Status returns to "Disconnected".

## Manual Test Checklist

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Enter hostname, click Connect | Status → "Connecting…", then credential prompt appears |
| 2 | Authenticate successfully | Remote desktop renders in window, status → "Connected to {host}" |
| 3 | Click Disconnect | Session ends, status → "Disconnected", Connect re-enabled |
| 4 | Empty hostname, click Connect | Button is disabled (cannot click) |
| 5 | Unreachable hostname, click Connect | Status shows error with reason after timeout |
| 6 | Close window while connected | Session disconnects cleanly, app exits |
| 7 | Cancel credential prompt | Status → "Disconnected" with reason, Connect re-enabled |

## Key Files

| File | Purpose |
|------|---------|
| `src/Remort/Connection/MainWindowViewModel.cs` | ViewModel — commands, state, status |
| `src/Remort/Connection/IRdpClient.cs` | Interface between ViewModel and COM layer |
| `src/Remort/Connection/ConnectionState.cs` | State enum |
| `src/Remort/Interop/RdpClientHost.cs` | AxHost subclass wrapping MsRdpClient ActiveX |
| `src/Remort/MainWindow.xaml` | UI layout |
| `src/Remort/MainWindow.xaml.cs` | WindowsFormsHost wiring |
| `src/Remort.Tests/Connection/MainWindowViewModelTests.cs` | ViewModel unit tests |
