using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Accounts;
using FinNavis.Application.Common;
using FinNavis.Application.Transactions;
using FinNavis.Application.Transfers;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;
using FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.IntegrationTests.Seeding;
using FinNavis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Quality;

[Collection(DatabaseCollection.Name)]
public sealed class UndoAndAtomicityTests : IAsyncLifetime
{
    private const string StatementPath = "/api/v1/import-runs/statement";

    private const string HistoryMigrationPath = "/api/v1/import-runs/history-migration";

    private const string ConfirmPath = "/api/v1/mapping-profiles";

    private const string GroceriesInFile = "Продукти";

    private const string TransportInFile = "Транспорт";

    private const string CafeInFile = "Кафе";

    private const string ChangedByHandCode = "Import.MovementsChangedByHand";

    private const string ChangedByHandMessage =
        "Some rows from this load were changed by hand afterwards, so the load cannot be undone as a whole.";

    private const int SmallExportRowCount = 40;

    private const int SmallStatementRowCount = 60;

    // A p95 over five samples is the slowest of the five.
    private const int UndoSampleCount = 5;

    private static readonly TimeSpan UndoLimit = TimeSpan.FromSeconds(30);

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public UndoAndAtomicityTests(PostgresFixture database)
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

    [Fact]
    public async Task LoadStatement_CommitFailsPartWay_LedgerIsUnchanged()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        Guid accountId = ledger.Accounts[0].Id;
        await ConfirmStatementRulesAsync(ledger);
        IReadOnlyList<AccountBalanceRow> balancesBefore = await BalancesAsync();
        byte[] statement = SyntheticStatement.Create(
            SyntheticExport.HistoryRowCount, [GroceriesInFile, CafeInFile, TransportInFile], LedgerCurrency());
        using WebApplicationFactory<Program> failingHost = _host.WithCommitFailingPartWay(_database);
        using HttpClient failingClient = failingHost.CreateClient();
        using MultipartFormDataContent upload =
            ImportUpload.Statement(statement, accountId, SyntheticStatement.FileName);

