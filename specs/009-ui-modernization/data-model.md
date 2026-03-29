# Data Model: Modern Windows UI

**Feature**: 009-ui-modernization  
**Date**: 2026-03-28

## Entities

### Device

Represents a remote machine the user can connect to. Central entity — one per physical/virtual machine.

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Id | `Guid` | `Guid.NewGuid()` | Stable identifier; survives renames |
| Name | `string` | (required) | Display name on card and titlebar |
| Hostname | `string` | (required) | DNS name or IP; used by RDP client |
| IsFavorite | `bool` | `false` | Shown on Favorites page when true |
| IsDiscovered | `bool` | `false` | `true` for DevBox-resolved devices; `false` for manually added |
| LastScreenshotPath | `string?` | `null` | Relative path to PNG in screenshots dir |
| ConnectionSettings | `DeviceConnectionSettings` | (defaults) | Per-device connection preferences |
| GeneralSettings | `DeviceGeneralSettings` | (defaults) | Per-device general preferences |
| DisplaySettings | `DeviceDisplaySettings` | (defaults) | Per-device display preferences |
| RedirectionSettings | `DeviceRedirectionSettings` | (defaults) | Per-device redirection toggles |

**Validation rules**:
- `Name` must be non-empty (whitespace-trimmed)
- `Hostname` must be non-empty (whitespace-trimmed)
- `Id` is auto-generated and immutable after creation

### DeviceConnectionSettings

Per-device connection preferences (embedded in `Device`).

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| AutoconnectOnStart | `bool` | `false` | Connect when app launches |
| AutoconnectWhenVisible | `bool` | `false` | Connect when virtual desktop becomes active |
| StartOnStartup | `bool` | `false` | Register app to launch at Windows logon |
| MaxRetryCount | `int` | `3` | Max connection retry attempts |

### DeviceGeneralSettings

Per-device general preferences (embedded in `Device`).

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| AutohideTitleBar | `bool` | `false` | Hide titlebar when cursor moves away |

### DeviceDisplaySettings

Per-device display preferences (embedded in `Device`).

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| PinToVirtualDesktop | `bool` | `false` | Pin device window to current virtual desktop |
| FitSessionToWindow | `bool` | `true` | Scale RDP session to window size |
| UseAllMonitors | `bool` | `false` | Span all monitors when maximized |

### DeviceRedirectionSettings

Per-device RDP local resource redirection toggles (embedded in `Device`).

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Clipboard | `bool` | `true` | Redirect clipboard |
| Printers | `bool` | `false` | Redirect printers |
| Drives | `bool` | `false` | Redirect local drives |
| AudioPlayback | `bool` | `true` | Play remote audio locally |
| AudioRecording | `bool` | `false` | Redirect microphone |
| SmartCards | `bool` | `false` | Redirect smart cards |
| SerialPorts | `bool` | `false` | Redirect serial/COM ports |
| UsbDevices | `bool` | `false` | Redirect USB devices |

### AppSettings (existing — modified)

Global application settings. Replaces current flat structure with multi-device awareness.

| Field | Type | Default | Change |
|-------|------|---------|--------|
| Theme | `AppTheme` | `System` | **New** — replaces `ActiveProfileName` |
| DevBoxDiscoveryEnabled | `bool` | `true` | **New** — from Settings page |
| LastSelectedDeviceId | `Guid?` | `null` | **New** — restore selection on launch |

**Removed fields** (migrated to per-device `Device` entity):
- `MaxRetryCount` → `DeviceConnectionSettings.MaxRetryCount`
- `AutoReconnectEnabled` → `DeviceConnectionSettings.AutoconnectOnStart`
- `LastConnectedHost` → removed (implicit from device selection)
- `PinToDesktopEnabled` → `DeviceDisplaySettings.PinToVirtualDesktop`
- `ReconnectOnDesktopSwitchEnabled` → `DeviceConnectionSettings.AutoconnectWhenVisible`
- `ActiveProfileName` → `AppSettings.Theme` (enum)

### AppTheme (enum)

| Value | Description |
|-------|-------------|
| `Light` | Light theme |
| `Dark` | Dark theme |
| `System` | Follow Windows system theme |

## Relationships

```
AppSettings (1)
    └── Theme: AppTheme

DeviceStore (collection)
    └── Device (*)
         ├── ConnectionSettings: DeviceConnectionSettings (1:1 embedded)
         ├── GeneralSettings: DeviceGeneralSettings (1:1 embedded)
         ├── DisplaySettings: DeviceDisplaySettings (1:1 embedded)
         └── RedirectionSettings: DeviceRedirectionSettings (1:1 embedded)
```

## State Transitions

### Device Lifecycle

```
[Added] → [Configured] → [Connecting] → [Connected] → [Disconnected]
                                ↑              │
                                └──────────────┘ (reconnect)
                                
[Any State] → [Deleted]
```

### Connection State (existing — unchanged)

```
Disconnected → Resolving → Connecting → Connected
     ↑              │           │           │
     └──────────────┴───────────┴───────────┘
```

## Persistence

| Store | File | Content |
|-------|------|---------|
| `JsonSettingsStore` | `%APPDATA%/Remort/settings.json` | `AppSettings` (global prefs) |
| `JsonDeviceStore` | `%APPDATA%/Remort/devices.json` | `List<Device>` (all devices) |
| Screenshots | `%APPDATA%/Remort/screenshots/{id}.png` | Last-known session screenshots |

Both stores use `System.Text.Json` with camelCase naming. Missing files default to empty/default values. Schema changes must be backward-compatible (new fields with defaults).
