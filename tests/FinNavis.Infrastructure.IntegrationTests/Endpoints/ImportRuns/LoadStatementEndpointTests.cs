using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinNavis.Application.Accounts;
using FinNavis.Application.Categories;
using FinNavis.Application.Common;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

[Collection(DatabaseCollection.Name)]
public sealed partial class LoadStatementEndpointTests : IAsyncLifetime
{
    private const string LoadPath = "/api/v1/import-runs/statement";

    private const string ConfirmPath = "/api/v1/mapping-profiles";

    private const string FileName = "test-statement-2026-03.csv";

    private const string GroceriesInFile = "Продукти";

    private const string CafeInFile = "Кафе";

    private const string OwnCardInFile = "Переказ";

    private const string ForeignCurrency = "XTS";

    private static readonly string[] RunKeys =
    [
        "import_run_id", "kind", "status", "file_name", "account_id", "recorded_row_count",
        "already_present_row_count", "created_at", "set_aside",
    ];

    private static readonly string[] SetAsideKeys = ["row_number", "reason", "source_category", "raw_line"];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public LoadStatementEndpointTests(PostgresFixture database)
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

    // A load that set rows aside finished, so it is a 200 carrying every one of them — not an error
    // and not a 207. The run id travels in the body, so there is nothing to put in a Location header.
    [Fact]
    public async Task Post_StatementWithCoveredAndSetAsideRows_Returns200WithTheCountsAndEverySetAsideRow()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        string[] rows = StatementRows(LedgerCurrency());

