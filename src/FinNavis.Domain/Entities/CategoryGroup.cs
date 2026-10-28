using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.Entities;

/// <summary>
/// A roll-up over several categories — "Utilities" over electricity, water and gas. An aggregate
/// root with a lifecycle of its own (US-09, sad.md §5).
/// </summary>
/// <remarks>
/// <b>A group belongs to one side of the ledger</b>, exactly as a category does (AC-26). Only
/// categories of that same side can sit in it — see <see cref="Category.PlaceInGroup"/>, which owns
/// the refusal.
/// <para>
/// <b>The Default group is not one of these.</b> It is a bucket the breakdown produces for
/// categories with no group, and it is never stored (sad.md §12, <c>data-model.md</c> decision 8).
/// Nothing here creates it.
/// </para>
/// <para>
/// <b>Name uniqueness per side (AC-31) is not here.</b> The entity cannot see its siblings. The
/// check lives in the Application layer, backed by <c>ux_category_groups_name_kind</c> as the last
/// guard (sad.md §6 flow 13).
/// </para>
/// </remarks>
public sealed class CategoryGroup
{
    /// <summary>
    /// The longest name the ledger stores. Matches <c>category_groups.name VARCHAR(100)</c>
    /// (<c>data-model.md</c>). The PRD states no bound — 100 is the working one.
    /// </summary>
    public const int MaxNameLength = 100;

    private CategoryGroup(Guid id, string name, CategoryKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Kind = kind;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// The identity of the group. A UUID v7 minted in the Application layer (ADR-0005).
    /// </summary>
    /// <remarks>
    /// A category points at the group by this value and never by name, which is what makes AC-28
    /// free: a rename rewrites no category row and no transaction row.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>The label the owner chose. Trimmed, between 1 and 100 characters.</summary>
    public string Name { get; private set; }

    /// <summary>
    /// The side of the ledger the group belongs to (AC-26). Stored as a string, never as an int
    /// (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// There is no operation to change it. <c>data-model.md</c> decision 7 rejected a composite FK
    /// partly because it would freeze this value; the entity does not offer the change either,
    /// because moving a group to the other side would leave every category in it on the wrong one.
    /// </remarks>
    public CategoryKind Kind { get; private set; }

    /// <summary>When the group was added, in UTC. An audit timestamp, taken from <c>IClock</c>.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a group on one side of the ledger. It holds no categories yet (AC-26).
    /// </summary>
    /// <param name="id">A UUID v7 minted by the caller (ADR-0005).</param>
    /// <param name="name">The label. Leading and trailing spaces are dropped.</param>
    /// <param name="kind">The side of the ledger this group belongs to.</param>
    /// <param name="createdAt">The current instant in UTC, from <c>IClock</c>.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>CategoryGroup.NameInvalid</c>.
    /// </exception>
    public static CategoryGroup Create(
        Guid id,
        string name,
        CategoryKind kind,
        DateTimeOffset createdAt) =>
        new(id, RequireValidName(name), kind, createdAt);

    /// <summary>
    /// Gives the group a new name and keeps the same identity and the same side (AC-28).
    /// </summary>
    /// <param name="name">The new label. Leading and trailing spaces are dropped.</param>
    /// <exception cref="DomainRuleViolationException">
    /// The name is blank or longer than <see cref="MaxNameLength"/> characters —
    /// <c>CategoryGroup.NameInvalid</c>.
    /// </exception>
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
