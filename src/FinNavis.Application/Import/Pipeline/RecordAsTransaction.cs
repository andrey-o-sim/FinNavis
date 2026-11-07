using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Import.Pipeline;

public sealed record RecordAsTransaction(
    Guid AccountId,
    Guid CategoryId,
    Money Amount,
    DateOnly OccurredOn,
    string? Note) : RowDecision;
