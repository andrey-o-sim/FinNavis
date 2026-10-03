namespace FinNavis.Application.Abstractions;

/// <summary>
/// Commits everything a use case staged, as one unit. The other side is a database
/// transaction, so this is a port.
/// </summary>
/// <remarks>
/// It lives in Application, not Domain: committing a set of changes is an orchestration
/// concern, not a domain rule. Repositories stage; this commits.
/// </remarks>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
