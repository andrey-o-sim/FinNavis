using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.DependencyInjection;

/// <summary>
/// Registers every adapter Infrastructure supplies: the DbContext and the ports it implements,
/// from Domain and from Application alike.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FinNavisDbContext>(options =>
        {
            // Read the connection string HERE, inside the lambda. Do not hoist it into a local
            // above this call. WebApplicationFactory applies its configuration override while
            // builder.Build() runs, which is after this method. A hoisted read captures the
            // empty string from appsettings.json and every integration test points at nothing.
            //
            // No ArgumentException.ThrowIfNullOrWhiteSpace either. `dotnet ef` runs Program up
            // to builder.Build() with no connection string set, so failing fast here would
            // break `migrate` on a clean clone. UseNpgsql("") does not validate; nothing
            // connects until a query runs.
            options.UseNpgsql(configuration.GetConnectionString("FinNavisDb"));
        });

        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
