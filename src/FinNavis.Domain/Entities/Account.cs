using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

/// <summary>
/// A place the owner keeps money — a card, a cash wallet, a savings jar. An aggregate root.
/// </summary>
/// <remarks>
/// The account is a label the owner chooses, not a bank identifier (PRD §6.1).
/// <para>
/// <b>There is no balance field.</b> AC-14 sums the movements at read time (ADR-0002), so a stored
/// balance would have nothing to keep it honest and AC-21 and AC-22 would need a correction step.
/// </para>
/// <para>
/// <b>Name uniqueness (AC-35) is not here.</b> The entity cannot see other accounts. The check
/// lives in the Application layer, backed by the unique index as the last guard (sad.md §6
/// flow 13).
/// </para>
/// </remarks>
public sealed class Account
{
    /// <summary>
    /// The longest name the ledger stores. Matches <c>accounts.name VARCHAR(100)</c>
    /// (<c>data-model.md</c>). The PRD states no bound — 100 is the working one.
    /// </summary>
    public const int MaxNameLength = 100;

    private Account(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// The identity of the account. A UUID v7 minted in the Application layer (ADR-0005).
    /// </summary>
    /// <remarks>
    /// Every movement points at the account by this value and never by name, which is what makes
    /// AC-08 free: a rename rewrites nothing.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>The label the owner chose. Trimmed, between 1 and 100 characters.</summary>
    public string Name { get; private set; }

    /// <summary>When the account was added, in UTC. An audit timestamp, taken from <c>IClock</c>.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Opens an account under a name the owner chose.
    /// </summary>
    /// <param name="id">A UUID v7 minted by the caller (ADR-0005).</param>
    /// <param name="name">The label. Leading and trailing spaces are dropped.</param>
    /// <param name="createdAt">The current instant in UTC, from <c>IClock</c>.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>Account.NameInvalid</c>.
    /// </exception>
    public static Account Create(Guid id, string name, DateTimeOffset createdAt) =>
        new(id, RequireValidName(name), createdAt);

    /// <summary>
    /// Gives the account a new name and keeps the same identity (AC-08).
    /// </summary>
    /// <param name="name">The new label. Leading and trailing spaces are dropped.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>Account.NameInvalid</c>.
    /// </exception>
    public void Rename(string name) => Name = RequireValidName(name);

    private static string RequireValidName(string name)
    {
        // Nullable is a compiler promise, not a runtime one: the name comes from the request body.
        string trimmed = name is null ? string.Empty : name.Trim();

        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainRuleViolationException(
                "Account.NameInvalid",
                "An account name must be between 1 and 100 characters.");
        }

        return trimmed;
    }
}
