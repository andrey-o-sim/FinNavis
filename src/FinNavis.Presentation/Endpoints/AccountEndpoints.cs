using FinNavis.Application.Common;
using FinNavis.Application.Features.Accounts;
using FinNavis.Presentation.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FinNavis.Presentation.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithTags("Accounts");

        group.MapPost("/", CreateAccountAsync);
        group.MapGet("/", ListAccountsAsync);
        group.MapGet("/{id:guid}", GetAccountByIdAsync);
    }

    private static async Task<Results<Created<AccountResponse>, ValidationProblem, ProblemHttpResult>> CreateAccountAsync(
        CreateAccountRequest request,
        CreateAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(request.Name, request.Type, request.OpeningBalance, request.Note);
        var result = await useCase.ExecuteAsync(command, cancellationToken);

        if (result.IsSuccess)
        {
            var response = ToResponse(result.Value);
            return TypedResults.Created($"/accounts/{response.Id}", response);
        }

        var error = result.Error;

        // Switch, not an if with a fallback. A fallback would answer 409 for any ErrorType added
        // later, which is a wrong status code that nothing would catch. This throws at the point
        // of the mistake instead.
        Results<Created<AccountResponse>, ValidationProblem, ProblemHttpResult> failure = error.Type switch
        {
            ErrorType.Validation => TypedResults.ValidationProblem(error.FieldErrors!, title: error.Title),
            ErrorType.Conflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: error.Title,
                detail: error.Detail),
            _ => throw new InvalidOperationException($"Unhandled error type '{error.Type}'."),
        };

        return failure;
    }

    private static async Task<Ok<IReadOnlyList<AccountResponse>>> ListAccountsAsync(
        ListAccountsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var accounts = await useCase.ExecuteAsync(cancellationToken);
        IReadOnlyList<AccountResponse> response = [.. accounts.Select(ToResponse)];

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<AccountResponse>, ProblemHttpResult>> GetAccountByIdAsync(
        Guid id,
        GetAccountByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var account = await useCase.ExecuteAsync(id, cancellationToken);

        if (account is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Account not found",
                detail: $"No account with id '{id}'.");
        }

        return TypedResults.Ok(ToResponse(account));
    }

    private static AccountResponse ToResponse(AccountDto dto)
    {
        return new AccountResponse(dto.Id, dto.Name, dto.Type, dto.OpeningBalance, dto.Note);
    }
}
