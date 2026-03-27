# Quickstart: Reconnect on Virtual Desktop Switch

**Feature**: 006-reconnect-on-desktop-switch

## Prerequisites

- Windows 10 (version 1803+) or Windows 11
- .NET 8 SDK (`dotnet --version` ≥ 8.0)
- At least 2 virtual desktops configured (Ctrl+Win+D to create, or Task View)
- A reachable RDP host for end-to-end testing

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

All tests must pass. Desktop switch detector tests use mocked `IVirtualDesktopService` and `IDesktopSwitchDetector` — no actual virtual desktop switching required.

## Run the Application

```powershell
dotnet run --project src/Remort/Remort.csproj
```

## Manual Test Checklist

| # | Scenario | Steps | Expected |
|---|----------|-------|----------|
| 1 | Auto-reconnect on desktop switch | Enable "Pin to desktop" → Connect to host → Switch to Desktop 2 → Wait for disconnect → Switch back to Desktop 1 | Remort shows "Auto-reconnecting to host…" and reconnects |
| 2 | No reconnect when session active | Enable both toggles → Connect to host → Switch to Desktop 2 → Switch back quickly (before disconnect) | No reconnect attempt; existing session continues |
| 3 | No reconnect when disabled | Disable "Reconnect on desktop switch" → Disconnect → Switch desktops back | No reconnect attempt |
| 4 | No reconnect when pin disabled | Enable reconnect toggle but disable pin toggle → Switch desktops | No reconnect attempt; toggle is grayed out |
| 5 | No reconnect with no host | Enable both toggles (fresh install, never connected) → Switch desktops | No reconnect attempt; idle state shown |
| 6 | Toggle enabled persists | Enable both toggles → Close Remort → Reopen Remort | Both toggles still enabled |
| 7 | Toggle disabled by default | Fresh settings → Open Remort | Reconnect-on-switch toggle is off |
| 8 | Toggle disabled when pin disabled | Disable pin-to-desktop | Reconnect-on-switch checkbox is grayed out with tooltip |
| 9 | Cancel auto-reconnect | Trigger auto-reconnect via desktop switch → Click Disconnect | Reconnect cancelled; returns to Disconnected state |
| 10 | Reconnect failure | Enable both toggles → Connect to unreachable host → Switch away → Switch back | Status shows auto-reconnect failed with reason |
| 11 | Rapid desktop switching | Enable both toggles → Disconnect → Switch through 3+ desktops rapidly, landing on pinned desktop | Only one reconnect attempt fires |
| 12 | Coexistence with login reconnect | Enable both features → Lock workstation → Unlock on pinned desktop | Only one reconnect attempt fires (first event wins) |
| 13 | Immediate effect | Toggle reconnect-on-switch on while disconnected on pinned desktop (no desktop switch) | No reconnect (switch didn't happen); next desktop switch triggers normally |
