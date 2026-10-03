using FinNavis.Domain.Enums;

namespace FinNavis.Domain.Entities;

/// <summary>
/// An account that holds money: a wallet, a card, a savings account.
/// </summary>
/// <remarks>
/// The rules live here and nowhere else. <see cref="Create"/> enforces them by throwing,
/// which is a guard against a programmer error. A use case calls <see cref="ValidateName"/>
/// and <see cref="ValidateInitialBalance"/> first, so bad user input becomes a 400 instead
/// of an exception. Both paths read the same rule and the same wording.
/// </remarks>
public sealed class Account
{
    /// <summary>Longest account name we accept. The database column uses the same number.</summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Largest initial balance we accept, in either direction. Mirrors the numeric(14,2)
    /// column, so an absurd amount is a 400 and not a failed INSERT.
    /// </summary>
    public const decimal MaxAbsoluteBalance = 999_999_999_999.99m;

    private const int BalanceDecimalPlaces = 2;

    // EF Core binds this constructor by parameter name, so there is no parameterless
    // constructor and no need to silence CS8618.
    private Account(Guid id, string name, AccountType type, decimal initialBalance, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Type = type;
        InitialBalance = initialBalance;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public AccountType Type { get; private set; }

    /// <summary>
    /// The balance the account started with. May be negative: a credit card is overdrawn
    /// by design.
    /// </summary>
    public decimal InitialBalance { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates an account. The only place an account id is generated.
    /// </summary>
    /// <exception cref="ArgumentException">A value breaks an account rule.</exception>
    public static Account Create(string name, AccountType type, decimal initialBalance, DateTimeOffset createdAt)
    {
        var nameError = ValidateName(name);
        if (nameError is not null)
        {
            throw new ArgumentException(nameError, nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException($"'{type}' is not a known account type.", nameof(type));
        }

        var balanceError = ValidateInitialBalance(initialBalance);
        if (balanceError is not null)
        {
            throw new ArgumentException(balanceError, nameof(initialBalance));
        }

        return new Account(Guid.CreateVersion7(), name.Trim(), type, initialBalance, createdAt);
    }

    /// <summary>
    /// Checks the account name rule. Returns <c>null</c> when the name is fine,
    /// otherwise the message to show the user.
    /// </summary>
    public static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return $"Name must be {MaxNameLength} characters or fewer.";
        }

        return null;
    }

    /// <summary>
    /// Checks the initial balance rule. Returns <c>null</c> when the value is fine,
    /// otherwise the message to show the user. A negative value is allowed.
    /// </summary>
    public static string? ValidateInitialBalance(decimal value)
    {
        if (decimal.Round(value, BalanceDecimalPlaces) != value)
        {
            return $"Initial balance must have {BalanceDecimalPlaces} decimal places or fewer.";
        }

        if (Math.Abs(value) > MaxAbsoluteBalance)
        {
            return $"Initial balance must be between -{MaxAbsoluteBalance} and {MaxAbsoluteBalance}.";
        }

        return null;
    }

    /// <summary>
    /// The one place the balance formula lives.
    /// </summary>
    /// <param name="transactionsTotal">
    /// Income minus expenses for this account, already summed. It is passed in rather than
    /// computed here so that, once transactions exist, Infrastructure can produce the sum
    /// with a SQL aggregate and still go through this method.
    /// </param>
    public decimal CalculateBalance(decimal transactionsTotal)
    {
        return InitialBalance + transactionsTotal;
    }
}
