using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Exceptions;

namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// Creates an account. Validation beats conflict: a bad name or type never returns 409.
/// </summary>
public sealed class CreateAccountUseCase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateAccountUseCase(IAccountRepository accountRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AccountDto>> ExecuteAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        // Every broken rule is reported at once, type included. Returning the type error on its
        // own would make the caller fix it, resubmit, and only then discover the name is missing —
        // the retry loop Account.Create exists to avoid.
        var errors = new Dictionary<string, string[]>();

        if (!TryParseType(command.Type, out var type))
        {
            errors["type"] = ["Type must be one of Cash, Card, Savings."];
        }

        Account? account = null;
        try
        {
            // When the type is bad this still runs, on the default type, purely to collect the
            // name and note errors. The entity it would build is discarded below.
            account = Account.Create(command.Name, type, command.OpeningBalance, command.Note, _clock);
        }
        catch (DomainValidationException ex)
        {
            foreach (var (field, messages) in ex.Errors)
            {
                errors[field] = messages;
            }
        }

        // The null check is the same condition: Account.Create only returns null-shaped here by
        // throwing, and that always adds at least one error. It is spelled out so the compiler can
        // narrow the type without a suppression.
        if (errors.Count > 0 || account is null)
        {
            return Result<AccountDto>.Failure(Error.Validation(errors));
        }

        if (await _accountRepository.ExistsWithNormalizedNameAsync(account.NormalizedName, cancellationToken))
        {
            return Result<AccountDto>.Failure(Error.Conflict($"An account named '{account.Name}' already exists."));
        }

        _accountRepository.Add(account);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<AccountDto>.Failure(Error.Conflict($"An account named '{account.Name}' already exists."));
        }

        return Result<AccountDto>.Success(ToDto(account));
    }

    private static bool TryParseType(string? value, out AccountType type)
    {
        // Do not use Enum.TryParse here. On untrusted input it accepts far more than the three
        // names: the underlying number ("0" is Cash), a number with surrounding spaces or a sign
        // (" 1", "+0"), and a comma-separated list ("Cash,Card"). Enum.IsDefined does not help,
        // because all of those land on a defined value. Match the literal names instead, so the
        // accepted set is exactly Cash/Card/Savings in any letter case.
        foreach (var name in Enum.GetNames<AccountType>())
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                type = Enum.Parse<AccountType>(name);
                return true;
            }
        }

        type = default;
        return false;
    }

    private static AccountDto ToDto(Account account)
    {
        return new AccountDto(account.Id, account.Name, account.Type.ToString(), account.OpeningBalance, account.Note);
    }
}
