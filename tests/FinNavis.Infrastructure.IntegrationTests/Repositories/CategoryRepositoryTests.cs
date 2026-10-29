using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;

namespace FinNavis.Infrastructure.IntegrationTests.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class CategoryRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _database;

    public CategoryRepositoryTests(PostgresFixture database) => _database = database;

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_AfterTheUnitOfWorkCommits_ReadsTheCategoryBackFromPostgres()
    {
        CategoryGroup group = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        Category category = LedgerData.NewCategory("Electricity", CategoryKind.Expense, group);

        await using (var write = _database.CreateDbContext())
        {
            new CategoryGroupRepository(write).Add(group);
            new CategoryRepository(write).Add(category);
            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Category? stored = await new CategoryRepository(read)
            .GetByIdAsync(category.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Electricity");
        stored.Kind.Should().Be(CategoryKind.Expense);
        stored.CategoryGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task ListByGroupAsync_WithCategoriesOnBothSidesOfTheLink_ReturnsOnlyTheGroupsOwn()
    {
        CategoryGroup group = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        Category inGroup = LedgerData.NewCategory("Electricity", CategoryKind.Expense, group);
        Category outsideGroup = LedgerData.NewCategory("Coffee", CategoryKind.Expense);

        await using (var write = _database.CreateDbContext())
        {
            new CategoryGroupRepository(write).Add(group);

            var repository = new CategoryRepository(write);
            repository.Add(inGroup);
            repository.Add(outsideGroup);

            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        IReadOnlyList<Category> categories = await new CategoryRepository(read)
            .ListByGroupAsync(group.Id, CancellationToken.None);

        categories.Should().ContainSingle().Which.Id.Should().Be(inGroup.Id);
    }

    // AC-29: the link is cleared on every category of the group, in the same unit of work that
    // removes the group row. The rows ListByGroupAsync hands back have to be tracked for that.
    [Fact]
    public async Task ListByGroupAsync_WhenTheLoadedCategoriesAreCleared_CommitsTheClearedLink()
    {
        CategoryGroup group = LedgerData.NewGroup("Utilities", CategoryKind.Expense);
        Category category = LedgerData.NewCategory("Electricity", CategoryKind.Expense, group);

        await using (var seed = _database.CreateDbContext())
        {
            new CategoryGroupRepository(seed).Add(group);
            new CategoryRepository(seed).Add(category);
            await new UnitOfWork(seed).SaveChangesAsync(CancellationToken.None);
        }

        await using (var write = _database.CreateDbContext())
        {
            var categories = new CategoryRepository(write);
            var groups = new CategoryGroupRepository(write);

            foreach (Category linked in await categories.ListByGroupAsync(group.Id, CancellationToken.None))
            {
                linked.ClearGroup();
            }

            CategoryGroup loaded = (await groups.GetByIdAsync(group.Id, CancellationToken.None))!;
            groups.Remove(loaded);

            await new UnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using var read = _database.CreateDbContext();
        Category? stored = await new CategoryRepository(read)
            .GetByIdAsync(category.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.CategoryGroupId.Should().BeNull();
    }

    [Fact]
    public async Task ExistsWithNameAsync_WithTheNameAndSideACategoryAlreadyCarries_IsTrue()
    {
        await StoreAsync(LedgerData.NewCategory("Gifts", CategoryKind.Expense));

        await using var read = _database.CreateDbContext();
        bool exists = await new CategoryRepository(read)
            .ExistsWithNameAsync("Gifts", CategoryKind.Expense, CancellationToken.None);

        exists.Should().BeTrue();
    }

    // ux_categories_name_kind is scoped to the name and the side together, so the same name on the
    // other side is a different category (AC-36).
    [Fact]
    public async Task ExistsWithNameAsync_WithTheSameNameOnTheOtherSide_IsFalse()
    {
        await StoreAsync(LedgerData.NewCategory("Gifts", CategoryKind.Expense));

        await using var read = _database.CreateDbContext();
        bool exists = await new CategoryRepository(read)
            .ExistsWithNameAsync("Gifts", CategoryKind.Income, CancellationToken.None);

        exists.Should().BeFalse();
    }

    private async Task StoreAsync(Category category)
    {
        await using var context = _database.CreateDbContext();

        new CategoryRepository(context).Add(category);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }
}
