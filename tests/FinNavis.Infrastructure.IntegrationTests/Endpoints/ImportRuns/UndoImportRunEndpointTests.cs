using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinNavis.Application.Accounts;
using FinNavis.Application.Categories;
using FinNavis.Application.Common;
using FinNavis.Application.Transactions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

[Collection(DatabaseCollection.Name)]
public sealed partial class UndoImportRunEndpointTests : IAsyncLifetime
{
    private const string LoadPath = "/api/v1/import-runs/statement";

    private const string ConfirmPath = "/api/v1/mapping-profiles";

    private const string FileName = "test-statement-2026-03.csv";

    private const string GroceriesInFile = "Продукти";

    private const string CafeInFile = "Кафе";

    private const string OwnCardInFile = "Переказ";

    private static readonly DateTimeOffset HandedOverAt = new(2026, 10, 21, 9, 30, 0, TimeSpan.Zero);

    private static readonly string[] RunKeys =
    [
        "import_run_id", "kind", "status", "file_name", "account_id", "recorded_row_count",
        "already_present_row_count", "created_at", "set_aside",
    ];

    private static readonly string[] PageKeys = ["items", "has_next", "has_prev", "next_cursor", "prev_cursor"];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public UndoImportRunEndpointTests(PostgresFixture database)
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

