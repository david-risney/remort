using FluentAssertions;
using NSubstitute;
using Remort.Devices;

namespace Remort.Tests.Devices;

/// <summary>
/// Tests for <see cref="DeviceCardViewModel"/>.
/// </summary>
public class DeviceCardViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _windowManager;

    public DeviceCardViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _windowManager = Substitute.For<IDeviceWindowManager>();
    }

    [Fact]
    public void Name_ReflectsDeviceName()
    {
        var device = new Device { Name = "MyDevice", Hostname = "host.local" };

        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        sut.Name.Should().Be("MyDevice");
    }

    [Fact]
    public void IsFavorite_ReflectsDeviceFavoriteStatus()
    {
        var device = new Device { Name = "Fav", Hostname = "host.local", IsFavorite = true };

        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        sut.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void BackgroundBrush_WithNoScreenshot_ReturnsGradient()
    {
        var device = new Device { Name = "NoScreenshot", Hostname = "host.local" };

        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        sut.BackgroundBrush.Should().NotBeNull();
    }

    [Fact]
    public void BackgroundBrush_SameDevice_ReturnsSameGradient()
    {
        var device = new Device { Name = "Stable", Hostname = "host.local" };

        var card1 = new DeviceCardViewModel(device, _deviceStore, _windowManager);
        var card2 = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        card1.BackgroundBrush.Should().BeSameAs(card2.BackgroundBrush);
    }

    [Fact]
    public void ToggleFavoriteCommand_UpdatesStoreAndSaves()
    {
        var device = new Device { Name = "Test", Hostname = "host.local", IsFavorite = false };
        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        sut.ToggleFavoriteCommand.Execute(null);

        _deviceStore.Received(1).Update(Arg.Is<Device>(d => d.IsFavorite == true));
        _deviceStore.Received(1).Save();
    }

    [Fact]
    public void OpenCommand_CallsWindowManager()
    {
        var device = new Device { Name = "Test", Hostname = "host.local" };
        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        sut.OpenCommand.Execute(null);

        _windowManager.Received(1).OpenOrFocus(device);
    }

    [Fact]
    public void Refresh_UpdatesNameAndFavorite()
    {
        var device = new Device { Name = "Old", Hostname = "host.local" };
        var sut = new DeviceCardViewModel(device, _deviceStore, _windowManager);

        Device updated = device with { Name = "New", IsFavorite = true };
        sut.Refresh(updated);

        sut.Name.Should().Be("New");
        sut.IsFavorite.Should().BeTrue();
    }
}
