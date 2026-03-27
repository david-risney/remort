using FluentAssertions;

namespace Remort.Tests;

public class SmokeTests
{
    [Fact]
    public void ProjectBuildsAndTestInfrastructureWorks()
    {
        // Verifies that the test project references the main project,
        // xUnit runs, and FluentAssertions is wired up.
        true.Should().BeTrue();
    }
}
