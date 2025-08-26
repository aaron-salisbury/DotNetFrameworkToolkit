using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Specifies the contract for a collection of service descriptors.
/// </summary>
public interface IServiceCollection : IList<ServiceDescriptor>
{
    /// <summary>
    /// For the given lifetime, adds a service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <param name="lifetime">The lifetime of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection Add(Type serviceType, Type implementationType, ServiceLifetime lifetime);

    /// <summary>
    /// For the given lifetime and instance specified, adds a service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <param name="lifetime">The lifetime of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddInstance(Type serviceType, object implementationInstance, ServiceLifetime lifetime);

    /// <summary>
    /// Creates a <see cref="IServiceProvider"/> containing services from the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> containing services.</param>
    /// <returns>The <see cref="IServiceProvider"/>.</returns>
    IServiceProvider BuildServiceProvider();

    #region Generic Overloads
    //----------------------------------------Transient----------------------------------------//

    /// <summary>
    /// Adds a transient service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddTransient(Type serviceType, Type implementationType);

    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds a transient service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddTransient(Type serviceType);

    /// <summary>
    /// Adds a transient service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddTransient<TService>() where TService : class;

    //-----------------------------------------Scoped------------------------------------------//

    /// <summary>
    /// Adds a scoped service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddScoped(Type serviceType, Type implementationType);

    /// <summary>
    /// Adds a scoped service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds a scoped service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddScoped(Type serviceType);

    /// <summary>
    /// Adds a scoped service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddScoped<TService>() where TService : class;

    //----------------------------------------Singleton----------------------------------------//

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/> with an
    /// implementation of the type specified in <paramref name="implementationType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton(Type serviceType, Type implementationType);

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService"/> with an
    /// implementation type specified in <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService;

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register and the implementation to use.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton(Type serviceType);

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton<TService>() where TService : class;

    /// <summary>
    /// Adds a singleton service of the type specified in <paramref name="serviceType"/> with an
    /// instance specified in <paramref name="implementationInstance"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton(Type serviceType, object implementationInstance);

    /// <summary>
    /// Adds a singleton service of the type specified in <typeparamref name="TService" /> with an
    /// instance specified in <paramref name="implementationInstance"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    IServiceCollection AddSingleton<TService>(TService implementationInstance) where TService : class;
    #endregion
}
