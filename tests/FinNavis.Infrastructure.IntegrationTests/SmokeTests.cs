namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Placeholder so the test project has at least one test.
/// Does not start Docker yet. Real tests here will use Testcontainers for PostgreSQL.
/// Delete this once real integration tests exist.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void InfrastructureAssembly_IsReferenced()
    {
        var assembly = typeof(FinNavis.Infrastructure.AssemblyMarker).Assembly;

        assembly.GetName().Name.Should().Be("FinNavis.Infrastructure");
    }
}
