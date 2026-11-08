using FinNavis.Application.Import.InspectFile;

namespace FinNavis.Presentation.Endpoints.MappingProfiles.Inspect;

public sealed record FileInspectionResponse(
    string ShapeKey,
    Guid? MappingProfileId,
    IReadOnlyList<string> SourceCategories)
{
    public static FileInspectionResponse From(FileInspection inspection) =>
        new(inspection.ShapeKey, inspection.MappingProfileId, inspection.SourceCategories);
}
