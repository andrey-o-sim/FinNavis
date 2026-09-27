namespace FinNavis.ArchitectureTests;

/// <summary>
/// Finds files in the repository from inside a test run.
/// </summary>
internal static class Repo
{
    /// <summary>Absolute path of the folder that holds FinNavis.slnx.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>Loads a project file by its path relative to the repository root.</summary>
    public static ProjectFile Project(string relativePath)
    {
        return ProjectFile.Load(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>Loads a src project by name, for example "FinNavis.Domain".</summary>
    public static ProjectFile SrcProject(string name)
    {
        return Project($"src/{name}/{name}.csproj");
    }

    private static string FindRoot()
    {
        // The test binary runs from tests/<project>/bin/<config>/<tfm>, so walk up to the solution.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinNavis.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find FinNavis.slnx in any folder above '{AppContext.BaseDirectory}'.");
    }
}
