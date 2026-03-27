using FluentAssertions;
using Remort.Theme;

namespace Remort.Tests.Theme;

public class PresetProfilesTests
{
    [Fact]
    public void All_ContainsThreeProfiles()
    {
        PresetProfiles.All.Should().HaveCount(3);
    }

    [Fact]
    public void Default_IsDark()
    {
        PresetProfiles.Default.Name.Should().Be("Dark");
    }

    [Fact]
    public void All_ArePreset()
    {
        PresetProfiles.All.Should().AllSatisfy(p => p.IsPreset.Should().BeTrue());
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    [InlineData("Midnight Blue")]
    public void Profile_HasValidHexColors(string name)
    {
        ColorProfile profile = PresetProfiles.All.Single(p => p.Name == name);

        AssertValidHex(profile.PrimaryBackground);
        AssertValidHex(profile.SecondaryBackground);
        AssertValidHex(profile.Accent);
        AssertValidHex(profile.TextPrimary);
        AssertValidHex(profile.TextSecondary);
        AssertValidHex(profile.Border);
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    [InlineData("Midnight Blue")]
    public void Profile_TextOnPrimaryBackground_MeetsWcagAA(string name)
    {
        ColorProfile profile = PresetProfiles.All.Single(p => p.Name == name);

        double ratio = ContrastRatio(profile.TextPrimary, profile.PrimaryBackground);
        ratio.Should().BeGreaterOrEqualTo(4.5, $"WCAG AA requires 4.5:1 for {name} TextPrimary on PrimaryBackground");
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    [InlineData("Midnight Blue")]
    public void Profile_TextOnSecondaryBackground_MeetsWcagAA(string name)
    {
        ColorProfile profile = PresetProfiles.All.Single(p => p.Name == name);

        double ratio = ContrastRatio(profile.TextPrimary, profile.SecondaryBackground);
        ratio.Should().BeGreaterOrEqualTo(4.5, $"WCAG AA requires 4.5:1 for {name} TextPrimary on SecondaryBackground");
    }

    private static void AssertValidHex(string hex)
    {
        hex.Should().MatchRegex(@"^#[0-9A-Fa-f]{6}$", $"'{hex}' should be a valid 6-digit hex color");
    }

    private static double ContrastRatio(string foregroundHex, string backgroundHex)
    {
        double fgLum = RelativeLuminance(foregroundHex);
        double bgLum = RelativeLuminance(backgroundHex);

        double lighter = Math.Max(fgLum, bgLum);
        double darker = Math.Min(fgLum, bgLum);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        int r = Convert.ToInt32(hex.Substring(1, 2), 16);
        int g = Convert.ToInt32(hex.Substring(3, 2), 16);
        int b = Convert.ToInt32(hex.Substring(5, 2), 16);

        double rs = LinearizeChannel(r / 255.0);
        double gs = LinearizeChannel(g / 255.0);
        double bs = LinearizeChannel(b / 255.0);

        return (0.2126 * rs) + (0.7152 * gs) + (0.0722 * bs);
    }

    private static double LinearizeChannel(double channel)
    {
        return channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
