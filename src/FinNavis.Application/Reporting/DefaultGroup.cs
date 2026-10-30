namespace FinNavis.Application.Reporting;

/// <summary>
/// The bucket the breakdown reports every category belonging to no group under.
/// </summary>
/// <remarks>
/// <b>It is not a row, an entity or an id</b> (sad.md §12, <c>data-model.md</c> decision 8). It
/// cannot be renamed, deleted or picked when filing a transaction, and no category is ever
/// assigned to it — a category reaches it by having no group at all. An implementer who creates a
/// real <c>Default</c> row has misread AC-29.
/// <para>
/// The name is here rather than inline so the report and anything reading it cannot disagree on
/// the one string the owner sees.
/// </para>
/// </remarks>
public static class DefaultGroup
{
    /// <summary>What the report calls the bucket. Fixed by the contract.</summary>
    public const string Name = "Default group";
}
