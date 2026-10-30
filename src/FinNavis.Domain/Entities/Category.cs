using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

public sealed class Category
{
    public const int MaxNameLength = 100;

    private Category(
        Guid id,
        string name,
        CategoryKind kind,
        Guid? categoryGroupId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Kind = kind;
        CategoryGroupId = categoryGroupId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public CategoryKind Kind { get; private set; }

    public Guid? CategoryGroupId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Category Create(
        Guid id,
        string name,
        CategoryKind kind,
        CategoryGroup? group,
        DateTimeOffset createdAt)
    {
        Category category = new(id, RequireValidName(name), kind, categoryGroupId: null, createdAt);

        if (group is not null)
        {
            category.PlaceInGroup(group);
        }

        return category;
    }

    public void Rename(string name) => Name = RequireValidName(name);

    public void PlaceInGroup(CategoryGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (group.Kind != Kind)
        {
            // "An expense" and "An income" both read correctly, so the article never varies.
            throw new DomainRuleViolationException(
                "Category.GroupKindMismatch",
                $"An {Label(Kind)} category cannot sit in an {Label(group.Kind)} category group.");
        }

        CategoryGroupId = group.Id;
    }

    public void ClearGroup() => CategoryGroupId = null;

    private static string Label(CategoryKind kind) => kind.ToString().ToLowerInvariant();

    private static string RequireValidName(string name)
    {
        // Nullable is a compiler promise, not a runtime one: the name comes from the request body.
        string trimmed = name is null ? string.Empty : name.Trim();

        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainRuleViolationException(
                "Category.NameInvalid",
                "A category name must be between 1 and 100 characters.");
        }

        return trimmed;
    }
}
