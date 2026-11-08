using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinNavis.Application.Accounts;
using FinNavis.Application.Categories;
using FinNavis.Application.Common;
using FinNavis.Application.Transactions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Quality;

// Every load goes through the real endpoint against a real database: the candidate snapshot is a
// SQL query over an account and a date range, so a test double would prove nothing about which rows
// it actually selects.
[Collection(DatabaseCollection.Name)]
public sealed class DuplicateSafetyTests : IAsyncLifetime
{
    private const string LoadPath = "/api/v1/import-runs/statement";

    private const string ConfirmPath = "/api/v1/mapping-profiles";

    private const string FileName = "test-statement-2026-03.csv";

    private const string GroceriesInFile = "Продукти";

    private const string CafeInFile = "Кафе";

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public DuplicateSafetyTests(PostgresFixture database)
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
    public async Task LoadStatement_SameFileTwice_RecordsNothingTheSecondTime()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(groceriesId: await CreateCategoryAsync("Test groceries"), cafeId: null);
        byte[] statement = Statement(
            GroceriesRow("01.03.2026", "Test Shop One", "12.5"),
            GroceriesRow("02.03.2026", "Test Shop Two", "30"),
            GroceriesRow("05.03.2026", "Test Shop Three", "7.25"));
        JsonElement first = await LoadAsync(statement, walletId);
        first.GetProperty("recorded_row_count").GetInt32().Should().Be(3);

        JsonElement second = await LoadAsync(statement, walletId);

