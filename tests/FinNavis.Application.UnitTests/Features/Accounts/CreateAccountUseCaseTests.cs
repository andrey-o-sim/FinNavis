using FinNavis.Application.Common;
using FinNavis.Application.Features.Accounts;
using FinNavis.Application.UnitTests.Fakes;

namespace FinNavis.Application.UnitTests.Features.Accounts;

public sealed class CreateAccountUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly InMemoryAccountRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateAccountUseCase _useCase;

    public CreateAccountUseCaseTests()
    {
        _useCase = new CreateAccountUseCase(_repository, _unitOfWork, new FixedClock(Now));
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_ReturnsSuccessWithFullDto()
    {
        var command = new CreateAccountCommand("Cash wallet", "Cash", 123.45m, "a note");

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Cash wallet");
        result.Value.Type.Should().Be("Cash");
        result.Value.OpeningBalance.Should().Be(123.45m);
        result.Value.Note.Should().Be("a note");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("cash", "Cash")]
    [InlineData("CARD", "Card")]
    [InlineData("savings", "Savings")]
    [InlineData("SaViNgS", "Savings")]
    // Guards the accept side of the type match. Without this, tightening the comparison to
    // case-sensitive would still leave every other test green.
    public async Task ExecuteAsync_TypeInAnyLetterCase_ReturnsSuccessWithCanonicalType(string sent, string expected)
    {
        var command = new CreateAccountCommand($"Account {sent}", sent, 0m, null);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteAsync_BadTypeAndBadName_ReturnsBothErrorsAtOnce()
    {
        // The type check used to return early, so the caller fixed the type, resubmitted, and only
        // then learned the name was missing.
        var command = new CreateAccountCommand("   ", "Bitcoin", 0m, null);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().ContainKey("type");
        result.Error.FieldErrors.Should().ContainKey("name");
    }

    [Fact]
    public async Task ExecuteAsync_BadTypeAndOverlongNote_ReturnsBothErrorsAtOnce()
    {
        var command = new CreateAccountCommand("Cash", "Bitcoin", 0m, new string('n', 501));

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.Error.FieldErrors.Should().ContainKey("type");
        result.Error.FieldErrors.Should().ContainKey("note");
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateNameIgnoringCaseAndSpaces_ReturnsConflict()
    {
        await _useCase.ExecuteAsync(new CreateAccountCommand("Cash", "Cash", 0m, null), CancellationToken.None);

        var result = await _useCase.ExecuteAsync(new CreateAccountCommand(" cash ", "Cash", 0m, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bitcoin")]
    [InlineData("0")]
    [InlineData("5")]
    // Enum.TryParse would accept all five of these. Only the three literal names are valid.
    [InlineData(" 0 ")]
    [InlineData("0 ")]
    [InlineData(" 1")]
    [InlineData("+0")]
    [InlineData("Cash,Card")]
    public async Task ExecuteAsync_InvalidType_ReturnsValidationWithTypeField(string? type)
    {
        var command = new CreateAccountCommand("Cash", type, 0m, null);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().ContainKey("type");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_InvalidName_ReturnsValidationWithNameField(string? name)
    {
        var command = new CreateAccountCommand(name, "Cash", 0m, null);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().ContainKey("name");
    }

    [Fact]
    public async Task ExecuteAsync_OverLongNote_ReturnsValidationWithNoteField()
    {
        var command = new CreateAccountCommand("Cash", "Cash", 0m, new string('n', 501));

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().ContainKey("note");
    }

    [Fact]
    public async Task ExecuteAsync_ValidationFails_NothingIsSaved()
    {
        var command = new CreateAccountCommand(null, "Cash", 0m, null);

        await _useCase.ExecuteAsync(command, CancellationToken.None);

        _repository.Accounts.Should().BeEmpty();
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_UnitOfWorkThrowsUniqueConstraintViolation_ReturnsConflict()
    {
        _unitOfWork.ThrowUniqueConstraintViolationOnNextSave();
        var command = new CreateAccountCommand("Cash", "Cash", 0m, null);

        var result = await _useCase.ExecuteAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
