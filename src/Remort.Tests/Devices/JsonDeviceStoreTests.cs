using System.IO;
using FluentAssertions;
using Remort.Devices;

namespace Remort.Tests.Devices;

public sealed class JsonDeviceStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JsonDeviceStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "remort-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "devices.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetAll_WhenEmpty_ReturnsEmptyList()
    {
        var store = new JsonDeviceStore(_filePath);

        store.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Add_ThenGetAll_ReturnsDevice()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };

        store.Add(device);

        store.GetAll().Should().ContainSingle().Which.Name.Should().Be("Test");
    }

    [Fact]
    public void GetById_WhenExists_ReturnsDevice()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        Device? result = store.GetById(device.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
    }

    [Fact]
    public void GetById_WhenNotFound_ReturnsNull()
    {
        var store = new JsonDeviceStore(_filePath);

        store.GetById(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void Add_DuplicateId_ThrowsArgumentException()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        Action act = () => store.Add(device);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ReplacesDevice()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        Device updated = device with { Name = "Updated" };
        store.Update(updated);

        store.GetById(device.Id)!.Name.Should().Be("Updated");
    }

    [Fact]
    public void Update_NotFound_ThrowsKeyNotFoundException()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Ghost", Hostname = "ghost.local" };

        Action act = () => store.Update(device);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Remove_DeletesDevice()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        store.Remove(device.Id);

        store.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Remove_NotFound_DoesNotThrow()
    {
        var store = new JsonDeviceStore(_filePath);

        Action act = () => store.Remove(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device
        {
            Name = "RoundTrip",
            Hostname = "rt.local",
            IsFavorite = true,
            ConnectionSettings = new DeviceConnectionSettings { MaxRetryCount = 5 },
        };
        store.Add(device);
        store.Save();

        var reloaded = new JsonDeviceStore(_filePath);

        reloaded.GetAll().Should().ContainSingle();
        Device loaded = reloaded.GetAll()[0];
        loaded.Name.Should().Be("RoundTrip");
        loaded.IsFavorite.Should().BeTrue();
        loaded.ConnectionSettings.MaxRetryCount.Should().Be(5);
    }

    [Fact]
    public void Add_RaisesDeviceAddedEvent()
    {
        var store = new JsonDeviceStore(_filePath);
        Device? raised = null;
        store.DeviceAdded += (_, e) => raised = e.Device;

        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        raised.Should().NotBeNull();
        raised!.Id.Should().Be(device.Id);
    }

    [Fact]
    public void Update_RaisesDeviceUpdatedEvent()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        Device? raised = null;
        store.DeviceUpdated += (_, e) => raised = e.Device;

        store.Update(device with { Name = "Updated" });

        raised.Should().NotBeNull();
        raised!.Name.Should().Be("Updated");
    }

    [Fact]
    public void Remove_RaisesDeviceRemovedEvent()
    {
        var store = new JsonDeviceStore(_filePath);
        var device = new Device { Name = "Test", Hostname = "test.local" };
        store.Add(device);

        Guid? raised = null;
        store.DeviceRemoved += (_, e) => raised = e.DeviceId;

        store.Remove(device.Id);

        raised.Should().Be(device.Id);
    }
}
