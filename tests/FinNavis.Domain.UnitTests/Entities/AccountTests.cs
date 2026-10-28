using System.Reflection;

using FinNavis.Domain.Entities;
using FinNavis.Domain.Errors;

namespace FinNavis.Domain.UnitTests.Entities;

public sealed class AccountTests
{
    private const string NameInvalid = "Account.NameInvalid";

    private static readonly DateTimeOffset CreatedAt =
        new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidName_KeepsTheName()
    {
        Account account = Account.Create(Guid.CreateVersion7(), "Cash", CreatedAt);

        account.Name.Should().Be("Cash");
        account.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_NameWithSurroundingSpaces_IsTrimmed()
    {
        Account account = Account.Create(Guid.CreateVersion7(), "  Cash  ", CreatedAt);

        account.Name.Should().Be("Cash");
    }

    [Fact]
    public void Create_NameOfExactlyHundredCharacters_IsAccepted()
    {
        string name = new('a', Account.MaxNameLength);

        Account account = Account.Create(Guid.CreateVersion7(), name, CreatedAt);

        account.Name.Should().Be(name);
    }

    [Fact]
    public void Create_NameOverHundredCharacters_IsRefused()
    {
        string name = new('a', Account.MaxNameLength + 1);

        Func<Account> act = () => Account.Create(Guid.CreateVersion7(), name, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_BlankName_IsRefused(string name)
    {
        Func<Account> act = () => Account.Create(Guid.CreateVersion7(), name, CreatedAt);

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
    }

    [Fact]
    public void Rename_ValidName_KeepsSameIdentity()
    {
        Guid id = Guid.CreateVersion7();
        Account account = Account.Create(id, "Cash", CreatedAt);

        account.Rename("Pocket money");

        // AC-08: every movement points at the account by id, so a rename rewrites nothing.
        account.Id.Should().Be(id);
        account.Name.Should().Be("Pocket money");
        account.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rename_NameOverHundredCharacters_IsRefusedAndKeepsTheOldName()
    {
        Account account = Account.Create(Guid.CreateVersion7(), "Cash", CreatedAt);

        Action act = () => account.Rename(new string('a', Account.MaxNameLength + 1));

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        account.Name.Should().Be("Cash");
    }

    [Fact]
    public void Rename_BlankName_IsRefusedAndKeepsTheOldName()
    {
        Account account = Account.Create(Guid.CreateVersion7(), "Cash", CreatedAt);

        Action act = () => account.Rename("   ");

        act.Should().Throw<DomainRuleViolationException>()
            .Which.Code.Should().Be(NameInvalid);
        account.Name.Should().Be("Cash");
    }

    [Fact]
    public void Account_HasNoBalanceField()
    {
        // AC-14 is computed at read time (ADR-0002). A stored balance would have nothing to keep
        // it honest, so the entity must never grow one.
        IEnumerable<string> members = typeof(Account)
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(member => member.Name);

        members.Should().NotContain(name => name.Contains("Balance", StringComparison.OrdinalIgnoreCase));
    }
}
