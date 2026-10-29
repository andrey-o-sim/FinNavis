using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class CategoryGroupRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public CategoryGroupRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_AfterTheUnitOfWorkCommits_ReadsTheGroupBackFromPostgres()
    {
        CategoryGroup group = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        await StoreAsync(group);

        await using var read = _database.CreateDbContext();
        CategoryGroup? stored = await new CategoryGroupRepository(read)
            .GetByIdAsync(group.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Utilities");
        stored.Kind.Should().Be(CategoryKind.Expense);
    }

    [Fact]
    public async Task ListAsync_WithAGroupHoldingNoCategories_StillReturnsIt()
    {
        await StoreAsync(LedgerData.NewGroup("Utilities", CategoryKind.Expense));

        await using var read = _database.CreateDbContext();
        IReadOnlyList<CategoryGroup> groups = await new CategoryGroupRepository(read)
            .ListAsync(CancellationToken.None);

        groups.Should().ContainSingle().Which.Name.Should().Be("Utilities");
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithTheNameAndSideAGroupAlreadyCarries_IsTrue()
    {
        await StoreAsync(LedgerData.NewGroup("Gifts", CategoryKind.Expense));

        await using var read = _database.CreateDbContext();
        bool exists = await new CategoryGroupRepository(read)
            .ExistsWithNameAsync("Gifts", CategoryKind.Expense, CancellationToken.None);

        exists.Should().BeTrue();
    }

    // ux_category_groups_name_kind is scoped to the name and the side together, so the same name
    // on the other side is a different group (AC-31).
    [Fact]
    public async Task ExistsWithNameAsync_WithTheSameNameOnTheOtherSide_IsFalse()
    {
        await StoreAsync(LedgerData.NewGroup("Gifts", CategoryKind.Expense));

        await using var read = _database.CreateDbContext();
        bool exists = await new CategoryGroupRepository(read)
            .ExistsWithNameAsync("Gifts", CategoryKind.Income, CancellationToken.None);

        exists.Should().BeFalse();
    }

    private async Task StoreAsync(CategoryGroup group)
    {
        await using var context = _database.CreateDbContext();

        new CategoryGroupRepository(context).Add(group);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
