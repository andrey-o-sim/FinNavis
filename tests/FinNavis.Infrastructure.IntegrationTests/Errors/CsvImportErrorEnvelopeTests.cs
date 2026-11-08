using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

[Collection(DatabaseCollection.Name)]
public sealed class CsvImportErrorEnvelopeTests : IDisposable
{
    private static readonly byte[] StatementWithoutAmount = SyntheticFile.Statement(
        SyntheticFile.StatementTitle("Виписка за період 01.03.2026 - 31.03.2026"),
        ["Дата", "Категорія", "Опис операції"],
        "01.03.2026 19:08:34,Продукти,Northside Grocer");

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public CsvImportErrorEnvelopeTests(PostgresFixture database)
    {
        _host = new ErrorTestHost(database, Environments.Development);
        _client = _host.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    [Theory]
    [InlineData("/api/v1/import-runs/01937a1e-0000-7000-8000-00000000dead/no-such-action")]
    [InlineData("/api/v1/mapping-profiles/01937a1e-0000-7000-8000-00000000dead/no-such-action")]
    public async Task Get_UnmappedRouteUnderAnImportResource_ReturnsRouteNotFoundEnvelope(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(path);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Route.NotFound");
    }

    [Fact]
    public async Task Get_ImportRunWithLimitThatDoesNotBind_ReturnsInvalidRequestEnvelope()
    {
        string path = string.Format(ImportProbeRoutes.ImportRunRoute, Guid.CreateVersion7());

        HttpResponseMessage response = await _client.GetAsync($"{path}?limit=not-a-number");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Get_UnknownImportRunId_ReturnsImportNotFoundEnvelope()
    {
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response =
            await _client.GetAsync(string.Format(ImportProbeRoutes.ImportRunRoute, unknownId));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Import.NotFound");
        body.GetProperty("message").GetString().Should().Be($"No load in this installation has id {unknownId}.");
    }

    [Fact]
    public async Task Post_UndoOfUnknownImportRunId_ReturnsImportNotFoundEnvelope()
    {
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response =
            await _client.PostAsync(string.Format(ImportProbeRoutes.UndoRoute, unknownId), content: null);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Import.NotFound");
    }

    [Fact]
    public async Task Post_RuleNamingUnknownCategory_ReturnsCategoryNotFoundWithThePrdWording()
    {
        Guid unknownCategoryId = Guid.CreateVersion7();

        HttpResponseMessage response = await _client.PostAsync(
            string.Format(ImportProbeRoutes.ConfirmRoute, unknownCategoryId),
            content: null);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Category.NotFound");
        body.GetProperty("message").GetString()
            .Should().Be("There is no category with that name, so a rule cannot point at it.");
        body.GetProperty("details").GetProperty("source_category").GetString()
            .Should().Be(ImportProbeRoutes.SourceCategory);
        body.GetProperty("details").GetProperty("target_category_id").GetGuid().Should().Be(unknownCategoryId);
    }

    [Fact]
    public async Task Post_StatementWithoutAmountColumn_ReturnsMissingRequiredColumnWithThePrdWording()
    {
        using ByteArrayContent file = CsvFile(StatementWithoutAmount);

        HttpResponseMessage response = await _client.PostAsync(ImportProbeRoutes.InspectRoute, file);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Import.MissingRequiredColumn");
        body.GetProperty("message").GetString()
            .Should().Be("This file does not say the date or the amount, so nothing can be recorded from it.");
        body.GetProperty("details").GetProperty("missing").EnumerateArray()
            .Select(column => column.GetString()).Should().Equal("amount");
    }

    [Fact]
    public async Task Post_StatementForUnknownAccount_ReturnsAccountNotFoundWithThePrdWording()
    {
        Guid unknownAccountId = Guid.CreateVersion7();
        using ByteArrayContent file = CsvFile(StatementWithoutAmount);

        HttpResponseMessage response =
            await _client.PostAsync(string.Format(ImportProbeRoutes.LoadRoute, unknownAccountId), file);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Account.NotFound");
        body.GetProperty("message").GetString().Should().Be(
            "You do not have an account with that name, so there was nothing to load this file onto. "
                + "Nothing was recorded and no account was created.");
        body.GetProperty("details").GetProperty("account_id").GetGuid().Should().Be(unknownAccountId);
    }

    private static ByteArrayContent CsvFile(byte[] file)
    {
        ByteArrayContent content = new(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        return content;
    }
}
