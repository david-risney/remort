# Contract: IDeviceWindowManager

**Feature**: 009-ui-modernization  
**Date**: 2026-03-28

## Purpose

Manages the lifecycle of per-device windows. Enforces the single-window-per-device constraint (FR-012) and provides cross-window coordination.

## Interface

```
IDeviceWindowManager
├── OpenOrFocus(Device device) → void
├── CloseForDevice(Guid deviceId) → void
├── CloseAll() → void
├── IsOpen(Guid deviceId) → bool
└── event DeviceWindowClosed(Guid deviceId)
```

## Behaviors

| Method | Preconditions | Postconditions | Errors |
|--------|--------------|----------------|--------|
| `OpenOrFocus(device)` | None | If window exists for device: focus it. Otherwise: create and show new device window. | Never throws |
| `CloseForDevice(id)` | None | If window open: close it gracefully (disconnect if active). No-op if not open. | Never throws |
| `CloseAll()` | None | All device windows closed. | Never throws |
| `IsOpen(id)` | None | Returns true if a window is currently open for the device. | Never throws |

## Events

| Event | Payload | When |
|-------|---------|------|
| `DeviceWindowClosed` | `Guid` (device Id) | After a device window closes (user-initiated or programmatic) |

## Threading

- All methods must be called from the UI thread (WPF `Dispatcher` context)
- Window creation and focus operations are synchronous
