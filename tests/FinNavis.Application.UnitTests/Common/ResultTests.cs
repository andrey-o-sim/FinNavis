using FinNavis.Application.Common;

namespace FinNavis.Application.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_CarriesTheValueAndNoErrors()
    {
        var result = Result<string>.Success("ok");

        result.Kind.Should().Be(ResultKind.Success);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_WithOneField_CarriesThatFieldAndMessage()
    {
        var result = Result<string>.Invalid("name", "Name is required.");

        result.Kind.Should().Be(ResultKind.Invalid);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainKey("name")
            .WhoseValue.Should().Equal("Name is required.");
    }

    [Fact]
    public void Invalid_WithSeveralFields_KeepsEveryEntry()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["name"] = ["Name is required."],
            ["type"] = ["Unknown type."],
        };

        var result = Result<string>.Invalid(errors);

        result.Errors.Keys.Should().BeEquivalentTo(["name", "type"]);
    }

    [Fact]
    public void Invalid_WithNoErrors_Throws()
    {
        var create = () => Result<string>.Invalid(new Dictionary<string, string[]>());

        create.Should().Throw<ArgumentException>().WithParameterName("errors");
    }

    [Fact]
    public void NotFound_HasNoValueAndNoErrors()
    {
        var result = Result<string>.NotFound();

        result.Kind.Should().Be(ResultKind.NotFound);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ResultKind.Invalid)]
    [InlineData(ResultKind.NotFound)]
    public void Value_OnAFailedResult_Throws(ResultKind kind)
    {
        var result = kind == ResultKind.Invalid
            ? Result<string>.Invalid("name", "Name is required.")
            : Result<string>.NotFound();

        var readValue = () => result.Value;

        readValue.Should().Throw<InvalidOperationException>();
    }
}
