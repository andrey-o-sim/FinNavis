using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FinNavis.Presentation.Middleware;

/// <summary>
/// Turns any exception that escapes an endpoint into a ProblemDetails response, so no endpoint
/// can leak an HTML error page. Writes through <see cref="IProblemDetailsService"/> so the body
/// and the <c>application/problem+json</c> media type match the 400, 404, and 409 responses the
/// endpoints produce themselves.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A body we cannot read is the caller's mistake, not a server fault. Minimal APIs throw
        // BadHttpRequestException, carrying its own status code, when binding fails — for example
        // when openingBalance arrives as "abc". Forcing 500 here would blame us for a typo.
        var statusCode = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : "The request body could not be read.",
            },
        });
    }
}
