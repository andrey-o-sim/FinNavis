using FinNavis.Application.Common;
using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class GetAccountByIdTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly GetAccountById _useCase;

    public GetAccountByIdTests()
    {
        _useCase = new GetAccountById(_accounts);
    }

    [Fact]
    public async Task ExecuteAsync_WithKnownId_ReturnsTheAccount()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 120.50m, CreatedAt);
        _accounts.Items.Add(account);

        var result = await _useCase.ExecuteAsync(account.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(account.Id);
        result.Value.Name.Should().Be("Wallet");
        result.Value.Type.Should().Be("Cash");
        result.Value.InitialBalance.Should().Be(120.50m);
        result.Value.Balance.Should().Be(120.50m);
        result.Value.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownId_ReturnsNotFound()
    {
        var result = await _useCase.ExecuteAsync(Guid.CreateVersion7(), CancellationToken.None);

        result.Kind.Should().Be(ResultKind.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownId_ReadingValueThrows()
    {
        var result = await _useCase.ExecuteAsync(Guid.CreateVersion7(), CancellationToken.None);

        var readValue = () => result.Value;

        readValue.Should().Throw<InvalidOperationException>();
    }
}
