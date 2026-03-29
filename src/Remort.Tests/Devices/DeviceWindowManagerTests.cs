using FluentAssertions;
using Remort.Devices;

namespace Remort.Tests.Devices;

/// <summary>
/// Tests for <see cref="DeviceWindowManager"/>.
/// Note: These tests validate tracking logic only. Window creation/focus
/// requires a WPF dispatcher and is covered by E2E tests.
/// </summary>
public class DeviceWindowManagerTests
{
    [Fact]
    public void IsOpen_WhenNoWindowOpen_ReturnsFalse()
    {
        var manager = new DeviceWindowManager(_ => throw new InvalidOperationException("Should not be called"));

        manager.IsOpen(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CloseForDevice_WhenNoWindowOpen_DoesNotThrow()
    {
        var manager = new DeviceWindowManager(_ => throw new InvalidOperationException("Should not be called"));

        Action act = () => manager.CloseForDevice(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void CloseAll_WhenNoWindowsOpen_DoesNotThrow()
    {
        var manager = new DeviceWindowManager(_ => throw new InvalidOperationException("Should not be called"));

        Action act = () => manager.CloseAll();

        act.Should().NotThrow();
    }
}
