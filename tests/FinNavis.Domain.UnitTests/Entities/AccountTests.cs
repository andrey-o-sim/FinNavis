using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class AccountTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_SetsEveryProperty()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 120.50m, CreatedAt);

        account.Name.Should().Be("Wallet");
        account.Type.Should().Be(AccountType.Cash);
        account.InitialBalance.Should().Be(120.50m);
        account.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_Always_GeneratesVersion7Id()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 0m, CreatedAt);

        account.Id.Should().NotBe(Guid.Empty);
        account.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var first = Account.Create("Wallet", AccountType.Cash, 0m, CreatedAt);
        var second = Account.Create("Wallet", AccountType.Cash, 0m, CreatedAt);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithPaddedName_TrimsTheName()
    {
        var account = Account.Create("  Wallet  ", AccountType.Cash, 0m, CreatedAt);

        account.Name.Should().Be("Wallet");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Throws(string name)
    {
        var create = () => Account.Create(name, AccountType.Cash, 0m, CreatedAt);

        create.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WithNameOverMaxLength_Throws()
    {
        var name = new string('a', Account.MaxNameLength + 1);

        var create = () => Account.Create(name, AccountType.Cash, 0m, CreatedAt);

        create.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WithNameExactlyMaxLength_Succeeds()
    {
        var name = new string('a', Account.MaxNameLength);

        var account = Account.Create(name, AccountType.Cash, 0m, CreatedAt);

        account.Name.Should().Be(name);
    }

    [Fact]
    public void Create_WithNegativeInitialBalance_Succeeds()
    {
        var account = Account.Create("Visa", AccountType.Card, -340.00m, CreatedAt);

        account.InitialBalance.Should().Be(-340.00m);
    }

    [Fact]
    public void Create_WithThreeDecimalPlaces_Throws()
    {
        var create = () => Account.Create("Wallet", AccountType.Cash, 1.005m, CreatedAt);

        create.Should().Throw<ArgumentException>().WithParameterName("initialBalance");
    }

    [Fact]
    public void Create_WithUndefinedType_Throws()
    {
        var create = () => Account.Create("Wallet", (AccountType)7, 0m, CreatedAt);

        create.Should().Throw<ArgumentException>().WithParameterName("type");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_WithBlankName_ReturnsMessage(string? name)
    {
        Account.ValidateName(name).Should().Be("Name is required.");
    }

    [Fact]
    public void ValidateName_WithNameOverMaxLength_ReturnsMessage()
    {
        var name = new string('a', Account.MaxNameLength + 1);

        Account.ValidateName(name).Should().Be("Name must be 100 characters or fewer.");
    }

    [Fact]
    public void ValidateName_WithPaddedNameThatFitsAfterTrimming_ReturnsNull()
    {
        var name = $"  {new string('a', Account.MaxNameLength)}  ";

        Account.ValidateName(name).Should().BeNull();
    }

    [Fact]
    public void ValidateName_WithValidName_ReturnsNull()
    {
        Account.ValidateName("Wallet").Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120.50)]
    [InlineData(-340.00)]
    public void ValidateInitialBalance_WithAllowedValue_ReturnsNull(decimal value)
    {
        Account.ValidateInitialBalance(value).Should().BeNull();
    }

    [Theory]
    [InlineData(1.005)]
    [InlineData(-1.005)]
    public void ValidateInitialBalance_WithThreeDecimalPlaces_ReturnsMessage(decimal value)
    {
        Account.ValidateInitialBalance(value)
            .Should().Be("Initial balance must have 2 decimal places or fewer.");
    }

    [Fact]
    public void ValidateInitialBalance_AtMaxMagnitude_ReturnsNull()
    {
        Account.ValidateInitialBalance(Account.MaxAbsoluteBalance).Should().BeNull();
        Account.ValidateInitialBalance(-Account.MaxAbsoluteBalance).Should().BeNull();
    }

    [Fact]
    public void ValidateInitialBalance_OverMaxMagnitude_ReturnsMessage()
    {
        var tooBig = Account.MaxAbsoluteBalance + 0.01m;

        Account.ValidateInitialBalance(tooBig).Should().NotBeNull();
        Account.ValidateInitialBalance(-tooBig).Should().NotBeNull();
    }

    [Fact]
    public void CalculateBalance_WithNoTransactions_ReturnsInitialBalance()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 120.50m, CreatedAt);

        account.CalculateBalance(0m).Should().Be(120.50m);
    }

    [Fact]
    public void CalculateBalance_WithPositiveTotal_AddsItToInitialBalance()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 120.50m, CreatedAt);

        account.CalculateBalance(10.25m).Should().Be(130.75m);
    }

    [Fact]
    public void CalculateBalance_WithNegativeTotal_SubtractsItFromInitialBalance()
    {
        var account = Account.Create("Wallet", AccountType.Cash, 120.50m, CreatedAt);

        account.CalculateBalance(-200.50m).Should().Be(-80.00m);
    }
}
