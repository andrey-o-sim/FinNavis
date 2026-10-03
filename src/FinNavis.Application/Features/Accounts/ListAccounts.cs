using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// Returns every account, ordered by name.
/// </summary>
/// <remarks>
/// Listing cannot fail and has no input, so it returns the list directly instead of a
/// <c>Result</c>. An empty list is a normal answer, not a not-found.
/// </remarks>
public sealed class ListAccounts(IAccountRepository accounts)
{
    public async Task<IReadOnlyList<AccountDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var found = await accounts.ListAsync(cancellationToken);

        return found.Select(AccountDto.FromAccount).ToArray();
    }
}
