using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Defines a disposable service scope.
/// </summary>
/// <remarks>
/// The <see cref="IDisposable.Dispose"/> method ends the scope lifetime. Once Dispose
/// is called, services owned by the scope are disposed. ServiceCollectionPNP-based scopes
/// track their created scoped and transient disposables. Raw Unity wrappers retain native
/// container ownership rules and do not automatically track native transients.
/// </remarks>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve dependencies from the scope.
    /// </summary>
    IServiceProvider ServiceProvider
    {
        get;
    }
}
