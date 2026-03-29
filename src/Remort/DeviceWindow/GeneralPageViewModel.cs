using CommunityToolkit.Mvvm.ComponentModel;
using Remort.Devices;

namespace Remort.DeviceWindow;

/// <summary>
/// ViewModel for the General settings page in a device window.
/// </summary>
public partial class GeneralPageViewModel : ObservableObject
{
    private readonly IDeviceStore _deviceStore;
    private readonly Guid _deviceId;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _hostname;

    [ObservableProperty]
    private bool _autohideTitleBar;

    [ObservableProperty]
    private bool _isFavorite;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralPageViewModel"/> class.
    /// </summary>
    /// <param name="device">The device whose general settings to manage.</param>
    /// <param name="deviceStore">The device store for persisting changes.</param>
    public GeneralPageViewModel(Device device, IDeviceStore deviceStore)
    {
        ArgumentNullException.ThrowIfNull(device);

        _deviceStore = deviceStore;
        _deviceId = device.Id;
        _name = device.Name;
        _hostname = device.Hostname;
        _autohideTitleBar = device.GeneralSettings.AutohideTitleBar;
        _isFavorite = device.IsFavorite;
    }

    partial void OnNameChanged(string value)
    {
        PersistDevice(d => d with { Name = value.Trim() });
    }

    partial void OnHostnameChanged(string value)
    {
        PersistDevice(d => d with { Hostname = value.Trim() });
    }

    partial void OnAutohideTitleBarChanged(bool value)
    {
        PersistDevice(d => d with { GeneralSettings = d.GeneralSettings with { AutohideTitleBar = value } });
    }

    partial void OnIsFavoriteChanged(bool value)
    {
        PersistDevice(d => d with { IsFavorite = value });
    }

    private void PersistDevice(Func<Device, Device> update)
    {
        Device? device = _deviceStore.GetById(_deviceId);
        if (device is null)
        {
            return;
        }

        _deviceStore.Update(update(device));
        _deviceStore.Save();
    }
}
