namespace FinNavis.ArchitectureTests;

/// <summary>
/// Plain-text checks on <c>docker-compose.yml</c>. Sits next to the dependency-rule checks
/// because this project already knows how to find the repository root (<see cref="Repo"/>).
/// </summary>
public sealed class DockerComposeFileTests
{
    private static string ComposeFileContent => File.ReadAllText(Path.Combine(Repo.Root, "docker-compose.yml"));

    [Fact]
    public void ComposeFile_PostgresImage_IsPinnedToASpecificVersionNotLatest()
    {
        // Scoped to the postgres image on purpose. A second service — a Dockerfile for the API is
        // the named next step — must not turn this into a "sequence contains more than one
        // element" crash that hides the real assertion.
        var imageLine = ComposeFileContent
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("image:", StringComparison.Ordinal))
            .Should().ContainSingle(line => line.Contains("postgres", StringComparison.Ordinal))
            .Subject;

        var image = imageLine["image:".Length..].Trim();

        image.Should().Contain(":", "the image must carry a tag, not be bare");
        image.Should().NotEndWith(":latest", "the image must be pinned to a specific major version");
    }

    [Fact]
    public void ComposeFile_Port_IsPublishedOnLocalhostOnly()
    {
        ComposeFileContent.Should().Contain(
            "127.0.0.1:",
            "the PostgreSQL port must be published on 127.0.0.1 only, not on every interface");
    }

    [Fact]
    public void ComposeFile_NamedVolume_IsDeclaredAndMounted()
    {
        var lines = ComposeFileContent.Split('\n').Select(line => line.Trim()).ToList();

        // The mount is asserted in full. A bare "contains finnavis-pgdata" would still pass if the
        // service mount were dropped or changed to a bind mount, and the data would stop surviving
        // `docker compose down`. The path is PGDATA for postgres:17 — it moved in 18.
        lines.Should().Contain(
            "- finnavis-pgdata:/var/lib/postgresql/data",
            "the service must mount the named volume at the PostgreSQL data directory");

        lines.Should().Contain(
            "finnavis-pgdata:",
            "the volume must be declared as a top-level named volume, not created ad hoc");
    }

    [Fact]
    public void ComposeFile_Healthcheck_IsPresent()
    {
        ComposeFileContent.Should().Contain("healthcheck:", "the service must declare a healthcheck");
    }
}
