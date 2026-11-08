namespace FinNavis.Presentation.Errors;

// ASP.NET Core hands every Problem Details body it wants to write to this service — status code
// pages, Results.Problem, Results.ValidationProblem and the developer exception page. Owning the
// service turns all of them into the one envelope, so no response carries type, title or instance.
public sealed class EnvelopeProblemDetailsService(ErrorEnvelopeWriter writer) : IProblemDetailsService
{
    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        int status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;

        ErrorEnvelope envelope = context.ProblemDetails is HttpValidationProblemDetails { Errors.Count: > 0 } validation
            ? PipelineErrors.ValidationFailed(validation.Errors)
            : PipelineErrors.ForStatus(status);

        await writer.WriteAsync(context.HttpContext, status, envelope);
    }
}
