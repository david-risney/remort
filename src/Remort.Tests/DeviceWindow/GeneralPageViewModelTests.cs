using FluentAssertions;
using NSubstitute;
using Remort.Devices;
using Remort.DeviceWindow;

namespace Remort.Tests.DeviceWindow;

/// <summary>
/// Tests for <see cref="GeneralPageViewModel" />.
/// </summary>
public class GeneralPageViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly Device _device;

    public GeneralPageViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _device = new Device { Name = "Test", Hostname = "test.local" };
        _deviceStore.GetById(_device.Id).Returns(_device);
    }

    [Fact]
    public void Initializes_FromDevice()
    {
        var sut = new GeneralPageViewModel(_device, _deviceStore);

        sut.Name.Should().Be("Test");
        sut.Hostname.Should().Be("test.local");
        sut.AutohideTitleBar.Should().BeFalse();
        sut.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public void Name_WhenChanged_Persists()
    {
        var sut = new GeneralPageViewModel(_device, _deviceStore);

        sut.Name = "NewName";

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.Name == "NewName"));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void Hostname_WhenChanged_Persists()
    {
        var sut = new GeneralPageViewModel(_device, _deviceStore);

        sut.Hostname = "new.host";

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.Hostname == "new.host"));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void AutohideTitleBar_WhenChanged_Persists()
    {
        var sut = new GeneralPageViewModel(_device, _deviceStore);

        sut.AutohideTitleBar = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.GeneralSettings.AutohideTitleBar));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void IsFavorite_WhenChanged_Persists()
    {
        var sut = new GeneralPageViewModel(_device, _deviceStore);

        sut.IsFavorite = true;

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.IsFavorite));
        _deviceStore.Received(1).Save();
    }
}
