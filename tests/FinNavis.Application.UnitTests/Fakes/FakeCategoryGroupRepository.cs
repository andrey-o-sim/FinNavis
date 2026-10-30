using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeCategoryGroupRepository : ICategoryGroupRepository
{
    private readonly Dictionary<Guid, CategoryGroup> _stored = [];
    private readonly Dictionary<Guid, string> _committedNames = [];
    private readonly List<CategoryGroup> _staged = [];
    private readonly List<CategoryGroup> _stagedForRemoval = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<CategoryGroup> Stored => _stored.Values;

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
