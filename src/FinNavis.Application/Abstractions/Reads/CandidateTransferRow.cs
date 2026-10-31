using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public sealed record CandidateTransferRow(
    Guid Id,
    DateOnly OccurredOn,
    Money Amount,
    Guid SourceAccountId,
    Guid DestinationAccountId);
