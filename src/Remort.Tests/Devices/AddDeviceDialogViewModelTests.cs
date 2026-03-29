using FluentAssertions;
using Remort.Devices;

namespace Remort.Tests.Devices;

/// <summary>
/// Tests for <see cref="AddDeviceDialogViewModel"/>.
/// </summary>
public class AddDeviceDialogViewModelTests
{
    [Fact]
    public void TryConfirm_WithEmptyName_ReturnsFalseAndSetsError()
    {
        var sut = new AddDeviceDialogViewModel { Name = string.Empty, Hostname = "host.local" };

        bool result = sut.TryConfirm();

        result.Should().BeFalse();
        sut.ValidationError.Should().Contain("Name");
        sut.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public void TryConfirm_WithEmptyHostname_ReturnsFalseAndSetsError()
    {
        var sut = new AddDeviceDialogViewModel { Name = "Test", Hostname = string.Empty };

        bool result = sut.TryConfirm();

        result.Should().BeFalse();
        sut.ValidationError.Should().Contain("Hostname");
        sut.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public void TryConfirm_WithWhitespaceOnly_ReturnsFalse()
    {
        var sut = new AddDeviceDialogViewModel { Name = "   ", Hostname = "host.local" };

        sut.TryConfirm().Should().BeFalse();
    }

    [Fact]
    public void TryConfirm_WithValidInputs_ReturnsTrueAndSetsConfirmed()
    {
        var sut = new AddDeviceDialogViewModel { Name = "My Device", Hostname = "host.local" };

        bool result = sut.TryConfirm();

        result.Should().BeTrue();
        sut.IsConfirmed.Should().BeTrue();
        sut.ValidationError.Should().BeEmpty();
    }

    [Fact]
    public void TryConfirm_TrimsInputs()
    {
        var sut = new AddDeviceDialogViewModel { Name = "  Test  ", Hostname = "  host.local  " };

        sut.TryConfirm();

        sut.Name.Should().Be("Test");
        sut.Hostname.Should().Be("host.local");
    }

    [Fact]
    public void CreateDevice_WhenConfirmed_ReturnsDeviceWithDefaults()
    {
        var sut = new AddDeviceDialogViewModel { Name = "Test", Hostname = "host.local" };
        sut.TryConfirm();

        Device device = sut.CreateDevice();

        device.Name.Should().Be("Test");
        device.Hostname.Should().Be("host.local");
        device.IsFavorite.Should().BeFalse();
        device.IsDiscovered.Should().BeFalse();
        device.ConnectionSettings.MaxRetryCount.Should().Be(3);
        device.DisplaySettings.FitSessionToWindow.Should().BeTrue();
        device.RedirectionSettings.Clipboard.Should().BeTrue();
    }

    [Fact]
    public void CreateDevice_WhenNotConfirmed_ThrowsInvalidOperation()
    {
        var sut = new AddDeviceDialogViewModel { Name = "Test", Hostname = "host.local" };

        Action act = () => sut.CreateDevice();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateDevice_AssignsUniqueId()
    {
        var sut1 = new AddDeviceDialogViewModel { Name = "A", Hostname = "a.local" };
        sut1.TryConfirm();
        var sut2 = new AddDeviceDialogViewModel { Name = "B", Hostname = "b.local" };
        sut2.TryConfirm();

        sut1.CreateDevice().Id.Should().NotBe(sut2.CreateDevice().Id);
    }
}
