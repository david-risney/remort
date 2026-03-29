using CommunityToolkit.Mvvm.ComponentModel;
using Remort.Devices;

namespace Remort.DeviceWindow;

/// <summary>
/// ViewModel for the Redirections page in a device window.
/// </summary>
public partial class RedirectionsPageViewModel : ObservableObject
{
    private readonly IDeviceStore _deviceStore;
    private readonly Guid _deviceId;

    [ObservableProperty]
    private bool _clipboard;

    [ObservableProperty]
    private bool _printers;

    [ObservableProperty]
    private bool _drives;

    [ObservableProperty]
    private bool _audioPlayback;

    [ObservableProperty]
    private bool _audioRecording;

    [ObservableProperty]
    private bool _smartCards;

    [ObservableProperty]
    private bool _serialPorts;

    [ObservableProperty]
    private bool _usbDevices;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectionsPageViewModel"/> class.
    /// </summary>
    /// <param name="device">The device whose redirection settings to manage.</param>
    /// <param name="deviceStore">The device store for persisting changes.</param>
    public RedirectionsPageViewModel(Device device, IDeviceStore deviceStore)
    {
        ArgumentNullException.ThrowIfNull(device);

        _deviceStore = deviceStore;
        _deviceId = device.Id;
        _clipboard = device.RedirectionSettings.Clipboard;
        _printers = device.RedirectionSettings.Printers;
        _drives = device.RedirectionSettings.Drives;
        _audioPlayback = device.RedirectionSettings.AudioPlayback;
        _audioRecording = device.RedirectionSettings.AudioRecording;
        _smartCards = device.RedirectionSettings.SmartCards;
        _serialPorts = device.RedirectionSettings.SerialPorts;
        _usbDevices = device.RedirectionSettings.UsbDevices;
    }

    partial void OnClipboardChanged(bool value) => PersistRedirectionSettings(s => s with { Clipboard = value });

    partial void OnPrintersChanged(bool value) => PersistRedirectionSettings(s => s with { Printers = value });

    partial void OnDrivesChanged(bool value) => PersistRedirectionSettings(s => s with { Drives = value });

    partial void OnAudioPlaybackChanged(bool value) => PersistRedirectionSettings(s => s with { AudioPlayback = value });

    partial void OnAudioRecordingChanged(bool value) => PersistRedirectionSettings(s => s with { AudioRecording = value });

    partial void OnSmartCardsChanged(bool value) => PersistRedirectionSettings(s => s with { SmartCards = value });

    partial void OnSerialPortsChanged(bool value) => PersistRedirectionSettings(s => s with { SerialPorts = value });

    partial void OnUsbDevicesChanged(bool value) => PersistRedirectionSettings(s => s with { UsbDevices = value });

    private void PersistRedirectionSettings(Func<DeviceRedirectionSettings, DeviceRedirectionSettings> update)
    {
        Device? device = _deviceStore.GetById(_deviceId);
        if (device is null)
        {
            return;
        }

        DeviceRedirectionSettings updated = update(device.RedirectionSettings);
        _deviceStore.Update(device with { RedirectionSettings = updated });
        _deviceStore.Save();
    }
}
