namespace Remort.Devices;

/// <summary>
/// Event arguments carrying a device Id.
/// </summary>
public sealed class DeviceIdEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceIdEventArgs"/> class.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    public DeviceIdEventArgs(Guid deviceId)
    {
        DeviceId = deviceId;
    }

    /// <summary>Gets the device identifier.</summary>
    public Guid DeviceId { get; }
}
