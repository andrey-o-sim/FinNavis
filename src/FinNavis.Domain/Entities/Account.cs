using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

public sealed class Account
{
    public const int MaxNameLength = 100;

    private Account(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Account Create(Guid id, string name, DateTimeOffset createdAt) =>
        new(id, RequireValidName(name), createdAt);

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
