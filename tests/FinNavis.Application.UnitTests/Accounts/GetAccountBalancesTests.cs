using FinNavis.Application.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Accounts;

public sealed class GetAccountBalancesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeLedgerQueries _ledger;

    public GetAccountBalancesTests() => _ledger = new FakeLedgerQueries(_accounts);

    // AC-15: the total is the per-account figures added up, so an amount that left one account
    // and arrived in another cancels itself out — there is no second query to disagree.
    [Fact]
    public async Task GetAccountBalances_OverEveryAccount_TotalsThePerAccountFigures()
    {
        Account wallet = Seed("Cash wallet", 320m);
        Account jar = Seed("Savings jar", 180m);

        AccountBalances balances = await ReadAsync(accountId: null);

        balances.Accounts.Should().HaveCount(2);
        balances.Accounts.Single(row => row.AccountId == wallet.Id).Balance.Should().Be(new Money(320m));
        balances.Accounts.Single(row => row.AccountId == jar.Id).Balance.Should().Be(new Money(180m));
        balances.Total.Should().Be(new Money(500m));
    }

    [Fact]
    public async Task GetAccountBalances_ForOneAccount_ReturnsThatAccountOnly()
    {
        Account wallet = Seed("Cash wallet", 320m);
        Seed("Savings jar", 180m);

        AccountBalances balances = await ReadAsync(wallet.Id);

        balances.Accounts.Should().ContainSingle().Which.AccountId.Should().Be(wallet.Id);
        balances.Total.Should().Be(new Money(320m));
    }

    [Fact]
    public async Task GetAccountBalances_WithANegativeBalance_KeepsTheSign()
    {
        Seed("Cash wallet", 320m);
        Seed("Overdrawn card", -40.5m);

        AccountBalances balances = await ReadAsync(accountId: null);

        balances.Total.Should().Be(new Money(279.5m));
    }

    // AC-25: an account the ledger does not hold is reported as unknown. No zero is invented for
    // it, and nothing is created on the way.
    [Fact]
    public async Task GetAccountBalances_UnknownAccountId_IsRefusedAndInventsNoBalance()
    {
        Seed("Cash wallet", 320m);
        Guid unknownId = Guid.CreateVersion7();

        Func<Task> read = () => ReadAsync(unknownId);

        var refusal = (await read.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.NotFound");
        refusal.Message.Should().Be($"No account in the ledger has id {unknownId}.");
        _accounts.Stored.Should().ContainSingle();
    }

    // The ledger holding no account at all is an empty answer, not an unknown account.
    [Fact]
    public async Task GetAccountBalances_WithNoAccountInTheLedger_IsAnEmptyAnswer()
    {
        AccountBalances balances = await ReadAsync(accountId: null);

        balances.Accounts.Should().BeEmpty();
        balances.Total.Should().Be(Money.Zero);
    }

    [Fact]
    public async Task GetAccountBalances_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Seed("Cash wallet", 320m);
        using var cancellation = new CancellationTokenSource();

        await new GetAccountBalances(_ledger).ExecuteAsync(accountId: null, cancellation.Token);

        _ledger.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
    }

    private Account Seed(string name, decimal balance)
    {
        Account account = Account.Create(Guid.CreateVersion7(), name, Now);
        _accounts.Seed(account);
        _ledger.Balances[account.Id] = balance;

        return account;
    }

    private Task<AccountBalances> ReadAsync(Guid? accountId) =>
        new GetAccountBalances(_ledger).ExecuteAsync(accountId, CancellationToken.None);
}
