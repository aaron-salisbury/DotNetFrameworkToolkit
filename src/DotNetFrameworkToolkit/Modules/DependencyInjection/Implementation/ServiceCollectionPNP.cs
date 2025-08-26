using Microsoft.Practices.Unity;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Specifies the contract for a collection of service descriptors.
/// </summary>
/// <remarks>
/// This implementation uses the Patterns & Practices Enterprise Library.
/// </remarks>
public class ServiceCollectionPNP : IServiceCollection
{
    private readonly object _syncRoot = new();
    private readonly List<ServiceDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCollectionPNP"/> class.
    /// </summary>
    public ServiceCollectionPNP()
    {
        _descriptors = [];
    }

    /// <inheritdoc/>
    public IServiceCollection Add(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        Add(new ServiceDescriptor()
        {
            Lifetime = lifetime,
            ServiceType = serviceType,
            ImplementationType = implementationType,
            ImplementationInstance = null
        });

        return this;
    }

    /// <inheritdoc/>
    public IServiceCollection AddInstance(Type serviceType, object implementationInstance, ServiceLifetime lifetime)
    {
        Add(new ServiceDescriptor()
        {
            Lifetime = lifetime,
            ServiceType = serviceType,
            ImplementationType = null,
            ImplementationInstance = implementationInstance
        });

        return this;
    }

    /// <inheritdoc/>
    public IServiceProvider BuildServiceProvider()
    {
        lock (_syncRoot)
        {
            IUnityContainer container = new UnityContainer();

            foreach (ServiceDescriptor descriptor in _descriptors)
            {
                if (descriptor.ImplementationInstance != null)
                {
                    container.RegisterInstance(descriptor.ServiceType, descriptor.ImplementationInstance);
                }
                else
                {
                    container.RegisterType(descriptor.ServiceType, descriptor.ImplementationType, LifetimeManagerForServiceLifetime(descriptor.Lifetime));
                }
            }

            return new ServiceProviderPNP(container);
        }
    }

    #region Explicit Interface Implementation of Generic Overloads
    //----------------------------------------Transient----------------------------------------//

    /// <inheritdoc/>
    public IServiceCollection AddTransient(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return Add(serviceType, implementationType, ServiceLifetime.Transient);
    }

    /// <inheritdoc/>
    public IServiceCollection AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        return AddTransient(typeof(TService), typeof(TImplementation));
    }

    /// <inheritdoc/>
    public IServiceCollection AddTransient(Type serviceType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddTransient(serviceType, serviceType);
    }

    /// <inheritdoc/>
    public IServiceCollection AddTransient<TService>() where TService : class
    {
        return AddTransient(typeof(TService));
    }

    //-----------------------------------------Scoped------------------------------------------//

    /// <inheritdoc/>
    public IServiceCollection AddScoped(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return Add(serviceType, implementationType, ServiceLifetime.Scoped);
    }

    /// <inheritdoc/>
    public IServiceCollection AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        return AddScoped(typeof(TService), typeof(TImplementation));
    }

    /// <inheritdoc/>
    public IServiceCollection AddScoped(Type serviceType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddScoped(serviceType, serviceType);
    }

    /// <inheritdoc/>
    public IServiceCollection AddScoped<TService>() where TService : class
    {
        return AddScoped(typeof(TService));
    }

    //----------------------------------------Singleton----------------------------------------//

    /// <inheritdoc/>
    public IServiceCollection AddSingleton(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationType == null)
        {
            throw new ArgumentNullException(nameof(implementationType));
        }

        return Add(serviceType, implementationType, ServiceLifetime.Singleton);
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        return AddSingleton(typeof(TService), typeof(TImplementation));
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton(Type serviceType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return AddSingleton(serviceType, serviceType);
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton<TService>() where TService : class
    {
        return AddSingleton(typeof(TService));
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton(Type serviceType, object implementationInstance)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (implementationInstance == null)
        {
            throw new ArgumentNullException(nameof(implementationInstance));
        }

        return AddInstance(serviceType, implementationInstance, ServiceLifetime.Singleton);
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton<TService>(TService implementationInstance) where TService : class
    {
        if (implementationInstance == null)
        {
            throw new ArgumentNullException(nameof(implementationInstance));
        }

        return AddSingleton(typeof(TService), implementationInstance);
    }
    #endregion

    #region IList Members
    /// <summary>
    /// Gets the number of service descriptors contained in the collection.
    /// </summary>
    public int Count => _descriptors.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the <see cref="ServiceDescriptor"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The service descriptor at the specified index.</returns>
    public ServiceDescriptor this[int index]
    {
        get
        {
            return _descriptors[index];
        }
        set
        {
            _descriptors[index] = value;
        }
    }

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    /// <param name="item">The service descriptor to locate.</param>
    /// <returns>The index of the item if found; otherwise, -1.</returns>
    public int IndexOf(ServiceDescriptor item)
    {
        return _descriptors.IndexOf(item);
    }

    /// <summary>
    /// Inserts a service descriptor at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The service descriptor to insert.</param>
    public void Insert(int index, ServiceDescriptor item)
    {
        lock (_syncRoot)
        {
            // Subsequent attempts to add the same type replaces the previous addition.
            // Could possibly enhance by letting more than one of a type in the collection, 
            // but would need to keep track of names. Would then need to update the
            // equality overrides of ServiceDescriptor as well.
            int existingIndex = IndexOf(item);
            if (existingIndex >= 0)
            {
                RemoveAt(existingIndex);

                if (existingIndex < index)
                {
                    --index;
                }
            }

            if (index < 0 || index > _descriptors.Count - 1)
            {
                _descriptors.Add(item);
            }
            else
            {
                _descriptors.Insert(index, item);
            }
        }
    }

    /// <summary>
    /// Removes the service descriptor at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        lock (_syncRoot)
        {
            _descriptors.RemoveAt(index);
        }
    }

    /// <summary>
    /// Adds a service descriptor to the end of the collection.
    /// </summary>
    /// <param name="item">The service descriptor to add.</param>
    public void Add(ServiceDescriptor item)
    {
        lock (_syncRoot)
        {
            Insert(_descriptors.Count, item);
        }
    }

    /// <summary>
    /// Removes all service descriptors from the collection.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            _descriptors.Clear();
        }
    }

    /// <summary>
    /// Determines whether the collection contains a specific service descriptor.
    /// </summary>
    /// <param name="item">The service descriptor to locate.</param>
    /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
    public bool Contains(ServiceDescriptor item)
    {
        return _descriptors.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        _descriptors.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Removes the first occurrence of a specific service descriptor from the collection.
    /// </summary>
    /// <param name="item">The service descriptor to remove.</param>
    /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(ServiceDescriptor item)
    {
        lock (_syncRoot)
        {
            return _descriptors.Remove(item);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<ServiceDescriptor> GetEnumerator()
    {
        return _descriptors.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    private static LifetimeManager LifetimeManagerForServiceLifetime(ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Transient => new TransientLifetimeManager(),
            ServiceLifetime.Scoped => new PerThreadLifetimeManager(),
            ServiceLifetime.Singleton => new ContainerControlledLifetimeManager(),
            _ => new TransientLifetimeManager(),
        };
    }
}
