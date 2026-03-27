using System.IO;
using FluentAssertions;
using Remort.Theme;

namespace Remort.Tests.Theme;

public sealed class JsonProfileStoreTests : IDisposable
{
    private readonly string _tempFile;

    public JsonProfileStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"remort-test-profiles-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public void LoadCustomProfiles_ReturnsEmpty_WhenFileMissing()
    {
        var store = new JsonProfileStore(_tempFile);
        store.LoadCustomProfiles().Should().BeEmpty();
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var store = new JsonProfileStore(_tempFile);
        var profiles = new List<ColorProfile>
        {
            new()
            {
                Name = "Custom1",
                IsPreset = false,
                PrimaryBackground = "#111111",
                SecondaryBackground = "#222222",
                Accent = "#333333",
                TextPrimary = "#EEEEEE",
                TextSecondary = "#AAAAAA",
                Border = "#444444",
            },
        };

        store.SaveCustomProfiles(profiles);
        IReadOnlyList<ColorProfile> loaded = store.LoadCustomProfiles();

        loaded.Should().HaveCount(1);
        loaded[0].Name.Should().Be("Custom1");
        loaded[0].IsPreset.Should().BeFalse();
        loaded[0].PrimaryBackground.Should().Be("#111111");
    }

    [Fact]
    public void LoadCustomProfiles_EnforcesIsPresetFalse()
    {
        // Write a profile with IsPreset = true to file
        string json = """[{"name": "Hack", "isPreset": true, "primaryBackground": "#000000", "secondaryBackground": "#111111", "accent": "#222222", "textPrimary": "#FFFFFF", "textSecondary": "#AAAAAA", "border": "#333333"}]""";
        File.WriteAllText(_tempFile, json);

        var store = new JsonProfileStore(_tempFile);
        IReadOnlyList<ColorProfile> loaded = store.LoadCustomProfiles();

        loaded.Should().HaveCount(1);
        loaded[0].IsPreset.Should().BeFalse("custom profiles must never be preset");
    }

    [Fact]
    public void LoadCustomProfiles_ReturnsEmpty_OnMalformedJson()
    {
        File.WriteAllText(_tempFile, "not valid json!!!");
        var store = new JsonProfileStore(_tempFile);

        store.LoadCustomProfiles().Should().BeEmpty();
    }

    [Fact]
    public void DeleteAll_RemovesFile()
    {
        var store = new JsonProfileStore(_tempFile);
        store.SaveCustomProfiles(new List<ColorProfile>());

        File.Exists(_tempFile).Should().BeTrue();

        store.DeleteAll();

        File.Exists(_tempFile).Should().BeFalse();
    }
}
