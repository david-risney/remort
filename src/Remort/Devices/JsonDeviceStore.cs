using System.IO;
using System.Text.Json;

namespace Remort.Devices;

/// <summary>
/// Persists <see cref="Device"/> collection as JSON to the user's app-data folder.
/// </summary>
public sealed class JsonDeviceStore : IDeviceStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly List<Device> _devices = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeviceStore"/> class
    /// using the default path (<c>%APPDATA%/Remort/devices.json</c>).
    /// </summary>
    public JsonDeviceStore()
        : this(GetDefaultFilePath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeviceStore"/> class
    /// with a custom file path (for testing).
    /// </summary>
    /// <param name="filePath">The full path to the devices JSON file.</param>
    public JsonDeviceStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    /// <inheritdoc/>
    public event EventHandler<DeviceEventArgs>? DeviceAdded;

    /// <inheritdoc/>
    public event EventHandler<DeviceEventArgs>? DeviceUpdated;

    /// <inheritdoc/>
    public event EventHandler<DeviceIdEventArgs>? DeviceRemoved;

    /// <inheritdoc/>
    public IReadOnlyList<Device> GetAll() => _devices.AsReadOnly();

    /// <inheritdoc/>
    public Device? GetById(Guid id) => _devices.Find(d => d.Id == id);

    /// <inheritdoc/>
    public void Add(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (_devices.Any(d => d.Id == device.Id))
        {
            throw new ArgumentException($"A device with Id '{device.Id}' already exists.", nameof(device));
        }

        _devices.Add(device);
        DeviceAdded?.Invoke(this, new DeviceEventArgs(device));
    }

    /// <inheritdoc/>
    public void Update(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);

        int index = _devices.FindIndex(d => d.Id == device.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"No device with Id '{device.Id}' found.");
        }

        _devices[index] = device;
        DeviceUpdated?.Invoke(this, new DeviceEventArgs(device));
    }

    /// <inheritdoc/>
    public void Remove(Guid id)
    {
        int index = _devices.FindIndex(d => d.Id == id);
        if (index < 0)
        {
            return;
        }

        _devices.RemoveAt(index);
        DeviceRemoved?.Invoke(this, new DeviceIdEventArgs(id));
    }

    /// <inheritdoc/>
    public void Save()
    {
        string directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        string json = JsonSerializer.Serialize(_devices, s_jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static string GetDefaultFilePath()
    {
        return System.IO.Path.Combine(Settings.AppDataDirectory.Path, "devices.json");
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            List<Device>? loaded = JsonSerializer.Deserialize<List<Device>>(json, s_jsonOptions);
            if (loaded is not null)
            {
                _devices.AddRange(loaded);
            }
        }
#pragma warning disable CA1031 // Fall back to empty list on malformed JSON
        catch (JsonException)
#pragma warning restore CA1031
        {
            // Ignore — start with empty list.
        }
    }
}
