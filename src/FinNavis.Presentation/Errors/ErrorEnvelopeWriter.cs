using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace FinNavis.Presentation.Errors;

public sealed class ErrorEnvelopeWriter(IOptions<JsonOptions> jsonOptions)
{
    private const string ContentType = "application/json";

    public Task WriteAsync(HttpContext context, int status, ErrorEnvelope envelope)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(
            envelope,
            jsonOptions.Value.SerializerOptions,
            ContentType,
            context.RequestAborted);
    }
}
