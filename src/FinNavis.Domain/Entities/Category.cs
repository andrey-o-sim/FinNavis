using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

/// <summary>
/// A label the owner files a transaction under — "Groceries", "Salary". An aggregate root.
/// </summary>
/// <remarks>
/// A category sits on one side of the ledger and may belong to one <see cref="CategoryGroup"/> or
/// to none (sad.md §12).
/// <para>
/// <b>A category with no group is not in a group called Default.</b> The Default group is a bucket
/// the breakdown produces at read time and never stores (sad.md §12, <c>data-model.md</c>
/// decision 8). <see cref="ClearGroup"/> leaves the link empty and creates nothing.
/// </para>
/// <para>
/// <b>Name uniqueness per side (AC-36) is not here.</b> The entity cannot see its siblings. The
/// check lives in the Application layer, backed by <c>ux_categories_name_kind</c> as the last guard
/// (sad.md §6 flow 13).
/// </para>
/// </remarks>
public sealed class Category
{
    /// <summary>
    /// The longest name the ledger stores. Matches <c>categories.name VARCHAR(100)</c>
    /// (<c>data-model.md</c>). The PRD states no bound — 100 is the working one.
    /// </summary>
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

    /// <summary>
    /// The identity of the category. A UUID v7 minted in the Application layer (ADR-0005).
    /// </summary>
    /// <remarks>
    /// A transaction names its category by this value and never by name or by group. That is what
    /// makes AC-34 and AC-13 free: a rename or a move rewrites no movement row, so a closed
    /// period's figure cannot change.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>The label the owner chose. Trimmed, between 1 and 100 characters.</summary>
    public string Name { get; private set; }

    /// <summary>
    /// The side of the ledger this category sits on. Stored as a string, never as an int
    /// (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// There is no operation to change it. Flipping a recorded category's side would flip the sign
    /// of every movement already filed under it.
    /// </remarks>
    public CategoryKind Kind { get; private set; }

    /// <summary>
    /// The group this category sits in, or <c>null</c> when it sits in none. A group is optional
    /// (sad.md §12).
    /// </summary>
    /// <remarks>
    /// The link is the group's id, not the group object: the breakdown resolves the mapping at
    /// query time (ADR-0002), so nothing here needs the group loaded.
    /// </remarks>
    public Guid? CategoryGroupId { get; private set; }

    /// <summary>When the category was added, in UTC. An audit timestamp, taken from <c>IClock</c>.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a category, with a group or without one (AC-11).
    /// </summary>
    /// <param name="id">A UUID v7 minted by the caller (ADR-0005).</param>
    /// <param name="name">The label. Leading and trailing spaces are dropped.</param>
    /// <param name="kind">The side of the ledger this category sits on.</param>
    /// <param name="group">The group to place it in, or <c>null</c> for no group.</param>
    /// <param name="createdAt">The current instant in UTC, from <c>IClock</c>.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>Category.NameInvalid</c>. Or the group is on the other side of the ledger —
    /// <c>Category.GroupKindMismatch</c> (AC-30).
    /// </exception>
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

    /// <summary>
    /// Gives the category a new name and keeps the same identity (AC-34).
    /// </summary>
    /// <param name="name">The new label. Leading and trailing spaces are dropped.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>Category.NameInvalid</c>.
    /// </exception>
    public void Rename(string name) => Name = RequireValidName(name);

    /// <summary>
    /// Puts the category in a group. The same operation moves it between groups (AC-13) — in both
    /// cases only this row's link changes.
    /// </summary>
    /// <param name="group">The group to sit in. It must be on this category's side (AC-30).</param>
    /// <exception cref="DomainRuleViolationException">
    /// The group is on the other side of the ledger — <c>Category.GroupKindMismatch</c>.
    /// </exception>
    /// <remarks>
    /// <b>The invariant is refused here, not by the schema.</b> <c>data-model.md</c> decision 7
    /// weighed a composite FK and rejected it: it costs a second unique index no query reads, and
    /// it freezes a group's <see cref="CategoryGroup.Kind"/>. Refusing in the entity gives the
    /// owner AC-30's wording instead of a foreign key violation.
    /// </remarks>
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

    /// <summary>
    /// Takes the category out of every group (AC-13, and the domain half of AC-29). Identity, name
    /// and side are unchanged, and all of its recorded history stays.
    /// </summary>
    /// <remarks>
    /// Nothing is assigned here. The breakdown reports a category with no group under the Default
    /// group, which it produces and never stores (sad.md §12).
    /// </remarks>
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
