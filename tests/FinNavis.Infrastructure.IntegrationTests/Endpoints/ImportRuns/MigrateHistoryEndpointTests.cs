using System.Net;
using System.Text.Json;
using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

[Collection(DatabaseCollection.Name)]
public sealed class MigrateHistoryEndpointTests : IAsyncLifetime
{
    private const string MigratePath = "/api/v1/import-runs/history-migration";

    private const string ExportFileName = "test-export.csv";

    private static readonly string[] RunKeys =
    [
        "import_run_id", "kind", "status", "file_name", "account_id", "recorded_row_count",
        "already_present_row_count", "created_at", "set_aside",
    ];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public MigrateHistoryEndpointTests(PostgresFixture database)
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

    // An export names an account on every row, so the run itself names none. The run id travels in
    // the body, so there is nothing to put in a Location header.
    [Fact]
    public async Task Post_SampleExport_Returns200WithTheCountsAndNoAccountOfItsOwn()
    {
        byte[] export = await File.ReadAllBytesAsync(SampleFiles.ExportPath);

        HttpResponseMessage response = await MigrateAsync(export);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
        JsonElement body = await ReadJsonAsync(response);
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(RunKeys);
        body.GetProperty("kind").GetString().Should().Be("HistoryMigration");
        body.GetProperty("status").GetString().Should().Be("Completed");
        body.GetProperty("file_name").GetString().Should().Be(ExportFileName);
        body.GetProperty("account_id").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(0);
        body.GetProperty("set_aside").ValueKind.Should().Be(JsonValueKind.Array);
        body.GetProperty("set_aside").GetArrayLength().Should().Be(0);

        await using FinNavisDbContext context = _database.CreateDbContext();
        ImportRun stored = await context.ImportRuns.AsNoTracking().SingleAsync();
        body.GetProperty("import_run_id").GetGuid().Should().Be(stored.Id);
        int recorded = body.GetProperty("recorded_row_count").GetInt32();
        recorded.Should().BePositive();
        (await context.Transactions.CountAsync()).Should().Be(recorded);
        (await context.Accounts.CountAsync()).Should().BePositive();
    }

    [Fact]
    public async Task Post_SameExportAgain_RecordsNothingTwiceAndCountsEveryRowAsAlreadyThere()
    {
        byte[] export = await File.ReadAllBytesAsync(SampleFiles.ExportPath);
        int firstRecorded = (await ReadJsonAsync(await MigrateAsync(export))).GetProperty("recorded_row_count").GetInt32();

        HttpResponseMessage again = await MigrateAsync(export);

        again.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement body = await ReadJsonAsync(again);
        body.GetProperty("recorded_row_count").GetInt32().Should().Be(0);
        body.GetProperty("already_present_row_count").GetInt32().Should().Be(firstRecorded);
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.Transactions.CountAsync()).Should().Be(firstRecorded);
    }

    [Fact]
    public async Task Post_ExportWithoutAnAccountColumn_Returns400MissingRequiredColumn()
    {
        string[] header = [.. SyntheticFile.ExportHeader.Where(column => column != "Account")];
        byte[] export = SyntheticFile.Export(
            header,
            ["2023.01.02", "-468", string.Empty, "Books", "Leisure", "Test Shop"]);

        HttpResponseMessage response = await MigrateAsync(export);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.MissingRequiredColumn");
        body.GetProperty("message").GetString().Should().Be(
            "This file does not say the date or the amount, or the account each row belongs to, "
            + "so nothing can be recorded from it.");
        body.GetProperty("details").GetProperty("missing").EnumerateArray()
            .Select(column => column.GetString()).Should().Equal("account");
        await AssertLedgerIsEmptyAsync();
    }

    [Fact]
    public async Task Post_FileTheReaderRefusesAsNotAnExport_Returns400FileNotReadable()
    {
        using WebApplicationFactory<Program> host = _host.WithRowCeilingOfOne();
        using HttpClient client = host.CreateClient();
        using MultipartFormDataContent upload =
            ImportUpload.Export(await File.ReadAllBytesAsync(SampleFiles.ExportPath), ExportFileName);

        HttpResponseMessage response = await client.PostAsync(MigratePath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.FileNotReadable");
        body.GetProperty("message").GetString()
            .Should().Be("This file could not be read as a statement, so nothing was recorded.");
        await AssertLedgerIsEmptyAsync();
    }

    // The accounts and categories the export would have created are part of the one commit, so a
    // failure part-way leaves none of them behind either.
    [Fact]
    public async Task Post_CommitFailsPartWay_Returns500NotRecordedAndNothingIsInTheLedger()
    {
        using WebApplicationFactory<Program> host = _host.WithCommitFailingPartWay(_database);
        using HttpClient client = host.CreateClient();
        byte[] export = SyntheticFile.Export(
            SyntheticFile.ExportHeader,
            ["2023.01.02", "-468", string.Empty, "Test cash wallet (UAH)", "Test fuel", "Test car", "Test Fuel Stop"],
            ["2023.01.03", "-375", string.Empty, "Test savings jar (UAH)", "Test books", "Test leisure", "Test Books"]);
        using MultipartFormDataContent upload = ImportUpload.Export(export, ExportFileName);

        HttpResponseMessage response = await client.PostAsync(MigratePath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.InternalServerError);
        body.GetProperty("code").GetString().Should().Be("Import.NotRecorded");
        body.GetProperty("message").GetString().Should().Be(
            "The load stopped before it finished, so nothing from it was recorded and every balance is unchanged.");
        await AssertLedgerIsEmptyAsync();
    }

    [Fact]
    public async Task Post_FormWithoutAFilePart_Returns400InvalidRequest()
    {
        using MultipartFormDataContent form = new() { { new StringContent("not a file"), "note" } };

        HttpResponseMessage response = await _client.PostAsync(MigratePath, form);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private async Task<HttpResponseMessage> MigrateAsync(byte[] export)
    {
        using MultipartFormDataContent upload = ImportUpload.Export(export, ExportFileName);

        return await _client.PostAsync(MigratePath, upload);
    }

    private async Task AssertLedgerIsEmptyAsync()
    {
        await using FinNavisDbContext context = _database.CreateDbContext();
        (await context.ImportRuns.CountAsync()).Should().Be(0);
        (await context.Transactions.CountAsync()).Should().Be(0);
        (await context.Accounts.CountAsync()).Should().Be(0);
        (await context.Categories.CountAsync()).Should().Be(0);
    }
}
