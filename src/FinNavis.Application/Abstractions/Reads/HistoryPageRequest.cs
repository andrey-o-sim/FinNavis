namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// Which page of history to read, and how large it is.
/// </summary>
/// <param name="After">
/// Opaque cursor — the page starts after the movement it points at. A <c>NextCursor</c> handed back
/// unread, never a value the caller built.
/// </param>
/// <param name="Before">
/// Opaque cursor — the page ends before the movement it points at. A <c>PrevCursor</c> handed back
/// unread.
/// </param>
/// <param name="Limit">
/// How many movements the page holds. Between <see cref="MinLimit"/> and <see cref="MaxLimit"/>;
/// <see cref="DefaultLimit"/> when the caller asked for no size.
/// </param>
/// <remarks>
/// <b>A cursor is an opaque string, never a bare id</b>
/// (<c>.claude/rules/openapi.md</c> deviation 3). History is a <c>UNION ALL</c> of
/// <c>transactions</c> and <c>transfers</c> ordered by <c>(occurred_on, id)</c> (ADR-0001), so one
/// id cannot locate a page boundary across the two tables. The server mints the token and is the
/// only side that reads it.
/// <para>
/// Both ends are carried, because the <c>Before</c> direction is unusable without a cursor to go
/// with it.
/// </para>
/// </remarks>
public sealed record HistoryPageRequest(string? After, string? Before, int Limit)
{
    /// <summary>The smallest page the ledger serves.</summary>
    public const int MinLimit = 1;

    /// <summary>
    /// The largest page the ledger serves. Settled at the api-forge gate against the PRD §6 budget
    /// of p95 ≤ 400 ms per page over ~20 000 movements.
    /// </summary>
    public const int MaxLimit = 200;

    /// <summary>
    /// The page size used when the caller asked for none. At ~550 movements a month it pulls a
    /// month in about eleven requests (api-forge gate).
    /// </summary>
    public const int DefaultLimit = 50;
}
