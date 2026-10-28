using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

/// <summary>
/// A transaction as it appears in history. A read row, not a <c>Transaction</c> entity.
/// </summary>
/// <param name="Id">The identity of the entry. A time-ordered UUID v7 (ADR-0005).</param>
/// <param name="OccurredOn">The calendar day the owner filed the entry under.</param>
/// <param name="Amount">How much moved. Always above zero.</param>
/// <param name="AccountId">The one account the money moved on.</param>
/// <param name="AccountName">The account's name, joined in so a caller needs no second read.</param>
/// <param name="CategoryId">The category the entry is filed under.</param>
/// <param name="CategoryName">The category's name, joined in for the same reason.</param>
/// <param name="CategoryKind">
/// Whether the row lowered or raised the balance. <paramref name="Amount"/> is always positive, so
/// direction comes from here (<c>data-model.md</c> decision 3).
/// </param>
/// <param name="Note">What the owner wrote, or <c>null</c>. Shown on every transaction (AC-16).</param>
/// <param name="CreatedAt">When the entry was recorded, in UTC.</param>
public sealed record HistoryTransactionRow(
    Guid Id,
    DateOnly OccurredOn,
    Money Amount,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    CategoryKind CategoryKind,
    string? Note,
    DateTimeOffset CreatedAt) : IHistoryRow;
