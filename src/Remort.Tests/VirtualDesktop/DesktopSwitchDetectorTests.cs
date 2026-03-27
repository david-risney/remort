using FluentAssertions;
using NSubstitute;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Unit tests for <see cref="DesktopSwitchDetector"/>.
/// Uses <see cref="DesktopSwitchDetector.SimulateTick"/> to drive poll cycles
/// synchronously without a dispatcher message pump.
/// </summary>
public class DesktopSwitchDetectorTests
{
    private static readonly IntPtr s_testHwnd = new(12345);

    private readonly IVirtualDesktopService _virtualDesktopService;

    public DesktopSwitchDetectorTests()
    {
        _virtualDesktopService = Substitute.For<IVirtualDesktopService>();
        _virtualDesktopService.IsSupported.Returns(true);
        _virtualDesktopService.IsOnCurrentDesktop(Arg.Any<IntPtr>()).Returns(true);
    }

    [Fact]
    public void SwitchedToDesktop_Fires_OnFalseToTrueTransition()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        // Window leaves current desktop (true→false).
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(false);
        sut.SimulateTick();
        fired.Should().BeFalse("true→false should not fire");

        // Window returns to current desktop (false→true).
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        sut.SimulateTick();
        fired.Should().BeTrue("false→true should fire");
    }

    [Fact]
    public void SwitchedToDesktop_DoesNotFire_OnTrueToTrueSteadyState()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        sut.SimulateTick();

        fired.Should().BeFalse("steady state true→true should not fire");
    }

    [Fact]
    public void SwitchedToDesktop_DoesNotFire_OnTrueToFalseTransition()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(false);
        sut.SimulateTick();

        fired.Should().BeFalse("true→false should not fire");
    }

    [Fact]
    public void SwitchedToDesktop_DoesNotFire_AfterStopMonitoring()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        // Move off desktop.
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(false);
        sut.SimulateTick();

        sut.StopMonitoring();

        // Move back—should NOT fire because monitoring is stopped.
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        sut.SimulateTick();

        fired.Should().BeFalse("events should not fire after StopMonitoring");
    }

    [Fact]
    public void StartMonitoring_WithZeroHwnd_IsNoOp()
    {
        using var sut = new DesktopSwitchDetector(_virtualDesktopService);
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        sut.StartMonitoring(IntPtr.Zero);
        sut.SimulateTick();

        fired.Should().BeFalse("zero hwnd should be a no-op");
    }

    [Fact]
    public void StartMonitoring_IsIdempotent()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using var sut = new DesktopSwitchDetector(_virtualDesktopService);

        // Start twice—should not throw and should work correctly.
        sut.StartMonitoring(s_testHwnd);
        sut.StartMonitoring(s_testHwnd);

        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(false);
        sut.SimulateTick();
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        sut.SimulateTick();

        fired.Should().BeTrue("detector should work after idempotent start");
    }

    [Fact]
    public void StopMonitoring_IsIdempotent()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();

        Action act = () =>
        {
            sut.StopMonitoring();
            sut.StopMonitoring();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(false);
        sut.SimulateTick();

        sut.Dispose();

        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        sut.SimulateTick();

        fired.Should().BeFalse("events should not fire after Dispose");
    }

    [Fact]
    public void SwitchedToDesktop_DoesNotFire_OnStartup_WhenWindowAlreadyOnDesktop()
    {
        _virtualDesktopService.IsOnCurrentDesktop(s_testHwnd).Returns(true);
        using DesktopSwitchDetector sut = CreateAndStart();
        bool fired = false;
        sut.SwitchedToDesktop += (_, _) => fired = true;

        // First tick: still on current desktop (true→true).
        sut.SimulateTick();

        fired.Should().BeFalse("should not fire false→true on startup when already on desktop");
    }

    private DesktopSwitchDetector CreateAndStart()
    {
        var sut = new DesktopSwitchDetector(_virtualDesktopService);
        sut.StartMonitoring(s_testHwnd);
        return sut;
    }
}
