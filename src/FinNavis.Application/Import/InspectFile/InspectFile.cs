using FinNavis.Application.Abstractions;
using FinNavis.Domain.Abstractions;

namespace FinNavis.Application.Import.InspectFile;

public sealed class InspectFile
{
    public InspectFile(IStatementReader statementReader, IMappingProfileRepository mappingProfiles)
    {
        throw new NotImplementedException();
    }

    public Task<FileInspection> ExecuteAsync(Stream statement, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
