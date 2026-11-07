using System.Collections.ObjectModel;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;

namespace FinNavis.Application.UnitTests.Import;

public sealed class InspectFileTests
{
    private const string ShapeKey = "date|description|category|amount";
    private const string MissingColumnMessage =
        "This file does not say the date or the amount, so nothing can be recorded from it.";

    private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly string[] StatementColumns = ["Date", "Description", "Category", "Amount"];

    private readonly FakeMappingProfileRepository _profiles = new();

    [Fact]
    public async Task ExecuteAsync_NewShape_ListsEachOfTheFilesCategoriesOnce()
    {
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"),
            Row(3, "2026-10-02", "TEST BUS LINE", "TEST TRANSPORT", "-2.4000"),
            Row(4, "2026-10-03", "TEST SHOP TWO", "TEST GROCERIES", "-8.1000"),
            Row(5, "2026-10-04", "TEST OWN SAVINGS", "TEST TRANSFER", "-100.0000"));

        FileInspection inspection = await InspectAsync(reader);

        inspection.ShapeKey.Should().Be(ShapeKey);
        inspection.SourceCategories.Should().OnlyHaveUniqueItems();
        inspection.SourceCategories.Should().BeEquivalentTo(
            ["TEST GROCERIES", "TEST TRANSPORT", "TEST TRANSFER"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShapeNeverConfirmed_SaysNoRulesExistYet()
    {
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"));

        FileInspection inspection = await InspectAsync(reader);

        inspection.MappingProfileId.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShapeAlreadyConfirmed_ReturnsTheProfileOfThatShape()
    {
        MappingProfile confirmed = MappingProfile.Create(
            Guid.CreateVersion7(),
            ShapeKey,
            [
                MappingRule.Create(
                    Guid.CreateVersion7(),
                    "TEST GROCERIES",
                    MappingRuleTarget.Category,
                    Guid.CreateVersion7(),
                    Now),
            ],
            Now);
        _profiles.Seed(confirmed);
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"));

        FileInspection inspection = await InspectAsync(reader);

        inspection.MappingProfileId.Should().Be(confirmed.Id);
        inspection.SourceCategories.Should().BeEquivalentTo(["TEST GROCERIES"]);
    }

    [Theory]
    [InlineData(StatementColumn.Date, "date")]
    [InlineData(StatementColumn.Amount, "amount")]
    public async Task ExecuteAsync_FileNamesNoDateOrNoAmount_IsRefusedNamingTheMissingOne(
        StatementColumn missing,
        string missingName)
    {
        FakeStatementReader reader = new(
            Header(missing, StatementColumn.Account),
            [Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000")]);

        Func<Task> inspect = () => InspectAsync(reader);

        DomainRuleViolationException refusal =
            (await inspect.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MissingRequiredColumn");
        refusal.Message.Should().Be(MissingColumnMessage);
        MissingNames(refusal).Should().BeEquivalentTo([missingName]);
        _profiles.Staged.Should().BeEmpty();
        _profiles.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_FileNamesNeitherDateNorAmount_IsRefusedNamingBoth()
    {
        FakeStatementReader reader = new(
            Header(StatementColumn.Date, StatementColumn.Amount, StatementColumn.Account),
            []);

        Func<Task> inspect = () => InspectAsync(reader);

        DomainRuleViolationException refusal =
            (await inspect.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Import.MissingRequiredColumn");
        MissingNames(refusal).Should().BeEquivalentTo(["date", "amount"]);
    }

    // A statement covers the one account the owner names when handing it over, so a statement
    // header without an account column is the normal case, not a gap.
    [Fact]
    public async Task ExecuteAsync_StatementWithoutAnAccountColumn_IsNotRefused()
    {
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"));

        FileInspection inspection = await InspectAsync(reader);

        inspection.SourceCategories.Should().BeEquivalentTo(["TEST GROCERIES"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_RecordsNothing()
    {
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"));

        await InspectAsync(reader);

        _profiles.Staged.Should().BeEmpty();
        _profiles.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_PassesTheCancellationTokenThrough()
    {
        FakeStatementReader reader = StatementReader(
            Row(2, "2026-10-01", "TEST SHOP ONE", "TEST GROCERIES", "-12.5000"));
        using var cancellation = new CancellationTokenSource();
        using var statement = new MemoryStream();

        await new InspectFile(reader, _profiles).ExecuteAsync(
            statement,
            ImportFileKind.Statement,
            cancellation.Token);

        reader.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _profiles.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
    }

    private static IEnumerable<string> MissingNames(DomainRuleViolationException refusal)
    {
        refusal.Details.Should().NotBeNull().And.ContainKey("missing");

        return refusal.Details!["missing"].Should().BeAssignableTo<IEnumerable<string>>().Subject;
    }

    private static FakeStatementReader StatementReader(params StatementRow[] rows) =>
        new(Header(StatementColumn.Account), rows);

    private static StatementHeader Header(params StatementColumn[] missing)
    {
        string[] columnNames =
        [
            .. StatementColumns.Where(name => !missing.Any(
                column => string.Equals(column.ToString(), name, StringComparison.OrdinalIgnoreCase))),
        ];

        return new StatementHeader(
            columnNames,
            ShapeKey,
            missing.ToHashSet(),
            ReadOnlyDictionary<StatementColumn, int>.Empty);
    }

    private static StatementRow Row(int lineNumber, params string[] values) =>
        new(lineNumber, values, string.Join(',', values));

    private async Task<FileInspection> InspectAsync(FakeStatementReader reader)
    {
        using var statement = new MemoryStream();

        return await new InspectFile(reader, _profiles).ExecuteAsync(
            statement,
            ImportFileKind.Statement,
            CancellationToken.None);
    }
}
