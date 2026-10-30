namespace FinNavis.Application.Transactions;

public sealed record EditTransactionChanges(
    Guid? AccountId = null,
    Guid? SecondAccountId = null,
    Guid? CategoryId = null,
    decimal? Amount = null,
    DateOnly? OccurredOn = null,
    NoteChange Note = default);
