using System.Text.RegularExpressions;
using FinNavis.Presentation.Errors;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

public sealed class ErrorCodeDriftTests
{
    private const string ContractPath = "docs/features/core-ledger/contracts/openapi.yaml";

    private const string CodePattern = @"^[A-Z][A-Za-z0-9]*\.[A-Z][A-Za-z0-9]*$";

    // Import and MappingProfile codes belong to csv-import and are checked against its own contract.
    private static readonly string[] CoreLedgerModules =
        ["Account", "Category", "CategoryGroup", "Transaction", "Transfer", "History", "Breakdown"];

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
    public void RaisedCodes_ComparedWithTheContract_HaveTheSameCoreLedgerCodes()
    {
        IEnumerable<string> raised = SourceErrorCodes.Raised().Where(IsCoreLedger);

        raised.Should().BeEquivalentTo(
            ContractStatusesByCode.Keys.Where(IsCoreLedger),
            "a code src raises must be advertised by the contract, and a code the contract advertises must be raised");
    }

    [Fact]
    public void StatusMap_ComparedWithTheContract_GivesEveryCoreLedgerCodeTheContractStatus()
    {
        IDictionary<string, int> mapped = ErrorStatusCodes.All
            .Where(entry => IsCoreLedger(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        IDictionary<string, int> advertised = ContractStatusesByCode
            .Where(entry => IsCoreLedger(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value.Single());

        mapped.Should().BeEquivalentTo(advertised);
    }

    [Fact]
    public void StatusMap_EveryCode_MatchesTheCodePattern()
    {
        ErrorStatusCodes.All.Keys.Should().AllSatisfy(code => code.Should().MatchRegex(CodePattern));
    }

    private static bool IsCoreLedger(string code) =>
        Regex.IsMatch(code, CodePattern) && CoreLedgerModules.Contains(code[..code.IndexOf('.')]);
}
