using FinNavis.Application.Accounts;
using FinNavis.Domain.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

// Feature endpoints do not exist yet, so the test host adds its own routes after the API's
// pipeline. A failure on one of them still travels back through every middleware Program set up.
internal sealed class ProbeRoutes : IStartupFilter
{
    public const string AccountBalances = "/probes/accounts/{0}/balances";

    public const string Numbers = "/probes/numbers";

    public const string Body = "/probes/body";

    public const string ValidationProblem = "/probes/validation-problem";

    public const string Problem = "/probes/problem";

    public const string RefusalWithDetails = "/probes/refusal-with-details";

    public const string Unexpected = "/probes/unexpected";

    public const string UnexpectedMessage = "secret internal detail";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.UseRouting();
        app.UseEndpoints(Map);
    };

    private static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/probes/accounts/{accountId:guid}/balances",
            async (Guid accountId, GetAccountBalances useCase, CancellationToken cancellationToken) =>
                Results.Ok(await useCase.ExecuteAsync(accountId, cancellationToken)));

        endpoints.MapGet(Numbers, (int count) => Results.Ok(count));

        endpoints.MapPost(Body, (ProbeRequest request) => Results.Ok(request));

        endpoints.MapGet(
            ValidationProblem,
            () => Results.ValidationProblem(new Dictionary<string, string[]> { ["DisplayName"] = ["Too long."] }));

        endpoints.MapGet(Problem, () => Results.Problem(statusCode: StatusCodes.Status409Conflict));

        endpoints.MapGet(
            RefusalWithDetails,
            () => ThrowRefusalWithDetails());

        endpoints.MapGet(Unexpected, () => ThrowUnexpected());
    }

    private static IResult ThrowRefusalWithDetails() =>
        throw new DomainRuleViolationException(
            "Account.HasRecordedHistory",
            "Recorded history depends on this account, so it cannot be deleted.",
            new Dictionary<string, object> { ["transactionCount"] = 42, ["transfer_count"] = 3 });

    private static IResult ThrowUnexpected() => throw new InvalidOperationException(UnexpectedMessage);
}
