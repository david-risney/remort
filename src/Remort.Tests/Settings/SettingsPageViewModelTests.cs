using FluentAssertions;
using NSubstitute;
using Remort.Settings;

namespace Remort.Tests.Settings;

/// <summary>
/// Tests for <see cref="SettingsPageViewModel"/>.
/// </summary>
public class SettingsPageViewModelTests
{
    private readonly ISettingsStore _settingsStore;

    public SettingsPageViewModelTests()
    {
        _settingsStore = Substitute.For<ISettingsStore>();
        _settingsStore.Load().Returns(new AppSettings());
    }

    [Fact]
    public void Constructor_LoadsThemeFromSettings()
    {
        _settingsStore.Load().Returns(new AppSettings { Theme = AppTheme.Light });

        var sut = new SettingsPageViewModel(_settingsStore);

        sut.SelectedTheme.Should().Be(AppTheme.Light);
    }

    [Fact]
    public void Constructor_DefaultsToSystem()
    {
        var sut = new SettingsPageViewModel(_settingsStore);

        sut.SelectedTheme.Should().Be(AppTheme.System);
    }

    [Fact]
    public void DevBoxDiscoveryEnabled_WhenChanged_Persists()
    {
        var sut = new SettingsPageViewModel(_settingsStore);

        sut.DevBoxDiscoveryEnabled = false;

        _settingsStore.Received(1).Save(Arg.Is<AppSettings>(s => !s.DevBoxDiscoveryEnabled));
    }

    [Fact]
    public void AppVersion_ReturnsNonEmpty()
    {
        SettingsPageViewModel.AppVersion.Should().NotBeNullOrEmpty();
    }
}
