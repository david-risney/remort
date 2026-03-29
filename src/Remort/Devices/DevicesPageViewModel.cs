using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Remort.Devices;

/// <summary>
/// ViewModel for the Devices page in the main window.
/// </summary>
public partial class DevicesPageViewModel : ObservableObject
{
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _deviceWindowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicesPageViewModel"/> class.
    /// </summary>
    /// <param name="deviceStore">The device store.</param>
    /// <param name="deviceWindowManager">The device window manager.</param>
    public DevicesPageViewModel(IDeviceStore deviceStore, IDeviceWindowManager deviceWindowManager)
    {
        _deviceStore = deviceStore;
        _deviceWindowManager = deviceWindowManager;

        foreach (Device device in _deviceStore.GetAll())
        {
            Devices.Add(new DeviceCardViewModel(device, _deviceStore, _deviceWindowManager));
        }

        _deviceStore.DeviceAdded += OnDeviceAdded;
        _deviceStore.DeviceUpdated += OnDeviceUpdated;
        _deviceStore.DeviceRemoved += OnDeviceRemoved;
    }

    /// <summary>Gets the collection of device card ViewModels.</summary>
    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];

    /// <summary>
    /// Gets or sets a delegate that shows the Add Device dialog and returns the confirmed ViewModel, or <see langword="null"/> if cancelled.
    /// Injected by the View to keep the ViewModel free of UI types.
    /// </summary>
    public Func<Task<AddDeviceDialogViewModel?>>? ShowAddDeviceDialog { get; set; }

    /// <summary>
    /// Shows the Add Device dialog and adds the device to the store if confirmed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        if (ShowAddDeviceDialog is null)
        {
            return;
        }

        AddDeviceDialogViewModel? result = await ShowAddDeviceDialog().ConfigureAwait(true);
        if (result is null)
        {
            return;
        }

        Device device = result.CreateDevice();
        _deviceStore.Add(device);
        _deviceStore.Save();
    }

    /// <summary>
    /// Opens a device window for the specified card.
    /// </summary>
    /// <param name="card">The card to open.</param>
    [RelayCommand]
    private void OpenDevice(DeviceCardViewModel? card)
    {
        if (card is not null)
        {
            _deviceWindowManager.OpenOrFocus(card.Device);
        }
    }

    private void OnDeviceAdded(object? sender, DeviceEventArgs e)
    {
        Devices.Add(new DeviceCardViewModel(e.Device, _deviceStore, _deviceWindowManager));
    }

    private void OnDeviceUpdated(object? sender, DeviceEventArgs e)
    {
        DeviceCardViewModel? card = FindCard(e.Device.Id);
        card?.Refresh(e.Device);
    }

    private void OnDeviceRemoved(object? sender, DeviceIdEventArgs e)
    {
        DeviceCardViewModel? card = FindCard(e.DeviceId);
        if (card is not null)
        {
            Devices.Remove(card);
        }
    }

    private DeviceCardViewModel? FindCard(Guid deviceId)
    {
        foreach (DeviceCardViewModel card in Devices)
        {
            if (card.Device.Id == deviceId)
            {
                return card;
            }
        }

        return null;
    }
}
