using System.Reflection;
using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Application.Import.ConfirmMappingProfile;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Application.UnitTests.Fakes;
using FinNavis.Domain.Entities;
using FinNavis.Domain.Enums;
using FinNavis.Domain.Errors;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.UnitTests.Import;

public sealed class ConfirmMappingProfileTests
{
    private const string ShapeKey = "date|description|category|amount";
    private const string UnknownCategoryMessage =
        "There is no category with that name, so a rule cannot point at it.";

    private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LastMonth = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeMappingProfileRepository _profiles = new();
    private readonly FakeUnitOfWork _unitOfWork;

    public ConfirmMappingProfileTests() =>
        _unitOfWork = new FakeUnitOfWork(() =>
        {
            _categories.Commit();
            _profiles.Commit();
        });

    [Fact]
    public async Task ExecuteAsync_NewShape_RemembersTheRulesUnderItsShapeKey()
    {
        Category groceries = SeedCategory("Groceries");

        MappingProfileConfirmation confirmation = await ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, groceries.Id),
            new RuleToConfirm("TEST TRANSFER", MappingRuleTarget.TransferBetweenOwnAccounts, null));

        confirmation.IsNewShape.Should().BeTrue();
        _unitOfWork.Commits.Should().Be(1);

        MappingProfile? remembered = await _profiles.GetByShapeKeyAsync(ShapeKey, CancellationToken.None);
        remembered.Should().NotBeNull();
        remembered!.Id.Should().Be(confirmation.Profile.Id);
        remembered.Rules.Select(rule => (rule.SourceCategory, rule.Target, rule.TargetCategoryId))
            .Should().BeEquivalentTo(
            [
                ("TEST GROCERIES", MappingRuleTarget.Category, (Guid?)groceries.Id),
                ("TEST TRANSFER", MappingRuleTarget.TransferBetweenOwnAccounts, (Guid?)null),
            ]);
    }

    [Fact]
    public async Task ExecuteAsync_AfterConfirm_NextFileOfThatShapeNeedsNoConfirmation()
    {
        Category groceries = SeedCategory("Groceries");

        MappingProfileConfirmation confirmation = await ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, groceries.Id));

        HashSet<StatementColumn> statementHasNoAccountColumn = [StatementColumn.Account];
        FakeStatementReader nextFile = new(
            new StatementHeader(
                ["Date", "Description", "Category", "Amount"],
                ShapeKey,
                statementHasNoAccountColumn),
            [new StatementRow(2, ["2026-11-01", "TEST SHOP ONE", "TEST GROCERIES", "-9.9000"], "")]);
        using var statement = new MemoryStream();

        FileInspection inspection =
            await new InspectFile(nextFile, _profiles).ExecuteAsync(statement, CancellationToken.None);

        inspection.MappingProfileId.Should().Be(confirmation.Profile.Id);
    }

    // A category the owner does not want filed gets no rule at all; its rows are set aside at load,
    // so a confirm with no rule is legal and still remembers the shape.
    [Fact]
    public async Task ExecuteAsync_NoRules_RemembersTheShapeWithNoRule()
    {
        MappingProfileConfirmation confirmation = await ConfirmAsync(ShapeKey);

        confirmation.IsNewShape.Should().BeTrue();
        MappingProfile? remembered = await _profiles.GetByShapeKeyAsync(ShapeKey, CancellationToken.None);
        remembered.Should().NotBeNull();
        remembered!.Rules.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_RulePointsAtUnknownCategory_IsRefusedNamingItBack()
    {
        Guid unknownCategoryId = Guid.CreateVersion7();

        Func<Task> confirm = () => ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, unknownCategoryId));

        DomainRuleViolationException refusal =
            (await confirm.Should().ThrowAsync<DomainRuleViolationException>()).Which;
        refusal.Code.Should().Be("Category.NotFound");
        refusal.Message.Should().Be(UnknownCategoryMessage);
        refusal.Details.Should().NotBeNull();
        refusal.Details!.Should().Contain("source_category", "TEST GROCERIES");
        refusal.Details.Should().Contain("target_category_id", unknownCategoryId);
    }

    // The refusal is about the whole confirm: a valid rule next to an unknown one is not kept on
    // its own, and the unknown category is not created to make the rule fit.
    [Fact]
    public async Task ExecuteAsync_OneOfSeveralRulesPointsAtUnknownCategory_CreatesNothing()
    {
        Category groceries = SeedCategory("Groceries");

        Func<Task> confirm = () => ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, groceries.Id),
            new RuleToConfirm("TEST TRANSPORT", MappingRuleTarget.Category, Guid.CreateVersion7()));

        await confirm.Should().ThrowAsync<DomainRuleViolationException>();

        _unitOfWork.Commits.Should().Be(0);
        _profiles.Staged.Should().BeEmpty();
        _profiles.Stored.Should().BeEmpty();
        _categories.Stored.Should().ContainSingle().Which.Id.Should().Be(groceries.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ReconfirmWithUnknownCategory_KeepsTheRulesItHad()
    {
        Category groceries = SeedCategory("Groceries");
        MappingProfile existing = SeedProfile(
            ("TEST GROCERIES", groceries.Id));

        Func<Task> confirm = () => ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, Guid.CreateVersion7()));

        await confirm.Should().ThrowAsync<DomainRuleViolationException>();

        _unitOfWork.Commits.Should().Be(0);
        existing.Rules.Should().ContainSingle().Which.TargetCategoryId.Should().Be(groceries.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShapeAlreadyConfirmed_ReplacesEveryRule()
    {
        Category groceries = SeedCategory("Groceries");
        Category transport = SeedCategory("Transport");
        Category food = SeedCategory("Food and drink");
        MappingProfile existing = SeedProfile(
            ("TEST GROCERIES", groceries.Id),
            ("TEST TRANSPORT", transport.Id));

        MappingProfileConfirmation confirmation = await ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, food.Id));

        confirmation.IsNewShape.Should().BeFalse();
        confirmation.Profile.Id.Should().Be(existing.Id);
        _unitOfWork.Commits.Should().Be(1);
        _profiles.Staged.Should().BeEmpty();
        _profiles.Stored.Should().ContainSingle();

        MappingProfile? remembered = await _profiles.GetByShapeKeyAsync(ShapeKey, CancellationToken.None);
        remembered!.Rules.Select(rule => (rule.SourceCategory, rule.TargetCategoryId))
            .Should().BeEquivalentTo([("TEST GROCERIES", (Guid?)food.Id)]);
    }

    // No recorded movement points at a rule, so a new rule can only steer later loads. The rows
    // already recorded under the old rule keep the category they were given.
    [Fact]
    public async Task ExecuteAsync_ShapeAlreadyConfirmed_LeavesRecordedMovementsWithTheirCategory()
    {
        Category groceries = SeedCategory("Groceries");
        Category food = SeedCategory("Food and drink");
        SeedProfile(("TEST GROCERIES", groceries.Id));
        FakeTransactionRepository transactions = new();
        Transaction[] recorded =
        [
            RecordedUnder(groceries, 12.5000m),
            RecordedUnder(groceries, 8.1000m),
        ];
        transactions.Seed(recorded);

        await ConfirmAsync(
            ShapeKey,
            new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, food.Id));

        recorded.Should().AllSatisfy(
            entry => transactions.Committed(entry.Id).CategoryId.Should().Be(groceries.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_PassesTheCancellationTokenThrough()
    {
        Category groceries = SeedCategory("Groceries");
        using var cancellation = new CancellationTokenSource();

        await NewUseCase().ExecuteAsync(
            ShapeKey,
            [new RuleToConfirm("TEST GROCERIES", MappingRuleTarget.Category, groceries.Id)],
            cancellation.Token);

        _categories.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _profiles.TokensSeen.Should().NotBeEmpty().And.AllSatisfy(
            token => token.Should().Be(cancellation.Token));
        _unitOfWork.TokensSeen.Should().AllSatisfy(token => token.Should().Be(cancellation.Token));
    }

    // The file is handed over twice per new bank — once to inspect, once to load. Confirm carries
    // the shape key inspect returned, so it must have no way to take the file a third time.
    [Fact]
    public void ConfirmMappingProfile_Signature_TakesTheShapeKeyAndNoFile()
    {
        Type useCase = typeof(ConfirmMappingProfile);

        ConstructorInfo[] constructors = useCase.GetConstructors();
        constructors.SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Should().NotContain(typeof(IStatementReader));

        MethodInfo execute = useCase.GetMethod(nameof(ConfirmMappingProfile.ExecuteAsync))!;
        ParameterInfo[] parameters = execute.GetParameters();
        parameters.Should().Contain(parameter =>
            parameter.ParameterType == typeof(string) && parameter.Name == "shapeKey");
        parameters.Select(parameter => parameter.ParameterType)
            .Should().NotContain(type => typeof(Stream).IsAssignableFrom(type));
    }

    private static Transaction RecordedUnder(Category category, decimal amount) =>
        Transaction.Record(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            category.Id,
            new Money(amount),
            new DateOnly(2026, 10, 3),
            note: null,
            LastMonth);

    private Category SeedCategory(string name)
    {
        Category category = Category.Create(
            Guid.CreateVersion7(),
            name,
            CategoryKind.Expense,
            group: null,
            LastMonth);
        _categories.Seed(category);

        return category;
    }

    private MappingProfile SeedProfile(params (string SourceCategory, Guid TargetCategoryId)[] rules)
    {
        MappingProfile profile = MappingProfile.Create(
            Guid.CreateVersion7(),
            ShapeKey,
            [
                .. rules.Select(rule => MappingRule.Create(
                    Guid.CreateVersion7(),
                    rule.SourceCategory,
                    MappingRuleTarget.Category,
                    rule.TargetCategoryId,
                    LastMonth)),
            ],
            LastMonth);
        _profiles.Seed(profile);

        return profile;
    }

    private ConfirmMappingProfile NewUseCase() =>
        new(_categories, _profiles, _unitOfWork, new FixedClock(Now));

    private Task<MappingProfileConfirmation> ConfirmAsync(string shapeKey, params RuleToConfirm[] rules) =>
        NewUseCase().ExecuteAsync(shapeKey, rules, CancellationToken.None);
}
