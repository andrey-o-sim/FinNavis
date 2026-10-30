namespace FinNavis.ArchitectureTests;

internal static class Repo
{
    public static string Root { get; } = FindRoot();

    public static ProjectFile Project(string relativePath)
    {
        return ProjectFile.Load(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

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
