namespace FinNavis.Application.UnitTests;

/// <summary>
/// Placeholder so the test project has at least one test.
/// Confirms the project references Application and that xUnit + AwesomeAssertions work.
/// Delete this once real Application tests exist.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void ApplicationAssembly_IsReferenced()
    {
        var assembly = typeof(FinNavis.Application.AssemblyMarker).Assembly;

        assembly.GetName().Name.Should().Be("FinNavis.Application");
    }
}
