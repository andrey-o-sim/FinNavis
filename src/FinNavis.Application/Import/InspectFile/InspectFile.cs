using FinNavis.Application.Abstractions;
using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.Abstractions;
using FinNavis.Domain.Entities;

namespace FinNavis.Application.Import.InspectFile;

public sealed class InspectFile(IStatementReader statementReader, IMappingProfileRepository mappingProfiles)
{
    // A statement covers the one account the owner names when handing it over, so only the date and
    // the amount are required of every file at this step.
    private static readonly Dictionary<StatementColumn, string> RequiredColumnNames = new()
    {
        [StatementColumn.Date] = "date",
        [StatementColumn.Amount] = "amount",
    };

    public async Task<FileInspection> ExecuteAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        StatementHeader header = await statementReader.ReadHeaderAsync(statement, kind, cancellationToken);
        RequireDateAndAmount(header);

        IReadOnlyList<string> sourceCategories =
            await ReadSourceCategoriesAsync(statement, kind, header, cancellationToken);
        MappingProfile? profile = await mappingProfiles.GetByShapeKeyAsync(header.ShapeKey, cancellationToken);

        return new FileInspection(header.ShapeKey, profile?.Id, sourceCategories);
    }

    private static void RequireDateAndAmount(StatementHeader header)
    {
        List<string> missingColumnNames =
        [
            .. RequiredColumnNames
                .Where(required => header.MissingColumns.Contains(required.Key))
                .Select(required => required.Value),
        ];

        if (missingColumnNames.Count > 0)
        {
            throw ImportErrors.MissingRequiredColumn(missingColumnNames);
        }
    }

    private async Task<IReadOnlyList<string>> ReadSourceCategoriesAsync(
        Stream statement,
        ImportFileKind kind,
        StatementHeader header,
        CancellationToken cancellationToken)
    {
        if (!header.ColumnPositions.TryGetValue(StatementColumn.Category, out int categoryIndex))
        {
            return [];
        }

        List<string> sourceCategories = [];

        await foreach (StatementRow row in statementReader.ReadRowsAsync(statement, kind, cancellationToken))
        {
            bool rowHasCategory = categoryIndex < row.Values.Count;

            if (rowHasCategory && !sourceCategories.Contains(row.Values[categoryIndex], StringComparer.Ordinal))
            {
                sourceCategories.Add(row.Values[categoryIndex]);
            }
        }

        return sourceCategories;
    }
}
