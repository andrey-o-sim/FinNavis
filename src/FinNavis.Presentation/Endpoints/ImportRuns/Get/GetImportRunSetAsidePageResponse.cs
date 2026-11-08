using FinNavis.Application.Import.GetImportRun;

namespace FinNavis.Presentation.Endpoints.ImportRuns.Get;

public sealed record GetImportRunSetAsidePageResponse(
    IReadOnlyList<GetImportRunSetAsideRowResponse> Items,
    bool HasNext,
    bool HasPrev,
    string? NextCursor,
    string? PrevCursor)
{
    public static GetImportRunSetAsidePageResponse From(SetAsideRowPage page) =>
        new(
            [.. page.Items.Select(GetImportRunSetAsideRowResponse.From)],
            page.HasNext,
            page.HasPrev,
            page.NextCursor,
            page.PrevCursor);
}
