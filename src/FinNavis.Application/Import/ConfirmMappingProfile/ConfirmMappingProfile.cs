using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Import.ConfirmMappingProfile;

public sealed class ConfirmMappingProfile
{
    public ConfirmMappingProfile(
        ICategoryRepository categories,
        IMappingProfileRepository mappingProfiles,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        throw new NotImplementedException();
    }

    public Task<MappingProfileConfirmation> ExecuteAsync(
        string shapeKey,
        IReadOnlyList<RuleToConfirm> rules,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
