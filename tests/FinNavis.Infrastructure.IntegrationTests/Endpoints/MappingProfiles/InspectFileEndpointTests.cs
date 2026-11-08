using System.Net;
using System.Text;
using System.Text.Json;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.MappingProfiles;

[Collection(DatabaseCollection.Name)]
public sealed class InspectFileEndpointTests : IAsyncLifetime
{
    private const string InspectPath = "/api/v1/mapping-profiles/inspect";

    private const string MissingColumnMessage =
        "This file does not say the date or the amount, so nothing can be recorded from it.";

    private const string BakeryRow =
        "05.03.2026 14:22:10,Продукти,**** 0001,Corner Bakery,-45.5,UAH,45.5,UAH,24954.5,UAH";

    private static readonly string[] SampleStatementCategories =
        ["Продукти", "Кафе", "Книги", "Надходження", "Пальне", "Подорожі", "Транспорт"];

    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public InspectFileEndpointTests(PostgresFixture database)
    {
        _database = database;
        _host = new ErrorTestHost(database, Environments.Development);
        _client = _host.CreateClient();
    }

    public static TheoryData<string, string> MissingColumns => new()
    {
        { "Сума в валюті транзакції", "amount" },
        { "Дата", "date" },
    };

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _host.Dispose();

        return Task.CompletedTask;
    }

    // The use case reads the header and then the rows from the one uploaded stream. Every category
    // of the sample coming back proves the second pass started at the top of the file.
    [Fact]
    public async Task Post_SampleStatement_ReturnsTheShapeKeyAndEveryCategoryOfTheFileOnce()
    {
        using MultipartFormDataContent upload =
            StatementUpload.Of(await File.ReadAllBytesAsync(SampleFiles.StatementPath));

        HttpResponseMessage response = await _client.PostAsync(InspectPath, upload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        JsonElement body = await ReadJsonAsync(response);
        body.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(["shape_key", "mapping_profile_id", "source_categories"]);
        body.GetProperty("shape_key").GetString().Should().Be(SyntheticFile.StatementShapeKey);
        body.GetProperty("mapping_profile_id").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("source_categories").EnumerateArray().Select(category => category.GetString())
            .Should().BeEquivalentTo(SampleStatementCategories);
    }

    [Theory]
    [MemberData(nameof(MissingColumns))]
    public async Task Post_StatementWithoutDateOrAmount_ReturnsMissingRequiredColumnNamingWhich(
        string droppedColumn,
        string missingName)
    {
        string[] header = [.. SyntheticFile.StatementHeader.Where(column => column != droppedColumn)];
        byte[] file = SyntheticFile.Statement(
            SyntheticFile.StatementTitle("Виписка за період 01.03.2026 - 31.03.2026"),
            header,
            BakeryRow);
        using MultipartFormDataContent upload = StatementUpload.Of(file);

        HttpResponseMessage response = await _client.PostAsync(InspectPath, upload);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.MissingRequiredColumn");
        body.GetProperty("message").GetString().Should().Be(MissingColumnMessage);
        body.GetProperty("details").GetProperty("missing").EnumerateArray()
            .Select(column => column.GetString()).Should().Equal(missingName);
    }

    [Fact]
    public async Task Post_FormWithoutAFilePart_ReturnsInvalidRequestEnvelope()
    {
        using MultipartFormDataContent form = new() { { new StringContent("not a file"), "note" } };

        HttpResponseMessage response = await _client.PostAsync(InspectPath, form);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Post_FileSentAsAPlainBody_ReturnsUnsupportedMediaTypeEnvelope()
    {
        using ByteArrayContent plainBody = new(Encoding.UTF8.GetBytes("Date,Amount\n01.03.2026,-1.00\n"));

        HttpResponseMessage response = await _client.PostAsync(InspectPath, plainBody);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.UnsupportedMediaType);
        body.GetProperty("code").GetString().Should().Be("Request.UnsupportedMediaType");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
