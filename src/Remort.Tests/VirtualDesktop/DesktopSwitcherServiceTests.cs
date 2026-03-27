using FluentAssertions;
using Remort.VirtualDesktop;

namespace Remort.Tests.VirtualDesktop;

/// <summary>
/// Unit tests for <see cref="DesktopSwitcherService"/> and <see cref="VirtualDesktopInfo"/>.
/// These tests verify behaviors that are safe to test without real virtual desktops:
/// record equality, graceful degradation when the registry key is absent, and no-op switching.
/// </summary>
public class DesktopSwitcherServiceTests
{
    [Fact]
    public void VirtualDesktopInfo_RecordEquality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new VirtualDesktopInfo(id, "Desktop 1", 0);
        var b = new VirtualDesktopInfo(id, "Desktop 1", 0);

        a.Should().Be(b);
    }

    [Fact]
    public void VirtualDesktopInfo_RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new VirtualDesktopInfo(Guid.NewGuid(), "Desktop 1", 0);
        var b = new VirtualDesktopInfo(Guid.NewGuid(), "Desktop 2", 1);

        a.Should().NotBe(b);
    }

    [Fact]
    public void VirtualDesktopInfo_Properties_AreCorrect()
    {
        var id = Guid.NewGuid();
        var info = new VirtualDesktopInfo(id, "My Desktop", 3);

        info.Id.Should().Be(id);
        info.Name.Should().Be("My Desktop");
        info.Index.Should().Be(3);
    }

    [Fact]
    public void GetDesktops_ReturnsNonEmptyList_WhenSupported()
    {
        // This test relies on the CI/dev machine having virtual desktops.
        // It tests the real service against the live registry.
        using var sut = new DesktopSwitcherService();

        if (!sut.IsSupported)
        {
            // Graceful skip: registry key not present on this machine.
            return;
        }

        IReadOnlyList<VirtualDesktopInfo> desktops = sut.GetDesktops();

        desktops.Should().NotBeEmpty();
        desktops[0].Index.Should().Be(0);
        desktops[0].Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetCurrentDesktopIndex_ReturnsValidIndex_WhenSupported()
    {
        using var sut = new DesktopSwitcherService();

        if (!sut.IsSupported)
        {
            return;
        }

        int index = sut.GetCurrentDesktopIndex();

        index.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SwitchToDesktop_IsNoOp_WhenTargetEqualsCurrent()
    {
        using var sut = new DesktopSwitcherService();

        // Should not throw or send any input when target == current.
        sut.SwitchToDesktop(0, 0);
    }

    [Fact]
    public void SwitchToDesktop_IsNoOp_WhenIndicesAreNegative()
    {
        using var sut = new DesktopSwitcherService();

        // Should not throw when given invalid indices.
        sut.SwitchToDesktop(-1, 0);
        sut.SwitchToDesktop(0, -1);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var sut = new DesktopSwitcherService();

        sut.Dispose();
        sut.Dispose(); // Should not throw.
    }
}
