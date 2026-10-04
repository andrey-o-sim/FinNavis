using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinNavis.Infrastructure.IntegrationTests.Fakes;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints;

[Collection("Database")]
public sealed class GetAccountByIdEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private readonly AccountsApiFactory _factory;
    private readonly HttpClient _client;

    public GetAccountByIdEndpointTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _factory = new AccountsApiFactory(postgresFixture.ConnectionString, new MutableClock());
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _postgresFixture.ResetAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Get_IdExists_Returns200WithMatchingBody()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new { name = "Cash", type = "Cash", openingBalance = 50m, note = "n" });
        var createdBody = await created.Content.ReadFromJsonAsync<AccountItem>();

        var response = await _client.GetAsync($"/accounts/{createdBody!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountItem>();
        body!.Name.Should().Be("Cash");
        body.OpeningBalance.Should().Be(50m);
        body.Note.Should().Be("n");
    }

    [Fact]
    public async Task Get_IdExists_ResponseHasNoCreationTimestampProperty()
    {
        var created = await _client.PostAsJsonAsync("/accounts", new { name = "Cash", type = "Cash", openingBalance = 0m, note = (string?)null });
        var createdBody = await created.Content.ReadFromJsonAsync<AccountItem>();

        var response = await _client.GetAsync($"/accounts/{createdBody!.Id}");
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var propertyNames = document!.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        propertyNames.Should().BeEquivalentTo(["id", "name", "type", "openingBalance", "note"]);
    }

    [Fact]
    public async Task Get_IdDoesNotExist_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/accounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private sealed record AccountItem(Guid Id, string Name, string Type, decimal OpeningBalance, string? Note);
}
