using FinNavis.Application.Accounts;
using FinNavis.Application.Transfers;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Transfers;

public sealed class RemoveTransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 10, 29);

    private readonly FakeTransferRepository _transfers = new();
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeLedgerQueries _ledger;
    private readonly FakeUnitOfWork _unitOfWork;

    public RemoveTransferTests()
    {
        _ledger = new FakeLedgerQueries(_accounts, transfers: _transfers);
        _unitOfWork = new FakeUnitOfWork(_transfers.Commit);
    }

    // Both balances return to what they would have been, and the owner's total was unchanged
    // either way.
    [Fact]
    public async Task RemoveTransfer_Entry_DisappearsFromHistory()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        Transfer transfer = SeedTransfer(wallet, savings, 200.0000m);

        AccountBalances before = await ReadBalancesAsync();
        BalanceOf(before, wallet).Should().Be(new Money(300.0000m));

        await RemoveAsync(transfer.Id);

        AccountBalances after = await ReadBalancesAsync();

        _transfers.Stored.Should().BeEmpty();
        BalanceOf(after, wallet).Should().Be(new Money(500.0000m));
        BalanceOf(after, savings).Should().Be(new Money(100.0000m));
        after.Total.Should().Be(before.Total);
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RemoveTransfer_OneOfTwo_LeavesTheOther()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        Transfer removed = SeedTransfer(wallet, savings, 200.0000m);
        Transfer kept = SeedTransfer(wallet, savings, 50.0000m);

        await RemoveAsync(removed.Id);

        _transfers.Stored.Should().ContainSingle().Which.Id.Should().Be(kept.Id);

        AccountBalances after = await ReadBalancesAsync();

        BalanceOf(after, wallet).Should().Be(new Money(450.0000m));
        BalanceOf(after, savings).Should().Be(new Money(150.0000m));
    }

    [Fact]
    public async Task RemoveTransfer_UnknownTransferId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> remove = () => RemoveAsync(unknownId);

        var refusal = (await remove.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Transfer.NotFound");
        refusal.Message.Should().Be($"No transfer in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RemoveTransfer_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account wallet = SeedAccount("Cash wallet", holding: 500.0000m);
        Account savings = SeedAccount("Savings jar", holding: 100.0000m);
        Transfer transfer = SeedTransfer(wallet, savings, 200.0000m);
        using var cancellation = new CancellationTokenSource();

        await new RemoveTransfer(_transfers, _unitOfWork).ExecuteAsync(transfer.Id, cancellation.Token);

        _transfers.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
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

    private Transfer SeedTransfer(Account source, Account destination, decimal amount)
    {
        Transfer transfer = Transfer.Record(
            Guid.CreateVersion7(),
            source.Id,
            destination.Id,
            new Money(amount),
            OccurredOn,
            Now);

        _transfers.Seed(transfer);

        return transfer;
    }

    private Task RemoveAsync(Guid transferId) =>
        new RemoveTransfer(_transfers, _unitOfWork).ExecuteAsync(transferId, CancellationToken.None);

    private Task<AccountBalances> ReadBalancesAsync() =>
        new GetAccountBalances(_ledger).ExecuteAsync(accountId: null, CancellationToken.None);
}
