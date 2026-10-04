using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Features.Accounts;

/// <summary>Looks up one account by id. Returns <c>null</c> when no account has that id.</summary>
public sealed class GetAccountByIdUseCase
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountByIdUseCase(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);

        return account is null ? null : ToDto(account);
    }

    private static AccountDto ToDto(Account account)
    {
        return new AccountDto(account.Id, account.Name, account.Type.ToString(), account.OpeningBalance, account.Note);
    }
}
