using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class TransactionTests
{
    private const string AmountNotPositive = "Transaction.AmountNotPositive";
    private const string NoteTooLong = "Transaction.NoteTooLong";
    private const string KindChangeNotAllowed = "Transaction.KindChangeNotAllowed";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly OccurredOn = new(2026, 2, 28);

    [Fact]
    public void Record_ValidInput_KeepsTheNoteWithTheEntry()
    {
        Guid id = Guid.CreateVersion7();
        Guid accountId = Guid.CreateVersion7();
        Guid categoryId = Guid.CreateVersion7();

        Transaction transaction = Transaction.Record(
            id, accountId, categoryId, new Money(42.5000m), OccurredOn, "Weekly shop", CreatedAt);

        // AC-01: the entry names one account and one category, and the note is kept with it.
        transaction.Id.Should().Be(id);
        transaction.AccountId.Should().Be(accountId);
        transaction.CategoryId.Should().Be(categoryId);
        transaction.Amount.Should().Be(new Money(42.5000m));
        transaction.OccurredOn.Should().Be(OccurredOn);
        transaction.Note.Should().Be("Weekly shop");
        transaction.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Record_NoNote_LeavesTheEntryWithoutOne()
    {
        Transaction transaction = Record(note: null);

        transaction.Note.Should().BeNull();
    }

    [Fact]
    public void Record_Always_LeavesTheExternalReferenceNull()
    {
        // ADR-0006: the column ships unused, so csv-import needs no migration later. Nothing in
        // this feature sets it.
        Transaction transaction = Record();

        transaction.ExternalRef.Should().BeNull();
    }

    [Fact]
    public void Record_AmountWithFourDecimalPlaces_KeepsEveryPlace()
    {
        // Money is a decimal, so the fourth place that NUMERIC(19,4) stores survives the round trip.
        Transaction transaction = Record(amount: new Money(0.0001m));

        transaction.Amount.Amount.Should().Be(0.0001m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.0001)]
    [InlineData(-19.99)]
    public void Record_AmountZeroOrBelow_IsRefused(decimal amount)
    {
        Func<Transaction> act = () => Record(amount: new Money(amount));

        // AC-02: the refusal carries the acceptance criterion's wording, and it comes from the
        // entity — there is no CHECK constraint (.claude/rules/migrations.md).
        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(AmountNotPositive);
        refusal.Message.Should().Be("The amount must be above zero.");
    }

    [Fact]
    public void Record_NoteOfExactlyFiveHundredCharacters_IsAccepted()
    {
        string note = new('a', Transaction.MaxNoteLength);

        Transaction transaction = Record(note: note);

        transaction.Note.Should().Be(note);
    }

    [Fact]
    public void Record_NoteOverFiveHundredCharacters_IsRefused()
    {
        string note = new('a', Transaction.MaxNoteLength + 1);

        Func<Transaction> act = () => Record(note: note);

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(NoteTooLong);
        refusal.Message.Should().Be("A note may be at most 500 characters.");
    }

    [Fact]
    public void Edit_NewAmount_AppliedInPlace()
    {
        Guid id = Guid.CreateVersion7();
        Guid accountId = Guid.CreateVersion7();
        Guid categoryId = Guid.CreateVersion7();
        Transaction transaction = Transaction.Record(
            id, accountId, categoryId, new Money(42.0000m), OccurredOn, "Weekly shop", CreatedAt);

        transaction.Edit(
            accountId,
            secondAccountId: null,
            categoryId,
            new Money(38.0000m),
            OccurredOn,
            "Weekly shop");

        // AC-21 and ADR-0003: the same row carries the new amount. There is no reversing entry —
        // the only Transaction here is still the one that was recorded, with the same identity and
        // the same created_at. Every balance follows on the next read, because none is stored.
        transaction.Id.Should().Be(id);
        transaction.Amount.Should().Be(new Money(38.0000m));
        transaction.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Edit_NewCategoryDateAndNote_AppliedInPlace()
    {
        Guid id = Guid.CreateVersion7();
        Guid accountId = Guid.CreateVersion7();
        Guid newCategoryId = Guid.CreateVersion7();
        DateOnly newDay = new(2026, 3, 3);
        Transaction transaction = Record(id: id, accountId: accountId);

        transaction.Edit(
            accountId, secondAccountId: null, newCategoryId, new Money(5.0000m), newDay, "Corrected");

        transaction.Id.Should().Be(id);
        transaction.CategoryId.Should().Be(newCategoryId);
        transaction.OccurredOn.Should().Be(newDay);
        transaction.Note.Should().Be("Corrected");
    }

    [Fact]
    public void Edit_NoteClearedToNull_LeavesTheEntryWithoutOne()
    {
        Transaction transaction = Record(note: "Weekly shop");

        transaction.Edit(
            transaction.AccountId,
            secondAccountId: null,
            transaction.CategoryId,
            transaction.Amount,
            transaction.OccurredOn,
            note: null);

        transaction.Note.Should().BeNull();
    }

    [Fact]
    public void Edit_SecondAccount_IsRefused()
    {
        Guid accountId = Guid.CreateVersion7();
        Transaction transaction = Record(accountId: accountId, amount: new Money(42.0000m));

        Action act = () => transaction.Edit(
            accountId,
            secondAccountId: Guid.CreateVersion7(),
            transaction.CategoryId,
            new Money(38.0000m),
            OccurredOn,
            note: null);

        // AC-24: a transaction that touched two accounts would be a transfer, and the two are
        // separate entities (ADR-0001). The refusal names the way through.
        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(KindChangeNotAllowed);
        refusal.Message.Should().Be(
            "A transaction and a transfer are different kinds of entry. "
                + "Remove this entry and record it again as a transfer.");

        // sad.md §6 flow 9: the entry is unchanged, so nothing the same call also asked for landed.
        transaction.AccountId.Should().Be(accountId);
        transaction.Amount.Should().Be(new Money(42.0000m));
    }

    [Fact]
    public void Edit_AmountZeroOrBelow_IsRefusedAndKeepsTheOldAmount()
    {
        Transaction transaction = Record(amount: new Money(42.0000m));

        Action act = () => transaction.Edit(
            transaction.AccountId,
            secondAccountId: null,
            transaction.CategoryId,
            new Money(0m),
            OccurredOn,
            note: null);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(AmountNotPositive);
        transaction.Amount.Should().Be(new Money(42.0000m));
    }

    [Fact]
    public void Edit_NoteOverFiveHundredCharacters_IsRefusedAndKeepsTheOldNote()
    {
        Transaction transaction = Record(note: "Weekly shop");

        Action act = () => transaction.Edit(
            transaction.AccountId,
            secondAccountId: null,
            transaction.CategoryId,
            transaction.Amount,
            transaction.OccurredOn,
            new string('a', Transaction.MaxNoteLength + 1));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NoteTooLong);
        transaction.Note.Should().Be("Weekly shop");
    }

    [Fact]
    public void Transaction_HasNoCategoryGroupField()
    {
        // data-model.md: a transaction names its category and never its group, which is what makes
        // AC-13 and AC-34 free — moving or renaming a category rewrites no movement row.
        typeof(Transaction).GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(name => name.Contains("Group", StringComparison.Ordinal));
    }

    private static Transaction Record(
        Guid? id = null,
        Guid? accountId = null,
        Guid? categoryId = null,
        Money? amount = null,
        string? note = null) =>
        Transaction.Record(
            id ?? Guid.CreateVersion7(),
            accountId ?? Guid.CreateVersion7(),
            categoryId ?? Guid.CreateVersion7(),
            amount ?? new Money(19.9900m),
            OccurredOn,
            note,
            CreatedAt);
}
