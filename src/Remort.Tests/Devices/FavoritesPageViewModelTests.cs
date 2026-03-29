using FluentAssertions;
using NSubstitute;
using Remort.Devices;

namespace Remort.Tests.Devices;

/// <summary>
/// Tests for <see cref="FavoritesPageViewModel"/>.
/// </summary>
public class FavoritesPageViewModelTests
{
    private readonly IDeviceStore _deviceStore;
    private readonly IDeviceWindowManager _windowManager;

    public FavoritesPageViewModelTests()
    {
        _deviceStore = Substitute.For<IDeviceStore>();
        _deviceStore.GetAll().Returns([]);
        _windowManager = Substitute.For<IDeviceWindowManager>();
    }

    [Fact]
    public void Constructor_OnlyShowsFavorites()
    {
        var fav = new Device { Name = "Fav", Hostname = "fav.local", IsFavorite = true };
        var notFav = new Device { Name = "NotFav", Hostname = "notfav.local" };
        _deviceStore.GetAll().Returns(new[] { fav, notFav });

        var sut = new FavoritesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().ContainSingle();
        sut.Devices[0].Name.Should().Be("Fav");
    }

    [Fact]
    public void Constructor_WhenNoFavorites_ListIsEmpty()
    {
        var notFav = new Device { Name = "NotFav", Hostname = "notfav.local" };
        _deviceStore.GetAll().Returns(new[] { notFav });

        var sut = new FavoritesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().BeEmpty();
    }

    [Fact]
    public void DeviceUpdated_RefreshesFilteredList()
    {
        var device = new Device { Name = "Test", Hostname = "test.local", IsFavorite = false };
        _deviceStore.GetAll().Returns(new[] { device });
        var sut = new FavoritesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().BeEmpty();

        // Simulate device updated to be a favorite
        Device updated = device with { IsFavorite = true };
        _deviceStore.GetAll().Returns(new[] { updated });
        _deviceStore.DeviceUpdated += Raise.EventWith(new DeviceEventArgs(updated));

        sut.Devices.Should().ContainSingle();
    }

    [Fact]
    public void DeviceRemoved_RefreshesFilteredList()
    {
        var device = new Device { Name = "Fav", Hostname = "fav.local", IsFavorite = true };
        _deviceStore.GetAll().Returns(new[] { device });
        var sut = new FavoritesPageViewModel(_deviceStore, _windowManager);

        sut.Devices.Should().ContainSingle();

        _deviceStore.GetAll().Returns(Array.Empty<Device>());
        _deviceStore.DeviceRemoved += Raise.EventWith(new DeviceIdEventArgs(device.Id));

        sut.Devices.Should().BeEmpty();
    }
}
