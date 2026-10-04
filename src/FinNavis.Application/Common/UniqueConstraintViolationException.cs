namespace FinNavis.Application.Common;

/// <summary>
/// Thrown by <c>IUnitOfWork.SaveChangesAsync</c> when the store rejects a duplicate that slipped
/// past the use case's own check — the race path. Infrastructure translates the database-specific
/// exception into this one, so Application never learns about PostgreSQL or EF Core.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string constraintName, Exception innerException)
        : base($"Unique constraint '{constraintName}' was violated.", innerException)
    {
        ConstraintName = constraintName;
    }

    /// <summary>The name of the violated database constraint, for diagnostics.</summary>
    public string ConstraintName { get; }
}
