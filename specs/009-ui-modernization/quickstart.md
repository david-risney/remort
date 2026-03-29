# Quickstart: Modern Windows UI

**Feature**: 009-ui-modernization  
**Date**: 2026-03-28

## Build & Run

```powershell
# Build the solution
dotnet build Remort.sln

# Run the app
dotnet run --project src/Remort/Remort.csproj

# Run all tests
dotnet test Remort.sln
```

## New Dependency

```xml
<!-- Added to src/Remort/Remort.csproj -->
<PackageReference Include="WPF-UI" Version="4.*" />
```

## Key New Files

### Data Model

| File | Purpose |
|------|---------|
| `src/Remort/Devices/Device.cs` | Device entity with nested settings records |
| `src/Remort/Devices/IDeviceStore.cs` | Device CRUD interface |
| `src/Remort/Devices/JsonDeviceStore.cs` | JSON file persistence for devices |

### Main Window (Devices/Favorites/Settings)

| File | Purpose |
|------|---------|
| `src/Remort/MainWindow.xaml` | FluentWindow + NavigationView shell |
| `src/Remort/Devices/DevicesPage.xaml` | Device card list with Add button |
| `src/Remort/Devices/DevicesPageViewModel.cs` | Devices page logic |
| `src/Remort/Devices/FavoritesPage.xaml` | Favorites-filtered card list |
| `src/Remort/Devices/AddDeviceDialog.xaml` | Add device ContentDialog |
| `src/Remort/Settings/SettingsPage.xaml` | Theme, DevBox config, About |

### Device Window (Connection/General/Display/Redirections)

| File | Purpose |
|------|---------|
| `src/Remort/DeviceWindow/DeviceWindow.xaml` | FluentWindow + NavigationView + titlebar toggle |
| `src/Remort/DeviceWindow/DeviceWindowViewModel.cs` | Window VM (owns connection service instance) |
| `src/Remort/DeviceWindow/ConnectionPage.xaml` | Connect/Disconnect + status + checkboxes |
| `src/Remort/DeviceWindow/GeneralPage.xaml` | Name, hostname, autohide, favorite |
| `src/Remort/DeviceWindow/DisplayPage.xaml` | Pin, fit, all-monitors checkboxes |
| `src/Remort/DeviceWindow/RedirectionsPage.xaml` | RDP redirection checkboxes |

### Tests

| File | Purpose |
|------|---------|
| `src/Remort.Tests/Devices/DeviceStoreTests.cs` | Device CRUD + persistence |
| `src/Remort.Tests/Devices/DevicesPageViewModelTests.cs` | Devices page logic |
| `src/Remort.Tests/DeviceWindow/ConnectionPageViewModelTests.cs` | Connection page logic |
| `src/Remort.Tests/DeviceWindow/DeviceWindowViewModelTests.cs` | Window VM toggle, titlebar |

## Architecture Pattern

```
MainWindow (FluentWindow)
    └── NavigationView
         ├── DevicesPage → DevicesPageViewModel → IDeviceStore
         ├── FavoritesPage → FavoritesPageViewModel → IDeviceStore
         └── SettingsPage → SettingsPageViewModel → ISettingsStore

DeviceWindow (FluentWindow) — one per device
    ├── TitleBar [device name] [toggle ▼] [task view ⊞]
    ├── NavigationView (visible when toggle = nav mode)
    │    ├── ConnectionPage → ConnectionPageViewModel → IConnectionService
    │    ├── GeneralPage → GeneralPageViewModel → IDeviceStore
    │    ├── DisplayPage → DisplayPageViewModel → IDeviceStore
    │    └── RedirectionsPage → RedirectionsPageViewModel → IDeviceStore
    └── WindowsFormsHost > RdpClientHost (visible when toggle = RDP mode)
```

## Data Flow

```
User clicks device card
    → DevicesPageViewModel.OpenDeviceCommand
    → IDeviceWindowManager.OpenOrFocus(device)
    → new DeviceWindow(device) + new DeviceWindowViewModel(device, ...)
    → ConnectionPage shown by default

User edits settings in device window
    → PageViewModel updates Device record
    → IDeviceStore.Update(device) + Save()
    → DeviceUpdated event → main window card refreshes
```

## Settings & Persistence

```
%APPDATA%/Remort/
├── settings.json         # AppSettings (theme, global prefs)
├── devices.json          # List<Device> (all devices + settings)
├── color-profiles.json   # Removed (replaced by Wpf.Ui themes)
└── screenshots/
    └── {guid}.png        # Last-known session screenshots
```
