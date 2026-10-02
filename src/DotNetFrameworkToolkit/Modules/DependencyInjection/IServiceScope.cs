using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Defines a disposable service scope.
/// </summary>
/// <remarks>
/// The <see cref="IDisposable.Dispose"/> method ends the scope lifetime. Once Dispose
/// is called, any scoped services and any transient services that have been resolved from
/// <see cref="IServiceProvider"/> will be disposed.
/// </remarks>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve dependencies from the scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
