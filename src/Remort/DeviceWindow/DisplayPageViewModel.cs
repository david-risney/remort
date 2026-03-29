using CommunityToolkit.Mvvm.ComponentModel;
using Remort.Devices;

namespace Remort.DeviceWindow;

/// <summary>
/// ViewModel for the Display settings page in a device window.
/// </summary>
public partial class DisplayPageViewModel : ObservableObject
{
    private readonly IDeviceStore _deviceStore;
    private readonly Guid _deviceId;

    [ObservableProperty]
    private bool _pinToVirtualDesktop;

    [ObservableProperty]
    private bool _fitSessionToWindow;

    [ObservableProperty]
    private bool _useAllMonitors;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayPageViewModel"/> class.
    /// </summary>
    /// <param name="device">The device whose display settings to manage.</param>
    /// <param name="deviceStore">The device store for persisting changes.</param>
    public DisplayPageViewModel(Device device, IDeviceStore deviceStore)
    {
        ArgumentNullException.ThrowIfNull(device);

        _deviceStore = deviceStore;
        _deviceId = device.Id;
        _pinToVirtualDesktop = device.DisplaySettings.PinToVirtualDesktop;
        _fitSessionToWindow = device.DisplaySettings.FitSessionToWindow;
        _useAllMonitors = device.DisplaySettings.UseAllMonitors;
    }

    partial void OnPinToVirtualDesktopChanged(bool value)
    {
        PersistDisplaySettings(s => s with { PinToVirtualDesktop = value });
    }

    partial void OnFitSessionToWindowChanged(bool value)
    {
        PersistDisplaySettings(s => s with { FitSessionToWindow = value });
    }

    partial void OnUseAllMonitorsChanged(bool value)
    {
        PersistDisplaySettings(s => s with { UseAllMonitors = value });
    }

    private void PersistDisplaySettings(Func<DeviceDisplaySettings, DeviceDisplaySettings> update)
    {
        Device? device = _deviceStore.GetById(_deviceId);
        if (device is null)
        {
            return;
        }

        DeviceDisplaySettings updated = update(device.DisplaySettings);
        _deviceStore.Update(device with { DisplaySettings = updated });
        _deviceStore.Save();
    }
}
