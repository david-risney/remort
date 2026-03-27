# remort Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-23

## Active Technologies
- C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), MsRdpClient10 ActiveX (`mstscax.dll`), System.Text.Json (003-cap-connection-retries)
- JSON settings file (`%APPDATA%/Remort/settings.json`) (003-cap-connection-retries)
- C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), MsRdpClient10 ActiveX (`mstscax.dll`), System.Text.Json, `Microsoft.Win32.SystemEvents` (004-auto-reconnect-on-login)
- JSON settings file (`%APPDATA%/Remort/settings.json`) — existing from spec 003 (004-auto-reconnect-on-login)
- C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), `IVirtualDesktopManager` COM interface (Windows SDK, `shobjidl_core.h`) (005-pin-window-virtual-desktop)
- JSON settings file (`%APPDATA%/Remort/settings.json`) — existing `ISettingsStore` (005-pin-window-virtual-desktop)
- C# / .NET 8 (`net8.0-windows`) + CommunityToolkit.Mvvm, WPF, WindowsFormsHost, MsRdpClient ActiveX (006-reconnect-on-desktop-switch)
- JSON settings file via `ISettingsStore` / `JsonSettingsStore` (006-reconnect-on-desktop-switch)
- C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), `user32.dll` (`SendInput` P/Invoke), `Microsoft.Win32.Registry` (007-quick-desktop-switch)
- JSON settings file (`%APPDATA%/Remort/settings.json`) — no new settings fields required for this feature (007-quick-desktop-switch)

- C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), MsRdpClient10 ActiveX (`mstscax.dll`) (001-rdp-connect-disconnect)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET 8 (`net8.0-windows`)

## Code Style

C# / .NET 8 (`net8.0-windows`): Follow standard conventions

## Recent Changes
- 007-quick-desktop-switch: Added C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), `user32.dll` (`SendInput` P/Invoke), `Microsoft.Win32.Registry`
- 006-reconnect-on-desktop-switch: Added C# / .NET 8 (`net8.0-windows`) + CommunityToolkit.Mvvm, WPF, WindowsFormsHost, MsRdpClient ActiveX
- 005-pin-window-virtual-desktop: Added C# / .NET 8 (`net8.0-windows`) + WPF, WindowsFormsHost, CommunityToolkit.Mvvm (source generators), `IVirtualDesktopManager` COM interface (Windows SDK, `shobjidl_core.h`)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
