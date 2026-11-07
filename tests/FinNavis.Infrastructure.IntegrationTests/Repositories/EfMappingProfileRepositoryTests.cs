using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;
using Npgsql;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class EfMappingProfileRepositoryTests : IAsyncLifetime
{
    private const string ShapeKey = "date|description|amount|category";

    private const string ShapeKeyIndex = "ux_mapping_profiles_shape_key";

    private static readonly DateTimeOffset ConfirmedAt = new(2026, 10, 31, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _database;

    public EfMappingProfileRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_ProfileWithRules_ReadsTheProfileAndEveryRuleBackByShapeKey()
    {
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        await StoreCategoriesAsync(groceries);

        MappingProfile profile = MappingProfile.Create(
            LedgerData.NewId(),
            ShapeKey,
            [
                CategoryRule("Food", groceries),
                MappingRule.Create(
                    LedgerData.NewId(),
                    "Own transfer",
                    MappingRuleTarget.TransferBetweenOwnAccounts,
                    null,
                    ConfirmedAt),
            ],
            ConfirmedAt);

        await StoreProfileAsync(profile);

        await using var read = _database.CreateDbContext();
        MappingProfile? stored = await new EfMappingProfileRepository(read)
            .GetByShapeKeyAsync(ShapeKey, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.Id.Should().Be(profile.Id);
        stored.ShapeKey.Should().Be(ShapeKey);
        stored.CreatedAt.Should().Be(ConfirmedAt);
        stored.Rules.Should().BeEquivalentTo(profile.Rules);
    }

    [Fact]
    public async Task GetByShapeKeyAsync_UnknownShapeKey_ReturnsNull()
    {
        await StoreProfileAsync(MappingProfile.Create(LedgerData.NewId(), ShapeKey, [], ConfirmedAt));

        await using var read = _database.CreateDbContext();
        MappingProfile? stored = await new EfMappingProfileRepository(read)
            .GetByShapeKeyAsync("date|amount", CancellationToken.None);

        stored.Should().BeNull();
    }

    // A test that only checks the returned profile also passes on a sequential scan. The plan
    // check is what stops a later migration from dropping the index without anyone noticing.
    [Fact]
    public async Task GetByShapeKeyAsync_ShapeKeyLookup_IsPlannedOnTheShapeKeyIndex()
    {
        RecordedCommand lookup = await RecordShapeKeyLookupAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        string plan = await QueryPlan.ExplainAsync(connection, transaction, lookup);

        plan.Should().Contain(ShapeKeyIndex);
    }

    // Proves the plan check above can fail: with the index dropped, the same lookup is planned
    // without it. The drop is rolled back, so the schema is unchanged for every other test.
    [Fact]
    public async Task GetByShapeKeyAsync_WithTheShapeKeyIndexDropped_IsPlannedWithoutIt()
    {
        RecordedCommand lookup = await RecordShapeKeyLookupAsync();

        await using var connection = await _database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await using (var drop = new NpgsqlCommand($"DROP INDEX {ShapeKeyIndex}", connection, transaction))
        {
            await drop.ExecuteNonQueryAsync();
        }

        string plan = await QueryPlan.ExplainAsync(connection, transaction, lookup);

        await transaction.RollbackAsync();

        plan.Should().NotContain(ShapeKeyIndex);
    }

    // Confirming a shape again keeps "Food" but points it somewhere else, so the new rule has
    // the same file category as an old one. The old rule has to be gone before the new one
    // arrives, or the unique rule per file category would refuse the commit.
    [Fact]
    public async Task ReplaceRules_AfterTheCommit_KeepsTheProfileIdAndRemovesEveryOldRule()
    {
        Category groceries = LedgerData.NewCategory("Groceries", CategoryKind.Expense);
        Category transport = LedgerData.NewCategory("Transport", CategoryKind.Expense);
        await StoreCategoriesAsync(groceries, transport);

        MappingProfile profile = MappingProfile.Create(
            LedgerData.NewId(),
            ShapeKey,
            [CategoryRule("Food", groceries), CategoryRule("Taxi", transport)],
            ConfirmedAt);
        Guid[] oldRuleIds = [.. profile.Rules.Select(rule => rule.Id)];

        await StoreProfileAsync(profile);

        MappingRule[] newRules =
        [
            CategoryRule("Food", transport),
            MappingRule.Create(
                LedgerData.NewId(),
                "Savings",
                MappingRuleTarget.TransferBetweenOwnAccounts,
                null,
                ConfirmedAt),
        ];

        await using (var write = _database.CreateDbContext())
        {
            var repository = new EfMappingProfileRepository(write);
            MappingProfile loaded = (await repository.GetByShapeKeyAsync(ShapeKey, CancellationToken.None))!;

            repository.ReplaceRules(loaded, newRules);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        MappingProfile stored = (await new EfMappingProfileRepository(read)
            .GetByShapeKeyAsync(ShapeKey, CancellationToken.None))!;

        stored.Id.Should().Be(profile.Id);
        stored.Rules.Select(rule => rule.Id).Should().BeEquivalentTo(newRules.Select(rule => rule.Id));
        stored.Rules.Single(rule => rule.SourceCategory == "Food").TargetCategoryId.Should().Be(transport.Id);

        (await ImportTables.CountAsync(_database, "mapping_profile_rules", "mapping_profile_id", profile.Id))
            .Should().Be(newRules.Length);

        foreach (Guid oldRuleId in oldRuleIds)
        {
            (await ImportTables.CountAsync(_database, "mapping_profile_rules", "id", oldRuleId)).Should().Be(0);
        }
    }

    private static MappingRule CategoryRule(string sourceCategory, Category target) =>
        MappingRule.Create(
            LedgerData.NewId(),
            sourceCategory,
            MappingRuleTarget.Category,
            target.Id,
            ConfirmedAt);

    private async Task<RecordedCommand> RecordShapeKeyLookupAsync()
    {
        await StoreProfileAsync(MappingProfile.Create(LedgerData.NewId(), ShapeKey, [], ConfirmedAt));

        var commands = new RecordingCommandInterceptor();

        await using (var read = _database.CreateDbContext(commands))
        {
            await new EfMappingProfileRepository(read).GetByShapeKeyAsync(ShapeKey, CancellationToken.None);
        }

        return commands.Recorded.First(command =>
            command.Text.Contains("mapping_profiles", StringComparison.Ordinal)
            && command.Text.Contains("shape_key", StringComparison.Ordinal));
    }

    private async Task StoreCategoriesAsync(params Category[] categories)
    {
        await using var context = _database.CreateDbContext();

        var repository = new CategoryRepository(context);
        foreach (Category category in categories)
        {
            repository.Add(category);
        }

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }

    private async Task StoreProfileAsync(MappingProfile profile)
    {
        await using var context = _database.CreateDbContext();

        new EfMappingProfileRepository(context).Add(profile);

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
