namespace FinNavis.ArchitectureTests;

/// <summary>
/// Checks the dependency rule against what the .csproj files declare.
/// This is the machine-readable form of the table in ARCHITECTURE.md and of the
/// second acceptance criterion in SPEC.md.
/// </summary>
/// <remarks>
/// These tests work on an empty scaffold. They catch a wrong reference the moment it is added,
/// before any code uses it. <see cref="LayerDependencyTests"/> covers the other half:
/// what the compiled code actually touches.
/// </remarks>
public sealed class ProjectReferenceTests
{
    /// <summary>
    /// The dependency rule, one row per project. Copied from the table in ARCHITECTURE.md.
    /// Keep the two in step: if this changes, that table changes too.
    /// </summary>
    public static TheoryData<string, string[]> AllowedProjectReferences => new()
    {
        { "FinNavis.Domain", [] },
        { "FinNavis.Application", ["FinNavis.Domain"] },
        { "FinNavis.Infrastructure", ["FinNavis.Application", "FinNavis.Domain"] },
        { "FinNavis.Presentation", ["FinNavis.Application", "FinNavis.Infrastructure"] },
    };

    /// <summary>
    /// Packages that would put the outside world inside the Application ring.
    /// Application says what it needs. Infrastructure says how it is done.
    /// </summary>
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
