using FinNavis.Application.Accounts;
using FinNavis.Application.Transactions;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transactions;

public sealed class RecordTransactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeLedgerQueries _ledger;
    private readonly FakeUnitOfWork _unitOfWork;

    public RecordTransactionTests()
    {
        _ledger = new FakeLedgerQueries(_accounts, _transactions, _categories);
        _unitOfWork = new FakeUnitOfWork(_transactions.Commit);
    }

    // AC-01. Nothing is written to make the balance fall: it is summed from the movements at read
    // time, and the category's side is what makes this entry subtract (ADR-0002).
    [Fact]
    public async Task RecordTransaction_ValidInput_RecordedWithNote()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Transaction recorded = await RecordAsync(wallet.Id, groceries.Id, 42.5000m, "Weekly shop");

        recorded.AccountId.Should().Be(wallet.Id);
        recorded.CategoryId.Should().Be(groceries.Id);
        recorded.Amount.Should().Be(new Money(42.5000m));
        recorded.OccurredOn.Should().Be(OccurredOn);
        recorded.Note.Should().Be("Weekly shop");
        recorded.CreatedAt.Should().Be(Now);

        AccountBalances balances = await ReadBalancesAsync();

        balances.Accounts.Should().ContainSingle().Which.Balance.Should().Be(new Money(-42.5000m));
        _unitOfWork.Commits.Should().Be(1);
    }

    // AC-03: the unknown category is named back, and no category row appears on the way.
    [Fact]
    public async Task RecordTransaction_UnknownCategory_IsRefusedAndCreatesNothing()
    {
        Account wallet = SeedAccount("Cash wallet");
        Guid unknownCategoryId = Guid.CreateVersion7();

        Func<Task> record = () => RecordAsync(wallet.Id, unknownCategoryId, 42.5000m, note: null);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be($"No category in the ledger has id {unknownCategoryId}.");
        _categories.Stored.Should().BeEmpty();
        _transactions.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // AC-25, on the account side.
    [Fact]
    public async Task RecordTransaction_UnknownAccount_IsRefusedAndCreatesNothing()
    {
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Guid unknownAccountId = Guid.CreateVersion7();

        Func<Task> record = () => RecordAsync(unknownAccountId, groceries.Id, 42.5000m, note: null);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.NotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownAccountId}.");
        _transactions.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // AC-01, the other side of the ledger: an earning raises the balance by the same mechanism.
    [Fact]
    public async Task RecordTransaction_UnderAnIncomeCategory_RaisesTheBalance()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category salary = SeedCategory("Salary", CategoryKind.Income);

        await RecordAsync(wallet.Id, salary.Id, 1200.0000m, note: null);

        AccountBalances balances = await ReadBalancesAsync();

        balances.Accounts.Should().ContainSingle().Which.Balance.Should().Be(new Money(1200.0000m));
    }

    [Fact]
    public async Task RecordTransaction_WithNoNote_IsRecordedWithoutOne()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Transaction recorded = await RecordAsync(wallet.Id, groceries.Id, 10.0000m, note: null);

        recorded.Note.Should().BeNull();
    }

    // AC-02 belongs to the entity; this layer only surfaces the refusal and writes nothing.
    [Fact]
    public async Task RecordTransaction_AmountNotAboveZero_IsRefusedAndWritesNothing()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Func<Task> record = () => RecordAsync(wallet.Id, groceries.Id, 0m, note: null);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transaction.AmountNotPositive");
        refusal.Message.Should().Be("The amount must be above zero.");
        _transactions.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // ADR-0006: nothing in this feature fills the outside reference. csv-import does, later.
    [Fact]
    public async Task RecordTransaction_ValidInput_LeavesExternalRefNull()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Transaction recorded = await RecordAsync(wallet.Id, groceries.Id, 42.5000m, "Weekly shop");

        recorded.ExternalRef.Should().BeNull();
    }

    // ADR-0005: the id is minted here, in the Application layer, and it is a UUID v7.
    [Fact]
    public async Task RecordTransaction_ValidInput_MintsATimeOrderedUuidV7()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);

        Transaction first = await RecordAsync(wallet.Id, groceries.Id, 10.0000m, note: null);
        Transaction second = await RecordAsync(wallet.Id, groceries.Id, 20.0000m, note: null);

        first.Id.Version.Should().Be(7);
        second.Id.Version.Should().Be(7);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task RecordTransaction_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        using var cancellation = new CancellationTokenSource();

        await UseCase().ExecuteAsync(
            wallet.Id,
            groceries.Id,
            42.5000m,
            OccurredOn,
            note: null,
            cancellation.Token);

        _accounts.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
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

    private RecordTransaction UseCase() =>
        new(_transactions, _accounts, _categories, _unitOfWork, new FixedClock(Now));

    private Task<Transaction> RecordAsync(Guid accountId, Guid categoryId, decimal amount, string? note) =>
        UseCase().ExecuteAsync(accountId, categoryId, amount, OccurredOn, note, CancellationToken.None);

    private Task<AccountBalances> ReadBalancesAsync() =>
        new GetAccountBalances(_ledger).ExecuteAsync(accountId: null, CancellationToken.None);
}
