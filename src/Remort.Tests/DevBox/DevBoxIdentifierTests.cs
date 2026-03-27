using FluentAssertions;
using Remort.DevBox;

namespace Remort.Tests.DevBox;

/// <summary>
/// Unit tests for <see cref="DevBoxIdentifier.Parse"/>.
/// </summary>
public class DevBoxIdentifierTests
{
    [Fact]
    public void Parse_StandardFqdn_ReturnsNotDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("myserver.contoso.com");

        result.IsDevBox.Should().BeFalse();
        result.FullyQualifiedName.Should().Be("myserver.contoso.com");
    }

    [Fact]
    public void Parse_ShortName_ReturnsNotDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("my-devbox");

        result.IsDevBox.Should().BeFalse();
        result.ShortName.Should().Be("my-devbox");
    }

    [Fact]
    public void Parse_DevBoxFqdn_ReturnsIsDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("my-devbox.runnergroup.devbox.microsoft.com");

        result.IsDevBox.Should().BeTrue();
        result.ShortName.Should().Be("my-devbox");
        result.FullyQualifiedName.Should().Be("my-devbox.runnergroup.devbox.microsoft.com");
    }

    [Fact]
    public void Parse_DevBoxFqdnCaseInsensitive_ReturnsIsDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("MyBox.group.DEVBOX.MICROSOFT.COM");

        result.IsDevBox.Should().BeTrue();
        result.ShortName.Should().Be("MyBox");
    }

    [Fact]
    public void Parse_Ipv4Address_ReturnsNotDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("10.0.0.1");

        result.IsDevBox.Should().BeFalse();
    }

    [Fact]
    public void Parse_Ipv6Address_ReturnsNotDevBox()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("::1");

        result.IsDevBox.Should().BeFalse();
    }

    [Fact]
    public void Parse_WhitespaceInput_ThrowsArgumentException()
    {
        Action act = () => DevBoxIdentifier.Parse("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_InputWithWhitespace_TrimsBeforeParsing()
    {
        DevBoxIdentifier result = DevBoxIdentifier.Parse("  my-devbox.project.devbox.microsoft.com  ");

        result.IsDevBox.Should().BeTrue();
        result.ShortName.Should().Be("my-devbox");
    }
}
