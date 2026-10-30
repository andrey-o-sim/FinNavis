namespace FinNavis.Application.Abstractions.Reads;

public sealed record HistoryFilter(
    DateOnly? From,
    DateOnly? To,
    Guid? AccountId,
    Guid? CategoryId);
