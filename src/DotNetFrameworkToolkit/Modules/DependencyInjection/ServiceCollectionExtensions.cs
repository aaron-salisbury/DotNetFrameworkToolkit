using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register services with various lifetimes,
/// such as transient, scoped, and singleton.
/// </summary>
public static class ServiceCollectionExtensions
{
    //----------------------------------------Transient----------------------------------------//

    /// <summary>
    /// Adds a transient service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddTransient(IServiceCollection services, Type serviceType, Type implementationType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return services.Add(serviceType, implementationType, ServiceLifetime.Transient);
    }

    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddTransient<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddTransient(services, typeof(TService), typeof(TImplementation));
    }

    /// <summary>
    /// Adds a transient service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddTransient(IServiceCollection services, Type serviceType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddTransient(services, serviceType, serviceType);
    }

    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddTransient<TService>(IServiceCollection services)
        where TService : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddTransient(services, typeof(TService));
    }

    //-----------------------------------------Scoped------------------------------------------//

    /// <summary>
    /// Adds a scoped service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddScoped(IServiceCollection services, Type serviceType, Type implementationType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return services.Add(serviceType, implementationType, ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Adds a scoped service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddScoped<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddScoped(services, typeof(TService), typeof(TImplementation));
    }

    /// <summary>
    /// Adds a scoped service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddScoped(IServiceCollection services, Type serviceType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddScoped(services, serviceType, serviceType);
    }

    /// <summary>
    /// Adds a scoped service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddScoped<TService>(IServiceCollection services)
        where TService : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddScoped(services, typeof(TService));
    }

    //----------------------------------------Singleton----------------------------------------//

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton(IServiceCollection services, Type serviceType, Type implementationType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return services.Add(serviceType, implementationType, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddSingleton(services, typeof(TService), typeof(TImplementation));
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton(IServiceCollection services, Type serviceType)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddSingleton(services, serviceType, serviceType);
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton<TService>(IServiceCollection services)
        where TService : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return AddSingleton(services, typeof(TService));
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/> with an
    /// instance specified in <paramref name="implementationInstance"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton(IServiceCollection services, Type serviceType, object implementationInstance)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationInstance == null)
        {
            throw new ArgumentNullException(nameof(implementationInstance));
        }

        return services.AddInstance(serviceType, implementationInstance, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService" /> with an
    /// instance specified in <paramref name="implementationInstance"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSingleton<TService>(IServiceCollection services, TService implementationInstance)
        where TService : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (implementationInstance == null)
        {
            throw new ArgumentNullException(nameof(implementationInstance));
        }

        return AddSingleton(services, typeof(TService), implementationInstance);
    }
}
