using FinNavis.Application.Abstractions;
using FinNavis.Application.Common;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.Features.Accounts;

/// <summary>
/// Creates an account.
/// </summary>
/// <remarks>
/// Every rule is checked before anything is staged, so an invalid command touches neither the
/// repository nor the unit of work. The rules themselves live in <see cref="Account"/>. This
/// class only adds the field names, which are a wire concern.
/// </remarks>
public sealed class CreateAccount(IAccountRepository accounts, IUnitOfWork unitOfWork)
{
    public async Task<Result<AccountDto>> ExecuteAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        var nameError = Account.ValidateName(command.Name);
        if (nameError is not null)
        {
            errors["name"] = [nameError];
        }

        var type = ParseType(command.Type);
        if (type is null)
        {
            errors["type"] = [$"'{command.Type}' is not a known account type. Use Cash, Card or Savings."];
        }

        var balanceError = Account.ValidateInitialBalance(command.InitialBalance);
        if (balanceError is not null)
        {
            errors["initialBalance"] = [balanceError];
        }

        if (errors.Count > 0)
        {
            return Result<AccountDto>.Invalid(errors);
        }

        var account = Account.Create(command.Name, type!.Value, command.InitialBalance, DateTimeOffset.UtcNow);

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AccountDto>.Success(AccountDto.FromAccount(account));
    }

    /// <summary>Returns the type, or <c>null</c> when the text names no known type.</summary>
    private static AccountType? ParseType(string? value)
    {
        // Match the name, do not use Enum.TryParse. TryParse also reads numbers: "7" gives the
        // undefined (AccountType)7, which HasConversion<string>() would store as "7", and "0"
        // silently gives Cash. The client sends a type name, so only a name is accepted.
        if (value is null)
        {
            return null;
        }

        foreach (var name in Enum.GetNames<AccountType>())
        {
            if (string.Equals(name, value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<AccountType>(name);
            }
        }

        return null;
    }
}
