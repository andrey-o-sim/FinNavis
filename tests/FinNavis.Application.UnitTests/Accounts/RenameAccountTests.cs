using FinNavis.Application.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Accounts;

public sealed class RenameAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeLedgerQueries _ledger;

    public RenameAccountTests()
    {
        _unitOfWork = new FakeUnitOfWork(_accounts.Commit);
        _ledger = new FakeLedgerQueries(_accounts);
    }

    // Every movement points at the account by id, so a rename rewrites nothing and the balance
    // the ledger reports is the same figure under the new name.
    [Fact]
    public async Task RenameAccount_AccountWithHistory_KeepsEveryMovement()
    {
        Account account = Seed("Cash wallet");
        _ledger.Balances[account.Id] = 299.75m;
        _ledger.AccountsWithRecordedHistory.Add(account.Id);

        Account renamed = await RenameAsync(account.Id, "Everyday card");

        AccountBalances balances = await new GetAccountBalances(_ledger)
            .ExecuteAsync(account.Id, CancellationToken.None);

        renamed.Id.Should().Be(account.Id);
        renamed.Name.Should().Be("Everyday card");
        balances.Accounts.Should().ContainSingle().Which.AccountId.Should().Be(account.Id);
        balances.Accounts[0].Name.Should().Be("Everyday card");
        balances.Accounts[0].Balance.Should().Be(new Money(299.75m));
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RenameAccount_NameAlreadyTaken_IsRefused()
    {
        Account account = Seed("Cash wallet");
        Seed("Savings jar");

        Func<Task> rename = () => RenameAsync(account.Id, "Savings jar");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.DuplicateName");
        refusal.Message.Should().Be("An account named 'Savings jar' already exists.");
        account.Name.Should().Be("Cash wallet");
        _unitOfWork.Commits.Should().Be(0);
    }

    // The name check answers true for the account's own row, so renaming it to what it already
    // carries would look like a clash. The refusal is about another account holding the name.
    [Fact]
    public async Task RenameAccount_ToTheNameItAlreadyCarries_IsAccepted()
    {
        Account account = Seed("Cash wallet");

        Account renamed = await RenameAsync(account.Id, "Cash wallet");

        renamed.Name.Should().Be("Cash wallet");
        _unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task RenameAccount_UnknownAccountId_IsRefusedAsNotFound()
    {
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> rename = () => RenameAsync(unknownId, "Everyday card");

        var refusal = (await rename.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.NotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _accounts.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameAccount_WithABlankName_IsRefusedAndLeavesTheNameAlone()
    {
        Account account = Seed("Cash wallet");

        Func<Task> rename = () => RenameAsync(account.Id, "   ");

        (await rename.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Account.NameInvalid");

        account.Name.Should().Be("Cash wallet");
        _unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task RenameAccount_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Account account = Seed("Cash wallet");
        using var cancellation = new CancellationTokenSource();

        await new RenameAccount(_accounts, _unitOfWork)
            .ExecuteAsync(account.Id, "Everyday card", cancellation.Token);

        _accounts.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Account Seed(string name)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);

        return account;
    }

    private Task<Account> RenameAsync(Guid accountId, string name) =>
        new RenameAccount(_accounts, _unitOfWork).ExecuteAsync(accountId, name, CancellationToken.None);
}
