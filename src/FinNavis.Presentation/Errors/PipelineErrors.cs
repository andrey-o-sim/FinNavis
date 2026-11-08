using System.Text.Json;

namespace FinNavis.Presentation.Errors;

// The failures that stop a request before any use case runs. No acceptance criterion words them,
// because the owner never causes one through the product — only a client with a bug does.
public static class PipelineErrors
{
    public static ErrorEnvelope Unreadable { get; } =
        new("Request.Invalid", "The request could not be read.");

    public static ErrorEnvelope Unexpected { get; } =
        new("Server.Unexpected", "The server could not complete the request.");

    public static ErrorEnvelope ForStatus(int status) => status switch
    {
        StatusCodes.Status404NotFound => new("Route.NotFound", "No endpoint answers this path."),
        StatusCodes.Status405MethodNotAllowed =>
            new("Request.MethodNotAllowed", "This endpoint does not accept that HTTP method."),
        StatusCodes.Status415UnsupportedMediaType =>
            new("Request.UnsupportedMediaType", "This endpoint does not accept that content type."),
        >= StatusCodes.Status500InternalServerError => Unexpected,
        _ => Unreadable,
    };

    public static ErrorEnvelope ValidationFailed(IDictionary<string, string[]> errors) =>
        ErrorEnvelope.Create(
            "Request.ValidationFailed",
            "Some fields in the request are not valid.",
            errors.ToDictionary(entry => entry.Key, entry => (object)entry.Value));
}
