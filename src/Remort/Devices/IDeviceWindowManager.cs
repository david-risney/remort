namespace Remort.Devices;

/// <summary>
/// Manages the lifecycle of per-device windows.
/// Enforces single-window-per-device constraint.
/// </summary>
public interface IDeviceWindowManager
{
    /// <summary>Raised after a device window closes.</summary>
    public event EventHandler<DeviceIdEventArgs>? DeviceWindowClosed;

    /// <summary>Opens a new device window or focuses the existing one.</summary>
    /// <param name="device">The device to open a window for.</param>
    public void OpenOrFocus(Device device);

    /// <summary>Closes the device window for the specified device, if open.</summary>
    /// <param name="deviceId">The device identifier.</param>
    public void CloseForDevice(Guid deviceId);

    /// <summary>Closes all open device windows.</summary>
    public void CloseAll();

    /// <summary>Returns <see langword="true"/> if a window is currently open for the device.</summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see langword="true"/> if the device window is open; otherwise <see langword="false"/>.</returns>
    public bool IsOpen(Guid deviceId);
}
