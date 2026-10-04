using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinNavis.Infrastructure.IntegrationTests.Fakes;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints;

[Collection("Database")]
public sealed class CreateAccountEndpointTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private readonly MutableClock _clock = new();
    private readonly AccountsApiFactory _factory;
    private readonly HttpClient _client;

    public CreateAccountEndpointTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
        _factory = new AccountsApiFactory(postgresFixture.ConnectionString, _clock);
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
    public async Task Post_ValidInput_Returns201WithLocationThatResolves()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 100.00m,
            note = "primary wallet",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var followUp = await _client.GetAsync(response.Headers.Location);
        followUp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_OpeningBalanceWiderThanTheColumn_Returns400NotServerError()
    {
        // PostgreSQL would raise "numeric field overflow" on save. That is not a 23505, so it used
        // to escape EfUnitOfWork and reach the caller as a 500 for plainly bad input.
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Too big",
            type = "Cash",
            openingBalance = 99999999999999999m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("errors").TryGetProperty("openingBalance", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_OpeningBalanceHasMoreThanTwoDecimals_ResponseMatchesWhatWasStored()
    {
        // The 201 body used to be built from the un-rounded in-memory entity, so it reported
        // 10.999 while the row held 11.00 and every later read disagreed with the create.
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Rounding",
            type = "Cash",
            openingBalance = 10.999m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var createdBalance = created!.RootElement.GetProperty("openingBalance").GetDecimal();
        createdBalance.Should().Be(11.00m);

        var followUp = await _client.GetAsync(response.Headers.Location);
        var fetched = await followUp.Content.ReadFromJsonAsync<JsonDocument>();
        fetched!.RootElement.GetProperty("openingBalance").GetDecimal().Should().Be(createdBalance);

        await using var dbContext = _postgresFixture.CreateDbContext();
        var row = await dbContext.Accounts.SingleAsync(a => a.Name == "Rounding");
        row.OpeningBalance.Should().Be(createdBalance);
    }

    [Fact]
    public async Task Post_BadTypeAndBadName_ReportsBothFieldsInOneResponse()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "   ",
            type = "Bitcoin",
            openingBalance = 0m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = body!.RootElement.GetProperty("errors");
        errors.TryGetProperty("type", out _).Should().BeTrue();
        errors.TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_OpeningBalanceIsNotANumber_Returns400ProblemDetails()
    {
        // A body we cannot bind must blame the caller, not the server. Minimal APIs throw
        // BadHttpRequestException here, and the global handler used to turn every exception into
        // a 500, so a typo in a numeric field looked like a server fault.
        var content = new StringContent(
            """{"name":"Cash","type":"Cash","openingBalance":"abc","note":null}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/accounts", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("cash", "Cash")]
    [InlineData("CARD", "Card")]
    [InlineData("savings", "Savings")]
    // The negative theory below proves bad types are rejected. This one proves the three real
    // names are still accepted in any letter case, so tightening the match to case-sensitive
    // would fail here instead of passing silently.
    public async Task Post_TypeInAnyLetterCase_Returns201WithCanonicalType(string sent, string expected)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = $"Account {sent}",
            type = sent,
            openingBalance = 0m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("type").GetString().Should().Be(expected);
    }

    [Fact]
    public async Task Post_ValidInput_BodyHasIdNameTypeOpeningBalanceAndNote()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 100.00m,
            note = "primary wallet",
        });

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        root.GetProperty("id").GetGuid().Should().NotBeEmpty();
        root.GetProperty("name").GetString().Should().Be("Cash");
        root.GetProperty("type").GetString().Should().Be("Cash");
        root.GetProperty("openingBalance").GetDecimal().Should().Be(100.00m);
        root.GetProperty("note").GetString().Should().Be("primary wallet");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_NameMissingEmptyOrWhitespace_Returns400ProblemDetails(string? name)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name,
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_NameOf101Characters_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = new string('a', 101),
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_NoteOf501Characters_Returns400ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 0m,
            note = new string('n', 501),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        body!.RootElement.GetProperty("errors").TryGetProperty("note", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("Bitcoin")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData(null)]
    // Enum.TryParse would accept all five of these. Only the three literal names are valid.
    [InlineData(" 0 ")]
    [InlineData("0 ")]
    [InlineData(" 1")]
    [InlineData("+0")]
    [InlineData("Cash,Card")]
    public async Task Post_InvalidType_Returns400ProblemDetails(string? type)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type,
            openingBalance = 0m,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-250.00)]
    public async Task Post_ZeroOrNegativeOpeningBalance_Returns201(decimal openingBalance)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = $"Account {openingBalance}",
            type = "Card",
            openingBalance,
            note = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_DuplicateNameIgnoringCaseAndSpaces_Returns409ProblemDetails()
    {
        var first = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/accounts", new
        {
            name = " cash ",
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        second.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Post_NameWithSurroundingSpaces_StoredRowHasNoSurroundingSpaces()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "  Cash  ",
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });
        var created = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var id = created!.RootElement.GetProperty("id").GetGuid();

        await using var dbContext = _postgresFixture.CreateDbContext();
        var row = await dbContext.Accounts.AsNoTracking().SingleAsync(a => a.Id == id);

        row.Name.Should().Be("Cash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_NoteMissingEmptyOrWhitespace_StoredAndReturnedAsNull(string? note)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = $"Account {note ?? "null"}",
            type = "Cash",
            openingBalance = 0m,
            note,
        });

        var created = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = created!.RootElement;
        root.GetProperty("note").ValueKind.Should().Be(JsonValueKind.Null);
        var id = root.GetProperty("id").GetGuid();

        await using var dbContext = _postgresFixture.CreateDbContext();
        var row = await dbContext.Accounts.AsNoTracking().SingleAsync(a => a.Id == id);

        row.Note.Should().BeNull();
    }

    [Fact]
    public async Task Post_ValidInput_ResponseHasNoCreationTimestampProperty()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var propertyNames = body!.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        propertyNames.Should().BeEquivalentTo(["id", "name", "type", "openingBalance", "note"]);
    }

    [Fact]
    public async Task Post_ValidInput_StoredCreatedAtEqualsControlledClock()
    {
        var fixedInstant = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        _clock.UtcNow = fixedInstant;

        var response = await _client.PostAsJsonAsync("/accounts", new
        {
            name = "Cash",
            type = "Cash",
            openingBalance = 0m,
            note = (string?)null,
        });
        var created = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var id = created!.RootElement.GetProperty("id").GetGuid();

        await using var dbContext = _postgresFixture.CreateDbContext();
        var row = await dbContext.Accounts.AsNoTracking().SingleAsync(a => a.Id == id);

        row.CreatedAt.Should().Be(fixedInstant);
        row.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }
}
