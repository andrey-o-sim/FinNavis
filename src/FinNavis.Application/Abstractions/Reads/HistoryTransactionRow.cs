using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

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
