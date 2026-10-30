using FinNavis.Application.Accounts;
using FinNavis.Application.Transactions;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transactions;

public sealed class RemoveTransactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLedgerQueries _ledger;
    private readonly FakeUnitOfWork _unitOfWork;

    public RemoveTransactionTests()
    {
        _ledger = new FakeLedgerQueries(_accounts, _transactions, _categories);
        _unitOfWork = new FakeUnitOfWork(_transactions.Commit);
    }

    // A hard delete: the entry is gone from history, and the balance it affected returns to
    // what it would have been — at no cost, because no balance is stored.
    [Fact]
    public async Task RemoveTransaction_Entry_DisappearsFromHistory()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Transaction entry = SeedEntry(wallet, groceries, 42.5000m);

        AccountBalances before = await ReadBalancesAsync();
        before.Accounts.Should().ContainSingle().Which.Balance.Should().Be(new Money(-42.5000m));

        await RemoveAsync(entry.Id);

        AccountBalances after = await ReadBalancesAsync();

        _transactions.Stored.Should().BeEmpty();
        after.Accounts.Should().ContainSingle().Which.Balance.Should().Be(Money.Zero);
        _unitOfWork.Commits.Should().Be(1);
    }

    // Removing one entry leaves every other entry where it was.
    [Fact]
    public async Task RemoveTransaction_OneOfTwoEntries_LeavesTheOther()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Transaction removed = SeedEntry(wallet, groceries, 42.5000m);
        Transaction kept = SeedEntry(wallet, groceries, 17.2500m);

        await RemoveAsync(removed.Id);

        _transactions.Stored.Should().ContainSingle().Which.Id.Should().Be(kept.Id);

        AccountBalances after = await ReadBalancesAsync();

        after.Accounts.Should().ContainSingle().Which.Balance.Should().Be(new Money(-17.2500m));
    }

    [Fact]
    public async Task RemoveTransaction_UnknownTransactionId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> remove = () => RemoveAsync(unknownId);

        var refusal = (await remove.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transaction.NotFound");
        refusal.Message.Should().Be($"No transaction in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RemoveTransaction_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Transaction entry = SeedEntry(wallet, groceries, 42.5000m);
        using var cancellation = new CancellationTokenSource();

        await new RemoveTransaction(_transactions, _unitOfWork)
            .ExecuteAsync(entry.Id, cancellation.Token);

        _transactions.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Account SeedAccount(string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);

        return account;
    }

    private Category SeedCategory(string name, CategoryKind kind)
    {
        Category category = Category.Create(Guid.CreateVersion7(), name, kind, group: null, Now);
        _categories.Seed(category);

        return category;
    }

    private Transaction SeedEntry(Account account, Category category, decimal amount)
    {
        Transaction entry = Transaction.Record(
            Guid.CreateVersion7(),
            account.Id,
            category.Id,
            new Money(amount),
            OccurredOn,
            note: null,
            Now);

        _transactions.Seed(entry);

        return entry;
    }

    private Task RemoveAsync(Guid transactionId) =>
        new RemoveTransaction(_transactions, _unitOfWork).ExecuteAsync(transactionId, CancellationToken.None);

    private Task<AccountBalances> ReadBalancesAsync() =>
        new GetAccountBalances(_ledger).ExecuteAsync(accountId: null, CancellationToken.None);
}
