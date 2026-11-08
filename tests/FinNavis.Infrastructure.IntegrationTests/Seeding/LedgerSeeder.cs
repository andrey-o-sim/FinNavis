using Npgsql;
using NpgsqlTypes;

namespace FinNavis.Infrastructure.IntegrationTests.Seeding;

// Three years of a single owner's ledger: the size core-ledger sizes itself for. Written with
// binary COPY instead of through EF Core, so seeding twenty thousand rows costs a fraction of a
// second and the timed part of a test is the only slow part.
internal static class LedgerSeeder
{
    public const int MovementCount = 20_000;

    public static readonly DateOnly FirstDay = new(2023, 1, 1);

    public static readonly DateOnly LastDay = new(2025, 12, 31);

    // Every tenth movement is a transfer between two own accounts; the rest are transactions.
    private const int TransferEvery = 10;

    // Every fifteenth transaction is income; the rest are spending.
    private const int IncomeEvery = 15;

    private const int RandomSeed = 20_231;

    private const string ExpenseKind = "Expense";

    private const string IncomeKind = "Income";

    private static readonly string[] AccountNames =
    [
        "Seeded current account", "Seeded cash wallet", "Seeded savings jar", "Seeded credit card",
    ];

    private static readonly string[] ExpenseCategoryNames =
    [
        "Seeded groceries", "Seeded cafe", "Seeded transport", "Seeded rent", "Seeded utilities",
        "Seeded health",
    ];

    private static readonly string[] IncomeCategoryNames = ["Seeded salary", "Seeded interest"];

    public static async Task<SeededLedger> SeedAsync(
        PostgresFixture database,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        List<SeededAccount> accounts = [.. AccountNames.Select(name => new SeededAccount(Guid.CreateVersion7(), name))];
        List<Guid> expenseCategoryIds = [.. ExpenseCategoryNames.Select(_ => Guid.CreateVersion7())];
        List<Guid> incomeCategoryIds = [.. IncomeCategoryNames.Select(_ => Guid.CreateVersion7())];

        await using NpgsqlConnection connection = await database.OpenConnectionAsync(cancellationToken);

        await CopyAccountsAsync(connection, accounts, createdAt, cancellationToken);
        await CopyCategoriesAsync(connection, expenseCategoryIds, ExpenseCategoryNames, ExpenseKind, createdAt, cancellationToken);
        await CopyCategoriesAsync(connection, incomeCategoryIds, IncomeCategoryNames, IncomeKind, createdAt, cancellationToken);

        Random random = new(RandomSeed);
        int transferCount = MovementCount / TransferEvery;
        int transactionCount = MovementCount - transferCount;

        await CopyTransactionsAsync(
            connection, accounts, expenseCategoryIds, incomeCategoryIds, transactionCount, random, createdAt, cancellationToken);
        await CopyTransfersAsync(connection, accounts, transferCount, random, createdAt, cancellationToken);

        return new SeededLedger(accounts, expenseCategoryIds, incomeCategoryIds, transactionCount, transferCount);
    }

    private static DateOnly DayOf(int index)
    {
        int daysInSpan = LastDay.DayNumber - FirstDay.DayNumber + 1;

        return FirstDay.AddDays(index % daysInSpan);
    }

    private static decimal AmountBetween(Random random, int smallestCents, int largestCents) =>
        random.Next(smallestCents, largestCents) / 100m;

    private static async Task CopyAccountsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<SeededAccount> accounts,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            "COPY accounts (id, name, created_at) FROM STDIN (FORMAT BINARY)", cancellationToken);

        foreach (SeededAccount account in accounts)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(account.Id, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(account.Name, NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteAsync(createdAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyCategoriesAsync(
        NpgsqlConnection connection,
        IReadOnlyList<Guid> ids,
        IReadOnlyList<string> names,
        string kind,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            "COPY categories (id, name, kind, category_group_id, created_at) FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        for (int index = 0; index < ids.Count; index++)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(ids[index], NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(names[index], NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteAsync(kind, NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteNullAsync(cancellationToken);
            await writer.WriteAsync(createdAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyTransactionsAsync(
        NpgsqlConnection connection,
        IReadOnlyList<SeededAccount> accounts,
        IReadOnlyList<Guid> expenseCategoryIds,
        IReadOnlyList<Guid> incomeCategoryIds,
        int count,
        Random random,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            "COPY transactions (id, account_id, category_id, amount, occurred_on, note, external_ref, created_at) "
            + "FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        for (int index = 0; index < count; index++)
        {
            bool isIncome = index % IncomeEvery == 0;
            Guid categoryId = isIncome
                ? incomeCategoryIds[index % incomeCategoryIds.Count]
                : expenseCategoryIds[index % expenseCategoryIds.Count];
            decimal amount = isIncome ? AmountBetween(random, 100_000, 5_000_000) : AmountBetween(random, 100, 300_000);

            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(Guid.CreateVersion7(), NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(accounts[index % accounts.Count].Id, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(categoryId, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(amount, NpgsqlDbType.Numeric, cancellationToken);
            await writer.WriteAsync(DayOf(index), NpgsqlDbType.Date, cancellationToken);
            await writer.WriteAsync($"Seeded movement {index}", NpgsqlDbType.Varchar, cancellationToken);
            await writer.WriteNullAsync(cancellationToken);
            await writer.WriteAsync(createdAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static async Task CopyTransfersAsync(
        NpgsqlConnection connection,
        IReadOnlyList<SeededAccount> accounts,
        int count,
        Random random,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await using NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
            "COPY transfers (id, source_account_id, destination_account_id, amount, occurred_on, created_at) "
            + "FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        for (int index = 0; index < count; index++)
        {
            SeededAccount source = accounts[index % accounts.Count];
            SeededAccount destination = accounts[(index + 1) % accounts.Count];

            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(Guid.CreateVersion7(), NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(source.Id, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(destination.Id, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(AmountBetween(random, 1_000, 1_000_000), NpgsqlDbType.Numeric, cancellationToken);
            await writer.WriteAsync(DayOf(index * TransferEvery), NpgsqlDbType.Date, cancellationToken);
            await writer.WriteAsync(createdAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }
}
