# Quickstart: Pin Window to Virtual Desktop

**Feature**: 005-pin-window-virtual-desktop

## Prerequisites

- Windows 10 (version 1803+) or Windows 11
- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- At least 2 virtual desktops configured (Ctrl+Win+D to create, or Task View)

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

All tests must pass. Virtual desktop service tests use a mocked `IVirtualDesktopService` — no actual virtual desktop switching required.

## Run the Application

```powershell
dotnet run --project src/Remort/Remort.csproj
```

## Manual Test Checklist

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| 1 | Pin enabled, switch desktops | Enable "Pin to desktop" toggle → switch to Desktop 2 (Ctrl+Win+→) | Remort window NOT visible on Desktop 2 |
| 2 | Pin enabled, switch back | From Desktop 2, switch back to Desktop 1 (Ctrl+Win+←) | Remort window visible exactly where it was |
| 3 | Pin disabled (default) | Ensure toggle is off → switch to Desktop 2 | Remort follows default OS behavior |
| 4 | Toggle on then off | Enable toggle → disable toggle → switch desktops | Window follows default OS behavior |
| 5 | Immediate effect | Enable toggle while on Desktop 1 | No restart or reconnect needed; indicator appears immediately |
| 6 | Persistence | Enable toggle → close Remort → reopen Remort | Toggle is still enabled; window is pinned |
| 7 | Visual indicator on | Enable toggle | Pin icon visible in status bar |
| 8 | Visual indicator off | Disable toggle | Pin icon not visible |
| 9 | Single desktop | Only one virtual desktop exists → enable toggle | No error; toggle works, no visible effect |
| 10 | Independent of connection | Enable toggle while disconnected → switch desktops | Window stays pinned regardless of connection state |
