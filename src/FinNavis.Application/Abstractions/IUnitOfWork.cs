namespace FinNavis.Application.Abstractions;

/// <summary>
/// Commits everything the repository ports staged, in one database transaction.
/// </summary>
/// <remarks>
/// A port: the other side of it is the data store, and Infrastructure supplies the adapter
/// (sad.md §5).
/// <para>
/// <b>It lives in Application, not Domain</b> (<c>CLAUDE.md</c>). Committing a set of changes is an
/// orchestration concern — a use case decides where one unit of work ends, and no domain rule is
/// expressed in terms of a transaction.
/// </para>
/// <para>
/// Nothing a repository staged reaches PostgreSQL until this is called. A failed commit rolls back
/// every change in the unit, so a half-written movement is not representable (AC-06,
/// sad.md §6 flow 2).
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Writes every staged change in one database transaction.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
