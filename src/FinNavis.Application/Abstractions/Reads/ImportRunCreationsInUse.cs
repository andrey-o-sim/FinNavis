namespace FinNavis.Application.Abstractions.Reads;

public sealed record ImportRunCreationsInUse(
    IReadOnlyCollection<Guid> AccountIds,
    IReadOnlyCollection<Guid> CategoryIds);
