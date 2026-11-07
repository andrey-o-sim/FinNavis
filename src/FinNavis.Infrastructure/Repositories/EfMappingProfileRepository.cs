using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;

namespace FinNavis.Infrastructure.Repositories;

public sealed class EfMappingProfileRepository : IMappingProfileRepository
{
    public EfMappingProfileRepository(FinNavisDbContext context) => throw new NotImplementedException();

    public Task<MappingProfile?> GetByShapeKeyAsync(string shapeKey, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public void Add(MappingProfile profile) => throw new NotImplementedException();

    public void ReplaceRules(MappingProfile profile, IReadOnlyCollection<MappingRule> rules) =>
        throw new NotImplementedException();
}
