using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// An in-memory stand-in for the category-group store. Staged changes become visible only when
/// <see cref="Commit"/> runs, so a use case that refuses before committing leaves nothing behind.
/// </summary>
internal sealed class FakeCategoryGroupRepository : ICategoryGroupRepository
{
    private readonly Dictionary<Guid, CategoryGroup> _stored = [];
    private readonly Dictionary<Guid, string> _committedNames = [];
    private readonly List<CategoryGroup> _staged = [];
    private readonly List<CategoryGroup> _stagedForRemoval = [];

    /// <summary>Every token the use case passed in, so a test can check it was carried through.</summary>
    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<CategoryGroup> Stored => _stored.Values;

    /// <summary>
    /// The names as PostgreSQL would hold them. A rename changes the entity in memory straight
    /// away, exactly as EF Core's change tracking does, so only this view can tell a committed
    /// rename from one that was rolled back.
    /// </summary>
    public IReadOnlyDictionary<Guid, string> CommittedNames => _committedNames;

    public void Seed(params CategoryGroup[] groups)
    {
        foreach (CategoryGroup group in groups)
        {
            _stored[group.Id] = group;
            _committedNames[group.Id] = group.Name;
        }
    }

    public Task<CategoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<CategoryGroup>> ListAsync(CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult<IReadOnlyList<CategoryGroup>>([.. _stored.Values]);
    }

    // Scoped as ux_category_groups_name_kind is: the name and the side together, and the group's
    // own row counts.
    public Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.Values.Any(group => group.Name == name && group.Kind == kind));
    }

    public void Add(CategoryGroup group) => _staged.Add(group);

    public void Remove(CategoryGroup group) => _stagedForRemoval.Add(group);

    public void Commit()
    {
        foreach (CategoryGroup group in _staged)
        {
            _stored[group.Id] = group;
        }

        foreach (CategoryGroup group in _stagedForRemoval)
        {
            _stored.Remove(group.Id);
            _committedNames.Remove(group.Id);
        }

        _staged.Clear();
        _stagedForRemoval.Clear();

        foreach (CategoryGroup group in _stored.Values)
        {
            _committedNames[group.Id] = group.Name;
        }
    }
}
