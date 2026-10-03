using FinNavis.Application.Features.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Application.DependencyInjection;

/// <summary>
/// Registers everything Application owns. Presentation calls this; it does not list use
/// cases one by one.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped, like the DbContext and the repositories they depend on.
        services.AddScoped<CreateAccount>();
        services.AddScoped<ListAccounts>();
        services.AddScoped<GetAccountById>();

        return services;
    }
}
