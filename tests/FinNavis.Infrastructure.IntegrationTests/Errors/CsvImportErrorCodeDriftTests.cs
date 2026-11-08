using System.Text.RegularExpressions;
using FinNavis.Presentation.Errors;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

public sealed class CsvImportErrorCodeDriftTests
{
    private const string ContractPath = "docs/features/csv-import/contracts/openapi.yaml";

    private const string CodePattern = @"^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$";

    // Account.NotFound and Category.NotFound are in this contract too, but core-ledger raises and
    // owns them, so only these two modules belong to csv-import alone.
    private static readonly string[] CsvImportModules = ["Import", "MappingProfile"];

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<int>> ContractStatusesByCode =
        ContractErrorCodes.StatusesByCode(RepositoryRoot.Combine(ContractPath));

    [Fact]
    public void Contract_EveryErrorCode_MatchesTheCodePattern()
    {
        ContractStatusesByCode.Keys.Should().NotBeEmpty()
            .And.AllSatisfy(code => code.Should().MatchRegex(CodePattern));
    }

    [Fact]
    public void Contract_EveryErrorCode_IsListedUnderOneStatus()
    {
        ContractStatusesByCode.Should().AllSatisfy(entry =>
            entry.Value.Should().ContainSingle($"{entry.Key} must answer one status wherever it appears"));
    }

    [Fact]
    public void Contract_ErrorCodes_AreTheThirteenOfTheFeature()
    {
        ContractStatusesByCode.Keys.Should().BeEquivalentTo(
        [
            "Import.MissingRequiredColumn",
            "Import.FileNotReadable",
            "Import.InvalidPaging",
            "Import.NotFound",
            "Import.MappingProfileNotConfirmed",
            "Import.MovementsChangedByHand",
            "Import.AlreadyUndone",
            "Import.NotRecorded",
            "MappingProfile.InvalidRule",
            "MappingProfile.ShapeKeyInvalid",
            "MappingProfile.DuplicateSourceCategory",
            "Account.NotFound",
            "Category.NotFound",
        ]);
    }

    [Fact]
    public void RaisedCodes_ComparedWithTheContract_RaiseEveryContractCode()
    {
        SourceErrorCodes.Raised().Should().Contain(
            ContractStatusesByCode.Keys,
            "a code the contract advertises must be raised somewhere in src");
    }

    [Fact]
    public void RaisedCodes_ComparedWithTheContract_HaveTheSameCsvImportCodes()
    {
        IEnumerable<string> raised = SourceErrorCodes.Raised().Where(IsCsvImport);

        raised.Should().BeEquivalentTo(
            ContractStatusesByCode.Keys.Where(IsCsvImport),
            "a code src raises must be advertised by the contract, and a code the contract advertises must be raised");
    }

    [Fact]
    public void StatusMap_ComparedWithTheContract_GivesEveryContractCodeTheContractStatus()
    {
        IDictionary<string, int> advertised = ContractStatusesByCode
            .ToDictionary(entry => entry.Key, entry => entry.Value.Single());

        IDictionary<string, int> mapped = ErrorStatusCodes.All
            .Where(entry => advertised.ContainsKey(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        mapped.Should().BeEquivalentTo(advertised);
    }

    [Fact]
    public void StatusMap_ComparedWithTheContract_MapsNoCsvImportCodeTheContractLacks()
    {
        IEnumerable<string> mapped = ErrorStatusCodes.All.Keys.Where(IsCsvImport);

        mapped.Should().BeEquivalentTo(ContractStatusesByCode.Keys.Where(IsCsvImport));
    }

    private static bool IsCsvImport(string code) =>
        Regex.IsMatch(code, CodePattern) && CsvImportModules.Contains(code[..code.IndexOf('.')]);
}
