# Contract: IDeviceStore

**Feature**: 009-ui-modernization  
**Date**: 2026-03-28

## Purpose

CRUD operations for the device collection. Abstracts persistence so ViewModels and services don't depend on JSON/file details.

## Interface

```
IDeviceStore
├── GetAll() → IReadOnlyList<Device>
├── GetById(Guid id) → Device?
├── Add(Device device) → void
├── Update(Device device) → void
├── Remove(Guid id) → void
├── Save() → void
├── event DeviceAdded(Device)
├── event DeviceUpdated(Device)
└── event DeviceRemoved(Guid)
```

## Behaviors

| Method | Preconditions | Postconditions | Errors |
|--------|--------------|----------------|--------|
| `GetAll()` | None | Returns snapshot of all devices | Never throws |
| `GetById(id)` | None | Returns device or null | Never throws |
| `Add(device)` | `device.Id` not already present | Device added to collection; `DeviceAdded` raised | `ArgumentException` if duplicate Id |
| `Update(device)` | Device with `device.Id` exists | Device replaced; `DeviceUpdated` raised | `KeyNotFoundException` if not found |
| `Remove(id)` | Device with `id` exists | Device removed; `DeviceRemoved` raised | No-op if not found |
| `Save()` | None | All changes persisted to disk | `IOException` on file system failure |

## Events

Events enable UI synchronization across windows (e.g., card list updates when device is modified in the device window).

| Event | Payload | When |
|-------|---------|------|
| `DeviceAdded` | `Device` | After `Add()` |
| `DeviceUpdated` | `Device` | After `Update()` |
| `DeviceRemoved` | `Guid` (device Id) | After `Remove()` |

## Threading

- All public methods are called from the UI thread
- Events are raised synchronously on the calling thread
- `Save()` writes to disk synchronously (acceptable for <50 devices)
