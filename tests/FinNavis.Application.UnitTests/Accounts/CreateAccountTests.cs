using FinNavis.Application.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Accounts;

public sealed class CreateAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 18, 0, 0, TimeSpan.Zero);

    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeLedgerQueries _ledger;

    public CreateAccountTests()
    {
        _unitOfWork = new FakeUnitOfWork(_accounts.Commit);
        _ledger = new FakeLedgerQueries(_accounts);
    }

    // AC-07: nothing is written to make the balance zero. Zero is what the sum of no movements
    // comes to, so a new account reads back at zero on its own (ADR-0002).
    [Fact]
    public async Task CreateAccount_NewName_CreatedWithZeroBalance()
    {
        Account account = await CreateAsync("Cash wallet");

        AccountBalances balances = await new GetAccountBalances(_ledger)
            .ExecuteAsync(accountId: null, CancellationToken.None);

        account.Name.Should().Be("Cash wallet");
        account.CreatedAt.Should().Be(Now);
        balances.Accounts.Should().ContainSingle().Which.AccountId.Should().Be(account.Id);
        balances.Accounts[0].Balance.Should().Be(Money.Zero);
        balances.Total.Should().Be(Money.Zero);
    }

    // AC-35. The check is scoped exactly as ux_accounts_name is, and the index stays the last
    // guard — this refusal is only what gives the owner the PRD's wording.
    [Fact]
    public async Task CreateAccount_NameAlreadyTaken_IsRefused()
    {
        _accounts.Seed(Account.Create(Guid.CreateVersion7(), "Cash wallet", Now));

        Func<Task> create = () => CreateAsync("Cash wallet");

        var refusal = (await create.Should().ThrowAsync<DomainRuleViolationException>()).Which;

        refusal.Code.Should().Be("Account.DuplicateName");
        refusal.Message.Should().Be("An account named 'Cash wallet' already exists.");
        _accounts.Stored.Should().ContainSingle();
        _unitOfWork.Commits.Should().Be(0);
    }

    // The stored name is trimmed, so the surrounding spaces must not slip past the check.
    [Fact]
    public async Task CreateAccount_NameTakenApartFromSurroundingSpaces_IsRefused()
    {
        _accounts.Seed(Account.Create(Guid.CreateVersion7(), "Cash wallet", Now));

        Func<Task> create = () => CreateAsync("  Cash wallet  ");

        (await create.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Account.DuplicateName");
    }

    [Fact]
    public async Task CreateAccount_WithABlankName_IsRefusedAndWritesNothing()
    {
        Func<Task> create = () => CreateAsync("   ");

        (await create.Should().ThrowAsync<DomainRuleViolationException>())
            .Which.Code.Should().Be("Account.NameInvalid");

        _accounts.Stored.Should().BeEmpty();
        _unitOfWork.Commits.Should().Be(0);
    }

    // ADR-0005: the id is minted here, in the Application layer, and it is a UUID v7.
    [Fact]
    public async Task CreateAccount_NewName_MintsATimeOrderedUuidV7()
    {
        Account first = await CreateAsync("Cash wallet");
        Account second = await CreateAsync("Savings jar");

        first.Id.Version.Should().Be(7);
        second.Id.Version.Should().Be(7);
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task CreateAccount_WhenCalled_PassesTheCancellationTokenThrough()
    {
        using var cancellation = new CancellationTokenSource();

        await new CreateAccount(_accounts, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync("Cash wallet", cancellation.Token);

        _accounts.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    private Task<Account> CreateAsync(string name) =>
        new CreateAccount(_accounts, _unitOfWork, new FixedClock(Now))
            .ExecuteAsync(name, CancellationToken.None);
}
