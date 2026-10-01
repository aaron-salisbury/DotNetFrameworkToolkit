using Microsoft.Practices.Unity.Utility;
using System;
using System.Threading;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// A type that facilitates the use of the <see cref="IServiceProvider"/> type.
/// The <see cref="Ioc"/> provides the ability to configure services in a singleton, thread-safe
/// service provider instance, which can then be used to resolve service instances.
/// </summary>
/// <remarks>
/// Modeled after the CommunityToolkit offering 
/// <see href="https://github.com/CommunityToolkit/dotnet/blob/main/src/CommunityToolkit.Mvvm/DependencyInjection/Ioc.cs">here</see>.
/// </remarks>
public sealed class Ioc : IServiceProvider, IDisposable
{
    /// <summary>
    /// Gets the default <see cref="Ioc"/> instance.
    /// </summary>
    public static Ioc Default { get; } = new();

    private volatile IServiceProvider serviceProvider;
    private readonly Core.OperationLifetime lifetime = new();

    /// <summary>
    /// Gets the service object of the specified type from the service container.
    /// </summary>
    /// <param name="serviceType">The type of service object to get.</param>
    /// <returns>
    /// A service object of type <paramref name="serviceType"/>. 
    /// Returns <c>null</c> if the service is not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the service provider has not been configured.</exception>
    public object GetService(Type serviceType)
    {
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));

        using (lifetime.Enter())
        {
            IServiceProvider provider = serviceProvider;
            if (provider == null) ThrowInvalidOperationExceptionForMissingInitialization();
            return provider.GetService(serviceType);
        }
    }

    /// <summary>
    /// Get service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T"/> or null if there is no such service.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service provider has not been configured.</exception>
    public T GetService<T>()
    {
        return (T)GetService(typeof(T));
    }

    /// <summary>
    /// Get service of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the current <see cref="Ioc"/> instance has not been initialized, or if the
    /// requested service type was not registered in the service provider currently in use.
    /// </exception>
    public T GetRequiredService<T>() where T : notnull
    {
        T service = GetService<T>();

        if (service is null)
        {
            ThrowInvalidOperationExceptionForUnregisteredType();
        }

        return service;
    }

    /// <summary>
    /// Initializes the shared <see cref="IServiceProvider"/> instance.
    /// </summary>
    /// <param name="serviceProvider">The input <see cref="IServiceProvider"/> instance to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    public void ConfigureServices(IServiceProvider serviceProvider)
    {
        Guard.ArgumentNotNull(serviceProvider, nameof(serviceProvider));

        using IDisposable operation = lifetime.Enter();
        IServiceProvider oldServices = Interlocked.CompareExchange(ref this.serviceProvider, serviceProvider, null);

        if (oldServices is not null)
        {
            ThrowInvalidOperationExceptionForRepeatedConfiguration();
        }
    }

    /// <summary>
    /// Disposes the configured <see cref="IServiceProvider"/> instance when it is disposable,
    /// then clears all internal references.
    /// </summary>
    public void Dispose()
    {
        lifetime.Dispose(() =>
        {
            IServiceProvider provider = Interlocked.Exchange(ref serviceProvider, null);
            if (provider is IDisposable disposable) disposable.Dispose();
        });
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the <see cref="IServiceProvider"/> property is used before initialization.
    /// </summary>
    private static void ThrowInvalidOperationExceptionForMissingInitialization()
    {
        throw new InvalidOperationException("The service provider has not been configured yet.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the <see cref="IServiceProvider"/> property is missing a type registration.
    /// </summary>
    private static void ThrowInvalidOperationExceptionForUnregisteredType()
    {
        throw new InvalidOperationException("The requested service type was not registered.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when a configuration is attempted more than once.
    /// </summary>
    private static void ThrowInvalidOperationExceptionForRepeatedConfiguration()
    {
        throw new InvalidOperationException("The default service provider has already been configured.");
    }
}
