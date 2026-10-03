using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// Returns one account by id.
/// </summary>
public sealed class GetAccountById(IAccountRepository accounts)
{
    public async Task<Result<AccountDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(id, cancellationToken);

        if (account is null)
        {
            return Result<AccountDto>.NotFound();
        }

        return Result<AccountDto>.Success(AccountDto.FromAccount(account));
    }
}
