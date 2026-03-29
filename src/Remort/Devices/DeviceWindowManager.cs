using System.Windows;

namespace Remort.Devices;

/// <summary>
/// Tracks open device windows and enforces single-window-per-device constraint.
/// </summary>
public sealed class DeviceWindowManager : IDeviceWindowManager
{
    private readonly Dictionary<Guid, Window> _openWindows = [];
    private readonly Func<Device, Window> _windowFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceWindowManager"/> class.
    /// </summary>
    /// <param name="windowFactory">Factory that creates a device window for the given device.</param>
    public DeviceWindowManager(Func<Device, Window> windowFactory)
    {
        _windowFactory = windowFactory;
    }

    /// <inheritdoc/>
    public event EventHandler<DeviceIdEventArgs>? DeviceWindowClosed;

    /// <inheritdoc/>
    public void OpenOrFocus(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (_openWindows.TryGetValue(device.Id, out Window? existing))
        {
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }

            existing.Activate();
            return;
        }

        Window window = _windowFactory(device);
        _openWindows[device.Id] = window;
        window.Closed += (_, _) => OnWindowClosed(device.Id);
        window.Show();
    }

    /// <inheritdoc/>
    public void CloseForDevice(Guid deviceId)
    {
        if (_openWindows.TryGetValue(deviceId, out Window? window))
        {
            window.Close();
        }
    }

    /// <inheritdoc/>
    public void CloseAll()
    {
        foreach (Window window in _openWindows.Values.ToList())
        {
            window.Close();
        }
    }

    /// <inheritdoc/>
    public bool IsOpen(Guid deviceId) => _openWindows.ContainsKey(deviceId);

    private void OnWindowClosed(Guid deviceId)
    {
        _openWindows.Remove(deviceId);
        DeviceWindowClosed?.Invoke(this, new DeviceIdEventArgs(deviceId));
    }
}
