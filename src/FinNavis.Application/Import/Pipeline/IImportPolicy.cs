using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.Pipeline;

public interface IImportPolicy
{
    ImportFileKind FileKind { get; }

    ImportRunKind RunKind { get; }

    Guid? RunAccountId { get; }

    int MatchWindowDays { get; }

    RowDecision Decide(StatementRow row);
}
