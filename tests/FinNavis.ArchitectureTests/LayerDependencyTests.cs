using System.Reflection;
using NetArchTest.Rules;

namespace FinNavis.ArchitectureTests;

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
