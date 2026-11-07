using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.ConfirmMappingProfile;

public sealed record MappingProfileConfirmation(MappingProfile Profile, bool IsNewShape);
