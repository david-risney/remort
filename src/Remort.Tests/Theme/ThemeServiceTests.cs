using System.Windows;
using FluentAssertions;
using Remort.Theme;

namespace Remort.Tests.Theme;

public class ThemeServiceTests
{
    [StaFact]
    public void Constructor_AppliesDefaultProfile()
    {
        var resources = new ResourceDictionary();
        var service = new ThemeService(resources);

        service.ActiveProfile.Should().Be(PresetProfiles.Default);
    }

    [StaFact]
    public void ApplyProfile_SetsActiveProfile()
    {
        var resources = new ResourceDictionary();
        var service = new ThemeService(resources);

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        service.ApplyProfile(light);

        service.ActiveProfile.Should().Be(light);
    }

    [StaFact]
    public void ApplyProfile_RaisesProfileChangedEvent()
    {
        var resources = new ResourceDictionary();
        var service = new ThemeService(resources);

        bool raised = false;
        service.ProfileChanged += (_, _) => raised = true;

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        service.ApplyProfile(light);

        raised.Should().BeTrue();
    }

    [StaFact]
    public void ApplyProfile_SetsResourceBrushes()
    {
        var resources = new ResourceDictionary();
        var service = new ThemeService(resources);

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        service.ApplyProfile(light);

        resources[ThemeResourceKeys.PrimaryBackground].Should().NotBeNull();
        resources[ThemeResourceKeys.Accent].Should().NotBeNull();
    }

    [StaFact]
    public void ApplyProfile_ReplacesOldDictionary()
    {
        var resources = new ResourceDictionary();
        var service = new ThemeService(resources);

        int initialCount = resources.MergedDictionaries.Count;

        ColorProfile light = PresetProfiles.All.Single(p => p.Name == "Light");
        service.ApplyProfile(light);

        // Should still only have one theme dictionary (replaced, not accumulated)
        resources.MergedDictionaries.Count.Should().Be(initialCount);
    }
}
