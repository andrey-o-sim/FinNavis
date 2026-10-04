using FinNavis.Application.Common;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.IntegrationTests.Fakes;
using FinNavis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Properties of the migration and the database itself, not of C#. These check the schema the
/// first migration produced, rather than going through HTTP.
/// </summary>
[Collection("Database")]
public sealed class AccountSchemaTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;

    public AccountSchemaTests(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    public async Task InitializeAsync()
    {
        await _postgresFixture.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AccountsTable_TypeColumn_IsStoredAsText()
    {
        await using var dbContext = _postgresFixture.CreateDbContext();
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT data_type FROM information_schema.columns WHERE table_name = 'accounts' AND column_name = 'type'";
        var dataType = (string?)await command.ExecuteScalarAsync();

        dataType.Should().Be("text");
    }

    [Fact]
    public async Task AccountsTable_StoredRow_HoldsTypeAsLiteralString()
    {
        var clock = new MutableClock();
        await using (var dbContext = _postgresFixture.CreateDbContext())
        {
            var account = Account.Create("Nest egg", AccountType.Savings, 0m, null, clock);
            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();
        }

        await using var readContext = _postgresFixture.CreateDbContext();
        var connection = (NpgsqlConnection)readContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT type FROM accounts WHERE name = 'Nest egg'";
        var storedType = (string?)await command.ExecuteScalarAsync();

        storedType.Should().Be("Savings");
    }

    [Fact]
    public async Task AccountsTable_HasUniqueIndexOnNormalizedName()
    {
        await using var dbContext = _postgresFixture.CreateDbContext();
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT indexdef FROM pg_indexes WHERE tablename = 'accounts' AND indexname = 'ix_accounts_normalized_name'";
        var indexDefinition = (string?)await command.ExecuteScalarAsync();

        indexDefinition.Should().NotBeNull();
        indexDefinition.Should().Contain("UNIQUE");
        indexDefinition.Should().Contain("normalized_name");
    }

    [Fact]
    public async Task AccountsTable_DuplicateNormalizedName_RawSaveChangesThrowsDbUpdateException()
    {
        var clock = new MutableClock();

        await using (var seedContext = _postgresFixture.CreateDbContext())
        {
            seedContext.Accounts.Add(Account.Create("Cash", AccountType.Cash, 0m, null, clock));
            await seedContext.SaveChangesAsync();
        }

        await using var raceContext = _postgresFixture.CreateDbContext();
        raceContext.Accounts.Add(Account.Create("cash", AccountType.Cash, 0m, null, clock));

        var act = async () => await raceContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AccountsTable_DuplicateNormalizedName_EfUnitOfWorkThrowsUniqueConstraintViolation()
    {
        var clock = new MutableClock();

        await using (var seedContext = _postgresFixture.CreateDbContext())
        {
            seedContext.Accounts.Add(Account.Create("Cash", AccountType.Cash, 0m, null, clock));
            await seedContext.SaveChangesAsync();
        }

        await using var raceContext = _postgresFixture.CreateDbContext();
        raceContext.Accounts.Add(Account.Create("cash", AccountType.Cash, 0m, null, clock));
        var unitOfWork = new EfUnitOfWork(raceContext);

        var act = async () => await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<UniqueConstraintViolationException>();
    }
}
