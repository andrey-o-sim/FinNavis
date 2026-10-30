using FinNavis.Application.Accounts;
using FinNavis.Application.Transfers;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transfers;

public sealed class RecordTransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeLedgerQueries _ledger;
    private readonly FakeUnitOfWork _unitOfWork;

    public RecordTransferTests()
    {
        _ledger = new FakeLedgerQueries(_accounts, transfers: _transfers);
        _unitOfWork = new FakeUnitOfWork(_transfers.Commit);
    }

    // AC-04, AC-15. One row carries both sides and one amount, so the two halves cannot disagree
    // and the total across the accounts cannot drift.
    [Fact]
    public async Task RecordTransfer_TwoAccounts_SourceFallsDestinationRises_TotalUnchanged()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        AccountBalances before = await ReadBalancesAsync();

        Transfer transfer = await RecordAsync(wallet.Id, savings.Id, 200.0000m);

        AccountBalances after = await ReadBalancesAsync();

        transfer.SourceAccountId.Should().Be(wallet.Id);
        transfer.DestinationAccountId.Should().Be(savings.Id);
        transfer.Amount.Should().Be(new Money(200.0000m));
        transfer.OccurredOn.Should().Be(OccurredOn);
        transfer.CreatedAt.Should().Be(Now);

        BalanceOf(after, wallet).Should().Be(new Money(300.0000m));
        BalanceOf(after, savings).Should().Be(new Money(300.0000m));
        after.Total.Should().Be(before.Total);
        _unitOfWork.Commits.Should().Be(1);
    }

    // AC-05. The entity owns the rule, and it runs before anything is staged.
    [Fact]
    public async Task RecordTransfer_SameAccountBothSides_IsRefused()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);

        Func<Task> record = () => RecordAsync(wallet.Id, wallet.Id, 200.0000m);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.SameAccount");
        refusal.Message.Should().Be("A transfer needs two different accounts.");
        _transfers.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // AC-06. A half-applied transfer is not representable: one row carries both sides, so a commit
    // that did not go through left both accounts where they were.
    [Fact]
    public async Task RecordTransfer_CommitFails_NeitherAccountChanges()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        var failingUnitOfWork = new FakeUnitOfWork(() => throw new InvalidOperationException("commit failed"));

        Func<Task> record = () => new RecordTransfer(
                _transfers,
                _accounts,
                failingUnitOfWork,
                new FixedClock(Now))
            .ExecuteAsync(wallet.Id, savings.Id, 200.0000m, OccurredOn, CancellationToken.None);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.NotRecorded");
        refusal.Message.Should().Be("The transfer could not be recorded. Neither account changed.");
        refusal.InnerException.Should().BeOfType<InvalidOperationException>();

        AccountBalances after = await ReadBalancesAsync();

        _transfers.Stored.Should().BeEmpty();
        BalanceOf(after, wallet).Should().Be(new Money(500.0000m));
        BalanceOf(after, savings).Should().Be(new Money(100.0000m));
    }

    // A cancellation is not a failed recording — nothing refused the owner, so it travels on.
    [Fact]
    public async Task RecordTransfer_WhenCancelled_DoesNotReportAFailedRecording()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        var cancellingUnitOfWork = new FakeUnitOfWork(() => throw new OperationCanceledException());

        Func<Task> record = () => new RecordTransfer(
                _transfers,
                _accounts,
                cancellingUnitOfWork,
                new FixedClock(Now))
            .ExecuteAsync(wallet.Id, savings.Id, 200.0000m, OccurredOn, CancellationToken.None);

        await record.Should().ThrowAsync<OperationCanceledException>();
    }

    // AC-25, and the half of docs/adr/0004 that keeps the role: the owner has to know which of the
    // two accounts to correct.
    [Fact]
    public async Task RecordTransfer_UnknownSourceAccount_IsRefusedAndWritesNothing()
    {
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> record = () => RecordAsync(unknownId, savings.Id, 200.0000m);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.SourceNotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _transfers.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RecordTransfer_UnknownDestinationAccount_IsRefusedAndWritesNothing()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> record = () => RecordAsync(wallet.Id, unknownId, 200.0000m);

        var refusal = (await record.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.DestinationNotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _transfers.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RecordTransfer_AmountNotAboveZero_IsRefusedAndWritesNothing()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);

        Func<Task> record = () => RecordAsync(wallet.Id, savings.Id, 0m);

        (await record.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Transfer.AmountNotPositive");

        _transfers.Stored.Should().BeEmpty();
    }

    // ADR-0005: the id is minted here, in the Application layer, and it is a UUID v7.
    [Fact]
    public async Task RecordTransfer_TwoAccounts_MintsATimeOrderedUuidV7()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);

        Transfer first = await RecordAsync(wallet.Id, savings.Id, 10.0000m);
        Transfer second = await RecordAsync(wallet.Id, savings.Id, 20.0000m);

        first.Id.Version.Should().Be(7);
        second.Id.Version.Should().Be(7);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task RecordTransfer_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        using var cancellation = new CancellationTokenSource();

        await UseCase().ExecuteAsync(
            wallet.Id,
            savings.Id,
            200.0000m,
            OccurredOn,
            cancellation.Token);

        _accounts.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private static Money BalanceOf(AccountBalances balances, Account account) =>
        balances.Accounts.Single(row => row.AccountId == account.Id).Balance;

    private Account SeedAccount(string name, decimal holding)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);
        _ledger.Balances[account.Id] = holding;

        return account;
    }

    private RecordTransfer UseCase() =>
        new(_transfers, _accounts, _unitOfWork, new FixedClock(Now));

    private Task<Transfer> RecordAsync(Guid sourceAccountId, Guid destinationAccountId, decimal amount) =>
        UseCase().ExecuteAsync(
            sourceAccountId,
            destinationAccountId,
            amount,
            OccurredOn,
            CancellationToken.None);

    private Task<AccountBalances> ReadBalancesAsync() =>
        new GetAccountBalances(_ledger).ExecuteAsync(accountId: null, CancellationToken.None);
}
