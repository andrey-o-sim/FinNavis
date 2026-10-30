using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;
using FinNavis.Infrastructure.Persistence;
using FinNavis.Infrastructure.Queries;
using FinNavis.Infrastructure.Repositories;
using FinNavis.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "FinNavisDb";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read here rather than inside the options callback: that callback runs the first time a
        // context is resolved, so a missing setting would surface on a request instead of at
        // startup.
        string connectionString = RequireConnectionString(configuration);

        services.AddDbContext<FinNavisDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryGroupRepository, CategoryGroupRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // The port is one interface; the SQL behind it is three unrelated statements, so the three
        // statement classes are registered alongside the adapter that fronts them.
        services.AddScoped<LedgerBalanceQueries>();
        services.AddScoped<LedgerHistoryQueries>();
        services.AddScoped<LedgerBreakdownQueries>();
        services.AddScoped<ILedgerQueries, LedgerQueries>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    private static string RequireConnectionString(IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string '{ConnectionStringName}' is not configured. "
                    + "Set ConnectionStrings__FinNavisDb in the environment, or in the git-ignored "
                    + "launchSettings.json for a local run.");
        }

        return connectionString;
    }
}
