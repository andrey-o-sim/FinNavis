using FinNavis.Application.Accounts;
using FinNavis.Application.Categories;
using FinNavis.Application.CategoryGroups;
using FinNavis.Application.Import.ConfirmMappingProfile;
using FinNavis.Application.Import.GetImportRun;
using FinNavis.Application.Import.InspectFile;
using FinNavis.Application.Import.LoadStatement;
using FinNavis.Application.Import.MigrateHistory;
using FinNavis.Application.Import.Pipeline;
using FinNavis.Application.Import.UndoImportRun;
using FinNavis.Application.Reporting;
using FinNavis.Application.Transactions;
using FinNavis.Application.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Application.DependencyInjection;

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

        services.AddScoped<InspectFile>();
        services.AddScoped<ConfirmMappingProfile>();
        services.AddScoped<GetImportRun>();
        services.AddScoped<UndoImportRun>();
        services.AddScoped<ImportPipeline>();
        services.AddScoped<LoadStatement>();
        services.AddScoped<MigrateHistory>();

        return services;
    }
}
