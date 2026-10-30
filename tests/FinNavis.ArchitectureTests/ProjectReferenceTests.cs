namespace FinNavis.ArchitectureTests;

public sealed class ProjectReferenceTests
{
    public static TheoryData<string, string[]> AllowedProjectReferences => new()
    {
        { "FinNavis.Domain", [] },
        { "FinNavis.Application", ["FinNavis.Domain"] },
        { "FinNavis.Infrastructure", ["FinNavis.Application", "FinNavis.Domain"] },
        { "FinNavis.Presentation", ["FinNavis.Application", "FinNavis.Infrastructure"] },
    };

    public static TheoryData<string> PackagesBannedFromApplication =>
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "Dapper",
        "Swashbuckle",
    ];

    [Theory]
    [MemberData(nameof(AllowedProjectReferences))]
    public void SrcProject_ReferencesOnlyAllowedProjects(string projectName, string[] allowed)
    {
        var project = Repo.SrcProject(projectName);

        project.ProjectReferences.Should().BeSubsetOf(
            allowed,
            "{0} may only reference {1} (ARCHITECTURE.md, 'The dependency rule')",
            projectName,
            allowed.Length == 0 ? "nothing" : string.Join(", ", allowed));
    }

    [Fact]
    public void Domain_DeclaresNoReferencesAtAll()
    {
        var domain = Repo.SrcProject("FinNavis.Domain");

        domain.ProjectReferences.Should().BeEmpty(
            "Domain is the innermost ring. It references nothing.");

        domain.PackageReferences.Should().BeEmpty(
            "Domain takes no NuGet packages. If it needs something from outside, "
            + "it declares an interface and Infrastructure implements it.");
    }

    [Theory]
    [MemberData(nameof(PackagesBannedFromApplication))]
    public void Application_DoesNotReferenceWebOrDataPackages(string bannedPrefix)
    {
        var application = Repo.SrcProject("FinNavis.Application");

        application.PackageReferences.Should().NotContain(
            package => package.StartsWith(bannedPrefix, StringComparison.OrdinalIgnoreCase),
            "Application contains no EF Core, no HTTP, and no SQL (ARCHITECTURE.md, 'Application')");
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        var application = Repo.SrcProject("FinNavis.Application");

        application.ProjectReferences.Should().NotContain(
            "FinNavis.Infrastructure",
            "an inner ring never knows about an outer ring");
    }

    [Fact]
    public void Infrastructure_DoesNotReferencePresentation()
    {
        var infrastructure = Repo.SrcProject("FinNavis.Infrastructure");

        infrastructure.ProjectReferences.Should().NotContain(
            "FinNavis.Presentation",
            "Presentation is the composition root. Nothing points back at it.");
    }

    [Fact]
    public void Presentation_IsTheOnlyProjectThatReferencesInfrastructure()
    {
        string[] allSrcProjects =
        [
            "FinNavis.Domain",
            "FinNavis.Application",
            "FinNavis.Infrastructure",
            "FinNavis.Presentation",
        ];

        var referencing = allSrcProjects
            .Select(Repo.SrcProject)
            .Where(project => project.ProjectReferences.Contains("FinNavis.Infrastructure"))
            .Select(project => project.Name)
            .ToArray();

        referencing.Should().Equal(
            ["FinNavis.Presentation"],
            "only the composition root may see concrete types, so it can register them in DI");
    }
}
