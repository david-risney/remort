namespace Remort.Tests.EndToEnd;

/// <summary>
/// xUnit collection definition that shares a single <see cref="LocalRdpServerFixture"/>
/// across all E2E tests, so the server is started once per test run.
/// </summary>
[CollectionDefinition("E2E")]
public sealed class EndToEndTestFixture : ICollectionFixture<LocalRdpServerFixture>
{
}
