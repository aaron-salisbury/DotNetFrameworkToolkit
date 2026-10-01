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
    /// disposed, any scoped services and any transient services that have been resolved from 
    /// the <see cref="IServiceProvider"/> will also be disposed.
    /// </returns>
    IServiceScope CreateScope();
}
