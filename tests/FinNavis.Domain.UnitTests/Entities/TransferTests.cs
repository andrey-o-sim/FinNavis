using System.Reflection;

using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class TransferTests
{
    private const string SameAccount = "Transfer.SameAccount";
    private const string AmountNotPositive = "Transfer.AmountNotPositive";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly OccurredOn = new(2026, 2, 28);

    [Fact]
    public void Record_TwoDifferentAccounts_KeepsBothSidesOnOneEntity()
    {
        Guid id = Guid.CreateVersion7();
        Guid sourceAccountId = Guid.CreateVersion7();
        Guid destinationAccountId = Guid.CreateVersion7();

        Transfer transfer = Transfer.Record(
            id, sourceAccountId, destinationAccountId, new Money(180.0000m), OccurredOn, CreatedAt);

        // One amount for both sides, so the source falls and the destination rises by the same
        // figure and the total across accounts cannot change.
        transfer.Id.Should().Be(id);
        transfer.SourceAccountId.Should().Be(sourceAccountId);
        transfer.DestinationAccountId.Should().Be(destinationAccountId);
        transfer.Amount.Should().Be(new Money(180.0000m));
        transfer.OccurredOn.Should().Be(OccurredOn);
        transfer.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Record_SameSourceAndDestination_IsRefused()
    {
        Guid accountId = Guid.CreateVersion7();

        Func<Transfer> act = () => Transfer.Record(
            Guid.CreateVersion7(),
            accountId,
            accountId,
            new Money(180.0000m),
            OccurredOn,
            CreatedAt);

        // The refusal carries the wording the owner reads, and it comes from the entity — there
        // is no CHECK constraint.
        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(SameAccount);
        refusal.Message.Should().Be("A transfer needs two different accounts.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.0001)]
    [InlineData(-19.99)]
    public void Record_AmountZeroOrBelow_IsRefused(decimal amount)
    {
        Func<Transfer> act = () => Record(amount: new Money(amount));

        DomainRuleViolationException refusal = act.Should()
            .Throw<DomainRuleViolationException>().Which;
        refusal.Code.Should().Be(AmountNotPositive);
        refusal.Message.Should().Be("The amount must be above zero.");
    }

    [Fact]
    public void Record_AmountWithFourDecimalPlaces_KeepsEveryPlace()
    {
        // Money is a decimal, so the fourth place that NUMERIC(19,4) stores survives the round trip.
        Transfer transfer = Record(amount: new Money(0.0001m));

        transfer.Amount.Amount.Should().Be(0.0001m);
    }

    [Fact]
    public void Transfer_CarriesBothSidesOnOneEntity()
    {
        // A half-applied transfer is unrepresentable rather than merely prevented. There is no
        // per-side leg to construct — Record returns one object that names both accounts and
        // one amount, so no test can build a transfer that landed on one side only.
        Transfer transfer = Record();

        transfer.SourceAccountId.Should().NotBe(Guid.Empty);
        transfer.DestinationAccountId.Should().NotBe(Guid.Empty);
        transfer.SourceAccountId.Should().NotBe(transfer.DestinationAccountId);

        // Every way to build a Transfer goes through Record, which takes both sides at once.
        typeof(Transfer).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Should().BeEmpty();
    }

    [Fact]
    public void Transfer_HasNoCategoryField()
    {
        // A transfer carries no category, so giving it one is unrepresentable rather than
        // validated — the field it would need does not exist. The Application layer turns an
        // attempt coming off the wire into Transfer.CategoryNotAllowed.
        typeof(Transfer).GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(name => name.Contains("Category", StringComparison.Ordinal));

        IEnumerable<string> parameterNames = typeof(Transfer).GetMethods()
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.Name ?? string.Empty);

        parameterNames.Should().NotContain(
            name => name.Contains("category", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Edit_NewAmount_AppliedInPlace()
    {
        Guid id = Guid.CreateVersion7();
        Transfer transfer = Record(id: id, amount: new Money(180.0000m));

        transfer.Edit(
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            new Money(150.0000m),
            OccurredOn);

        // The same row carries the new amount. There is no reversing entry, and both balances
        // follow on the next read because neither is stored.
        transfer.Id.Should().Be(id);
        transfer.Amount.Should().Be(new Money(150.0000m));
        transfer.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Edit_NewAccountsAndDay_AppliedInPlace()
    {
        Guid newSourceAccountId = Guid.CreateVersion7();
        Guid newDestinationAccountId = Guid.CreateVersion7();
        DateOnly newDay = new(2026, 3, 3);
        Transfer transfer = Record();

        transfer.Edit(
            newSourceAccountId, newDestinationAccountId, transfer.Amount, newDay);

        transfer.SourceAccountId.Should().Be(newSourceAccountId);
        transfer.DestinationAccountId.Should().Be(newDestinationAccountId);
        transfer.OccurredOn.Should().Be(newDay);
    }

    [Fact]
    public void Edit_SameSourceAndDestination_IsRefusedAndKeepsBothAccounts()
    {
        Transfer transfer = Record();
        Guid sourceAccountId = transfer.SourceAccountId;
        Guid destinationAccountId = transfer.DestinationAccountId;

        Action act = () => transfer.Edit(
            sourceAccountId, sourceAccountId, transfer.Amount, OccurredOn);

        // The rule holds on an edit too: a refused change leaves the entry as it was.
        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(SameAccount);
        transfer.SourceAccountId.Should().Be(sourceAccountId);
        transfer.DestinationAccountId.Should().Be(destinationAccountId);
    }

    [Fact]
    public void Edit_AmountZeroOrBelow_IsRefusedAndKeepsTheOldAmount()
    {
        Transfer transfer = Record(amount: new Money(180.0000m));

        Action act = () => transfer.Edit(
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            new Money(0m),
            OccurredOn);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(AmountNotPositive);
        transfer.Amount.Should().Be(new Money(180.0000m));
    }

    private static Transfer Record(Guid? id = null, Money? amount = null) =>
        Transfer.Record(
            id ?? Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            amount ?? new Money(180.0000m),
            OccurredOn,
            CreatedAt);
}
