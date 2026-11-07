using System.Reflection;
using NetArchTest.Rules;

namespace FinNavis.ArchitectureTests;

public sealed class StatementReaderBoundaryTests
{
    private const string ImportAdapterNamespace = "FinNavis.Infrastructure.Import";

    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.AssemblyMarker).Assembly;

    // The reader turns bytes into rows and nothing more. The moment it knows an account, a category
    // or a mapping rule, the rules move into Infrastructure, where they can be tested only against a
    // real file, and a second file format would need a second copy of them.
    [Fact]
    public void ImportAdapter_DependsOnNoLedgerOrMappingRuleType()
    {
        var adapterTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace(ImportAdapterNamespace)
            .GetTypes();

        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace(ImportAdapterNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FinNavis.Domain",
                "FinNavis.Infrastructure.Persistence",
                "FinNavis.Infrastructure.Repositories",
                "FinNavis.Infrastructure.Queries")
            .GetResult();

        adapterTypes.Should().NotBeEmpty("the CSV adapter lives in {0}", ImportAdapterNamespace);
        result.FailingTypeNames.Should().BeNullOrEmpty(
            "the statement reader knows nothing about the ledger or the mapping rules");
    }
}
