using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Repositories;
using FinNavis.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FinNavisDb");

        // Fail at startup, not on the first request. appsettings.json ships an empty connection
        // string, so without this a developer who skipped the launchSettings.json copy step gets
        // an API that starts cleanly and then answers 500 to everything, with nothing naming the
        // real cause. Design-time commands are exempt: `dotnet ef migrations add` never connects,
        // and it has to work on a clean clone.
        if (string.IsNullOrWhiteSpace(connectionString) && !IsDesignTime())
        {
            throw new InvalidOperationException(
                "Connection string 'FinNavisDb' is not set. Start the local database with " +
                "`docker compose up -d`, then copy " +
                "src/FinNavis.Presentation/Properties/launchSettings.example.json to " +
                "launchSettings.json and fill it in. See the \"Local database\" section in CLAUDE.md.");
        }

        services.AddDbContext<FinNavisDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Design time only, guaranteed by the guard above. `migrations add` builds the
                // model without ever opening a connection.
                options.UseNpgsql();
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    /// <summary>
    /// True when the host was built by a `dotnet ef` command rather than by the API itself.
    /// The EF tooling loads its design assembly into the process before it builds the host; the
    /// running API never loads it. There is no supported API for this question, so the assembly
    /// is the signal.
    /// </summary>
    private static bool IsDesignTime()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => assembly.GetName().Name == "Microsoft.EntityFrameworkCore.Design");
    }
}
