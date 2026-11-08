using System.Text.RegularExpressions;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

// A code is a string literal handed to a DomainRuleViolationException in an entity or a use case,
// not a constant on a type, so reflection cannot see it and the source text is read instead.
// Presentation is left out because it maps codes to statuses and never raises one.
internal static partial class SourceErrorCodes
{
    private static readonly string[] RaisingProjects =
    [
        "src/FinNavis.Domain",
        "src/FinNavis.Application",
        "src/FinNavis.Infrastructure",
    ];

    public static IReadOnlySet<string> Raised()
    {
        HashSet<string> codes = [];

        foreach (string project in RaisingProjects)
        {
            foreach (string file in Directory.EnumerateFiles(RepositoryRoot.Combine(project), "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsBuildOutput(file)))
            {
                foreach (Match match in CodeLiteral().Matches(File.ReadAllText(file)))
                {
                    codes.Add(match.Groups["code"].Value);
                }
            }
        }

        return codes;
    }

    private static bool IsBuildOutput(string file)
    {
        string[] segments = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Contains("bin") || segments.Contains("obj");
    }

    [GeneratedRegex(@"""(?<code>[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*)""")]
    private static partial Regex CodeLiteral();
}
