using System.Xml.Linq;

namespace FinNavis.ArchitectureTests;

/// <summary>
/// The references declared in one .csproj file.
/// </summary>
/// <remarks>
/// We read the project file instead of the compiled assembly on purpose.
/// An unused PackageReference leaves no trace in the assembly, so reflection cannot see it,
/// but it still breaks the dependency rule. The project file always shows the truth.
/// </remarks>
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

    /// <summary>File name without the extension, for example "FinNavis.Domain".</summary>
    public string Name { get; }

    /// <summary>Referenced project names, without path or extension. Sorted.</summary>
    public IReadOnlyList<string> ProjectReferences { get; }

    /// <summary>Referenced NuGet package ids. Sorted.</summary>
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

    /// <summary>Turns "..\FinNavis.Domain\FinNavis.Domain.csproj" into "FinNavis.Domain".</summary>
    private static string ToProjectName(string include)
    {
        // MSBuild writes Windows separators. Normalise so this works on Linux too.
        return Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
    }
}
