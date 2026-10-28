namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// One page of history, ordered by <c>(occurred_on, id)</c>.
/// </summary>
/// <param name="Items">
/// The movements on this page, each one a transaction row or a transfer row. Empty when the period
/// recorded nothing — an empty page is a normal answer, never a failure (AC-17).
/// </param>
/// <param name="HasNext">True when a further page follows this one.</param>
/// <param name="HasPrev">True when a page comes before this one.</param>
/// <param name="NextCursor">
/// Opaque token to hand back as <c>After</c> for the next page. <c>null</c> on the last page.
/// </param>
/// <param name="PrevCursor">
/// Opaque token to hand back as <c>Before</c> for the previous page. <c>null</c> on the first page.
/// </param>
/// <remarks>
/// No total count is carried. Counting the whole of history on every page would cost a second scan
/// of both tables and nothing in the PRD reads the figure.
/// </remarks>
public sealed record HistoryPage(
    IReadOnlyList<IHistoryRow> Items,
    bool HasNext,
    bool HasPrev,
    string? NextCursor,
    string? PrevCursor);
