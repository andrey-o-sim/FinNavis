using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class ListAccountsUseCaseTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task ExecuteAsync_RepositoryHasAccounts_ReturnsThemInOrder()
    {
        var repository = new InMemoryAccountRepository();
        repository.Add(Account.Create("Zebra", AccountType.Cash, 0m, null, Clock));
        repository.Add(Account.Create("Apple", AccountType.Cash, 0m, null, Clock));
        var useCase = new ListAccountsUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Select(a => a.Name).Should().Equal("Apple", "Zebra");
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryHasNothing_ReturnsEmpty()
    {
        var repository = new InMemoryAccountRepository();
        var useCase = new ListAccountsUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}
