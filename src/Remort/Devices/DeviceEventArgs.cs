namespace Remort.Devices;

/// <summary>
/// Event arguments carrying a <see cref="Device"/>.
/// </summary>
public sealed class DeviceEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceEventArgs"/> class.
    /// </summary>
    /// <param name="device">The device associated with the event.</param>
    public DeviceEventArgs(Device device)
    {
        Device = device;
    }

    /// <summary>Gets the device associated with the event.</summary>
    public Device Device { get; }
}