        HttpResponseMessage response = await failingClient.PostAsync(StatementPath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.InternalServerError);
        body.GetProperty("code").GetString().Should().Be("Import.NotRecorded");
        body.GetProperty("message").GetString().Should().Be(
            "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.");
        await AssertLedgerHoldsOnlyTheSeedAsync(ledger);
        (await BalancesAsync()).Should().Equal(balancesBefore);
    }

    [Fact]
    public async Task UndoImportRun_1500Rows_P95Under30s()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        byte[] export = SyntheticExport.Create(SyntheticExport.HistoryRowCount, ExportAccountNames(ledger));
        IReadOnlyList<AccountBalanceRow> balancesBefore = await BalancesAsync();
        List<TimeSpan> undoDurations = [];

        for (int sample = 0; sample < UndoSampleCount; sample++)
        {
            Guid runId = await MigrateHistoryAsync(export, SyntheticExport.HistoryRowCount);

            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await UndoAsync(runId);
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            undoDurations.Add(stopwatch.Elapsed);
            await AssertLedgerHoldsOnlyTheSeedAndUndoneRunsAsync(ledger, undoneRuns: sample + 1);
            (await BalancesAsync()).Should().Equal(balancesBefore);
        }

        undoDurations.Max().Should().BeLessThanOrEqualTo(
            UndoLimit, $"every one of the {UndoSampleCount} undos must finish in time: {string.Join(", ", undoDurations)}");
    }

    // A hand-typed movement recorded after the load stays, so the balance the undo must reach is
    // the one before the load plus that movement — not simply the one before the load.
    [Fact]
    public async Task UndoImportRun_CleanUndo_EveryBalanceIsAsIfTheLoadNeverHappened()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        IReadOnlyList<AccountBalanceRow> balancesBefore = await BalancesAsync();
        Guid runId = await MigrateHistoryAsync(
            SyntheticExport.Create(SmallExportRowCount, ExportAccountNames(ledger)), SmallExportRowCount);
        SeededAccount handTypedOn = ledger.Accounts[0];
        Money handTypedAmount = new(17.25m);
        await RecordByHandAsync(handTypedOn.Id, ledger.ExpenseCategoryIds[0], handTypedAmount.Amount);

        HttpResponseMessage response = await UndoAsync(runId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AccountBalanceRow[] expected =
        [
            .. balancesBefore.Select(row => row.AccountId == handTypedOn.Id
                ? row with { Balance = row.Balance - handTypedAmount }
                : row),
        ];
        (await BalancesAsync()).Should().Equal(expected);
        (await TableRows.CountAsync(_database, "transactions")).Should().Be(ledger.TransactionCount + 1);
        (await TableRows.CountAsync(_database, "accounts")).Should().Be(ledger.Accounts.Count);
        (await TableRows.CountAsync(_database, "categories")).Should().Be(ledger.CategoryCount);
    }

    // The theory walks every field the fingerprint covers on a transaction. A field that drops out
    // of the fingerprint is a field whose edit the undo silently deletes, and only its own case
    // would notice.
    [Theory]
    [InlineData(nameof(Transaction.AccountId))]
    [InlineData(nameof(Transaction.CategoryId))]
    [InlineData(nameof(Transaction.Amount))]
    [InlineData(nameof(Transaction.OccurredOn))]
    [InlineData(nameof(Transaction.Note))]
    public async Task UndoImportRun_MovementChangedByHand_IsRefusedAndNamesTheRows(string editedField)
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        Guid runId = await MigrateHistoryAsync(
            SyntheticExport.Create(SmallExportRowCount, ExportAccountNames(ledger)), SmallExportRowCount);
        Transaction[] recorded = await RecordedTransactionsOfAsync(runId);
        Transaction changed = recorded[0];
        Transaction deleted = recorded[^1];
        await EditByHandAsync(changed, editedField, ledger);
        await RemoveByHandAsync(deleted.Id);
        LedgerCounts countsBeforeUndo = await CountLedgerAsync();

        HttpResponseMessage response = await UndoAsync(runId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be(ChangedByHandCode);
        body.GetProperty("message").GetString().Should().Be(ChangedByHandMessage);
        JsonElement details = body.GetProperty("details");
        details.GetProperty("transaction_ids").EnumerateArray().Select(id => id.GetGuid())
            .Should().Equal(changed.Id);
        details.GetProperty("transfer_ids").GetArrayLength().Should().Be(0);

        (await CountLedgerAsync()).Should().Be(countsBeforeUndo);
        countsBeforeUndo.Transactions.Should().Be(ledger.TransactionCount + SmallExportRowCount - 1);
        countsBeforeUndo.CreatedAccountLinks.Should().Be(SyntheticExport.NewAccountNames.Length);
        countsBeforeUndo.Accounts.Should().Be(ledger.Accounts.Count + SyntheticExport.NewAccountNames.Length);
        countsBeforeUndo.CreatedCategoryLinks.Should().BePositive();
        countsBeforeUndo.Categories.Should().Be(ledger.CategoryCount + countsBeforeUndo.CreatedCategoryLinks);
        (await StatusOfAsync(runId)).Should().Be(ImportRunStatus.Completed);
    }

    // No load records a transfer today, so the run is written directly. The transfer side of the
    // fingerprint is otherwise untested, and a transfer the owner corrects is lost the same way.
    [Theory]
    [InlineData(nameof(Transfer.SourceAccountId))]
    [InlineData(nameof(Transfer.DestinationAccountId))]
    [InlineData(nameof(Transfer.Amount))]
    [InlineData(nameof(Transfer.OccurredOn))]
    public async Task UndoImportRun_TransferChangedByHand_IsRefusedAndNamesTheRows(string editedField)
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        (Guid runId, Transfer transfer) = await RecordRunWithOneTransferAsync(ledger);
        await EditByHandAsync(transfer, editedField, ledger);

        HttpResponseMessage response = await UndoAsync(runId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be(ChangedByHandCode);
        JsonElement details = body.GetProperty("details");
        details.GetProperty("transfer_ids").EnumerateArray().Select(id => id.GetGuid())
            .Should().Equal(transfer.Id);
        details.GetProperty("transaction_ids").GetArrayLength().Should().Be(0);
        (await TableRows.CountAsync(_database, "transfers")).Should().Be(ledger.TransferCount + 1);
        (await StatusOfAsync(runId)).Should().Be(ImportRunStatus.Completed);
    }

    // A deleted movement is simply absent at undo time: there is nothing left to remove, so it is
    // not a change and blocks nothing.
    [Fact]
    public async Task UndoImportRun_MovementDeletedByHand_UndoesTheRest()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        IReadOnlyList<AccountBalanceRow> balancesBefore = await BalancesAsync();
        Guid runId = await MigrateHistoryAsync(
            SyntheticExport.Create(SmallExportRowCount, ExportAccountNames(ledger)), SmallExportRowCount);
        Transaction[] recorded = await RecordedTransactionsOfAsync(runId);
        await RemoveByHandAsync(recorded[0].Id);
        await RemoveByHandAsync(recorded[^1].Id);

        HttpResponseMessage response = await UndoAsync(runId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StatusOfAsync(runId)).Should().Be(ImportRunStatus.Undone);
        await AssertLedgerHoldsOnlyTheSeedAndUndoneRunsAsync(ledger, undoneRuns: 1);
        (await BalancesAsync()).Should().Equal(balancesBefore);
    }

    [Fact]
    public async Task UndoImportRun_CreatedCategoryNowUsedElsewhere_IsKept()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        Guid runId = await MigrateHistoryAsync(
            SyntheticExport.Create(SmallExportRowCount, SyntheticExport.NewAccountNames), SmallExportRowCount);
        Guid usedElsewhereId = await CreatedExpenseCategoryIdAsync(SyntheticExport.ExpenseCategoryNames[0]);
        Guid unusedId = await CreatedExpenseCategoryIdAsync(SyntheticExport.ExpenseCategoryNames[1]);
        Guid handTypedId = await RecordByHandAsync(ledger.Accounts[0].Id, usedElsewhereId, 9.99m);

        HttpResponseMessage response = await UndoAsync(runId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Categories.AnyAsync(category => category.Id == usedElsewhereId)).Should().BeTrue();
        (await context.Categories.AnyAsync(category => category.Id == unusedId)).Should().BeFalse();
        (await context.Categories.CountAsync()).Should().Be(ledger.CategoryCount + 1);
        (await context.Transactions.AnyAsync(transaction => transaction.Id == handTypedId)).Should().BeTrue();
        (await context.Accounts.CountAsync()).Should().Be(ledger.Accounts.Count);
    }

    [Fact]
    public async Task LoadStatement_RunUndoneThenSameFileAgain_RecordsEverything()
    {
        SeededLedger ledger = await LedgerSeeder.SeedAsync(_database);
        Guid accountId = ledger.Accounts[0].Id;
        await ConfirmStatementRulesAsync(ledger);
        byte[] statement = SyntheticStatement.Create(
            SmallStatementRowCount, [GroceriesInFile, TransportInFile], LedgerCurrency());
        JsonElement first = await LoadStatementAsync(statement, accountId);
        first.GetProperty("recorded_row_count").GetInt32().Should().Be(SmallStatementRowCount);
        (await UndoAsync(first.GetProperty("import_run_id").GetGuid())).StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement again = await LoadStatementAsync(statement, accountId);

        again.GetProperty("status").GetString().Should().Be("Completed");
        again.GetProperty("recorded_row_count").GetInt32().Should().Be(SmallStatementRowCount);
        again.GetProperty("already_present_row_count").GetInt32().Should().Be(0);
        again.GetProperty("set_aside").GetArrayLength().Should().Be(0);
        (await TableRows.CountAsync(_database, "transactions"))
            .Should().Be(ledger.TransactionCount + SmallStatementRowCount);
        (await TableRows.CountAsync(_database, "import_transaction_links")).Should().Be(SmallStatementRowCount);
    }

    // Two seeded accounts, so the load moves balances the ledger already had, and two new ones, so
    // it also creates accounts the undo has to remove.
    private static string[] ExportAccountNames(SeededLedger ledger) =>
        [ledger.Accounts[0].Name, ledger.Accounts[1].Name, .. SyntheticExport.NewAccountNames];

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private string LedgerCurrency() => _host.Services.GetRequiredService<LedgerOptions>().Currency;

    private Task<HttpResponseMessage> UndoAsync(Guid importRunId) =>
        _client.PostAsync($"/api/v1/import-runs/{importRunId}/undo", content: null);

    private async Task<Guid> MigrateHistoryAsync(byte[] export, int expectedRecorded)
    {
        using MultipartFormDataContent upload = ImportUpload.Export(export, SyntheticExport.FileName);

        HttpResponseMessage response = await _client.PostAsync(HistoryMigrationPath, upload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(response);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(expectedRecorded);

        return body.GetProperty("import_run_id").GetGuid();
    }

    private async Task<JsonElement> LoadStatementAsync(byte[] statement, Guid accountId)
    {
        using MultipartFormDataContent upload =
            ImportUpload.Statement(statement, accountId, SyntheticStatement.FileName);

        HttpResponseMessage response = await _client.PostAsync(StatementPath, upload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadJsonAsync(response);
    }

    // The cafe has no rule, so a statement that names it would set rows aside — which makes "no
    // set-aside row survives" a real check on a failed load.
    private async Task ConfirmStatementRulesAsync(SeededLedger ledger)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(ConfirmPath, new
        {
            shape_key = SyntheticFile.StatementShapeKey,
            rules = new object[]
            {
                new { source_category = GroceriesInFile, target = "Category", target_category_id = (Guid?)ledger.ExpenseCategoryIds[0] },
                new { source_category = TransportInFile, target = "Category", target_category_id = (Guid?)ledger.ExpenseCategoryIds[2] },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<IReadOnlyList<AccountBalanceRow>> BalancesAsync()
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        GetAccountBalances getBalances = scope.ServiceProvider.GetRequiredService<GetAccountBalances>();

        return (await getBalances.ExecuteAsync(accountId: null, CancellationToken.None)).Accounts;
    }

    private async Task<Transaction[]> RecordedTransactionsOfAsync(Guid runId)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();
        List<Guid> linkedIds = await context.ImportTransactionLinks.AsNoTracking()
            .Where(link => link.ImportRunId == runId)
            .Select(link => link.TransactionId)
            .ToListAsync();

        return
        [
            .. await context.Transactions.AsNoTracking()
                .Where(transaction => linkedIds.Contains(transaction.Id))
                .OrderBy(transaction => transaction.Id)
                .ToListAsync(),
        ];
    }

    private async Task<Guid> CreatedExpenseCategoryIdAsync(string name)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();

        return await context.Categories.AsNoTracking()
            .Where(category => category.Name == name && category.Kind == CategoryKind.Expense)
            .Select(category => category.Id)
            .SingleAsync();
    }

    private async Task<ImportRunStatus> StatusOfAsync(Guid runId)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();

        return (await context.ImportRuns.AsNoTracking().SingleAsync(run => run.Id == runId)).Status;
    }

    // Through the same use cases the owner's own edits go through, so the test changes a row the
    // way the owner would rather than the way only a test could.
    private async Task EditByHandAsync(Transaction transaction, string field, SeededLedger ledger)
    {
        EditTransactionChanges changes = field switch
        {
            nameof(Transaction.AccountId) => new(
                AccountId: ledger.Accounts.First(account => account.Id != transaction.AccountId).Id),
            nameof(Transaction.CategoryId) => new(
                CategoryId: ledger.ExpenseCategoryIds.First(id => id != transaction.CategoryId)),
            nameof(Transaction.Amount) => new(Amount: transaction.Amount.Amount + 1m),
            nameof(Transaction.OccurredOn) => new(OccurredOn: transaction.OccurredOn.AddDays(1)),
            nameof(Transaction.Note) => new(Note: NoteChange.To("Test note typed by hand")),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field the fingerprint covers."),
        };

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EditTransaction>()
            .ExecuteAsync(transaction.Id, changes, CancellationToken.None);
    }

    private async Task EditByHandAsync(Transfer transfer, string field, SeededLedger ledger)
    {
        Guid unusedAccountId = ledger.Accounts
            .First(account => account.Id != transfer.SourceAccountId && account.Id != transfer.DestinationAccountId).Id;
        EditTransferChanges changes = field switch
        {
            nameof(Transfer.SourceAccountId) => new(SourceAccountId: unusedAccountId),
            nameof(Transfer.DestinationAccountId) => new(DestinationAccountId: unusedAccountId),
            nameof(Transfer.Amount) => new(Amount: transfer.Amount.Amount + 1m),
            nameof(Transfer.OccurredOn) => new(OccurredOn: transfer.OccurredOn.AddDays(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field the fingerprint covers."),
        };

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EditTransfer>()
            .ExecuteAsync(transfer.Id, changes, CancellationToken.None);
    }

    private async Task RemoveByHandAsync(Guid transactionId)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RemoveTransaction>()
            .ExecuteAsync(transactionId, CancellationToken.None);
    }

    private async Task<Guid> RecordByHandAsync(Guid accountId, Guid categoryId, decimal amount)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Transaction transaction = await scope.ServiceProvider.GetRequiredService<RecordTransaction>()
            .ExecuteAsync(accountId, categoryId, amount, SyntheticExport.FirstDay, note: null, CancellationToken.None);

        return transaction.Id;
    }

    private async Task<(Guid RunId, Transfer Transfer)> RecordRunWithOneTransferAsync(SeededLedger ledger)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(), ledger.Accounts[0].Id, ledger.Accounts[1].Id, new Money(250m), SyntheticExport.FirstDay, now);
        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), SyntheticExport.FileName, ImportRunKind.HistoryMigration, accountId: null, now);
        run.LinkRecordedTransfer(Guid.CreateVersion7(), transfer);

        await using FinNavisDbContext context = _database.CreateDbContext();
        context.Transfers.Add(transfer);
        context.ImportRuns.Add(run);
        await context.SaveChangesAsync();

        return (run.Id, transfer);
    }

    private async Task<LedgerCounts> CountLedgerAsync() =>
        new(
            await TableRows.CountAsync(_database, "transactions"),
            await TableRows.CountAsync(_database, "transfers"),
            await TableRows.CountAsync(_database, "accounts"),
            await TableRows.CountAsync(_database, "categories"),
            await TableRows.CountAsync(_database, "import_transaction_links"),
            await TableRows.CountAsync(_database, "import_created_account_links"),
            await TableRows.CountAsync(_database, "import_created_category_links"));

    private async Task AssertLedgerHoldsOnlyTheSeedAsync(SeededLedger ledger)
    {
        foreach (string table in TableRows.ImportTables)
        {
            (await TableRows.CountAsync(_database, table)).Should().Be(0, $"nothing of the failed load may survive in {table}");
        }

        await AssertSeedMovementsAndNamesOnlyAsync(ledger);
    }

    // The undone runs themselves stay, with status Undone, so the report keeps saying what each load
    // had done. Everything they recorded or created is gone.
    private async Task AssertLedgerHoldsOnlyTheSeedAndUndoneRunsAsync(SeededLedger ledger, int undoneRuns)
    {
        (await TableRows.CountAsync(_database, "import_runs")).Should().Be(undoneRuns);
        (await TableRows.CountAsync(_database, "import_transaction_links")).Should().Be(0);
        await AssertSeedMovementsAndNamesOnlyAsync(ledger);
    }

    private async Task AssertSeedMovementsAndNamesOnlyAsync(SeededLedger ledger)
    {
        (await TableRows.CountAsync(_database, "transactions")).Should().Be(ledger.TransactionCount);
        (await TableRows.CountAsync(_database, "transfers")).Should().Be(ledger.TransferCount);
        (await TableRows.CountAsync(_database, "accounts")).Should().Be(ledger.Accounts.Count);
        (await TableRows.CountAsync(_database, "categories")).Should().Be(ledger.CategoryCount);
    }

    private sealed record LedgerCounts(
        long Transactions,
        long Transfers,
        long Accounts,
        long Categories,
        long TransactionLinks,
        long CreatedAccountLinks,
        long CreatedCategoryLinks);
}
