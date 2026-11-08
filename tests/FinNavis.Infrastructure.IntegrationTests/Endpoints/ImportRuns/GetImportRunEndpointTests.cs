using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

[Collection(DatabaseCollection.Name)]
public sealed partial class GetImportRunEndpointTests : IAsyncLifetime
{
    private const string NotMintedMessage = "That is not a cursor this server handed out.";

    private static readonly DateTimeOffset HandedOverAt = new(2026, 10, 21, 9, 30, 0, TimeSpan.Zero);

    private static readonly string[] RunKeys =
    [
        "import_run_id", "kind", "status", "file_name", "account_id", "recorded_row_count",
        "already_present_row_count", "created_at", "set_aside",
    ];

    private static readonly string[] PageKeys = ["items", "has_next", "has_prev", "next_cursor", "prev_cursor"];

    private static readonly string[] SetAsideKeys = ["row_number", "reason", "source_category", "raw_line"];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public GetImportRunEndpointTests(PostgresFixture database)
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
    public async Task Get_RunWithSetAsideRows_Returns200WithTheContractShapeInSnakeCase()
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [9, 4]);

        HttpResponseMessage response = await _client.GetAsync(RunPath(run.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        JsonElement body = await ReadJsonAsync(response);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(RunKeys);
        KeysIn(body).Should().AllSatisfy(key => key.Should().MatchRegex(SnakeCaseKey()));
        body.GetProperty("import_run_id").GetGuid().Should().Be(run.Id);
        body.GetProperty("kind").GetString().Should().Be("HistoryMigration");
        body.GetProperty("status").GetString().Should().Be("Completed");
        body.GetProperty("file_name").GetString().Should().Be("test-export.csv");
        body.GetProperty("account_id").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(0);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(1);
        body.GetProperty("created_at").GetDateTimeOffset().Should().Be(HandedOverAt);

        JsonElement setAside = body.GetProperty("set_aside");
        setAside.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(PageKeys);
        setAside.GetProperty("has_next").GetBoolean().Should().BeFalse();
        setAside.GetProperty("has_prev").GetBoolean().Should().BeFalse();
        setAside.GetProperty("next_cursor").ValueKind.Should().Be(JsonValueKind.Null);
        setAside.GetProperty("prev_cursor").ValueKind.Should().Be(JsonValueKind.Null);
        JsonElement[] items = [.. setAside.GetProperty("items").EnumerateArray()];
        items.Should().AllSatisfy(
            row => row.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(SetAsideKeys));
        items.Select(row => row.GetProperty("row_number").GetInt32()).Should().Equal(4, 9);
        items[0].GetProperty("reason").GetString().Should().Be("NoRuleCovers");
        items[0].GetProperty("source_category").GetString().Should().Be("TEST CATEGORY 4");
        items[0].GetProperty("raw_line").GetString().Should().Be("2026-10-04\tTEST SHOP 4\t-4.5000");
    }

    [Fact]
    public async Task Get_NoLimitGiven_ReturnsTheFirstFiftyRowsAndANextCursor()
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [.. Enumerable.Range(1, 60)]);

        JsonElement page = await ReadPageAsync(RunPath(run.Id));

        Items(page).Should().Equal(Enumerable.Range(1, 50));
        page.GetProperty("has_next").GetBoolean().Should().BeTrue();
        page.GetProperty("has_prev").GetBoolean().Should().BeFalse();
        page.GetProperty("next_cursor").GetString().Should().NotBeNullOrEmpty();
        page.GetProperty("prev_cursor").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // The client never builds a cursor: it walks forward with the next_cursor and back with the
    // prev_cursor the server handed out, and the rows come in row_number order.
    [Fact]
    public async Task Get_FollowingTheMintedCursors_WalksThePagesForwardAndBackInRowNumberOrder()
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [30, 2, 17, 5, 11]);

        JsonElement first = await ReadPageAsync($"{RunPath(run.Id)}?limit=2");
        JsonElement second = await ReadPageAsync(
            $"{RunPath(run.Id)}?limit=2&after={Escape(first.GetProperty("next_cursor").GetString())}");
        JsonElement last = await ReadPageAsync(
            $"{RunPath(run.Id)}?limit=2&after={Escape(second.GetProperty("next_cursor").GetString())}");
        JsonElement back = await ReadPageAsync(
            $"{RunPath(run.Id)}?limit=2&before={Escape(second.GetProperty("prev_cursor").GetString())}");

        Items(first).Should().Equal(2, 5);
        Items(second).Should().Equal(11, 17);
        second.GetProperty("has_prev").GetBoolean().Should().BeTrue();
        second.GetProperty("has_next").GetBoolean().Should().BeTrue();
        Items(last).Should().Equal(30);
        last.GetProperty("has_next").GetBoolean().Should().BeFalse();
        last.GetProperty("next_cursor").ValueKind.Should().Be(JsonValueKind.Null);
        Items(back).Should().Equal(2, 5);
        back.GetProperty("has_prev").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task Get_LimitOutsideOneToTwoHundred_Returns400InvalidPaging(int limit)
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [1]);

        HttpResponseMessage response = await _client.GetAsync($"{RunPath(run.Id)}?limit={limit}");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.InvalidPaging");
        body.GetProperty("message").GetString().Should().Be("'limit' must be between 1 and 200.");
        body.GetProperty("details").GetProperty("limit").GetInt32().Should().Be(limit);
    }

    [Theory]
    [InlineData("after", "not-a-cursor")]
    [InlineData("before", "not-a-cursor")]
    [InlineData("after", "1")]
    public async Task Get_MalformedCursor_Returns400InvalidPaging(string parameter, string cursor)
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [1, 2]);

        HttpResponseMessage response = await _client.GetAsync($"{RunPath(run.Id)}?{parameter}={cursor}");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.InvalidPaging");
        body.GetProperty("message").GetString().Should().Be(NotMintedMessage);
    }

    [Fact]
    public async Task Get_CursorMintedForAnotherRun_Returns400InvalidPaging()
    {
        ImportRun other = await SeedRunAsync(rowNumbers: [1, 2]);
        ImportRun run = await SeedRunAsync(rowNumbers: [1, 2]);
        JsonElement otherPage = await ReadPageAsync($"{RunPath(other.Id)}?limit=1");

        HttpResponseMessage response = await _client.GetAsync(
            $"{RunPath(run.Id)}?after={Escape(otherPage.GetProperty("next_cursor").GetString())}");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.InvalidPaging");
        body.GetProperty("message").GetString().Should().Be(NotMintedMessage);
    }

    [Fact]
    public async Task Get_UnknownRun_Returns404ImportNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response = await _client.GetAsync(RunPath(unknownId));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Import.NotFound");
        body.GetProperty("message").GetString().Should().Be($"No load in this installation has id {unknownId}.");
    }

    [Fact]
    public async Task Get_LimitThatIsNotANumber_Returns400RequestInvalid()
    {
        ImportRun run = await SeedRunAsync(rowNumbers: [1]);

        HttpResponseMessage response = await _client.GetAsync($"{RunPath(run.Id)}?limit=not-a-number");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    private static string RunPath(Guid importRunId) => $"/api/v1/import-runs/{importRunId}";

    private static string Escape(string? cursor) => Uri.EscapeDataString(cursor!);

    private static IEnumerable<int> Items(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(row => row.GetProperty("row_number").GetInt32());

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

    private async Task<JsonElement> ReadPageAsync(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await ReadJsonAsync(response)).GetProperty("set_aside");
    }

    private async Task<ImportRun> SeedRunAsync(int[] rowNumbers)
    {
        ImportRun run = ImportRun.Record(
            Guid.CreateVersion7(), "test-export.csv", ImportRunKind.HistoryMigration, accountId: null, HandedOverAt);
        run.CountAlreadyPresentRow();
        foreach (int rowNumber in rowNumbers)
        {
            run.SetAside(
                Guid.CreateVersion7(),
                rowNumber,
                SetAsideReason.NoRuleCovers,
                $"TEST CATEGORY {rowNumber}",
                $"2026-10-{rowNumber:00}\tTEST SHOP {rowNumber}\t-{rowNumber}.5000");
        }

        await using FinNavisDbContext context = _database.CreateDbContext();
        context.ImportRuns.Add(run);
        await context.SaveChangesAsync();

        return run;
    }
}
