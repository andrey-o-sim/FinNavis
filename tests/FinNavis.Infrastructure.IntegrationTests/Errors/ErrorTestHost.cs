using FinNavis.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.IntegrationTests.Errors;

internal sealed class ErrorTestHost(PostgresFixture database, string environment) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting(
            $"ConnectionStrings:{InfrastructureServiceCollectionExtensions.ConnectionStringName}",
            database.ConnectionString);
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ProbeRoutes>());
    }
}
