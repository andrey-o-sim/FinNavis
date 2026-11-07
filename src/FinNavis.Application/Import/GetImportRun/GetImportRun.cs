using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Import.GetImportRun;

public sealed class GetImportRun(IImportRunRepository importRuns)
{
    public Task<ImportRunReport> ExecuteAsync(Guid importRunId, CancellationToken cancellationToken)
    {
        _ = importRuns;
        throw new NotImplementedException();
    }
}
