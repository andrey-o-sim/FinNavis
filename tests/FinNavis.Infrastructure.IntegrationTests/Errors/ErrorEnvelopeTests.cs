using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

[Collection(DatabaseCollection.Name)]
public sealed class ErrorEnvelopeTests : IDisposable
{
    private readonly PostgresFixture _database;

    private readonly ErrorTestHost _host;

    private readonly HttpClient _client;

    public ErrorEnvelopeTests(PostgresFixture database)
    {
        _database = database;
        _host = new ErrorTestHost(database, Environments.Development);
        _client = _host.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    [Fact]
    public async Task Get_UnmappedRoute_ReturnsRouteNotFoundEnvelope()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/no-such-route");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Route.NotFound");
    }

    [Fact]
    public async Task Delete_RouteThatOnlyAnswersGet_ReturnsMethodNotAllowedEnvelope()
    {
        HttpResponseMessage response = await _client.DeleteAsync(ProbeRoutes.Numbers);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.MethodNotAllowed);
        body.GetProperty("code").GetString().Should().Be("Request.MethodNotAllowed");
    }

    [Fact]
    public async Task Get_QueryValueThatDoesNotBind_ReturnsInvalidRequestEnvelope()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ProbeRoutes.Numbers}?count=not-a-number");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Post_BodyThatIsNotJson_ReturnsInvalidRequestEnvelope()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync(ProbeRoutes.Body, content);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Post_BodyWithWrongContentType_ReturnsUnsupportedMediaTypeEnvelope()
    {
        using var content = new StringContent("display_name=x", Encoding.UTF8, "text/plain");

        HttpResponseMessage response = await _client.PostAsync(ProbeRoutes.Body, content);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.UnsupportedMediaType);
        body.GetProperty("code").GetString().Should().Be("Request.UnsupportedMediaType");
    }

    [Fact]
    public async Task Get_HandlerReturningValidationProblem_ReturnsEnvelopeWithSnakeCaseDetails()
    {
        HttpResponseMessage response = await _client.GetAsync(ProbeRoutes.ValidationProblem);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.ValidationFailed");
        body.GetProperty("details").GetProperty("display_name")[0].GetString().Should().Be("Too long.");
    }

    [Fact]
    public async Task Get_HandlerReturningProblem_ReturnsEnvelopeWithTheSameStatus()
    {
        HttpResponseMessage response = await _client.GetAsync(ProbeRoutes.Problem);

        await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_UnknownAccountId_ReturnsAccountNotFoundWithThePrdWording()
    {
        Guid unknownId = Guid.CreateVersion7();

        HttpResponseMessage response =
            await _client.GetAsync(string.Format(ProbeRoutes.AccountBalances, unknownId));

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Account.NotFound");
        body.GetProperty("message").GetString().Should().Be($"No account in the ledger has id {unknownId}.");
        body.TryGetProperty("details", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Get_RefusalWithDetails_ReturnsContractStatusAndSnakeCaseDetailKeys()
    {
        HttpResponseMessage response = await _client.GetAsync(ProbeRoutes.RefusalWithDetails);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.Conflict);
        body.GetProperty("code").GetString().Should().Be("Account.HasRecordedHistory");
        body.GetProperty("message").GetString()
            .Should().Be("Recorded history depends on this account, so it cannot be deleted.");
        body.GetProperty("details").GetProperty("transaction_count").GetInt32().Should().Be(42);
        body.GetProperty("details").GetProperty("transfer_count").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Get_UnexpectedException_ReturnsServerEnvelopeWithoutTheExceptionText()
    {
        HttpResponseMessage response = await _client.GetAsync(ProbeRoutes.Unexpected);

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.InternalServerError);
        body.GetProperty("code").GetString().Should().Be("Server.Unexpected");
        body.GetRawText().Should().NotContain(ProbeRoutes.UnexpectedMessage);
    }

    [Fact]
    public async Task Get_QueryValueThatDoesNotBindOutsideDevelopment_ReturnsInvalidRequestEnvelope()
    {
        // Outside Development a binding failure is not thrown: ASP.NET Core sets 400 and leaves
        // the body empty, which is the path status code pages has to fill.
        using var production = new ErrorTestHost(_database, Environments.Production);
        using HttpClient client = production.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"{ProbeRoutes.Numbers}?count=not-a-number");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.BadRequest);
        body.GetProperty("code").GetString().Should().Be("Request.Invalid");
    }

    [Fact]
    public async Task Get_UnmappedRouteOutsideDevelopment_ReturnsRouteNotFoundEnvelope()
    {
        using var production = new ErrorTestHost(_database, Environments.Production);
        using HttpClient client = production.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/no-such-route");

        JsonElement body = await response.ShouldBeEnvelopeAsync(HttpStatusCode.NotFound);
        body.GetProperty("code").GetString().Should().Be("Route.NotFound");
    }
}
