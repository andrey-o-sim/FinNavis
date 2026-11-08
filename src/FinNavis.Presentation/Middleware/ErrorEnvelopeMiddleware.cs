using FinNavis.Domain.Errors;
using FinNavis.Presentation.Errors;

namespace FinNavis.Presentation.Middleware;

public sealed class ErrorEnvelopeMiddleware(
    RequestDelegate next,
    ErrorEnvelopeWriter writer,
    ILogger<ErrorEnvelopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            (int status, ErrorEnvelope envelope) = Translate(exception);

            context.Response.Clear();
            await writer.WriteAsync(context, status, envelope);
        }
    }

    private (int Status, ErrorEnvelope Envelope) Translate(Exception exception)
    {
        switch (exception)
        {
            case DomainRuleViolationException refusal when ErrorStatusCodes.TryGetStatus(refusal.Code, out int status):
                return (status, ErrorEnvelope.Create(refusal.Code, refusal.Message, refusal.Details));

            case DomainRuleViolationException refusal:
                logger.LogError(exception, "Refusal code {Code} has no HTTP status in ErrorStatusCodes", refusal.Code);
                return (StatusCodes.Status500InternalServerError, PipelineErrors.Unexpected);

            case BadHttpRequestException badRequest:
                return (badRequest.StatusCode, PipelineErrors.ForStatus(badRequest.StatusCode));

            default:
                logger.LogError(exception, "Unhandled exception while serving the request");
                return (StatusCodes.Status500InternalServerError, PipelineErrors.Unexpected);
        }
    }
}
