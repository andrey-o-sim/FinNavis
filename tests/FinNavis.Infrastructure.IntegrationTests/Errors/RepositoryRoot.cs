namespace FinNavis.Infrastructure.IntegrationTests.Errors;

internal static class RepositoryRoot
{
    public static string Path { get; } = Find();

    public static string Combine(string relativePath) =>
        System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    private static string Find()
    {
        // The test binary runs from tests/<project>/bin/<config>/<tfm>, so walk up to the solution.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "FinNavis.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find FinNavis.slnx in any folder above '{AppContext.BaseDirectory}'.");
    }
}
