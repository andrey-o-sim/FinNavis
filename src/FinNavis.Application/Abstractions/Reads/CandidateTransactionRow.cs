using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public sealed record CandidateTransactionRow(
    Guid Id,
    DateOnly OccurredOn,
    Money Amount,
    Guid AccountId,
    Guid CategoryId,
    CategoryKind CategoryKind,
    string? Note,
    string? ExternalRef);