        second.GetProperty("recorded_row_count").GetInt32().Should().Be(0);
        second.GetProperty("already_present_row_count").GetInt32().Should().Be(3);
        second.GetProperty("set_aside").GetArrayLength().Should().Be(0);
        JsonElement stored = await GetRunAsync(second.GetProperty("import_run_id").GetGuid());
        stored.GetProperty("already_present_row_count").GetInt32().Should().Be(3);
        stored.GetProperty("set_aside").GetProperty("items").GetArrayLength().Should().Be(0);
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Transactions.CountAsync()).Should().Be(3);
        (await context.SetAsideRows.CountAsync()).Should().Be(0);
    }

    // Two equal lines in one file are two real purchases, not one purchase written twice. The
    // snapshot is taken before the run writes anything, so the first line can never hide the second.
    [Fact]
    public async Task LoadStatement_FileCarriesTwoIdenticalRows_RecordsBoth()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        await ConfirmRulesAsync(groceriesId: await CreateCategoryAsync("Test groceries"), cafeId: null);
        string samePurchase = GroceriesRow("01.03.2026", "Test Shop One", "12.5");

        JsonElement run = await LoadAsync(Statement(samePurchase, samePurchase), walletId);

        run.GetProperty("recorded_row_count").GetInt32().Should().Be(2);
        run.GetProperty("already_present_row_count").GetInt32().Should().Be(0);
        await using FinNavisDbContext context = _database.CreateDbContext();
        List<Transaction> recorded = await context.Transactions.AsNoTracking().ToListAsync();
        recorded.Should().HaveCount(2).And.AllSatisfy(transaction =>
        {
            transaction.AccountId.Should().Be(walletId);
            transaction.Amount.Amount.Should().Be(12.5m);
            transaction.OccurredOn.Should().Be(new DateOnly(2026, 3, 1));
        });
    }

    // The bank's line is one day later than the day the owner typed, so only the proximity tier can
    // pair them: a hand-typed movement carries no external reference.
    [Fact]
    public async Task LoadStatement_RowOverlapsAHandTypedMovement_CountsItAsAlreadyThere()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        Guid groceriesId = await CreateCategoryAsync("Test groceries");
        await ConfirmRulesAsync(groceriesId, cafeId: null);
        Transaction typedByHand = await RecordByHandAsync(walletId, groceriesId, 12.5m, new DateOnly(2026, 3, 1));

        JsonElement run = await LoadAsync(Statement(GroceriesRow("02.03.2026", "Test Shop One", "12.5")), walletId);

        run.GetProperty("recorded_row_count").GetInt32().Should().Be(0);
        run.GetProperty("already_present_row_count").GetInt32().Should().Be(1);
        run.GetProperty("set_aside").GetArrayLength().Should().Be(0);
        await using FinNavisDbContext context = _database.CreateDbContext();
        Transaction onlyMovement = await context.Transactions.AsNoTracking().SingleAsync();
        onlyMovement.Id.Should().Be(typedByHand.Id);
        onlyMovement.ExternalRef.Should().BeNull();
        (await context.ImportTransactionLinks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task LoadStatement_RuleAddedForASetAsideRow_RecordsItOnTheSecondLoadAndNothingElseAgain()
    {
        Guid walletId = await CreateAccountAsync("Test wallet");
        Guid groceriesId = await CreateCategoryAsync("Test groceries");
        Guid cafeId = await CreateCategoryAsync("Test cafe");
        await ConfirmRulesAsync(groceriesId, cafeId: null);
        byte[] statement = Statement(
            GroceriesRow("01.03.2026", "Test Shop One", "12.5"),
            CafeRow("02.03.2026", "Test Cafe", "4.8"));
        JsonElement first = await LoadAsync(statement, walletId);
        first.GetProperty("recorded_row_count").GetInt32().Should().Be(1);
        first.GetProperty("set_aside").EnumerateArray().Select(row => row.GetProperty("reason").GetString())
            .Should().Equal("NoRuleCovers");

        await ConfirmRulesAsync(groceriesId, cafeId);
        JsonElement second = await LoadAsync(statement, walletId);

        second.GetProperty("recorded_row_count").GetInt32().Should().Be(1);
        second.GetProperty("already_present_row_count").GetInt32().Should().Be(1);
        second.GetProperty("set_aside").GetArrayLength().Should().Be(0);
        await using FinNavisDbContext context = _database.CreateDbContext();
        List<Transaction> recorded = await context.Transactions.AsNoTracking().ToListAsync();
        recorded.Select(transaction => (transaction.CategoryId, transaction.Amount.Amount))
            .Should().BeEquivalentTo([(groceriesId, 12.5m), (cafeId, 4.8m)]);
    }


    private static byte[] Statement(params string[] rows) =>
        SyntheticFile.Statement(
            SyntheticFile.StatementTitle("Виписка за період 01.03.2026 - 31.03.2026"),
            SyntheticFile.StatementHeader,
            rows);

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private string GroceriesRow(string day, string shop, string amount) => Row(day, GroceriesInFile, shop, amount);

    private string CafeRow(string day, string shop, string amount) => Row(day, CafeInFile, shop, amount);

    private string Row(string day, string sourceCategory, string shop, string amount)
    {
        string currency = _host.Services.GetRequiredService<LedgerOptions>().Currency;

        return $"{day} 10:00:00,{sourceCategory},**** 0001,{shop},-{amount},{currency},{amount},{currency},100,{currency}";
    }

    private async Task<JsonElement> LoadAsync(byte[] statement, Guid accountId)
    {
        using MultipartFormDataContent upload = ImportUpload.Statement(statement, accountId, FileName);

        HttpResponseMessage response = await _client.PostAsync(LoadPath, upload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadJsonAsync(response);
    }

    private async Task<JsonElement> GetRunAsync(Guid importRunId)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/import-runs/{importRunId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await ReadJsonAsync(response);
    }

    private async Task ConfirmRulesAsync(Guid groceriesId, Guid? cafeId)
    {
        List<object> rules = [new { source_category = GroceriesInFile, target = "Category", target_category_id = (Guid?)groceriesId }];
        if (cafeId is not null)
        {
            rules.Add(new { source_category = CafeInFile, target = "Category", target_category_id = cafeId });
        }

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            ConfirmPath, new { shape_key = SyntheticFile.StatementShapeKey, rules });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    private async Task<Transaction> RecordByHandAsync(Guid accountId, Guid categoryId, decimal amount, DateOnly occurredOn)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        RecordTransaction recordTransaction = scope.ServiceProvider.GetRequiredService<RecordTransaction>();

        return await recordTransaction.ExecuteAsync(accountId, categoryId, amount, occurredOn, note: null, CancellationToken.None);
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
