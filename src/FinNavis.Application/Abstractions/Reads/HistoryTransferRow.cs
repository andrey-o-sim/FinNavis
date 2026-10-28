using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// A transfer as it appears in history. A read row, not a <c>Transfer</c> entity.
/// </summary>
/// <param name="Id">The identity of the entry. A time-ordered UUID v7 (ADR-0005).</param>
/// <param name="OccurredOn">The calendar day the owner filed the entry under.</param>
/// <param name="Amount">
/// One amount for both sides: the source falls and the destination rises by it (AC-04).
/// </param>
/// <param name="SourceAccountId">The account the money left.</param>
/// <param name="SourceAccountName">That account's name, joined in so a caller needs no second read.</param>
/// <param name="DestinationAccountId">The account the money arrived in.</param>
/// <param name="DestinationAccountName">That account's name, joined in for the same reason.</param>
/// <param name="CreatedAt">When the entry was recorded, in UTC.</param>
/// <remarks>
/// <b>No category and no note.</b> Neither exists on the entity and neither column exists on the
/// table (ADR-0001, AC-23), so a category filter can never match a transfer.
/// </remarks>
public sealed record HistoryTransferRow(
    Guid Id,
    DateOnly OccurredOn,
    Money Amount,
    Guid SourceAccountId,
    string SourceAccountName,
    Guid DestinationAccountId,
    string DestinationAccountName,
    DateTimeOffset CreatedAt) : IHistoryRow;
