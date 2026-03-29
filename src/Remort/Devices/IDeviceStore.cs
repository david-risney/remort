namespace Remort.Devices;

/// <summary>
/// CRUD operations for the device collection.
/// </summary>
public interface IDeviceStore
{
    /// <summary>Raised after a device is added.</summary>
    public event EventHandler<DeviceEventArgs>? DeviceAdded;

    /// <summary>Raised after a device is updated.</summary>
    public event EventHandler<DeviceEventArgs>? DeviceUpdated;

    /// <summary>Raised after a device is removed.</summary>
    public event EventHandler<DeviceIdEventArgs>? DeviceRemoved;

    /// <summary>Returns a snapshot of all devices.</summary>
    /// <returns>A read-only list of all devices.</returns>
    public IReadOnlyList<Device> GetAll();

    /// <summary>Returns the device with the specified Id, or <see langword="null"/> if not found.</summary>
    /// <param name="id">The device identifier.</param>
    /// <returns>The matching device, or <see langword="null"/>.</returns>
    public Device? GetById(Guid id);

    /// <summary>Adds a device to the collection.</summary>
    /// <param name="device">The device to add.</param>
    /// <exception cref="ArgumentException">A device with the same Id already exists.</exception>
    public void Add(Device device);

    /// <summary>Replaces an existing device.</summary>
    /// <param name="device">The device with updated fields.</param>
    /// <exception cref="KeyNotFoundException">No device with the specified Id exists.</exception>
    public void Update(Device device);

    /// <summary>Removes the device with the specified Id. No-op if not found.</summary>
    /// <param name="id">The device identifier to remove.</param>
    public void Remove(Guid id);

    /// <summary>Persists all changes to the backing store.</summary>
    public void Save();
}
