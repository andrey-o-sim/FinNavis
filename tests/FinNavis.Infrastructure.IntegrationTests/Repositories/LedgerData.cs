using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

/// <summary>
/// Builds the entities the repository tests store. Synthetic values only — no real financial data
/// ever reaches a fixture (<c>CLAUDE.md</c> Safety).
/// </summary>
internal static class LedgerData
{
    private static readonly DateOnly Day = new(2026, 10, 29);

    /// <summary>A UUID v7, minted the way the Application layer mints one (ADR-0005).</summary>
    public static Guid NewId() => Guid.CreateVersion7();

    public static Account NewAccount(string name) => Account.Create(NewId(), name, DateTimeOffset.UtcNow);

    public static CategoryGroup NewGroup(string name, CategoryKind kind) =>
        CategoryGroup.Create(NewId(), name, kind, DateTimeOffset.UtcNow);

    public static Category NewCategory(string name, CategoryKind kind, CategoryGroup? group = null) =>
        Category.Create(NewId(), name, kind, group, DateTimeOffset.UtcNow);

    public static Transaction NewTransaction(
        Guid accountId,
        Guid categoryId,
        decimal amount,
        string? note = null) =>
        Transaction.Record(NewId(), accountId, categoryId, new Money(amount), Day, note, DateTimeOffset.UtcNow);

    public static Transfer NewTransfer(Guid sourceAccountId, Guid destinationAccountId, decimal amount) =>
        Transfer.Record(
            NewId(),
            sourceAccountId,
            destinationAccountId,
            new Money(amount),
            Day,
            DateTimeOffset.UtcNow);

    /// <summary>The day every entity above is filed under, so a test can assert on it.</summary>
    public static DateOnly OccurredOn => Day;
}
