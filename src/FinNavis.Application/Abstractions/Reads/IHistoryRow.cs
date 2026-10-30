using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public interface IHistoryRow
{
    Guid Id { get; }

    DateOnly OccurredOn { get; }

    Money Amount { get; }

    DateTimeOffset CreatedAt { get; }
}
