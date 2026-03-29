using FluentAssertions;
using Remort.Settings;

namespace Remort.Tests.Settings;

/// <summary>
/// Tests for <see cref="StartupRegistration"/>.
/// Note: These tests read/write HKCU\...\Run. They clean up after themselves.
/// </summary>
public sealed class StartupRegistrationTests : IDisposable
{
    public StartupRegistrationTests()
    {
        // Ensure clean state before each test
        StartupRegistration.Unregister();
    }

    public void Dispose()
    {
        StartupRegistration.Unregister();
    }

    [Fact]
    public void IsRegistered_WhenNotRegistered_ReturnsFalse()
    {
        StartupRegistration.IsRegistered().Should().BeFalse();
    }

    [Fact]
    public void Register_ThenIsRegistered_ReturnsTrue()
    {
        StartupRegistration.Register();

        StartupRegistration.IsRegistered().Should().BeTrue();
    }

    [Fact]
    public void Unregister_AfterRegister_ReturnsFalse()
    {
        StartupRegistration.Register();

        StartupRegistration.Unregister();

        StartupRegistration.IsRegistered().Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_True_Registers()
    {
        StartupRegistration.SetEnabled(true);

        StartupRegistration.IsRegistered().Should().BeTrue();
    }

    [Fact]
    public void SetEnabled_False_Unregisters()
    {
        StartupRegistration.SetEnabled(true);

        StartupRegistration.SetEnabled(false);

        StartupRegistration.IsRegistered().Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_IdempotentToggle()
    {
        StartupRegistration.SetEnabled(true);
        StartupRegistration.SetEnabled(true);

        StartupRegistration.IsRegistered().Should().BeTrue();
    }
}
