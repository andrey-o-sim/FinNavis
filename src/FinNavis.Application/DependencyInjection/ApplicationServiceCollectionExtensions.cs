using Microsoft.Extensions.DependencyInjection;

namespace FinNavis.Application.DependencyInjection;

/// <summary>
/// Application registers its own services here, and Presentation calls this one method
/// (<c>CLAUDE.md</c> §Dependency injection).
/// </summary>
/// <remarks>
/// A use case is a plain class that Presentation resolves and calls directly. There is no port per
/// use case and no interface to map to, so each one is registered as itself (<c>CLAUDE.md</c>
/// §Where interfaces go).
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
