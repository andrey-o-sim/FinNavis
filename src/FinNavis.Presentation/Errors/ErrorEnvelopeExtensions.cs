using FinNavis.Presentation.Middleware;

namespace FinNavis.Presentation.Errors;

public static class ErrorEnvelopeExtensions
{
    public static IServiceCollection AddErrorEnvelope(this IServiceCollection services)
    {
        services.AddSingleton<ErrorEnvelopeWriter>();
        services.AddSingleton<IProblemDetailsService, EnvelopeProblemDetailsService>();

        return services;
    }

    public static IApplicationBuilder UseErrorEnvelope(this IApplicationBuilder app)
    {
        app.UseMiddleware<ErrorEnvelopeMiddleware>();

        // A routing 404, a 405 or a binding failure outside Development leaves an empty body.
        // Status code pages fills it through the problem details service, which writes the envelope.
        app.UseStatusCodePages();

        return app;
    }
}
