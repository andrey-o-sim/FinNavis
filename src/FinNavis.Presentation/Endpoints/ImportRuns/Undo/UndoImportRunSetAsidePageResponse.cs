using FinNavis.Application.Import.GetImportRun;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Undo;

public sealed record UndoImportRunSetAsidePageResponse(
    IReadOnlyList<UndoImportRunSetAsideRowResponse> Items,
    bool HasNext,
    bool HasPrev,
    string? NextCursor,
    string? PrevCursor)
{
    public static UndoImportRunSetAsidePageResponse From(SetAsideRowPage page) =>
        new(
            [.. page.Items.Select(UndoImportRunSetAsideRowResponse.From)],
            page.HasNext,
            page.HasPrev,
            page.NextCursor,
            page.PrevCursor);
}
