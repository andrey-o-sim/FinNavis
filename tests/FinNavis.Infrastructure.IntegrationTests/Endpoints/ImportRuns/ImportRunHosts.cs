using FinNavis.Infrastructure.Import;
using FinNavis.Infrastructure.IntegrationTests.Errors;
using FinNavis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

internal static class ImportRunHosts
{
    // The production reader has no row ceiling. A ceiling of one row makes any file with two rows a
    // file that is not a statement, which is how the reader reports the wrong file.
    public static WebApplicationFactory<Program> WithRowCeilingOfOne(this ErrorTestHost host) =>
        host.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton(new CsvStatementReaderOptions(RowCeiling: 1))));

    // One statement per command makes the load several commands, so EF Core runs them in one
    // transaction and the fault lands after an insert has already been sent.
    public static WebApplicationFactory<Program> WithCommitFailingPartWay(this ErrorTestHost host, PostgresFixture database) =>
        host.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.ConfigureDbContext<FinNavisDbContext>(options => options
                .UseNpgsql(database.ConnectionString, npgsql => npgsql.MaxBatchSize(1))
                .AddInterceptors(new FailingInsertInterceptor()))));
}
