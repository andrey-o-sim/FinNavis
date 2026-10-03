using System.Net;
using System.Net.Http.Json;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Presentation.Contracts;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class AccountEndpointsTests(FinNavisApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        return factory.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Post_WithValidBody_Returns201WithLocationAndBody()
    {
        var request = new CreateAccountRequest("Wallet", "Cash", 120.50m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await ReadAccountAsync(response);
        created.Name.Should().Be("Wallet");
        created.Type.Should().Be("Cash");
        created.InitialBalance.Should().Be(120.50m);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/accounts/{created.Id}");
    }

    [Fact]
    public async Task Post_WithNoTransactions_ReturnsBalanceEqualToInitialBalance()
    {
        var request = new CreateAccountRequest("Wallet", "Cash", 120.50m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        var created = await ReadAccountAsync(response);
        created.Balance.Should().Be(created.InitialBalance);
    }

    [Fact]
    public async Task Post_WithValidBody_ReturnsVersion7Id()
    {
        var request = new CreateAccountRequest("Wallet", "Cash", 0m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        var created = await ReadAccountAsync(response);
        created.Id.Version.Should().Be(7);
    }

    [Fact]
    public async Task Post_WithLowercaseType_Returns201()
    {
        var request = new CreateAccountRequest("Wallet", "cash", 0m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadAccountAsync(response)).Type.Should().Be("Cash");
    }

    [Fact]
    public async Task Post_WithNegativeInitialBalance_Returns201()
    {
        var request = new CreateAccountRequest("Visa", "Card", -340.00m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadAccountAsync(response)).InitialBalance.Should().Be(-340.00m);
    }

    [Theory]
    [InlineData("", "Cash", 0, "name")]
    [InlineData("   ", "Cash", 0, "name")]
    [InlineData("Wallet", "Crypto", 0, "type")]
    [InlineData("Wallet", "7", 0, "type")]
    [InlineData("Wallet", "0", 0, "type")]
    [InlineData("Wallet", "Cash", 1.005, "initialBalance")]
    public async Task Post_WithInvalidField_Returns400WithThatField(
        string name,
        string type,
        decimal initialBalance,
        string expectedField)
    {
        var request = new CreateAccountRequest(name, type, initialBalance);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await ReadProblemAsync(response);
        problem.Errors.Should().ContainKey(expectedField);
    }

    [Fact]
    public async Task Post_WithNameOverMaxLength_Returns400WithNameError()
    {
        var request = new CreateAccountRequest(new string('a', 101), "Cash", 0m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadProblemAsync(response)).Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task Post_WithSeveralInvalidFields_ReturnsOneEntryPerField()
    {
        var request = new CreateAccountRequest("  ", "Crypto", 1.005m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        var problem = await ReadProblemAsync(response);
        problem.Errors.Keys.Should().BeEquivalentTo(["name", "type", "initialBalance"]);
    }

    [Fact]
    public async Task Post_WithInvalidBody_UsesProblemJsonContentType()
    {
        var request = new CreateAccountRequest("  ", "Cash", 0m);

        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Post_WithInvalidBody_SavesNothing()
    {
        var request = new CreateAccountRequest("  ", "Cash", 0m);

        await _client.PostAsJsonAsync("/accounts", request);

        (await ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WithNoAccounts_Returns200AndEmptyArray()
    {
        var response = await _client.GetAsync("/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WithSeveralAccounts_ReturnsThemOrderedByName()
    {
        await CreateAsync(new CreateAccountRequest("Wallet", "Cash", 0m));
        await CreateAsync(new CreateAccountRequest("Visa", "Card", -340.00m));
        await CreateAsync(new CreateAccountRequest("Deposit", "Savings", 1000.00m));

        var accounts = await ListAsync();

        accounts.Select(account => account.Name).Should().Equal("Deposit", "Visa", "Wallet");
    }

    [Fact]
    public async Task GetById_WithKnownId_Returns200AndTheAccount()
    {
        var created = await CreateAsync(new CreateAccountRequest("Wallet", "Cash", 120.50m));

        var response = await _client.GetAsync($"/accounts/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var found = await ReadAccountAsync(response);

        // CreatedAt is compared with a tolerance, the other fields exactly. PostgreSQL
        // timestamptz keeps microseconds, DateTimeOffset keeps 100-nanosecond ticks, so the
        // value the POST returned from memory is a fraction ahead of the one read back.
        found.Should().BeEquivalentTo(
            created,
            options => options
                .Using<DateTimeOffset>(context =>
                    context.Subject.Should().BeCloseTo(context.Expectation, TimeSpan.FromMicroseconds(1)))
                .WhenTypeIs<DateTimeOffset>());
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/accounts/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithMalformedId_Returns404()
    {
        // The {id:guid} route constraint takes care of this. Nothing reaches the use case.
        var response = await _client.GetAsync("/accounts/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        var response = await _client.PostAsJsonAsync("/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return await ReadAccountAsync(response);
    }

    private async Task<IReadOnlyList<AccountResponse>> ListAsync()
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountResponse>>("/accounts");

        accounts.Should().NotBeNull();

        return accounts!;
    }

    private static async Task<AccountResponse> ReadAccountAsync(HttpResponseMessage response)
    {
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();

        account.Should().NotBeNull();

        return account!;
    }

    private static async Task<ValidationProblem> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();

        problem.Should().NotBeNull();
        problem!.Errors.Should().NotBeNull();

        return problem;
    }

    /// <summary>
    /// Just enough of the problem+json body to assert on. Deserialised from the wire, so the
    /// test does not borrow the server's own type.
    /// </summary>
    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
