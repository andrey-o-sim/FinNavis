using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

public sealed class CategoryGroup
{
    public const int MaxNameLength = 100;

    private CategoryGroup(Guid id, string name, CategoryKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Kind = kind;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public CategoryKind Kind { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static CategoryGroup Create(
        Guid id,
        string name,
        CategoryKind kind,
        DateTimeOffset createdAt) =>
        new(id, RequireValidName(name), kind, createdAt);

    public void Rename(string name) => Name = RequireValidName(name);

    private static string RequireValidName(string name)
    {
        // Nullable is a compiler promise, not a runtime one: the name comes from the request body.
        string trimmed = name is null ? string.Empty : name.Trim();

        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainRuleViolationException(
                "CategoryGroup.NameInvalid",
                "A category group name must be between 1 and 100 characters.");
        }

        return trimmed;
    }
}
