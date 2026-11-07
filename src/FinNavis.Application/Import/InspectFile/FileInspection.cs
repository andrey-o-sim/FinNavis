namespace FinNavis.Application.Import.InspectFile;

public sealed record FileInspection(
    string ShapeKey,
    Guid? MappingProfileId,
    IReadOnlyList<string> SourceCategories);
