using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinNavis.Presentation.Errors;

public sealed record ErrorEnvelope(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, object>? Details = null)
{
    // The keys inside details come from whoever raised the refusal, so they are renamed here
    // rather than trusted: every key in the body is snake_case, the code is the one PascalCase value.
    public static ErrorEnvelope Create(string code, string message, IReadOnlyDictionary<string, object>? details) =>
        new(code, message, details?.ToDictionary(
            entry => JsonNamingPolicy.SnakeCaseLower.ConvertName(entry.Key),
            entry => entry.Value));
}
