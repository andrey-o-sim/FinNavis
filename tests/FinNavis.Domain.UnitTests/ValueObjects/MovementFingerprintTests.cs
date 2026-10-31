using System.Globalization;
using FinNavis.Domain.Enums;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Domain.UnitTests.ValueObjects;

public sealed class MovementFingerprintTests
{
    // Parsed from upper case on purpose: the hash must still use the lowercase form.
    private static readonly Guid AccountId = Guid.Parse("0198A3F0-0000-7000-8000-000000000001");
    private static readonly Guid CategoryId = Guid.Parse("0198a3f0-0000-7000-8000-000000000002");
    private static readonly Guid DestinationAccountId = Guid.Parse("0198a3f0-0000-7000-8000-000000000003");
    private static readonly Guid OtherId = Guid.Parse("0198a3f0-0000-7000-8000-000000000009");
    private static readonly DateOnly OccurredOn = new(2026, 3, 15);

    [Fact]
    public void OfTransaction_KnownFields_MatchesTheHashOfTheJoinedString()
    {
        // SHA-256 of "0198a3f0-0000-7000-8000-000000000001|0198a3f0-0000-7000-8000-000000000002|12.5000|2026-03-15|Coffee"
        var fingerprint = TransactionFingerprint();

        fingerprint.Value.Should().Be("eae7d0162db54b7aac59a078aea3ac0321b373b81e3b821398cf65af4691b3ac");
    }

    [Fact]
    public void OfTransfer_KnownFields_MatchesTheHashOfTheJoinedString()
    {
        // SHA-256 of "0198a3f0-0000-7000-8000-000000000001|0198a3f0-0000-7000-8000-000000000003|12.5000|2026-03-15"
        var fingerprint = TransferFingerprint();

        fingerprint.Value.Should().Be("c168e0feca3cee44c5c460e5420eabad637e2125d7a3cfd171371f3e23995111");
    }

