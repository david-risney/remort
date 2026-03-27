# Quickstart: Quick Virtual Desktop Switching

**Feature**: 007-quick-desktop-switch
**Date**: 2026-03-23

## Prerequisites

- Windows 10 (1803+) or Windows 11
- .NET 8 SDK
- Two or more virtual desktops created (use `Win+Ctrl+D` or Task View to create)

## Build

```powershell
cd C:\Users\davris\source\repos\remort
dotnet build Remort.sln
```

## Run

```powershell
dotnet run --project src/Remort/Remort.csproj
```

## Test

```powershell
dotnet test Remort.sln
```

## Verify the Feature

1. **Create multiple virtual desktops**: Press `Win+Tab` to open Task View, click "+ New desktop" at the top. Create at least 2 desktops.

2. **Launch Remort** on the first virtual desktop.

3. **Verify the desktop switcher ComboBox** appears in the connection bar (after the "Reconnect on desktop switch" checkbox). It should list all virtual desktops by name.

4. **Verify active desktop highlight**: The ComboBox should show the current desktop as its selected item.

5. **Switch via ComboBox**: Select a different desktop from the dropdown. Windows should switch to that desktop. If Remort is not pinned (spec 005), it will be on the original desktop.

6. **Switch back**: Use Task View or `Win+Ctrl+Left` to return to Remort's desktop. The ComboBox should now show the updated current desktop.

7. **Keyboard shortcuts**: With Remort focused, press `Ctrl+Alt+Right` to switch to the next desktop. Press `Ctrl+Alt+Left` to switch to the previous desktop.

8. **Add/remove desktops**: While Remort is running, create a new desktop via Task View. Within 1 second, the ComboBox should include the new desktop. Remove the desktop — it should disappear from the list.

9. **Custom desktop names** (Windows 11): In Task View, right-click a desktop and rename it. The ComboBox should reflect the custom name within 1 second.

10. **API unavailable**: If running on a system without virtual desktop support, the ComboBox should not appear (hidden, not broken).

## Key Files

| File | Purpose |
|------|---------|
| `src/Remort/VirtualDesktop/IDesktopSwitcherService.cs` | Service interface for enumeration + switching |
| `src/Remort/VirtualDesktop/DesktopSwitcherService.cs` | Implementation: registry reads + SendInput |
| `src/Remort/VirtualDesktop/VirtualDesktopInfo.cs` | Record: desktop Id, Name, Index |
| `src/Remort/Interop/NativeMethods.cs` | SendInput P/Invoke declarations |
| `src/Remort/Connection/MainWindowViewModel.cs` | ViewModel: desktop list, switch commands |
| `src/Remort/MainWindow.xaml` | XAML: ComboBox + InputBindings |
| `src/Remort/MainWindow.xaml.cs` | Code-behind: wire service, start/stop monitoring |
| `src/Remort.Tests/VirtualDesktop/DesktopSwitcherServiceTests.cs` | Service unit tests |
| `src/Remort.Tests/VirtualDesktop/DesktopSwitchViewModelTests.cs` | ViewModel switching tests |
