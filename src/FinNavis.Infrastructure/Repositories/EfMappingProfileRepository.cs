using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.Repositories;

public sealed class EfMappingProfileRepository(FinNavisDbContext context) : IMappingProfileRepository
{
    // Tracked, because confirming a shape again replaces the rules of the profile this returns.
    public Task<MappingProfile?> GetByShapeKeyAsync(string shapeKey, CancellationToken cancellationToken) =>
        context.MappingProfiles
            .Include(profile => profile.Rules)
            .FirstOrDefaultAsync(profile => profile.ShapeKey == shapeKey, cancellationToken);

    public void Add(MappingProfile profile) => context.MappingProfiles.Add(profile);

    // The new rules carry ids minted by the caller, so they are added explicitly: left to change
    // detection, a rule with a set key could be taken for an existing row and updated instead.
    public void ReplaceRules(MappingProfile profile, IReadOnlyCollection<MappingRule> rules)
    {
        MappingRule[] oldRules = [.. profile.Rules];

        profile.ReplaceRules(rules);

        context.MappingRules.RemoveRange(oldRules);
        context.MappingRules.AddRange(rules);
    }
}
