using FinNavis.Application.Accounts;
using FinNavis.Application.Categories;
using FinNavis.Application.CategoryGroups;
using FinNavis.Application.Reporting;
using FinNavis.Application.Transactions;
using FinNavis.Application.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Application.DependencyInjection;

/// <summary>
/// Application registers its own services here, and Presentation calls this one method
/// (<c>CLAUDE.md</c> §Dependency injection).
/// </summary>
/// <remarks>
/// A use case is a plain class that Presentation resolves and calls directly. There is no port per
/// use case and no interface to map to, so each one is registered as itself (<c>CLAUDE.md</c>
/// §Where interfaces go).
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAccount>();
        services.AddScoped<RenameAccount>();
        services.AddScoped<DeleteAccount>();
        services.AddScoped<GetAccountBalances>();

        services.AddScoped<CreateCategoryGroup>();
        services.AddScoped<ListCategoryGroups>();
        services.AddScoped<RenameCategoryGroup>();
        services.AddScoped<DeleteCategoryGroup>();

        services.AddScoped<CreateCategory>();
        services.AddScoped<ListCategories>();
        services.AddScoped<RenameCategory>();
        services.AddScoped<MoveCategoryToGroup>();
        services.AddScoped<DeleteCategory>();

        services.AddScoped<RecordTransaction>();
        services.AddScoped<EditTransaction>();
        services.AddScoped<RemoveTransaction>();

        services.AddScoped<RecordTransfer>();
        services.AddScoped<EditTransfer>();
        services.AddScoped<RemoveTransfer>();

        services.AddScoped<GetHistory>();
        services.AddScoped<GetBreakdown>();

        return services;
    }
}
