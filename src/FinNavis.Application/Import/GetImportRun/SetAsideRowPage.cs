namespace FinNavis.Application.Import.GetImportRun;

public sealed record SetAsideRowPage(
    IReadOnlyList<ReportedSetAsideRow> Items,
    bool HasNext,
    bool HasPrev,
    string? NextCursor,
    string? PrevCursor);
