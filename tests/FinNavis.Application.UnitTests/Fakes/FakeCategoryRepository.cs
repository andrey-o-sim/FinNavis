using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Fakes;

/// <summary>
/// An in-memory stand-in for the category store. Staged changes become visible only when
/// <see cref="Commit"/> runs, so a use case that refuses before committing leaves nothing behind.
/// </summary>
internal sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<Guid, Category> _stored = [];
    private readonly Dictionary<Guid, Guid?> _committedGroupLinks = [];
    private readonly List<Category> _staged = [];
    private readonly List<Category> _stagedForRemoval = [];

    /// <summary>Every token the use case passed in, so a test can check it was carried through.</summary>
    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<Category> Stored => _stored.Values;

    /// <summary>
    /// The group links as PostgreSQL would hold them. Clearing a link changes the entity in memory
    /// straight away, exactly as EF Core's change tracking does, so only this view can tell a
    /// committed unlink from one that was rolled back.
    /// </summary>
    public IReadOnlyDictionary<Guid, Guid?> CommittedGroupLinks => _committedGroupLinks;

    public void Seed(params Category[] categories)
    {
        foreach (Category category in categories)
        {
            _stored[category.Id] = category;
            _committedGroupLinks[category.Id] = category.CategoryGroupId;
        }
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult<IReadOnlyList<Category>>([.. _stored.Values]);
    }

    public Task<IReadOnlyList<Category>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult<IReadOnlyList<Category>>(
            [.. _stored.Values.Where(category => category.CategoryGroupId == groupId)]);
    }

    // Scoped as ux_categories_name_kind is: the name and the side together, and the category's own
    // row counts.
    public Task<bool> ExistsWithNameAsync(string name, CategoryKind kind, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(
            _stored.Values.Any(category => category.Name == name && category.Kind == kind));
    }

    public void Add(Category category) => _staged.Add(category);

    public void Remove(Category category) => _stagedForRemoval.Add(category);

    public void Commit()
    {
        foreach (Category category in _staged)
        {
            _stored[category.Id] = category;
        }

        foreach (Category category in _stagedForRemoval)
        {
            _stored.Remove(category.Id);
            _committedGroupLinks.Remove(category.Id);
        }

        _staged.Clear();
        _stagedForRemoval.Clear();

        foreach (Category category in _stored.Values)
        {
            _committedGroupLinks[category.Id] = category.CategoryGroupId;
        }
    }
}
