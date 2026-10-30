using System.Xml.Linq;

namespace FinNavis.ArchitectureTests;

internal sealed class ProjectFile
{
    private ProjectFile(
        string name,
        IReadOnlyList<string> projectReferences,
        IReadOnlyList<string> packageReferences)
    {
        Name = name;
        ProjectReferences = projectReferences;
        PackageReferences = packageReferences;
    }

    public string Name { get; }

    public IReadOnlyList<string> ProjectReferences { get; }

    public IReadOnlyList<string> PackageReferences { get; }

    public static ProjectFile Load(string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Project file not found: {absolutePath}", absolutePath);
        }

        var document = XDocument.Load(absolutePath);

        return new ProjectFile(
            Path.GetFileNameWithoutExtension(absolutePath),
            ReadIncludes(document, "ProjectReference", ToProjectName),
            ReadIncludes(document, "PackageReference", static value => value));
    }

    private static IReadOnlyList<string> ReadIncludes(
        XDocument document,
        string elementName,
        Func<string, string> transform)
    {
        return document.Descendants(elementName)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => transform(value!.Trim()))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ToProjectName(string include)
    {
        // MSBuild writes Windows separators. Normalise so this works on Linux too.
        return Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
    }
}