    [Fact]
    public void OfTransaction_AnyFields_GivesSixtyFourLowercaseHexCharacters()
    {
        var fingerprint = TransactionFingerprint();

        fingerprint.Value.Should().HaveLength(MovementFingerprint.Length);
        fingerprint.Value.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void OfTransaction_AmountWithAndWithoutTrailingZeros_HashesTheSame()
    {
        var shortForm = TransactionFingerprint(amount: 12.5m);
        var paddedForm = TransactionFingerprint(amount: 12.5000m);

        shortForm.Value.Should().Be(paddedForm.Value);
    }

    [Fact]
    public void OfTransfer_AmountWithAndWithoutTrailingZeros_HashesTheSame()
    {
        var shortForm = TransferFingerprint(amount: 12.5m);
        var paddedForm = TransferFingerprint(amount: 12.5000m);

        shortForm.Value.Should().Be(paddedForm.Value);
    }

    [Fact]
    public void OfTransaction_NullAndEmptyNote_HashTheSame()
    {
        var withNullNote = TransactionFingerprint(note: null);
        var withEmptyNote = TransactionFingerprint(note: string.Empty);

        withNullNote.Value.Should().Be(withEmptyNote.Value);
    }

    [Fact]
    public void OfTransaction_AccountChanged_ChangesTheHash()
    {
        TransactionFingerprint(accountId: OtherId).Value.Should().NotBe(TransactionFingerprint().Value);
    }

    [Fact]
    public void OfTransaction_CategoryChanged_ChangesTheHash()
    {
        TransactionFingerprint(categoryId: OtherId).Value.Should().NotBe(TransactionFingerprint().Value);
    }

    [Fact]
    public void OfTransaction_AmountChangedInTheLastDecimal_ChangesTheHash()
    {
        TransactionFingerprint(amount: 12.5001m).Value.Should().NotBe(TransactionFingerprint().Value);
    }

    [Fact]
    public void OfTransaction_DateChangedByOneDay_ChangesTheHash()
    {
        TransactionFingerprint(occurredOn: OccurredOn.AddDays(1)).Value
            .Should().NotBe(TransactionFingerprint().Value);
    }

    [Fact]
    public void OfTransaction_NoteChangedByOneCharacter_ChangesTheHash()
    {
        TransactionFingerprint(note: "Coffeo").Value.Should().NotBe(TransactionFingerprint().Value);
    }

    [Fact]
    public void OfTransfer_SourceChanged_ChangesTheHash()
    {
        TransferFingerprint(sourceAccountId: OtherId).Value.Should().NotBe(TransferFingerprint().Value);
    }

    [Fact]
    public void OfTransfer_DestinationChanged_ChangesTheHash()
    {
        TransferFingerprint(destinationAccountId: OtherId).Value.Should().NotBe(TransferFingerprint().Value);
    }

    [Fact]
    public void OfTransfer_AmountChangedInTheLastDecimal_ChangesTheHash()
    {
        TransferFingerprint(amount: 12.5001m).Value.Should().NotBe(TransferFingerprint().Value);
    }

    [Fact]
    public void OfTransfer_DateChangedByOneDay_ChangesTheHash()
    {
        TransferFingerprint(occurredOn: OccurredOn.AddDays(1)).Value.Should().NotBe(TransferFingerprint().Value);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("de-DE")]
    public void OfTransaction_CultureWithCommaDecimalSeparator_HashDoesNotMove(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var invariantHash = WithCulture(CultureInfo.InvariantCulture, () => TransactionFingerprint());

        var localHash = WithCulture(culture, () => TransactionFingerprint());

        culture.NumberFormat.NumberDecimalSeparator.Should().Be(",");
        localHash.Value.Should().Be(invariantHash.Value);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("de-DE")]
    public void OfTransfer_CultureWithCommaDecimalSeparator_HashDoesNotMove(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var invariantHash = WithCulture(CultureInfo.InvariantCulture, () => TransferFingerprint());

        var localHash = WithCulture(culture, () => TransferFingerprint());

        culture.NumberFormat.NumberDecimalSeparator.Should().Be(",");
        localHash.Value.Should().Be(invariantHash.Value);
    }

    [Fact]
    public void ImportRunKind_Values_AreExactlyStatementAndHistoryMigration()
    {
        Enum.GetNames<ImportRunKind>().Should().BeEquivalentTo(["Statement", "HistoryMigration"]);
    }

    [Fact]
    public void ImportRunStatus_Values_AreExactlyCompletedAndUndone()
    {
        Enum.GetNames<ImportRunStatus>().Should().BeEquivalentTo(["Completed", "Undone"]);
    }

    [Fact]
    public void SetAsideReason_Values_AreExactlyTheThreeReasons()
    {
        Enum.GetNames<SetAsideReason>().Should()
            .BeEquivalentTo(["NoRuleCovers", "TransferBetweenOwnAccounts", "ForeignCurrency"]);
    }

    [Fact]
    public void MappingRuleTarget_Values_AreExactlyCategoryAndTransferBetweenOwnAccounts()
    {
        Enum.GetNames<MappingRuleTarget>().Should().BeEquivalentTo(["Category", "TransferBetweenOwnAccounts"]);
    }

    private static MovementFingerprint TransactionFingerprint(
        Guid? accountId = null,
        Guid? categoryId = null,
        decimal amount = 12.5m,
        DateOnly? occurredOn = null,
        string? note = "Coffee") =>
        MovementFingerprint.OfTransaction(
            accountId ?? AccountId,
            categoryId ?? CategoryId,
            new Money(amount),
            occurredOn ?? OccurredOn,
            note);

    private static MovementFingerprint TransferFingerprint(
        Guid? sourceAccountId = null,
        Guid? destinationAccountId = null,
        decimal amount = 12.5m,
        DateOnly? occurredOn = null) =>
        MovementFingerprint.OfTransfer(
            sourceAccountId ?? AccountId,
            destinationAccountId ?? DestinationAccountId,
            new Money(amount),
            occurredOn ?? OccurredOn);

    private static MovementFingerprint WithCulture(CultureInfo culture, Func<MovementFingerprint> compute)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            return compute();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
