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
        (string shapeKey, IReadOnlyList<string> sourceCategories) =
            await ReadShapeAndCategoriesAsync(statement, kind, cancellationToken);
        MappingProfile? profile = await mappingProfiles.GetByShapeKeyAsync(shapeKey, cancellationToken);

        return new FileInspection(shapeKey, profile?.Id, sourceCategories);
    }

    // The reader throws InvalidDataException when the file turns out not to be a statement, for
    // example when it goes over the row ceiling. That is the owner handing over the wrong file, not a
    // fault, so it is refused like any other unusable file.
    private async Task<(string ShapeKey, IReadOnlyList<string> SourceCategories)> ReadShapeAndCategoriesAsync(
        Stream statement,
        ImportFileKind kind,
        CancellationToken cancellationToken)
    {
        try
        {
            StatementHeader header = await statementReader.ReadHeaderAsync(statement, kind, cancellationToken);
            RequireDateAndAmount(header);

            IReadOnlyList<string> sourceCategories =
                await ReadSourceCategoriesAsync(statement, kind, header, cancellationToken);

            return (header.ShapeKey, sourceCategories);
        }
        catch (InvalidDataException readFailure)
        {
            throw ImportErrors.FileNotReadable(readFailure);
        }
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
