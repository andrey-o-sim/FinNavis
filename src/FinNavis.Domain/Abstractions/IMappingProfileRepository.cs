using FinNavis.Domain.Entities;

namespace FinNavis.Domain.Abstractions;

public interface IMappingProfileRepository
{
    Task<MappingProfile?> GetByShapeKeyAsync(string shapeKey, CancellationToken cancellationToken);

    void Add(MappingProfile profile);

    void ReplaceRules(MappingProfile profile, IReadOnlyCollection<MappingRule> rules);
}
