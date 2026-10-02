using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Creates instances of <see cref="IServiceScope"/>, which is used to create
/// services within a scope.
/// </summary>
public interface IServiceScopeFactory
{
    /// <summary>
    /// Create an <see cref="IServiceScope"/> that contains an <see cref="IServiceProvider"/>
    /// used to resolve dependencies from a newly created scope.
    /// </summary>
    /// <returns>
    /// An <see cref="IServiceScope"/> controlling the lifetime of the scope. Once this is
    /// disposed, services owned by that scope will also be disposed according to its
    /// implementation's ownership rules.
    /// </returns>
    IServiceScope CreateScope();
}
