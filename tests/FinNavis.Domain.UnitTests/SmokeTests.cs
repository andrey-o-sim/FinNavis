namespace FinNavis.Domain.UnitTests;

/// <summary>
/// Placeholder so the test project has at least one test.
/// Confirms the project references Domain and that xUnit + AwesomeAssertions work.
/// Delete this once real Domain tests exist.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void DomainAssembly_IsReferenced()
    {
        var assembly = typeof(FinNavis.Domain.AssemblyMarker).Assembly;

        assembly.GetName().Name.Should().Be("FinNavis.Domain");
    }
}
