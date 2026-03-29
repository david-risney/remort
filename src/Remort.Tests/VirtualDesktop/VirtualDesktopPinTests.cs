using NSubstitute;
using Remort.Connection;
using Remort.Settings;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Virtual desktop pinning tests.
/// Pin-to-desktop behavior moved to per-device DisplayPageViewModel in 009-ui-modernization.
/// New tests will be added in Phase 7 (T049 DisplayPageViewModelTests).
/// </summary>
public class VirtualDesktopPinTests
{
    [Fact]
    public void PinToDesktopEnabled_SetTrue_CallsPinToCurrentDesktop()
    {
        IConnectionService connectionService = Substitute.For<IConnectionService>();
        IVirtualDesktopService virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        virtualDesktopService.IsSupported.Returns(true);
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.Load().Returns(new AppSettings());

        var sut = new MainWindowViewModel(connectionService, settingsStore: settingsStore, virtualDesktopService: virtualDesktopService);
        sut.SetWindowHandle(new IntPtr(12345));

        sut.PinToDesktopEnabled = true;

        virtualDesktopService.Received(1).PinToCurrentDesktop(new IntPtr(12345));
    }

    [Fact]
    public void PinToDesktopEnabled_SetFalse_CallsUnpin()
    {
        IConnectionService connectionService = Substitute.For<IConnectionService>();
        IVirtualDesktopService virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        virtualDesktopService.IsSupported.Returns(true);
        ISettingsStore settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.Load().Returns(new AppSettings());

        var sut = new MainWindowViewModel(connectionService, settingsStore: settingsStore, virtualDesktopService: virtualDesktopService);
        sut.SetWindowHandle(new IntPtr(12345));
        sut.PinToDesktopEnabled = true;

        sut.PinToDesktopEnabled = false;

        virtualDesktopService.Received(1).Unpin(new IntPtr(12345));
    }
}
