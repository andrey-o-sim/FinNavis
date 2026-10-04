using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Exceptions;

namespace FinNavis.Domain.Entities;

/// <summary>
/// Money kept in one place: cash, a card, or savings. The first aggregate in FinNavis.
/// </summary>
public sealed class Account
{
    /// <summary>Money is kept to two decimal places. The store column is numeric(18, 2).</summary>
    private const int MoneyScale = 2;

    // The widest value numeric(18, 2) can hold: 18 significant digits, 2 of them after the point.
    // Past this PostgreSQL raises "numeric field overflow", which would reach the caller as a 500
    // for what is plainly bad input, so the rule is enforced here instead.
    private const decimal MaxOpeningBalance = 9_999_999_999_999_999.99m;
    private const decimal MinOpeningBalance = -9_999_999_999_999_999.99m;

    // For EF Core only. Domain code must use Create.
    private Account()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// <see cref="Name"/>, trimmed and lower-cased. The single definition of "two names are the
    /// same" — used for the duplicate check and for sorting the list.
    /// </summary>
    public string NormalizedName { get; private set; }

    public AccountType Type { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new account, enforcing every rule at once. Throws
    /// <see cref="DomainValidationException"/> carrying every broken rule when the input is
    /// invalid, so the caller never has to fix one error, retry, and find the next one.
    /// </summary>
    public static Account Create(string? name, AccountType type, decimal openingBalance, string? note, IClock clock)
    {
        var errors = new Dictionary<string, string[]>();

        var trimmedName = name?.Trim() ?? string.Empty;
        var nameErrors = new List<string>();
        if (string.IsNullOrEmpty(trimmedName))
        {
            nameErrors.Add("Name is required.");
        }
        else if (trimmedName.Length > 100)
        {
            nameErrors.Add("Name must be 100 characters or fewer.");
        }

        if (nameErrors.Count > 0)
        {
            errors["name"] = [.. nameErrors];
        }

        var trimmedNote = note?.Trim();
        var normalizedNote = string.IsNullOrEmpty(trimmedNote) ? null : trimmedNote;
        if (normalizedNote is not null && normalizedNote.Length > 500)
        {
            errors["note"] = ["Note must be 500 characters or fewer."];
        }

        // Round here, to the scale the column uses, so the entity holds exactly what the store
        // will hold. Without this the create response reports the value the caller sent while the
        // database keeps a rounded one, and the two disagree from the first read onwards.
        // AwayFromZero, not the .NET default of ToEven, because that is how PostgreSQL rounds.
        var roundedBalance = decimal.Round(openingBalance, MoneyScale, MidpointRounding.AwayFromZero);
        if (roundedBalance < MinOpeningBalance || roundedBalance > MaxOpeningBalance)
        {
            errors["openingBalance"] =
                [$"Opening balance must be between {MinOpeningBalance} and {MaxOpeningBalance}."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return new Account
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            NormalizedName = Normalize(trimmedName),
            Type = type,
            OpeningBalance = roundedBalance,
            Note = normalizedNote,
            CreatedAt = clock.UtcNow,
        };
    }

    /// <summary>The single implementation of "the same name", ignoring case and surrounding spaces.</summary>
    private static string Normalize(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
