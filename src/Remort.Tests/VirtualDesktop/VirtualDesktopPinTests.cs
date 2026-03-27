using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Unit tests for virtual desktop pinning behavior in <see cref="MainWindowViewModel"/>.
/// </summary>
public class VirtualDesktopPinTests
{
    private readonly IConnectionService _connectionService;
    private readonly ISettingsStore _settingsStore;
    private readonly IVirtualDesktopService _virtualDesktopService;

    public VirtualDesktopPinTests()
    {
        _connectionService = Substitute.For<IConnectionService>();
        _settingsStore = Substitute.For<ISettingsStore>();
        _settingsStore.Load().Returns(new AppSettings());
        _virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        _virtualDesktopService.IsSupported.Returns(true);
    }

    [Fact]
    public void PinToDesktopEnabled_DefaultsToFalse()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.PinToDesktopEnabled.Should().BeFalse();
    }

    [Fact]
    public void PinToDesktopEnabled_LoadedFromSettings()
    {
        _settingsStore.Load().Returns(new AppSettings { PinToDesktopEnabled = true });

        MainWindowViewModel sut = CreateViewModel();

        sut.PinToDesktopEnabled.Should().BeTrue();
    }

    [Fact]
    public void PinToDesktopEnabled_WhenSetTrue_CallsPinToCurrentDesktop()
    {
        MainWindowViewModel sut = CreateViewModel();
        sut.SetWindowHandle(new IntPtr(12345));

        sut.PinToDesktopEnabled = true;

        _virtualDesktopService.Received(1).PinToCurrentDesktop(new IntPtr(12345));
    }

    [Fact]
    public void PinToDesktopEnabled_WhenSetFalse_CallsUnpin()
    {
        _settingsStore.Load().Returns(new AppSettings { PinToDesktopEnabled = true });
        MainWindowViewModel sut = CreateViewModel();
        sut.SetWindowHandle(new IntPtr(12345));

        sut.PinToDesktopEnabled = false;

        _virtualDesktopService.Received(1).Unpin(new IntPtr(12345));
    }

    [Fact]
    public void PinToDesktopEnabled_WhenChanged_PersistsSetting()
    {
        MainWindowViewModel sut = CreateViewModel();
        sut.SetWindowHandle(new IntPtr(12345));

        sut.PinToDesktopEnabled = true;

        _settingsStore.Received().Save(Arg.Is<AppSettings>(s => s.PinToDesktopEnabled));
    }

    [Fact]
    public void SetWindowHandle_WhenPinEnabled_AppliesInitialPin()
    {
        _settingsStore.Load().Returns(new AppSettings { PinToDesktopEnabled = true });
        MainWindowViewModel sut = CreateViewModel();

        sut.SetWindowHandle(new IntPtr(99999));

        _virtualDesktopService.Received(1).PinToCurrentDesktop(new IntPtr(99999));
    }

    [Fact]
    public void SetWindowHandle_WhenPinDisabled_DoesNotPin()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.SetWindowHandle(new IntPtr(99999));

        _virtualDesktopService.DidNotReceive().PinToCurrentDesktop(Arg.Any<IntPtr>());
    }

    [Fact]
    public void PinToDesktopEnabled_WithoutHwnd_DoesNotCallService()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.PinToDesktopEnabled = true;

        _virtualDesktopService.DidNotReceive().PinToCurrentDesktop(Arg.Any<IntPtr>());
    }

    [Fact]
    public void PinToDesktopEnabled_WithoutService_DoesNotThrow()
    {
        var sut = new MainWindowViewModel(_connectionService, settingsStore: _settingsStore);
        sut.SetWindowHandle(new IntPtr(12345));

        Action act = () => sut.PinToDesktopEnabled = true;

        act.Should().NotThrow();
    }

    [Fact]
    public void PinToDesktopEnabled_WhenSetTrue_PersistsTrue()
    {
        MainWindowViewModel sut = CreateViewModel();

        sut.PinToDesktopEnabled = true;

        _settingsStore.Received().Save(Arg.Is<AppSettings>(s => s.PinToDesktopEnabled == true));
    }

    [Fact]
    public void PinToDesktopEnabled_WhenSetFalse_PersistsFalse()
    {
        _settingsStore.Load().Returns(new AppSettings { PinToDesktopEnabled = true });
        MainWindowViewModel sut = CreateViewModel();

        sut.PinToDesktopEnabled = false;

        _settingsStore.Received().Save(Arg.Is<AppSettings>(s => s.PinToDesktopEnabled == false));
    }

    private MainWindowViewModel CreateViewModel() =>
        new(_connectionService, settingsStore: _settingsStore, virtualDesktopService: _virtualDesktopService);
}
