using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class ListAccountsTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly ListAccounts _useCase;

    public ListAccountsTests()
    {
        _useCase = new ListAccounts(_accounts);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAccounts_ReturnsEmptyList()
    {
        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithAccounts_ReturnsThemOrderedByName()
    {
        _accounts.Items.Add(Account.Create("Wallet", AccountType.Cash, 0m, CreatedAt));
        _accounts.Items.Add(Account.Create("Visa", AccountType.Card, 0m, CreatedAt));
        _accounts.Items.Add(Account.Create("Deposit", AccountType.Savings, 0m, CreatedAt));

        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        result.Select(account => account.Name).Should().Equal("Deposit", "Visa", "Wallet");
    }

    [Fact]
    public async Task ExecuteAsync_WithAccounts_MapsEveryField()
    {
        var account = Account.Create("Visa", AccountType.Card, -340.00m, CreatedAt);
        _accounts.Items.Add(account);

        var result = await _useCase.ExecuteAsync(CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(account.Id);
        dto.Name.Should().Be("Visa");
        dto.Type.Should().Be("Card");
        dto.InitialBalance.Should().Be(-340.00m);
        dto.Balance.Should().Be(-340.00m);
        dto.CreatedAt.Should().Be(CreatedAt);
    }
}
