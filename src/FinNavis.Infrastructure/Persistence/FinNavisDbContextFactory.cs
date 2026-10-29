using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinNavis.Infrastructure.Persistence;

/// <summary>
/// Builds the context for <c>dotnet ef</c> only. The running API gets its context from DI.
/// </summary>
/// <remarks>
/// Without this, <c>dotnet ef</c> would have to resolve the context out of the API's service
/// collection, which is not wired yet — that registration is its own task.
/// </remarks>
public sealed class FinNavisDbContextFactory : IDesignTimeDbContextFactory<FinNavisDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__FinNavisDb";

    // `migrations add` needs the provider, never a live server, so a missing variable must not
    // stop it. `database update` then fails naming this host, which is the intent: an unset
    // variable should be visible, not silently resolved to some default database.
    private const string NoServerConnectionString = "Host=connection-string-is-unset.invalid;Database=finnavis";

    public FinNavisDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        var options = new DbContextOptionsBuilder<FinNavisDbContext>()
            .UseNpgsql(string.IsNullOrWhiteSpace(connectionString) ? NoServerConnectionString : connectionString)
            .Options;

        return new FinNavisDbContext(options);
    }
}
