using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class GetAccountByIdUseCaseTests
{
    private static readonly IClock Clock = new FixedClock(new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task ExecuteAsync_IdExists_ReturnsDto()
    {
        var repository = new InMemoryAccountRepository();
        var account = Account.Create("Cash", AccountType.Cash, 0m, null, Clock);
        repository.Add(account);
        var useCase = new GetAccountByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(account.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.Name.Should().Be("Cash");
    }

    [Fact]
    public async Task ExecuteAsync_IdDoesNotExist_ReturnsNull()
    {
        var repository = new InMemoryAccountRepository();
        var useCase = new GetAccountByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
