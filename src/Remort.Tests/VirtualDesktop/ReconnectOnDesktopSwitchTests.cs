using FluentAssertions;
using NSubstitute;
using Remort.Connection;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Reconnect-on-desktop-switch tests.
/// Behavior moved to per-device DeviceWindowViewModel in 009-ui-modernization (T082).
/// Keeping basic ViewModel behavior tests only.
/// </summary>
public class ReconnectOnDesktopSwitchTests
{
    [Fact]
    public void TryReconnectOnDesktopSwitch_WhenDisconnected_DoesNotThrow()
    {
        IConnectionService connectionService = Substitute.For<IConnectionService>();
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.Load().Returns(new AppSettings());
        IVirtualDesktopService virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        virtualDesktopService.IsSupported.Returns(true);

        var sut = new MainWindowViewModel(connectionService, settingsStore: settingsStore, virtualDesktopService: virtualDesktopService);

        Action act = () => sut.TryReconnectOnDesktopSwitch();

        act.Should().NotThrow();
    }

    [Fact]
    public void ReconnectOnDesktopSwitchEnabled_DefaultsToFalse()
    {
        IConnectionService connectionService = Substitute.For<IConnectionService>();
        var sut = new MainWindowViewModel(connectionService);

        sut.ReconnectOnDesktopSwitchEnabled.Should().BeFalse();
    }
}
