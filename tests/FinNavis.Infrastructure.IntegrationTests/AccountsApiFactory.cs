using FinNavis.Domain.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinNavis.Infrastructure.IntegrationTests;

/// <summary>
/// Boots the real API against a given PostgreSQL connection string, with the clock swapped for
/// a fake a test can control.
/// </summary>
public sealed class AccountsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly IClock _clock;

    public AccountsApiFactory(string connectionString, IClock clock)
    {
        _connectionString = connectionString;
        _clock = clock;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:FinNavisDb", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton(_clock);
        });
    }
}
