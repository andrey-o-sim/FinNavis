using FinNavis.Application.Transactions;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transactions;

public sealed class EditTransactionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransactionRepository _transactions = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public EditTransactionTests() => _unitOfWork = new FakeUnitOfWork(_transactions.Commit);

    // AC-21. The row itself changes — one entry before, one entry after, and no reversing entry
    // beside it (ADR-0003).
    [Fact]
    public async Task EditTransaction_NewAmount_AppliedInPlace()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");

        Transaction edited = await EditAsync(entry.Id, new EditTransactionChanges(Amount: 38.0000m));

        edited.Id.Should().Be(entry.Id);
        edited.Amount.Should().Be(new Money(38.0000m));
        _transactions.Stored.Should().ContainSingle();
        _transactions.Committed(entry.Id).Amount.Should().Be(38.0000m);
        _unitOfWork.Commits.Should().Be(1);
    }

    // AC-24. The owner's way through is to remove the entry and record it again as a transfer.
    [Fact]
    public async Task EditTransaction_SecondAccount_IsRefused()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        Account destination = SeedAccount("Savings jar");

        Func<Task> edit = () => EditAsync(
            entry.Id,
            new EditTransactionChanges(SecondAccountId: destination.Id));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transaction.KindChangeNotAllowed");
        refusal.Message.Should().Be(
            "A transaction and a transfer are different kinds of entry. "
                + "Remove this entry and record it again as a transfer.");
        _unitOfWork.Commits.Should().Be(0);
    }

    // A field the owner left out keeps the value the entry already carries.
    [Fact]
    public async Task EditTransaction_OnlyTheDate_LeavesEveryOtherFieldAsItWas()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        var movedTo = new DateOnly(2026, 10, 12);

        Transaction edited = await EditAsync(entry.Id, new EditTransactionChanges(OccurredOn: movedTo));

        edited.OccurredOn.Should().Be(movedTo);
        edited.Amount.Should().Be(new Money(42.5000m));
        edited.Note.Should().Be("Weekly shop");
        edited.AccountId.Should().Be(entry.AccountId);
        edited.CategoryId.Should().Be(entry.CategoryId);
    }

    // The contract lets the owner send "note": null to clear it, and leave the property out to
    // keep it. Those are two different requests.
    [Fact]
    public async Task EditTransaction_NoteSetToNull_ClearsTheNote()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");

        Transaction edited = await EditAsync(
            entry.Id,
            new EditTransactionChanges(Note: NoteChange.To(null)));

        edited.Note.Should().BeNull();
        _transactions.Committed(entry.Id).Note.Should().BeNull();
    }

    [Fact]
    public async Task EditTransaction_NoteLeftOut_KeepsTheNote()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");

        Transaction edited = await EditAsync(entry.Id, new EditTransactionChanges(Amount: 1.0000m));

        edited.Note.Should().Be("Weekly shop");
    }

    [Fact]
    public async Task EditTransaction_NewCategory_IsFiledUnderIt()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        Category eatingOut = SeedCategory("Eating out", CategoryKind.Expense);

        Transaction edited = await EditAsync(
            entry.Id,
            new EditTransactionChanges(CategoryId: eatingOut.Id));

        edited.CategoryId.Should().Be(eatingOut.Id);
        _transactions.Committed(entry.Id).CategoryId.Should().Be(eatingOut.Id);
    }

    // AC-25, on the category the edit moves onto. Nothing is created and the entry is untouched.
    [Fact]
    public async Task EditTransaction_UnknownCategory_IsRefusedAndChangesNothing()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        Guid unknownCategoryId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(
            entry.Id,
            new EditTransactionChanges(CategoryId: unknownCategoryId));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be($"No category in the ledger has id {unknownCategoryId}.");
        _transactions.Committed(entry.Id).CategoryId.Should().Be(entry.CategoryId);
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task EditTransaction_UnknownAccount_IsRefusedAndChangesNothing()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        Guid unknownAccountId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(
            entry.Id,
            new EditTransactionChanges(AccountId: unknownAccountId));

        (await edit.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Account.NotFound");

        _transactions.Committed(entry.Id).AccountId.Should().Be(entry.AccountId);
        _unitOfWork.Commits.Should().Be(0);
    }

    // AC-25, on the entry itself.
    [Fact]
    public async Task EditTransaction_UnknownTransactionId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> edit = () => EditAsync(unknownId, new EditTransactionChanges(Amount: 1.0000m));

        var refusal = (await edit.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transaction.NotFound");
        refusal.Message.Should().Be($"No transaction in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    // AC-02 on the edit path: the entity refuses before it assigns the first field, so the entry
    // keeps every value it had.
    [Fact]
    public async Task EditTransaction_AmountNotAboveZero_IsRefusedAndChangesNothing()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");

        Func<Task> edit = () => EditAsync(entry.Id, new EditTransactionChanges(Amount: -1.0000m));

        (await edit.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Transaction.AmountNotPositive");

        _transactions.Committed(entry.Id).Amount.Should().Be(42.5000m);
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task EditTransaction_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Transaction entry = SeedEntry(42.5000m, "Weekly shop");
        using var cancellation = new CancellationTokenSource();

        await new EditTransaction(_transactions, _accounts, _categories, _unitOfWork)
            .ExecuteAsync(entry.Id, new EditTransactionChanges(Amount: 1.0000m), cancellation.Token);

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

    private Transaction SeedEntry(decimal amount, string? note)
    {
        Account wallet = SeedAccount("Cash wallet");
        Category groceries = SeedCategory("Groceries", CategoryKind.Expense);
        Transaction entry = Transaction.Record(
            Guid.CreateVersion7(),
            wallet.Id,
            groceries.Id,
            new Money(amount),
            OccurredOn,
            note,
            Now);

        _transactions.Seed(entry);

        return entry;
    }

    private Task<Transaction> EditAsync(Guid transactionId, EditTransactionChanges changes) =>
        new EditTransaction(_transactions, _accounts, _categories, _unitOfWork)
            .ExecuteAsync(transactionId, changes, CancellationToken.None);
}
