using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/> to simplify service retrieval.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Get service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
    public static T GetService<T>(IServiceProvider serviceProvider)
    {
        return (T)serviceProvider.GetService(typeof(T));
    }

    /// <summary>
    /// Get service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
    public static T GetRequiredService<T>(IServiceProvider serviceProvider) where T : notnull
    {
        T service = GetService<T>(serviceProvider);

        return service == null ? throw new InvalidOperationException($"There is no service of type {typeof(T)}") : service;
    }
}
