using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public sealed record HistoryTransferRow(
    Guid Id,
    DateOnly OccurredOn,
    Money Amount,
    Guid SourceAccountId,
    string SourceAccountName,
    Guid DestinationAccountId,
    string DestinationAccountName,
    DateTimeOffset CreatedAt) : IHistoryRow;
