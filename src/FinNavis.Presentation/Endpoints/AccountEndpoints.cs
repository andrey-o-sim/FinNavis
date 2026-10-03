using FinNavis.Application.Features.Accounts;
using FinNavis.Presentation.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FinNavis.Presentation.Endpoints;

/// <summary>
/// HTTP for accounts. Maps a use case result to a status code and nothing else.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/accounts").WithTags("Accounts");

        accounts.MapPost("/", CreateAsync).WithName(nameof(CreateAsync));
        accounts.MapGet("/", ListAsync).WithName(nameof(ListAsync));
        accounts.MapGet("/{id:guid}", GetByIdAsync).WithName(nameof(GetByIdAsync));

        return app;
    }

    private static async Task<Results<Created<AccountResponse>, ValidationProblem>> CreateAsync(
        CreateAccountRequest request,
        CreateAccount useCase,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountCommand(request.Name, request.Type, request.InitialBalance);

        var result = await useCase.ExecuteAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(ToProblemErrors(result.Errors));
        }

        var response = ToResponse(result.Value);

        return TypedResults.Created($"/accounts/{response.Id}", response);
    }

    private static async Task<Ok<IReadOnlyList<AccountResponse>>> ListAsync(
        ListAccounts useCase,
        CancellationToken cancellationToken)
    {
        var found = await useCase.ExecuteAsync(cancellationToken);

        IReadOnlyList<AccountResponse> response = found.Select(ToResponse).ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound>> GetByIdAsync(
        Guid id,
        GetAccountById useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToResponse(result.Value));
    }

    private static AccountResponse ToResponse(AccountDto account)
    {
        return new AccountResponse(
            account.Id,
            account.Name,
            account.Type,
            account.InitialBalance,
            account.Balance,
            account.CreatedAt);
    }

    /// <summary>
    /// Copies the errors into the mutable dictionary TypedResults.ValidationProblem wants.
    /// This one line is what keeps Microsoft.AspNetCore types out of Application.
    /// </summary>
    private static Dictionary<string, string[]> ToProblemErrors(
        IReadOnlyDictionary<string, string[]> errors)
    {
        return errors.ToDictionary(error => error.Key, error => error.Value);
    }
}