        HttpResponseMessage response = await LoadAsync(Statement(rows), walletId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
        JsonElement body = await ReadJsonAsync(response);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(RunKeys);
        KeysIn(body).Should().AllSatisfy(key => key.Should().MatchRegex(SnakeCaseKey()));
        body.GetProperty("kind").GetString().Should().Be("Statement");
        body.GetProperty("status").GetString().Should().Be("Completed");
        body.GetProperty("file_name").GetString().Should().Be(FileName);
        body.GetProperty("account_id").GetGuid().Should().Be(walletId);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(1);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(0);
        body.GetProperty("created_at").GetDateTimeOffset()
            .Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));

        JsonElement[] setAside = [.. body.GetProperty("set_aside").EnumerateArray()];
        setAside.Should().HaveCount(3).And.AllSatisfy(
            row => row.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(SetAsideKeys));
        setAside.Select(row => row.GetProperty("row_number").GetInt32()).Should().Equal(4, 5, 6);
        setAside.Select(row => row.GetProperty("reason").GetString())
            .Should().Equal("NoRuleCovers", "TransferBetweenOwnAccounts", "ForeignCurrency");
        setAside.Select(row => row.GetProperty("source_category").GetString())
            .Should().Equal(CafeInFile, OwnCardInFile, GroceriesInFile);
        setAside.Select(row => row.GetProperty("raw_line").GetString()).Should().Equal(rows[1..]);

        await using FinNavisDbContext context = _database.CreateDbContext();
        ImportRun stored = await context.ImportRuns.AsNoTracking().SingleAsync();
        body.GetProperty("import_run_id").GetGuid().Should().Be(stored.Id);
        (await context.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Post_SameStatementAgain_RecordsNothingTwiceAndCountsTheRowAsAlreadyThere()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        byte[] statement = Statement(StatementRows(LedgerCurrency()));
        (await LoadAsync(statement, walletId)).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage again = await LoadAsync(statement, walletId);

        again.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(again);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(0);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(1);
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Transactions.CountAsync()).Should().Be(1);
        (await context.ImportRuns.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Post_AccountNotInTheLedger_Returns404AccountNotFoundAndCreatesNothing()
    {
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        Guid unknownAccountId = Guid.CreateVersion7();

        HttpResponseMessage response = await LoadAsync(Statement(StatementRows(LedgerCurrency())), unknownAccountId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Account.NotFound");
        body.GetProperty("message").GetString().Should().Be(
            "You do not have an account with that name, so there was nothing to load this file onto. "
            + "Nothing was recorded and no account was created.");
        body.GetProperty("details").GetProperty("account_id").GetGuid().Should().Be(unknownAccountId);
        await AssertLedgerHoldsNoLoadAsync(expectedAccounts: 0);
    }

    // The request is well-formed and names no profile: the server derived the shape from the file.
    // The next move is to inspect and confirm, not to fix an id the client sent.
    [Fact]
    public async Task Post_ShapeWithNoConfirmedRules_Returns409MappingProfileNotConfirmed()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");

        HttpResponseMessage response = await LoadAsync(Statement(StatementRows(LedgerCurrency())), walletId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be("Import.MappingProfileNotConfirmed");
        body.GetProperty("message").GetString().Should().Be(
            "There are no confirmed rules for this kind of file yet, so its rows had nowhere to go. "
            + "Nothing was recorded.");
        body.GetProperty("details").GetProperty("shape_key").GetString().Should().Be(SyntheticFile.StatementShapeKey);
        await AssertLedgerHoldsNoLoadAsync(expectedAccounts: 1);
    }

    [Fact]
    public async Task Post_StatementWithoutAnAmountColumn_Returns400MissingRequiredColumn()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        string[] header = [.. SyntheticFile.StatementHeader.Where(column => column != "Сума в валюті транзакції")];
        byte[] file = SyntheticFile.Statement(
            SyntheticFile.StatementTitle("Виписка за період 01.03.2026 - 31.03.2026"),
            header,
            "01.03.2026 10:00:00,Продукти,**** 0001,Test Shop One,-12.5,EUR,EUR,100,EUR");

        HttpResponseMessage response = await LoadAsync(file, walletId);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.MissingRequiredColumn");
        body.GetProperty("message").GetString()
            .Should().Be("This file does not say the date or the amount, so nothing can be recorded from it.");
        body.GetProperty("details").GetProperty("missing").EnumerateArray()
            .Select(column => column.GetString()).Should().Equal("amount");
        await AssertLedgerHoldsNoLoadAsync(expectedAccounts: 1);
    }

    [Fact]
    public async Task Post_FileTheReaderRefusesAsNotAStatement_Returns400FileNotReadable()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        using WebApplicationFactory<Program> host = _host.WithRowCeilingOfOne();
        using HttpClient client = host.CreateClient();
        using MultipartFormDataContent upload =
            ImportUpload.Statement(Statement(StatementRows(LedgerCurrency())), walletId, FileName);

        HttpResponseMessage response = await client.PostAsync(LoadPath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.FileNotReadable");
        body.GetProperty("message").GetString()
            .Should().Be("This file could not be read as a statement, so nothing was recorded.");
        await AssertLedgerHoldsNoLoadAsync(expectedAccounts: 1);
    }

    [Fact]
    public async Task Post_CommitFailsPartWay_Returns500NotRecordedAndNothingIsInTheLedger()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(await CreateCategoryAsync("Test groceries"));
        using WebApplicationFactory<Program> host = _host.WithCommitFailingPartWay(_database);
        using HttpClient client = host.CreateClient();
        using MultipartFormDataContent upload =
            ImportUpload.Statement(Statement(StatementRows(LedgerCurrency())), walletId, FileName);

        HttpResponseMessage response = await client.PostAsync(LoadPath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.InternalServerError);
        body.GetProperty("code").GetString().Should().Be("Import.NotRecorded");
        body.GetProperty("message").GetString().Should().Be(
            "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.");
        await AssertLedgerHoldsNoLoadAsync(expectedAccounts: 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-id")]
    public async Task Post_AccountIdMissingOrNotAnId_Returns400InvalidRequest(string? accountId)
    {
        using MultipartFormDataContent upload = ImportUpload.Export(Statement(StatementRows("EUR")), FileName);
        if (accountId is not null)
        {
            upload.Add(new StringContent(accountId), "account_id");
        }

        HttpResponseMessage response = await _client.PostAsync(LoadPath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    // One row per outcome a statement load has: recorded, no rule, a move between own accounts, and
    // a purchase in a currency the ledger does not keep.
    private static string[] StatementRows(string ledgerCurrency) =>
    [
        $"01.03.2026 10:00:00,{GroceriesInFile},**** 0001,Test Shop One,-12.5,{ledgerCurrency},12.5,{ledgerCurrency},100,{ledgerCurrency}",
        $"02.03.2026 10:00:00,{CafeInFile},**** 0001,Test Cafe,-4.8,{ledgerCurrency},4.8,{ledgerCurrency},95.2,{ledgerCurrency}",
        $"03.03.2026 10:00:00,{OwnCardInFile},**** 0001,Test Own Card,-200,{ledgerCurrency},200,{ledgerCurrency},-104.8,{ledgerCurrency}",
        $"04.03.2026 10:00:00,{GroceriesInFile},**** 0001,Test Shop Two,-30,{ledgerCurrency},30,{ForeignCurrency},-134.8,{ledgerCurrency}",
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

    private string LedgerCurrency() => _host.Services.GetRequiredService<LedgerOptions>().Currency;

    private async Task<HttpResponseMessage> LoadAsync(byte[] statement, Guid accountId)
    {
        using MultipartFormDataContent upload = ImportUpload.Statement(statement, accountId, FileName);

        return await _client.PostAsync(LoadPath, upload);
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

    private async Task AssertLedgerHoldsNoLoadAsync(int expectedAccounts)
    {
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.ImportRuns.CountAsync()).Should().Be(0);
        (await context.Transactions.CountAsync()).Should().Be(0);
        (await context.SetAsideRows.CountAsync()).Should().Be(0);
        (await context.Accounts.CountAsync()).Should().Be(expectedAccounts);
    }
}
