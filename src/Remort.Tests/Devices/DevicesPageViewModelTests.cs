using FluentAssertions;
using NSubstitute;
using Remort.Devices;

namespace Remort.Tests.Devices;

/// <summary>
/// Tests for <see cref="DevicesPageViewModel"/>.
/// </summary>
public class DevicesPageViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _windowManager;

    public DevicesPageViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _deviceStore.GetAll().Returns([]);
        _windowManager = Substitute.For<IDeviceWindowManager>();
    }

    [Fact]
    public void Constructor_LoadsDevicesFromStore()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetAll().Returns(new[] { device });

        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().ContainSingle();
        sut.Devices[0].Name.Should().Be("Test");
    }

    [Fact]
    public void Constructor_WhenStoreEmpty_DevicesIsEmpty()
    {
        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().BeEmpty();
    }

    [Fact]
    public void OpenDeviceCommand_CallsWindowManager()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetAll().Returns(new[] { device });
        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);

        sut.OpenDeviceCommand.Execute(sut.Devices[0]);

        _windowManager.Received(1).OpenOrFocus(device);
    }

    [Fact]
    public void DeviceAdded_Event_AddsCardToCollection()
    {
        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);
        var device = new Device { Name = "New", Hostname = "new.local" };

        _deviceStore.DeviceAdded += Raise.EventWith(new DeviceEventArgs(device));

        sut.Devices.Should().ContainSingle();
        sut.Devices[0].Name.Should().Be("New");
    }

    [Fact]
    public void DeviceRemoved_Event_RemovesCardFromCollection()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetAll().Returns(new[] { device });
        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);

        _deviceStore.DeviceRemoved += Raise.EventWith(new DeviceIdEventArgs(device.Id));

        sut.Devices.Should().BeEmpty();
    }

    [Fact]
    public void DeviceUpdated_Event_RefreshesCard()
    {
        var device = new Device { Name = "Old", Hostname = "test.local" };
        _deviceStore.GetAll().Returns(new[] { device });
        var sut = new DevicesPageViewModel(_deviceStore, _windowManager);

        Device updated = device with { Name = "New" };
        _deviceStore.DeviceUpdated += Raise.EventWith(new DeviceEventArgs(updated));

        sut.Devices[0].Name.Should().Be("New");
    }
}
