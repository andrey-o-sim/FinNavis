namespace FinNavis.Application.Abstractions.Reads;

public sealed record HistoryPage(
    IReadOnlyList<IHistoryRow> Items,
    bool HasNext,
    bool HasPrev,
    string? NextCursor,
    string? PrevCursor);
