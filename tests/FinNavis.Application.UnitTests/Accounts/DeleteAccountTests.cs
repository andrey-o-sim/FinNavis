using FinNavis.Application.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.Accounts;

public sealed class DeleteAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeLedgerQueries _ledger;

    public DeleteAccountTests()
    {
        _unitOfWork = new FakeUnitOfWork(_accounts.Commit);
        _ledger = new FakeLedgerQueries(_accounts);
    }

    // AC-09: the account cannot see its own history, so the question goes to the read path. A
    // transfer counts from either side, which the query behind that port already covers.
    [Fact]
    public async Task DeleteAccount_WithRecordedHistory_IsRefused()
    {
        Account account = Seed("Cash wallet");
        _ledger.AccountsWithRecordedHistory.Add(account.Id);

        Func<Task> delete = () => DeleteAsync(account.Id);

        var refusal = (await delete.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.HasRecordedHistory");
        refusal.Message.Should().Be("Recorded history depends on this account, so it cannot be deleted.");
        _accounts.Stored.Should().ContainSingle();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAccount_WithNoMovement_IsRemoved()
    {
        Account account = Seed("Spare card");

        await DeleteAsync(account.Id);

        _accounts.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAccount_UnknownAccountId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> delete = () => DeleteAsync(unknownId);

        var refusal = (await delete.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.NotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAccount_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account account = Seed("Spare card");
        using var cancellation = new CancellationTokenSource();

        await new DeleteAccount(_accounts, _ledger, _unitOfWork)
            .ExecuteAsync(account.Id, cancellation.Token);

        _accounts.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _ledger.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Account Seed(string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);

        return account;
    }

    private Task DeleteAsync(Guid accountId) =>
        new DeleteAccount(_accounts, _ledger, _unitOfWork).ExecuteAsync(accountId, CancellationToken.None);
}
