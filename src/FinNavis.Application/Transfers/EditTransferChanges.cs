namespace FinNavis.Application.Transfers;

public sealed record EditTransferChanges(
    Guid? SourceAccountId = null,
    Guid? DestinationAccountId = null,
    decimal? Amount = null,
    DateOnly? OccurredOn = null,
    Guid? CategoryId = null);
