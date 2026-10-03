using FinNavis.Application.Common;
using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class CreateAccountTests
{
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly RecordingUnitOfWork _unitOfWork = new();
    private readonly CreateAccount _useCase;

    public CreateAccountTests()
    {
        _useCase = new CreateAccount(_accounts, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_ReturnsTheCreatedAccount()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", 120.50m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Wallet");
        result.Value.Type.Should().Be("Cash");
        result.Value.InitialBalance.Should().Be(120.50m);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_AddsTheAccountAndCommitsOnce()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", 120.50m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        _accounts.Items.Single().Id.Should().Be(result.Value.Id);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoTransactions_SetsBalanceToInitialBalance()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", 120.50m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Value.Balance.Should().Be(result.Value.InitialBalance);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_GeneratesVersion7Id()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Value.Id.Version.Should().Be(7);
    }

    [Theory]
    [InlineData("cash", "Cash")]
    [InlineData("CARD", "Card")]
    [InlineData("sAvInGs", "Savings")]
    public async Task ExecuteAsync_WithTypeInAnyCase_Succeeds(string input, string expected)
    {
        var command = new CreateAccountCommand("Wallet", input, 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WithPaddedName_TrimsTheName()
    {
        var command = new CreateAccountCommand("  Wallet  ", "Cash", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Value.Name.Should().Be("Wallet");
    }

    [Fact]
    public async Task ExecuteAsync_WithNegativeInitialBalance_Succeeds()
    {
        var command = new CreateAccountCommand("Visa", "Card", -340.00m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InitialBalance.Should().Be(-340.00m);
        result.Value.Balance.Should().Be(-340.00m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithBlankName_ReturnsNameError(string name)
    {
        var command = new CreateAccountCommand(name, "Cash", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task ExecuteAsync_WithNameOverMaxLength_ReturnsNameError()
    {
        var command = new CreateAccountCommand(new string('a', Account.MaxNameLength + 1), "Cash", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownType_ReturnsTypeError()
    {
        var command = new CreateAccountCommand("Wallet", "Crypto", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("type");
    }

    [Theory]
    [InlineData("7")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task ExecuteAsync_WithNumericType_ReturnsTypeError(string type)
    {
        // Enum.TryParse accepts a number and hands back an undefined value. Without
        // Enum.IsDefined this would create an account whose type column reads "7".
        var command = new CreateAccountCommand("Wallet", type, 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("type");
        _accounts.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithThreeDecimalPlaces_ReturnsInitialBalanceError()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", 1.005m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("initialBalance");
    }

    [Fact]
    public async Task ExecuteAsync_WithInitialBalanceOverMaxMagnitude_ReturnsInitialBalanceError()
    {
        var command = new CreateAccountCommand("Wallet", "Cash", Account.MaxAbsoluteBalance + 0.01m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Should().ContainKey("initialBalance");
    }

    [Fact]
    public async Task ExecuteAsync_WithSeveralInvalidFields_ReturnsOneEntryPerField()
    {
        var command = new CreateAccountCommand("  ", "Crypto", 1.005m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Kind.Should().Be(ResultKind.Invalid);
        result.Errors.Keys.Should().BeEquivalentTo(["name", "type", "initialBalance"]);
        result.Errors.Values.Should().AllSatisfy(messages => messages.Should().ContainSingle());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCommand_AddsNothingAndCommitsNothing()
    {
        var command = new CreateAccountCommand("  ", "Crypto", 1.005m);

        await _useCase.ExecuteAsync(command, CancellationToken.None);

        _accounts.Items.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCommand_ReadingValueThrows()
    {
        var command = new CreateAccountCommand("  ", "Cash", 0m);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        var readValue = () => result.Value;

        readValue.Should().Throw<InvalidOperationException>();
    }
}
