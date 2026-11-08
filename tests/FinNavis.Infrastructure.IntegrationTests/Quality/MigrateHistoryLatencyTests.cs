using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Seeding;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Quality;

[Collection(DatabaseCollection.Name)]
public sealed class MigrateHistoryLatencyTests : IAsyncLifetime
{
    private const string HistoryMigrationPath = "/api/v1/import-runs/history-migration";

    private const string OccurredOnAccountIndex = "idx_transactions_occurred_on_account_id";

    // A p95 over five samples is the slowest of the five.
    private const int LoadSampleCount = 5;

    private static readonly TimeSpan LoadLimit = TimeSpan.FromSeconds(60);

    // What a statement load asks for: one card's month, widened by the one-day match window.
    private static readonly DateRange MonthlySnapshotPeriod =
        DateRange.Create(new DateOnly(2025, 5, 31), new DateOnly(2025, 7, 1));

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public MigrateHistoryLatencyTests(PostgresFixture database)
    {
        _database = database;
        _host = new ErrorTestHost(database, Environments.Development);
        _client = _host.CreateClient();
    }

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _host.Dispose();

        return Task.CompletedTask;
    }

    // Each sample starts from the same freshly seeded ledger, so every one of the five does the
    // whole job — a second load of the same file would find every row already present.
    [Fact]
    public async Task MigrateHistory_1500RowsSeveralAccounts_P95Under60s()
    {
        List<TimeSpan> loadDurations = [];

        for (int sample = 0; sample < LoadSampleCount; sample++)
        {
            await _database.ResetAsync();
            SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
            IReadOnlyList<SyntheticExportRow> rows =
                SyntheticExport.RowsAcrossSeededYears(SyntheticExport.HistoryRowCount, ExportAccountNames(ledger));
            using MultipartFormDataContent upload =
                ImportUpload.Export(SyntheticExport.ToFile(rows), SyntheticExport.FileName);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await _client.PostAsync(HistoryMigrationPath, upload);
            stopwatch.Stop();

            loadDurations.Add(stopwatch.Elapsed);
            Guid runId = await ShouldHaveRecordedEveryRowAsync(response);
            await ShouldSitOnTheNamedAccountsAndCategoriesAsync(runId, rows);
            await ShouldHaveCreatedWhatTheLedgerLackedAsync(ledger);
        }

        loadDurations.Max().Should().BeLessThanOrEqualTo(
            LoadLimit, $"every one of the {LoadSampleCount} loads must finish in time: {string.Join(", ", loadDurations)}");
    }

    // Nothing else in this feature fails when this index goes missing: a statement load would only
    // get slower. The plan is the one place its absence shows.
    [Fact]
    public async Task CandidateSnapshot_OneAccountOneMonth_ReadsTheOccurredOnAccountIndex()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        CapturedCommand snapshotQuery = await CaptureMonthlySnapshotQueryAsync(ledger.Accounts[0].Id);
        await using NpgsqlConnection connection = await _database.OpenConnectionAsync();
        await AnalyzeAsync(connection);

        IReadOnlyList<JsonElement> plan = await ExplainAsync(connection, transaction: null, snapshotQuery);

        ShouldReadTransactionsThroughTheOccurredOnAccountIndex(plan);
    }

    // Without the index PostgreSQL does not fall back to a sequential scan: it reads the whole
    // account through idx_transactions_account_id and filters the month out afterwards. That still
    // hides the loss from every other test, so the proof is that the assertion above now fails and
    // that no read of transactions is bounded by date any more. The drop runs inside a transaction
    // that is rolled back, so the shared database keeps its index for every later test.
    [Fact]
    public async Task CandidateSnapshot_OccurredOnAccountIndexDropped_IndexAssertionFails()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        CapturedCommand snapshotQuery = await CaptureMonthlySnapshotQueryAsync(ledger.Accounts[0].Id);
        await using NpgsqlConnection connection = await _database.OpenConnectionAsync();
        await AnalyzeAsync(connection);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction, $"DROP INDEX {OccurredOnAccountIndex}");

        IReadOnlyList<JsonElement> plan = await ExplainAsync(connection, transaction, snapshotQuery);

        await transaction.RollbackAsync();
        Action indexAssertion = () => ShouldReadTransactionsThroughTheOccurredOnAccountIndex(plan);
        indexAssertion.Should().Throw<Exception>(PlanText(plan));
        plan.Should().Contain(node => ReadsTransactions(node), PlanText(plan));
        plan.Should().NotContain(node => ReadsTransactions(node) && IsBoundedByDate(node), PlanText(plan));
    }

    private static void ShouldReadTransactionsThroughTheOccurredOnAccountIndex(IReadOnlyList<JsonElement> plan)
    {
        plan.Should().Contain(node => IndexNameOf(node) == OccurredOnAccountIndex, PlanText(plan));
        plan.Should().NotContain(node => IsSequentialScanOfTransactions(node), PlanText(plan));
    }

    // Two seeded accounts, so the load reads the seeded movements into its snapshot, and two new
    // ones, so it also has accounts to create.
    private static string[] ExportAccountNames(SeededLedger ledger) =>
        [ledger.Accounts[0].Name, ledger.Accounts[1].Name, .. SyntheticExport.NewAccountNames];

    private static async Task<Guid> ShouldHaveRecordedEveryRowAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("status").GetString().Should().Be("Completed");
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(SyntheticExport.HistoryRowCount);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(0);
        body.GetProperty("set_aside").GetArrayLength().Should().Be(0);

        return body.GetProperty("import_run_id").GetGuid();
    }

    // Compared row for row on day, amount, account name, category name and side, so a row recorded
    // on the wrong account or category fails even when every count still adds up.
    private async Task ShouldSitOnTheNamedAccountsAndCategoriesAsync(
        Guid runId,
        IReadOnlyList<SyntheticExportRow> rows)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();
        HashSet<Guid> linkedIds =
        [
            .. await context.ImportTransactionLinks.AsNoTracking()
                .Where(link => link.ImportRunId == runId)
                .Select(link => link.TransactionId)
                .ToListAsync(),
        ];
        List<Transaction> recorded = await context.Transactions.AsNoTracking()
            .Where(transaction => linkedIds.Contains(transaction.Id))
            .ToListAsync();
        Dictionary<Guid, string> accountNames = await context.Accounts.AsNoTracking()
            .ToDictionaryAsync(account => account.Id, account => account.Name);
        Dictionary<Guid, Category> categories = await context.Categories.AsNoTracking()
            .ToDictionaryAsync(category => category.Id);

        RecordedRow[] actual =
        [
            .. recorded.Select(transaction => new RecordedRow(
                transaction.OccurredOn,
                transaction.Amount.Amount,
                accountNames[transaction.AccountId],
                categories[transaction.CategoryId].Name,
                categories[transaction.CategoryId].Kind)),
        ];
        RecordedRow[] expected =
        [
            .. rows.Select(row => new RecordedRow(
                row.Day,
                Math.Abs(row.SignedAmount),
                row.Account,
                row.Category,
                row.SignedAmount < 0m ? CategoryKind.Expense : CategoryKind.Income)),
        ];

        InStableOrder(actual).Should().Equal(InStableOrder(expected));
    }

    private async Task ShouldHaveCreatedWhatTheLedgerLackedAsync(SeededLedger ledger)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();
        List<string> accountNames = await context.Accounts.AsNoTracking().Select(account => account.Name).ToListAsync();
        List<Category> categories = await context.Categories.AsNoTracking().ToListAsync();
        string[] exportCategoryNames = [.. SyntheticExport.ExpenseCategoryNames, SyntheticExport.IncomeCategoryName];

        accountNames.Should().BeEquivalentTo(
            [.. ledger.Accounts.Select(account => account.Name), .. SyntheticExport.NewAccountNames]);
        categories.Should().HaveCount(ledger.CategoryCount + exportCategoryNames.Length);
        categories.Select(category => category.Name).Should().Contain(exportCategoryNames);
        categories.Should().ContainSingle(category =>
            category.Name == SyntheticExport.IncomeCategoryName && category.Kind == CategoryKind.Income);
        (await TableRows.CountAsync(_database, "import_created_account_links"))
            .Should().Be(SyntheticExport.NewAccountNames.Length);
        (await TableRows.CountAsync(_database, "import_created_category_links"))
            .Should().Be(exportCategoryNames.Length);
    }

    private static IEnumerable<RecordedRow> InStableOrder(IEnumerable<RecordedRow> rows) =>
        rows.OrderBy(row => row.Day)
            .ThenBy(row => row.Account, StringComparer.Ordinal)
            .ThenBy(row => row.Amount)
            .ThenBy(row => row.Category, StringComparer.Ordinal);

    // The plan is taken of the SQL the adapter really sends, captured on its way to the database,
    // so a change to the query is explained as changed rather than through a copy that went stale.
    private async Task<CapturedCommand> CaptureMonthlySnapshotQueryAsync(Guid accountId)
    {
        CommandCapture capture = new();
        await using FinNavisDbContext context = _database.CreateDbContext(capture);

        await new EfImportQueries(context).GetCandidateSnapshotAsync([accountId], MonthlySnapshotPeriod, CancellationToken.None);

        return capture.Captured ?? throw new InvalidOperationException("The candidate snapshot sent no query.");
    }

    // Seeding by COPY leaves no statistics behind, and without them the planner guesses the table
    // size. Autovacuum would gather them in a real ledger; here it may not have run yet.
    private static Task AnalyzeAsync(NpgsqlConnection connection) =>
        ExecuteAsync(connection, transaction: null, "ANALYZE transactions, transfers, categories");

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<JsonElement>> ExplainAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CapturedCommand query)
    {
        await using NpgsqlCommand explain = new($"EXPLAIN (FORMAT JSON) {query.Text}", connection, transaction);
        foreach (NpgsqlParameter parameter in query.Parameters)
        {
            explain.Parameters.Add(parameter.Clone());
        }

        string planJson = (string)(await explain.ExecuteScalarAsync())!;
        JsonElement root = JsonDocument.Parse(planJson).RootElement[0].GetProperty("Plan");

        return [.. NodesOf(root)];
    }

    private static IEnumerable<JsonElement> NodesOf(JsonElement node)
    {
        yield return node;

        if (!node.TryGetProperty("Plans", out JsonElement children))
        {
            yield break;
        }

        foreach (JsonElement descendant in children.EnumerateArray().SelectMany(NodesOf))
        {
            yield return descendant;
        }
    }

    private static string? IndexNameOf(JsonElement node) =>
        node.TryGetProperty("Index Name", out JsonElement name) ? name.GetString() : null;

    private static bool IsSequentialScanOfTransactions(JsonElement node) =>
        node.GetProperty("Node Type").GetString() == "Seq Scan"
        && node.TryGetProperty("Relation Name", out JsonElement relation)
        && relation.GetString() == "transactions";

    private static bool ReadsTransactions(JsonElement node) =>
        (node.TryGetProperty("Relation Name", out JsonElement relation) && relation.GetString() == "transactions")
        || IndexNameOf(node)?.Contains("transactions", StringComparison.Ordinal) == true;

    // A heap scan rechecks the date too, so only an index condition counts as reading by date.
    private static bool IsBoundedByDate(JsonElement node) =>
        node.TryGetProperty("Index Cond", out JsonElement condition)
        && condition.GetString()!.Contains("occurred_on", StringComparison.Ordinal);

    private static string PlanText(IReadOnlyList<JsonElement> plan) =>
        string.Join(
            "; ",
            plan.Select(node => $"{node.GetProperty("Node Type").GetString()} {IndexNameOf(node)}".TrimEnd()));

    private sealed record RecordedRow(DateOnly Day, decimal Amount, string Account, string Category, CategoryKind Kind);

    private sealed record CapturedCommand(string Text, IReadOnlyList<NpgsqlParameter> Parameters);

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public CapturedCommand? Captured { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Captured = new CapturedCommand(
                command.CommandText,
                [.. command.Parameters.Cast<NpgsqlParameter>().Select(parameter => parameter.Clone())]);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