    // The run row survives the undo with status Undone and the counts the load stored, so the
    // report keeps saying what the load had done.
    [Fact]
    public async Task Post_UndoOfACompletedLoad_Returns200UndoneWithTheStoredCountsAndRemovesTheMovement()
    {
        (Guid runId, Guid walletId) = await LoadStatementAsync();

        HttpResponseMessage response = await UndoAsync(runId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        JsonElement body = await ReadJsonAsync(response);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(RunKeys);
        KeysIn(body).Should().AllSatisfy(key => key.Should().MatchRegex(SnakeCaseKey()));
        body.GetProperty("import_run_id").GetGuid().Should().Be(runId);
        body.GetProperty("status").GetString().Should().Be("Undone");
        body.GetProperty("kind").GetString().Should().Be("Statement");
        body.GetProperty("file_name").GetString().Should().Be(FileName);
        body.GetProperty("account_id").GetGuid().Should().Be(walletId);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(1);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(0);

        JsonElement setAside = body.GetProperty("set_aside");
        setAside.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PageKeys);
        Items(setAside).Should().Equal(4, 5);
        setAside.GetProperty("has_next").GetBoolean().Should().BeFalse();
        setAside.GetProperty("next_cursor").ValueKind.Should().Be(JsonValueKind.Null);

        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Transactions.CountAsync()).Should().Be(0);
        ImportRun stored = await context.ImportRuns.AsNoTracking().SingleAsync();
        stored.Status.Should().Be(ImportRunStatus.Undone);
        (await context.Accounts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Post_RowChangedByHandAfterTheLoad_Returns409MovementsChangedByHandAndDeletesNothing()
    {
        (Guid runId, _) = await LoadStatementAsync();
        Guid changedId = await ChangeTheRecordedTransactionByHandAsync();

        HttpResponseMessage response = await UndoAsync(runId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be("Import.MovementsChangedByHand");
        body.GetProperty("message").GetString().Should().Be(
            "Some rows from this load were changed by hand afterwards, so the load cannot be undone as a whole.");
        JsonElement details = body.GetProperty("details");
        details.GetProperty("transaction_ids").EnumerateArray().Select(id => id.GetGuid())
            .Should().Equal(changedId);
        details.GetProperty("transfer_ids").GetArrayLength().Should().Be(0);

        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Transactions.CountAsync()).Should().Be(1);
        (await context.ImportTransactionLinks.CountAsync()).Should().Be(1);
        ImportRun stored = await context.ImportRuns.AsNoTracking().SingleAsync();
        stored.Status.Should().Be(ImportRunStatus.Completed);
    }

    [Fact]
    public async Task Post_SecondUndoOfTheSameRun_Returns409AlreadyUndone()
    {
        (Guid runId, _) = await LoadStatementAsync();
        (await UndoAsync(runId)).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage again = await UndoAsync(runId);

        JsonElement body = await again.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be("Import.AlreadyUndone");
        body.GetProperty("message").GetString()
            .Should().Be("This load was already undone, so there is nothing left to remove.");
    }

    [Fact]
    public async Task Post_UnknownRun_Returns404ImportNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await UndoAsync(unknownId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Import.NotFound");
        body.GetProperty("message").GetString().Should().Be($"No load in this installation has id {unknownId}.");
    }

    // The undo takes no paging parameters, so it answers the first page; the cursor it hands back
    // is one the run's read accepts.
    [Fact]
    public async Task Post_RunWithMoreSetAsideRowsThanAPage_AnswersTheFirstPageWithACursorTheReadAccepts()
    {
        ImportRun run = await SeedRunWithSetAsideRowsAsync(60);

        HttpResponseMessage response = await UndoAsync(run.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement setAside = (await ReadJsonAsync(response)).GetProperty("set_aside");
        Items(setAside).Should().Equal(Enumerable.Range(1, 50));
        setAside.GetProperty("has_next").GetBoolean().Should().BeTrue();
        string nextCursor = setAside.GetProperty("next_cursor").GetString()!;

        HttpResponseMessage rest = await _client.GetAsync(
            $"/api/v1/import-runs/{run.Id}?after={Uri.EscapeDataString(nextCursor)}");

        rest.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement restBody = await ReadJsonAsync(rest);
        restBody.GetProperty("status").GetString().Should().Be("Undone");
        Items(restBody.GetProperty("set_aside")).Should().Equal(Enumerable.Range(51, 10));
    }

    private static string UndoPath(Guid importRunId) => $"/api/v1/import-runs/{importRunId}/undo";

    private static IEnumerable<int> Items(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(row => row.GetProperty("row_number").GetInt32());

    // One row per outcome a statement load has: recorded, no rule, a move between own accounts.
    private static string[] StatementRows(string ledgerCurrency) =>
    [
        $"01.03.2026 10:00:00,{GroceriesInFile},**** 0001,Test Shop One,-12.5,{ledgerCurrency},12.5,{ledgerCurrency},100,{ledgerCurrency}",
        $"02.03.2026 10:00:00,{CafeInFile},**** 0001,Test Cafe,-4.8,{ledgerCurrency},4.8,{ledgerCurrency},95.2,{ledgerCurrency}",
        $"03.03.2026 10:00:00,{OwnCardInFile},**** 0001,Test Own Card,-200,{ledgerCurrency},200,{ledgerCurrency},-104.8,{ledgerCurrency}",
    ];

    private static byte[] Statement(string[] rows) =>
        SyntheticFile.Statement(
            SyntheticFile.StatementTitle("Виписка за період 01.03.2026 - 31.03.2026"),
            SyntheticFile.StatementHeader,
            rows);

    private static IEnumerable<string> KeysIn(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .SelectMany(property => KeysIn(property.Value).Prepend(property.Name)),
        JsonValueKind.Array => element.EnumerateArray().SelectMany(KeysIn),
        _ => [],
    };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [GeneratedRegex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$")]
    private static partial Regex SnakeCaseKey();

    private Task<HttpResponseMessage> UndoAsync(Guid importRunId) =>
        _client.PostAsync(UndoPath(importRunId), content: null);

    private async Task<(Guid RunId, Guid WalletId)> LoadStatementAsync()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        string ledgerCurrency = _host.Services.GetRequiredService<LedgerOptions>().Currency;
        using MultipartFormDataContent upload =
            ImportUpload.Statement(Statement(StatementRows(ledgerCurrency)), walletId, FileName);

        HttpResponseMessage response = await _client.PostAsync(LoadPath, upload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(response);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(1);

        return (body.GetProperty("import_run_id").GetGuid(), walletId);
    }

    private async Task<Guid> ChangeTheRecordedTransactionByHandAsync()
    {
        Guid transactionId;
        await using (FinNavisDbContext context = _database.CreateDbContext())
        {
            transactionId = (await context.Transactions.AsNoTracking().SingleAsync()).Id;
        }

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        EditTransaction editTransaction = scope.ServiceProvider.GetRequiredService<EditTransaction>();
        await editTransaction.ExecuteAsync(
            transactionId, new EditTransactionChanges(Amount: 99.99m), CancellationToken.None);

        return transactionId;
    }

    private async Task<ImportRun> SeedRunWithSetAsideRowsAsync(int count)
    {
        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), "test-export.csv", ImportRunKind.HistoryMigration, accountId: null, HandedOverAt);
        for (int rowNumber = 1; rowNumber <= count; rowNumber++)
        {
            run.SetAside(
                Guid.CreateVersion7(),
                rowNumber,
                SetAsideReason.NoRuleCovers,
                $"TEST CATEGORY {rowNumber}",
                $"row {rowNumber}\tTEST SHOP {rowNumber}\t-{rowNumber}.5000");
        }

        await using FinNavisDbContext context = _database.CreateDbContext();
        context.ImportRuns.Add(run);
        await context.SaveChangesAsync();

        return run;
    }

    private async Task ConfirmRulesAsync(Guid groceriesId)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(ConfirmPath, new
        {
            shape_key = SyntheticFile.StatementShapeKey,
            rules = new object[]
            {
                new { source_category = GroceriesInFile, target = "Category", target_category_id = (Guid?)groceriesId },
                new { source_category = OwnCardInFile, target = "TransferBetweenOwnAccounts", target_category_id = (Guid?)null },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> CreateAccountAsync(string name)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        CreateAccount createAccount = scope.ServiceProvider.GetRequiredService<CreateAccount>();

        Account account = await createAccount.ExecuteAsync(name, CancellationToken.None);

        return account.Id;
    }

    private async Task<Guid> CreateCategoryAsync(string name)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        CreateCategory createCategory = scope.ServiceProvider.GetRequiredService<CreateCategory>();

        Category category = await createCategory.ExecuteAsync(name, CategoryKind.Expense, null, CancellationToken.None);

        return category.Id;
    }
}
