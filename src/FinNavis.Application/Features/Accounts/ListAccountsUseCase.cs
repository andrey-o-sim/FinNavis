using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Features.Accounts;

/// <summary>Lists every account, ordered by name.</summary>
public sealed class ListAccountsUseCase
{
    private readonly IAccountRepository _accountRepository;

    public ListAccountsUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<IReadOnlyList<AccountDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var accounts = await _accountRepository.ListOrderedByNameAsync(cancellationToken);

        return [.. accounts.Select(ToDto)];
    }

    private static AccountDto ToDto(Account account)
    {
        return new AccountDto(account.Id, account.Name, account.Type.ToString(), account.OpeningBalance, account.Note);
    }
}
