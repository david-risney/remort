using FluentAssertions;
using NSubstitute;
using Remort.Settings;
using Remort.Theme;

namespace Remort.Tests.Theme;

public class ThemeSettingsViewModelTests
{
    [Fact]
    public void Constructor_PopulatesPresetProfiles()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.Profiles.Should().HaveCount(3);
        vm.Profiles.Should().AllSatisfy(p => p.IsPreset.Should().BeTrue());
    }

    [Fact]
    public void Constructor_SetsSelectedProfileToActive()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.SelectedProfile.Should().Be(PresetProfiles.Default);
    }

    [Fact]
    public void SelectingProfile_AppliesIt()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        vm.SelectedProfile = light;

        themeService.Received().ApplyProfile(light);
    }

    [Fact]
    public void SelectingProfile_PersistsName()
    {
        IThemeService themeService = CreateMockThemeService();
        var settingsStore = Substitute.For<ISettingsStore>();
        settingsStore.Load().Returns(new AppSettings());

        var vm = new ThemeSettingsViewModel(themeService, settingsStore);

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        vm.SelectedProfile = light;

        settingsStore.Received().Save(Arg.Is<AppSettings>(s => s.ActiveProfileName == "Light"));
    }

    [Fact]
    public void CreateProfile_RejectsDuplicateName()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.NewProfileName = "Dark";
        vm.CreateProfileCommand.Execute(null);

        vm.NameValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateProfile_RejectsEmptyName()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.NewProfileName = "   ";
        vm.CreateProfileCommand.Execute(null);

        vm.NameValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateProfile_AddsToProfiles()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.NewProfileName = "My Custom";
        vm.CreateProfileCommand.Execute(null);

        vm.Profiles.Should().Contain(p => p.Name == "My Custom");
        vm.IsEditing.Should().BeTrue();
    }

    [Fact]
    public void CancelEdit_RemovesUnsavedProfile()
    {
        IThemeService themeService = CreateMockThemeService();
        var profileStore = Substitute.For<IProfileStore>();
        profileStore.LoadCustomProfiles().Returns(new List<ColorProfile>());

        var vm = new ThemeSettingsViewModel(themeService, profileStore: profileStore);

        vm.NewProfileName = "Temp";
        vm.CreateProfileCommand.Execute(null);
        vm.Profiles.Should().Contain(p => p.Name == "Temp");

        vm.CancelEditCommand.Execute(null);
        vm.Profiles.Should().NotContain(p => p.Name == "Temp");
        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void DeleteProfile_RemovesFromCollection()
    {
        IThemeService themeService = CreateMockThemeService();
        var profileStore = Substitute.For<IProfileStore>();
        profileStore.LoadCustomProfiles().Returns(new List<ColorProfile>());

        var vm = new ThemeSettingsViewModel(themeService, profileStore: profileStore);

        // Create and save a profile first
        vm.NewProfileName = "ToDelete";
        vm.CreateProfileCommand.Execute(null);
        vm.SaveProfileCommand.Execute(null);
        vm.Profiles.Should().Contain(p => p.Name == "ToDelete");

        vm.SelectedProfile = vm.Profiles.Single(p => p.Name == "ToDelete");
        vm.DeleteProfileCommand.Execute(null);

        vm.Profiles.Should().NotContain(p => p.Name == "ToDelete");
    }

    [Fact]
    public void DeleteProfile_IgnoresPresetProfiles()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.SelectedProfile = PresetProfiles.Default;
        vm.DeleteProfileCommand.Execute(null);

        vm.Profiles.Should().HaveCount(3);
    }

    [Fact]
    public void Close_SetsShouldClose()
    {
        IThemeService themeService = CreateMockThemeService();
        var vm = new ThemeSettingsViewModel(themeService);

        vm.CloseCommand.Execute(null);

        vm.ShouldClose.Should().BeTrue();
    }

    [Fact]
    public void Constructor_LoadsCustomProfilesFromStore()
    {
        IThemeService themeService = CreateMockThemeService();
        var profileStore = Substitute.For<IProfileStore>();
        var customProfile = new ColorProfile
        {
            Name = "Saved Custom",
            IsPreset = false,
            PrimaryBackground = "#111111",
            SecondaryBackground = "#222222",
            Accent = "#333333",
            TextPrimary = "#EEEEEE",
            TextSecondary = "#AAAAAA",
            Border = "#444444",
        };
        profileStore.LoadCustomProfiles().Returns(new List<ColorProfile> { customProfile });

        var vm = new ThemeSettingsViewModel(themeService, profileStore: profileStore);

        vm.Profiles.Should().HaveCount(4); // 3 presets + 1 custom
        vm.Profiles.Should().Contain(p => p.Name == "Saved Custom");
    }

    private static IThemeService CreateMockThemeService()
    {
        var mock = Substitute.For<IThemeService>();
        mock.ActiveProfile.Returns(PresetProfiles.Default);
        return mock;
    }
}
