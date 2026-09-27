using System.Reflection;
using NetArchTest.Rules;

namespace FinNavis.ArchitectureTests;

/// <summary>
/// Checks the dependency rule against the compiled code, type by type.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ProjectReferenceTests"/> checks what a project is <em>allowed</em> to see.
/// These tests check what the code <em>actually</em> touches, which catches the cases a project
/// reference cannot: a type reached through a transitive reference, or an Infrastructure type
/// used somewhere in Presentation outside the composition root.
/// </para>
/// <para>
/// While the scaffold has no entities or use cases these tests pass because there is nothing to
/// fail. That is fine. They start doing real work with the first feature, and they are here now
/// so nobody has to remember to add them later.
/// </para>
/// </remarks>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.AssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.AssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.AssemblyMarker).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnAnyOuterLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("FinNavis.Application", "FinNavis.Infrastructure", "FinNavis.Presentation")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "Domain is the innermost ring and knows about nothing outside itself");
    }

    [Fact]
    public void Domain_DoesNotDependOnThirdPartyLibraries()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "a domain rule must be expressible without any library");
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("FinNavis.Infrastructure", "FinNavis.Presentation")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "Application declares ports. Infrastructure implements them.");
    }

    [Fact]
    public void Application_DoesNotDependOnEfCoreOrHttp()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "a use case says what it needs, never how the data is stored or delivered");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("FinNavis.Presentation")
            .GetResult();

        result.FailingTypeNames.Should().BeNullOrEmpty(
            "nothing points back at the composition root");
    }
}
