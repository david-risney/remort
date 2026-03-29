using FluentAssertions;
using NSubstitute;
using Remort.Devices;
using Remort.DeviceWindow;

namespace Remort.Tests.DeviceWindow;

/// <summary>
/// Tests for <see cref="RedirectionsPageViewModel"/>.
/// </summary>
public class RedirectionsPageViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly Device _device;

    public RedirectionsPageViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetById(_device.Id).Returns(_device);
    }

    [Fact]
    public void InitializesFromDeviceSettings()
    {
        var sut = new RedirectionsPageViewModel(_device, _deviceStore);

        sut.Clipboard.Should().BeTrue();
        sut.AudioPlayback.Should().BeTrue();
        sut.Printers.Should().BeFalse();
        sut.Drives.Should().BeFalse();
    }

    [Fact]
    public void Clipboard_WhenChanged_Persists()
    {
        var sut = new RedirectionsPageViewModel(_device, _deviceStore);

        sut.Clipboard = false;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => !d.RedirectionSettings.Clipboard));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void Drives_WhenChanged_Persists()
    {
        var sut = new RedirectionsPageViewModel(_device, _deviceStore);

        sut.Drives = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.RedirectionSettings.Drives));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void UsbDevices_WhenChanged_Persists()
    {
        var sut = new RedirectionsPageViewModel(_device, _deviceStore);

        sut.UsbDevices = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.RedirectionSettings.UsbDevices));
        _deviceStore.Received(1).Save();
    }
}
