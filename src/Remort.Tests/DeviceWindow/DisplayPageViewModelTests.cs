using FluentAssertions;
using NSubstitute;
using Remort.Devices;
using Remort.DeviceWindow;

namespace Remort.Tests.DeviceWindow;

/// <summary>
/// Tests for <see cref="DisplayPageViewModel"/>.
/// </summary>
public class DisplayPageViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly Device _device;

    public DisplayPageViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _device = new Device
        {
            Name = "Test",
            Hostname = "test.local",
            DisplaySettings = new DeviceDisplaySettings { FitSessionToWindow = true },
        };
        _deviceStore.GetById(_device.Id).Returns(_device);
    }

    [Fact]
    public void InitializesFromDeviceSettings()
    {
        var sut = new DisplayPageViewModel(_device, _deviceStore);

        sut.PinToVirtualDesktop.Should().BeFalse();
        sut.FitSessionToWindow.Should().BeTrue();
        sut.UseAllMonitors.Should().BeFalse();
    }

    [Fact]
    public void PinToVirtualDesktop_WhenChanged_Persists()
    {
        var sut = new DisplayPageViewModel(_device, _deviceStore);

        sut.PinToVirtualDesktop = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.DisplaySettings.PinToVirtualDesktop));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void FitSessionToWindow_WhenChanged_Persists()
    {
        var sut = new DisplayPageViewModel(_device, _deviceStore);

        sut.FitSessionToWindow = false;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => !d.DisplaySettings.FitSessionToWindow));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void UseAllMonitors_WhenChanged_Persists()
    {
        var sut = new DisplayPageViewModel(_device, _deviceStore);

        sut.UseAllMonitors = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.DisplaySettings.UseAllMonitors));
        _deviceStore.Received(1).Save();
    }
}
