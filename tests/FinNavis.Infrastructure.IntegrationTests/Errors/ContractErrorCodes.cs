using System.Text.RegularExpressions;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

// Reads the error codes out of an OpenAPI contract line by line. The test project has no YAML
// parser, and the contract keeps one fixed shape: a code sits either inline under a status key in
// paths, or in a shared response that a status key points at with $ref.
internal static partial class ContractErrorCodes
{
    public static IReadOnlyDictionary<string, IReadOnlySet<int>> StatusesByCode(string contractPath)
    {
        string? section = null;
        int? currentStatus = null;
        string? currentSharedResponse = null;

        Dictionary<string, HashSet<int>> statusesByCode = [];
        Dictionary<string, List<string>> codesBySharedResponse = [];
        List<(int Status, string Response)> sharedResponseReferences = [];

        foreach (string line in File.ReadLines(contractPath))
        {
            if (TopLevelSection().Match(line) is { Success: true } top)
            {
                section = top.Groups["name"].Value;
                continue;
            }

            if (ComponentsSection().Match(line) is { Success: true } component)
            {
                section = component.Groups["name"].Value;
                continue;
            }

            switch (section)
            {
                case "paths":
                    if (StatusKey().Match(line) is { Success: true } status)
                    {
                        currentStatus = int.Parse(status.Groups["status"].Value);
                    }
                    else if (SharedResponseReference().Match(line) is { Success: true } reference && currentStatus is int referencingStatus)
                    {
                        sharedResponseReferences.Add((referencingStatus, reference.Groups["name"].Value));
                    }
                    else if (CodeValue().Match(line) is { Success: true } code && currentStatus is int codeStatus)
                    {
                        Add(statusesByCode, code.Groups["code"].Value, codeStatus);
                    }

                    break;

                case "responses":
                    if (SharedResponseName().Match(line) is { Success: true } name)
                    {
                        currentSharedResponse = name.Groups["name"].Value;
                        codesBySharedResponse[currentSharedResponse] = [];
                    }
                    else if (CodeValue().Match(line) is { Success: true } code && currentSharedResponse is not null)
                    {
                        codesBySharedResponse[currentSharedResponse].Add(code.Groups["code"].Value);
                    }

                    break;
            }
        }

        foreach ((int status, string response) in sharedResponseReferences)
        {
            foreach (string code in codesBySharedResponse[response])
            {
                Add(statusesByCode, code, status);
            }
        }

        return statusesByCode.ToDictionary(entry => entry.Key, entry => (IReadOnlySet<int>)entry.Value);
    }

    private static void Add(Dictionary<string, HashSet<int>> statusesByCode, string code, int status)
    {
        if (!statusesByCode.TryGetValue(code, out HashSet<int>? statuses))
        {
            statuses = [];
            statusesByCode[code] = statuses;
        }

        statuses.Add(status);
    }

    [GeneratedRegex(@"^(?<name>[a-z]+):")]
    private static partial Regex TopLevelSection();

    [GeneratedRegex(@"^  (?<name>[a-z]+):\s*$")]
    private static partial Regex ComponentsSection();

    [GeneratedRegex(@"^\s+""(?<status>\d{3})"":")]
    private static partial Regex StatusKey();

    [GeneratedRegex(@"\$ref:\s*""#/components/responses/(?<name>\w+)""")]
    private static partial Regex SharedResponseReference();

    [GeneratedRegex(@"^    (?<name>\w+):\s*$")]
    private static partial Regex SharedResponseName();

    [GeneratedRegex(@"^\s+code:\s*""(?<code>[^""]+)""")]
    private static partial Regex CodeValue();
}
