using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Fakes;

internal sealed class FakeMappingProfileRepository : IMappingProfileRepository
{
    private readonly Dictionary<string, MappingProfile> _stored = new(StringComparer.Ordinal);
    private readonly List<MappingProfile> _staged = [];

    public List<CancellationToken> TokensSeen { get; } = [];

    public IReadOnlyCollection<MappingProfile> Stored => _stored.Values;

    public IReadOnlyCollection<MappingProfile> Staged => _staged;

    public void Seed(params MappingProfile[] profiles)
    {
        foreach (MappingProfile profile in profiles)
        {
            _stored[profile.ShapeKey] = profile;
        }
    }

    public Task<MappingProfile?> GetByShapeKeyAsync(string shapeKey, CancellationToken cancellationToken)
    {
        TokensSeen.Add(cancellationToken);

        return Task.FromResult(_stored.GetValueOrDefault(shapeKey));
    }

    public void Add(MappingProfile profile) => _staged.Add(profile);

    // The real adapter hands the replacement to the tracked profile at once and writes it on commit,
    // so the fake does the same: the entity's own refusal fires here, not at commit.
    public void ReplaceRules(MappingProfile profile, IReadOnlyCollection<MappingRule> rules) =>
        profile.ReplaceRules(rules);

    public void Commit()
    {
        foreach (MappingProfile profile in _staged)
        {
            _stored[profile.ShapeKey] = profile;
        }

        _staged.Clear();
    }
}
