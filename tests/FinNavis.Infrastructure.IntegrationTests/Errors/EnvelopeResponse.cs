using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

internal static partial class EnvelopeResponse
{
    private static readonly string[] ProblemDetailsMembers = ["type", "title", "instance"];

    // Asserts the shape every failure must have, and hands the body back for the case-specific checks.
    public static async Task<JsonElement> ShouldBeEnvelopeAsync(this HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.Should().Be(status);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        string body = await response.Content.ReadAsStringAsync();
        JsonElement root = JsonDocument.Parse(body).RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("code").GetString().Should().MatchRegex(ErrorCodePattern());
        root.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        root.EnumerateObject().Select(property => property.Name)
            .Should().BeSubsetOf(["code", "message", "details"])
            .And.NotContain(ProblemDetailsMembers);
        KeysIn(root).Should().AllSatisfy(key => key.Should().MatchRegex(SnakeCaseKey()));

        return root;
    }

    private static IEnumerable<string> KeysIn(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .SelectMany(property => KeysIn(property.Value).Prepend(property.Name)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(KeysIn),
        _ => [],
    };

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$")]
    private static partial Regex ErrorCodePattern();

    [GeneratedRegex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$")]
    private static partial Regex SnakeCaseKey();
}
