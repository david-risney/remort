using FluentAssertions;
using Remort.Theme;

namespace Remort.Tests.Theme;

public class ColorProfileTests
{
    [Fact]
    public void Record_EqualityByValue()
    {
        var a = new ColorProfile
        {
            Name = "Test",
            IsPreset = true,
            PrimaryBackground = "#1E1E1E",
            SecondaryBackground = "#252526",
            Accent = "#007ACC",
            TextPrimary = "#CCCCCC",
            TextSecondary = "#808080",
            Border = "#3F3F46",
        };

        var b = new ColorProfile
        {
            Name = "Test",
            IsPreset = true,
            PrimaryBackground = "#1E1E1E",
            SecondaryBackground = "#252526",
            Accent = "#007ACC",
            TextPrimary = "#CCCCCC",
            TextSecondary = "#808080",
            Border = "#3F3F46",
        };

        a.Should().Be(b);
    }

    [Fact]
    public void With_Expression_Creates_Modified_Copy()
    {
        ColorProfile original = PresetProfiles.Default;
        ColorProfile modified = original with { Name = "Custom", IsPreset = false };

        modified.Name.Should().Be("Custom");
        modified.IsPreset.Should().BeFalse();
        modified.PrimaryBackground.Should().Be(original.PrimaryBackground);
    }
}
