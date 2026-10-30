using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<Guid, Category> _stored = [];
    private readonly Dictionary<Guid, Guid?> _committedGroupLinks = [];
    private readonly List<Category> _staged = [];
    private readonly List<Category> _stagedForRemoval = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<Category> Stored => _stored.Values;

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
