using System.Globalization;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Exceptions;
using FinNavis.Domain.UnitTests.Fakes;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class AccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly FixedClock Clock = new(Now);

    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var account = Account.Create("Cash wallet", AccountType.Cash, 123.45m, "a note", Clock);

        account.Name.Should().Be("Cash wallet");
        account.Type.Should().Be(AccountType.Cash);
        account.OpeningBalance.Should().Be(123.45m);
        account.Note.Should().Be("a note");
        account.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_NameWithSurroundingSpaces_TrimsName()
    {
        var account = Account.Create("  Cash  ", AccountType.Cash, 0m, null, Clock);

        account.Name.Should().Be("Cash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NameMissingEmptyOrWhitespace_Throws(string? name)
    {
        var act = () => Account.Create(name, AccountType.Cash, 0m, null, Clock);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey("name");
    }

    [Fact]
    public void Create_NameOf100Characters_Accepted()
    {
        var name = new string('a', 100);

        var account = Account.Create(name, AccountType.Cash, 0m, null, Clock);

        account.Name.Should().Be(name);
    }

    [Fact]
    public void Create_NameOf101Characters_Throws()
    {
        var name = new string('a', 101);

        var act = () => Account.Create(name, AccountType.Cash, 0m, null, Clock);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey("name");
    }

    [Fact]
    public void Create_NoteOf500Characters_Accepted()
    {
        var note = new string('n', 500);

        var account = Account.Create("Cash", AccountType.Cash, 0m, note, Clock);

        account.Note.Should().Be(note);
    }

    [Fact]
    public void Create_NoteOf501Characters_Throws()
    {
        var note = new string('n', 501);

        var act = () => Account.Create("Cash", AccountType.Cash, 0m, note, Clock);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey("note");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NoteMissingEmptyOrWhitespace_StoredAsNull(string? note)
    {
        var account = Account.Create("Cash", AccountType.Cash, 0m, note, Clock);

        account.Note.Should().BeNull();
    }

    [Fact]
    public void Create_ZeroOpeningBalance_Accepted()
    {
        var account = Account.Create("Cash", AccountType.Cash, 0m, null, Clock);

        account.OpeningBalance.Should().Be(0m);
    }

    [Fact]
    public void Create_NegativeOpeningBalance_Accepted()
    {
        var account = Account.Create("Credit card", AccountType.Card, -250.00m, null, Clock);

        account.OpeningBalance.Should().Be(-250.00m);
    }

    // Money arrives as a string in every theory below. A C# attribute cannot hold a decimal
    // literal, so `[InlineData(9999999999999999.99)]` would silently be a double and arrive as
    // 1E+16 — the exact value these tests exist to pin.
    private static decimal Money(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("10.994", "10.99")]
    [InlineData("10.995", "11.00")]   // AwayFromZero, not banker's rounding, to match PostgreSQL
    [InlineData("10.565", "10.57")]   // ToEven would give 10.56 here
    [InlineData("-10.565", "-10.57")]
    public void Create_OpeningBalanceHasMoreThanTwoDecimals_RoundsToTheStoredScale(
        string sent, string expected)
    {
        var account = Account.Create("Cash", AccountType.Cash, Money(sent), null, Clock);

        account.OpeningBalance.Should().Be(Money(expected));
    }

    [Theory]
    [InlineData("9999999999999999.99")]
    [InlineData("-9999999999999999.99")]
    public void Create_OpeningBalanceAtTheEdgeOfTheColumn_Accepted(string openingBalance)
    {
        var account = Account.Create("Cash", AccountType.Cash, Money(openingBalance), null, Clock);

        account.OpeningBalance.Should().Be(Money(openingBalance));
    }

    [Theory]
    [InlineData("10000000000000000")]
    [InlineData("-10000000000000000")]
    [InlineData("99999999999999999")]
    [InlineData("9999999999999999.995")]   // rounds up, over the edge
    public void Create_OpeningBalanceWiderThanTheColumn_ThrowsWithOpeningBalanceKey(string value)
    {
        var openingBalance = Money(value);

        // Without this rule PostgreSQL raises "numeric field overflow" at save time, which is not
        // a unique-constraint violation, so it would surface to the caller as a 500.
        var act = () => Account.Create("Cash", AccountType.Cash, openingBalance, null, Clock);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey("openingBalance");
    }

    [Fact]
    public void Create_ValidInput_CreatedAtEqualsClockValue()
    {
        var account = Account.Create("Cash", AccountType.Cash, 0m, null, Clock);

        account.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_ValidInput_IdIsVersion7()
    {
        var account = Account.Create("Cash", AccountType.Cash, 0m, null, Clock);

        account.Id.Version.Should().Be(7);
    }

    [Fact]
    public void Create_NamesDifferingByCaseAndSpaces_ProduceSameNormalizedName()
    {
        var first = Account.Create(" Cash ", AccountType.Cash, 0m, null, Clock);
        var second = Account.Create("cash", AccountType.Cash, 0m, null, Clock);

        first.NormalizedName.Should().Be(second.NormalizedName);
    }

    [Fact]
    public void Create_InvalidNameAndNote_ErrorsKeyedByBothFields()
    {
        var act = () => Account.Create(null, AccountType.Cash, 0m, new string('n', 501), Clock);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Keys.Should().Contain(["name", "note"]);
    }
}
