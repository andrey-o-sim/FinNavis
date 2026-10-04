using FinNavis.Application.Features.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Application.DependencyInjection;

/// <summary>
/// Registers Application's own services. Infrastructure registers its own; see
/// <c>InfrastructureServiceCollectionExtensions</c>.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAccountUseCase>();
        services.AddScoped<ListAccountsUseCase>();
        services.AddScoped<GetAccountByIdUseCase>();

        return services;
    }
}
