namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

internal sealed record SeededLedger(
    IReadOnlyList<SeededAccount> Accounts,
    IReadOnlyList<Guid> ExpenseCategoryIds,
    IReadOnlyList<Guid> IncomeCategoryIds,
    int TransactionCount,
    int TransferCount)
{
    public int MovementCount => TransactionCount + TransferCount;

    public int CategoryCount => ExpenseCategoryIds.Count + IncomeCategoryIds.Count;
}
