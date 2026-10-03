using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Fixtures;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Reads the table with raw SQL, so the conventions in CLAUDE.md are checked by the build
/// rather than by eye.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AccountPersistenceTests(FinNavisApiFactory factory) : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    public Task InitializeAsync()
    {
        return factory.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Type_IsStoredAsText_NotAsAnInt()
    {
        await SaveAsync(Account.Create("Visa", AccountType.Card, 0m, CreatedAt));

        var stored = await ScalarAsync("SELECT type FROM accounts");

        stored.Should().Be("Card");
    }

    [Fact]
    public async Task Id_IsStoredAsUuid()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 0m, CreatedAt);
        await SaveAsync(account);

        var stored = await ScalarAsync("SELECT id FROM accounts");

        stored.Should().BeOfType<Guid>().Which.Should().Be(account.Id);
    }

    [Theory]
    [InlineData("120.50")]
    [InlineData("-340.00")]
    [InlineData("0")]
    [InlineData("999999999999.99")]
    public async Task InitialBalance_RoundTripsWithoutChange(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var account = Account.Create("Wallet", AccountType.Cash, amount, CreatedAt);

        await SaveAsync(account);

        var reloaded = await ReloadAsync(account.Id);
        reloaded.InitialBalance.Should().Be(amount);
    }

    [Fact]
    public async Task CreatedAt_IsStoredAsUtcWithZeroOffset()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 0m, DateTimeOffset.UtcNow);

        await SaveAsync(account);

        var reloaded = await ReloadAsync(account.Id);
        reloaded.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Accounts_HasSnakeCaseColumnsWithTheExpectedTypes()
    {
        var columns = await ColumnsAsync();

        columns.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["id"] = "uuid",
            ["name"] = "character varying(100)",
            ["type"] = "character varying(20)",
            ["initial_balance"] = "numeric(14,2)",
            ["created_at"] = "timestamp with time zone",
        });
    }

    [Fact]
    public async Task Accounts_HasNoIndexBeyondThePrimaryKey()
    {
        var indexes = new List<string>();

        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT indexname FROM pg_indexes WHERE tablename = 'accounts' ORDER BY indexname",
            connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        indexes.Should().Equal("pk_accounts");
    }

    private async Task SaveAsync(Account account)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinNavisDbContext>();

        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }

    private async Task<Account> ReloadAsync(Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinNavisDbContext>();

        var account = await context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);

        account.Should().NotBeNull();

        return account!;
    }

    private async Task<Dictionary<string, string>> ColumnsAsync()
    {
        var columns = new Dictionary<string, string>();

        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT column_name, format_type(a.atttypid, a.atttypmod)
            FROM information_schema.columns c
            JOIN pg_attribute a
              ON a.attrelid = 'accounts'::regclass AND a.attname = c.column_name
            WHERE c.table_name = 'accounts'
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns[reader.GetString(0)] = reader.GetString(1);
        }

        return columns;
    }

    private async Task<object?> ScalarAsync(string sql)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);

        return await command.ExecuteScalarAsync();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinNavisDbContext>();

        var connection = new NpgsqlConnection(context.Database.GetConnectionString());
        await connection.OpenAsync();

        return connection;
    }
}
