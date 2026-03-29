using FluentAssertions;
using Remort.Devices;
using Remort.DeviceWindow;

namespace Remort.Tests.DeviceWindow;

/// <summary>
/// Tests for <see cref="DeviceWindowViewModel"/>.
/// </summary>
public class DeviceWindowViewModelTests
{
    [Fact]
    public void WindowTitle_ReflectsDeviceName()
    {
        var device = new Device { Name = "MyBox", Hostname = "mybox.local" };

        var sut = new DeviceWindowViewModel(device);

        sut.WindowTitle.Should().Be("MyBox");
    }

    [Fact]
    public void IsRdpViewActive_DefaultsToFalse()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };

        var sut = new DeviceWindowViewModel(device);

        sut.IsRdpViewActive.Should().BeFalse();
    }

    [Fact]
    public void ToggleViewCommand_FlipsIsRdpViewActive()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };
        var sut = new DeviceWindowViewModel(device);

        sut.ToggleViewCommand.Execute(null);

        sut.IsRdpViewActive.Should().BeTrue();
    }

    [Fact]
    public void ToggleViewCommand_CalledTwice_ReturnsFalse()
    {
        var device = new Device { Name = "Test", Hostname = "test.local" };
        var sut = new DeviceWindowViewModel(device);

        sut.ToggleViewCommand.Execute(null);
        sut.ToggleViewCommand.Execute(null);

        sut.IsRdpViewActive.Should().BeFalse();
    }

    [Fact]
    public void IsAutohideEnabled_ReflectsDeviceGeneralSettings()
    {
        var device = new Device
        {
            Name = "Test",
            Hostname = "test.local",
            GeneralSettings = new DeviceGeneralSettings { AutohideTitleBar = true },
        };

        var sut = new DeviceWindowViewModel(device);

        sut.IsAutohideEnabled.Should().BeTrue();
    }
}
