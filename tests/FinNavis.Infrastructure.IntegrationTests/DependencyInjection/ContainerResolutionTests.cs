using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.IntegrationTests.DependencyInjection;

[Collection(DatabaseCollection.Name)]
public sealed class ContainerResolutionTests
{
    private const string DomainPortNamespace = "FinNavis.Domain.Abstractions";

    private const string ApplicationPortNamespace = "FinNavis.Application.Abstractions";

    private readonly PostgresFixture _database;

    public ContainerResolutionTests(PostgresFixture database) => _database = database;

    public static TheoryData<Type> Ports =>
    [
        .. PortsIn(typeof(Domain.AssemblyMarker).Assembly, DomainPortNamespace)
            .Concat(PortsIn(typeof(Application.AssemblyMarker).Assembly, ApplicationPortNamespace)),
    ];

    [Theory]
    [MemberData(nameof(Ports))]
    public void AddInfrastructureAndAddApplication_ForEveryDeclaredPort_ResolveAnAdapter(Type port)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService(port).Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithNoConnectionStringConfigured_SaysWhichSettingIsMissing()
    {
        IConfiguration empty = new ConfigurationBuilder().Build();

        Action register = () => new ServiceCollection().AddInfrastructure(empty);

        register.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{InfrastructureServiceCollectionExtensions.ConnectionStringName}*");
    }

    private static IEnumerable<Type> PortsIn(System.Reflection.Assembly assembly, string portNamespace) =>
        assembly.GetExportedTypes()
            .Where(type => type.IsInterface && type.Namespace == portNamespace);

    private ServiceProvider BuildProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{InfrastructureServiceCollectionExtensions.ConnectionStringName}"] =
                    _database.ConnectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }
}
