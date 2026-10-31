namespace FinNavis.Domain.Entities;

public sealed class ImportCreatedCategoryLink
{
    private ImportCreatedCategoryLink(
        Guid id,
        Guid importRunId,
        Guid categoryId,
        DateTimeOffset createdAt)
    {
        Id = id;
        ImportRunId = importRunId;
        CategoryId = categoryId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ImportRunId { get; private set; }

    public Guid CategoryId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static ImportCreatedCategoryLink For(
        Guid id,
        Guid importRunId,
        Category category,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new ImportCreatedCategoryLink(id, importRunId, category.Id, createdAt);
    }
}
