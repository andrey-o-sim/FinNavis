using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinNavis.Infrastructure.IntegrationTests.Fakes;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints;

[Collection("Database")]
public sealed class ListAccountsEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private readonly AccountsApiFactory _factory;
    private readonly HttpClient _client;

    public ListAccountsEndpointTests(PostgresFixture postgresFixture)
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
    public async Task Get_MixedCaseNames_ReturnsThemInAlphabeticalOrder()
    {
        await _client.PostAsJsonAsync("/accounts", new { name = "Zebra", type = "Cash", openingBalance = 0m, note = (string?)null });
        await _client.PostAsJsonAsync("/accounts", new { name = "apple", type = "Cash", openingBalance = 0m, note = (string?)null });
        await _client.PostAsJsonAsync("/accounts", new { name = "Middle", type = "Cash", openingBalance = 0m, note = (string?)null });

        var response = await _client.GetAsync("/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var accounts = await response.Content.ReadFromJsonAsync<List<AccountListItem>>();

        accounts!.Select(a => a.Name).Should().Equal("apple", "Middle", "Zebra");
    }

    [Fact]
    public async Task Get_NoAccounts_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountListItem>>();

        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_HasAccounts_ItemsHaveNoCreationTimestampProperty()
    {
        await _client.PostAsJsonAsync("/accounts", new { name = "Cash", type = "Cash", openingBalance = 0m, note = (string?)null });

        var response = await _client.GetAsync("/accounts");
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var propertyNames = body!.RootElement[0].EnumerateObject().Select(p => p.Name).ToArray();

        propertyNames.Should().BeEquivalentTo(["id", "name", "type", "openingBalance", "note"]);
    }

    private sealed record AccountListItem(Guid Id, string Name, string Type, decimal OpeningBalance, string? Note);
}
