# Architecture

## Domain Map

Remort is organized into four domains:

```
┌─────────────────────────────────────────────────────┐
│                     Remort App                       │
├──────────────┬──────────────┬───────────┬───────────┤
│  Connection  │   Virtual    │    UI /   │ Settings  │
│  Management  │   Desktop    │  Theming  │           │
├──────────────┴──────────────┴───────────┴───────────┤
│                  COM Interop Layer                   │
│           (MsRdpClient ActiveX via AxHost)           │
└─────────────────────────────────────────────────────┘
```

### Connection Management

Responsible for establishing, monitoring, and terminating RDP sessions. Owns retry logic, auto-reconnect policies, and connection state.

### Virtual Desktop

Integrates with Windows Virtual Desktop APIs to pin connections, detect virtual desktop changes, and switch desktops from the parent window.

### UI / Theming

WPF views, MVVM ViewModels, color profiles (preset and custom), and the application shell.

### Settings

Persistent user preferences: connection targets, reconnect policies, color profiles, window state.

## Layer Diagram

Within each domain, code flows through these layers:

```
  Views (.xaml / .xaml.cs)       ← UI only, minimal code-behind
       │
  ViewModels                     ← CommunityToolkit.Mvvm, commands, state
       │
  Services / Interfaces          ← Business logic, testable
       │
  COM Interop                    ← AxHost, MsRdpClient, event sinks
       │
  Win32 / Platform APIs          ← P/Invoke, Virtual Desktop COM
```

**Dependency rule**: Each layer may only depend on the layer directly below it. No upward dependencies. No cross-domain dependencies except through shared interfaces.

## Project Layout

```
src/
├── Remort/                      # Main app
│   ├── App.xaml(.cs)            # Application entry point
│   ├── MainWindow.xaml(.cs)     # Shell window
│   ├── Connection/              # Connection domain (future)
│   ├── VirtualDesktop/          # Virtual desktop domain (future)
│   ├── Theming/                 # UI/Theming domain (future)
│   ├── Settings/                # Settings domain (future)
│   └── Interop/                 # COM interop layer (future)
└── Remort.Tests/                # Unit + integration tests
    └── (mirrors src/Remort/ structure)
```

## Key Constraints

- **WPF, not WinUI** — WinUI adds XAML Islands complexity with no benefit for ActiveX hosting. See [ADR-001](decisions/001-wpf-over-winui.md).
- **Manual AxHost, no COMReference** — `<COMReference>` doesn't work with `dotnet build`. See [ADR-002](decisions/002-manual-axhost-com-interop.md).
- **UseWindowsForms = true** — Required for `WindowsFormsHost` / `AxHost` bridge.
- **AllowUnsafeBlocks = true** — Required for some COM interop scenarios.
