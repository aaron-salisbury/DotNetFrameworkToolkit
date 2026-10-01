using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Utility;
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
        List<ServiceDescriptor> snapshot;
        lock (_syncRoot)
        {
            snapshot = new List<ServiceDescriptor>();
            foreach (ServiceDescriptor descriptor in _descriptors)
            {
                snapshot.Add(Copy(descriptor));
            }
        }

        return new ServiceProviderPNP(new UnityContainer(), snapshot);
    }

    internal static ServiceDescriptor Copy(ServiceDescriptor item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        ServiceDescriptor copy = new()
        { 
            ServiceType = item.ServiceType, 
            ImplementationType = item.ImplementationType,
            ImplementationInstance = item.ImplementationInstance, 
            Lifetime = item.Lifetime
        };

        if (copy.ServiceType == null)
        {
            throw new ArgumentException("A service type is required.", nameof(item));
        }

        if (copy.ServiceType.ContainsGenericParameters || (copy.ImplementationType != null && copy.ImplementationType.ContainsGenericParameters))
        {
            throw new NotSupportedException("This Unity 1.2 adapter requires closed service types; register each required closed generic explicitly.");
        }

        if (!Enum.IsDefined(typeof(ServiceLifetime), copy.Lifetime))
        {
            throw new ArgumentException("Unknown lifetime.", nameof(item));
        }

        if (copy.ImplementationInstance != null)
        {
            if (copy.ImplementationType != null || !copy.ServiceType.IsInstanceOfType(copy.ImplementationInstance))
            {
                throw new ArgumentException("The instance must implement the service type.", nameof(item));
            }

            if (copy.Lifetime != ServiceLifetime.Singleton)
            {
                throw new ArgumentException("Supplied instances must be singletons and remain caller-owned.", nameof(item));
            }
        }
        else if (copy.ImplementationType == null || copy.ImplementationType.IsAbstract || copy.ImplementationType.IsInterface ||
            (!copy.ServiceType.IsGenericTypeDefinition && !copy.ServiceType.IsAssignableFrom(copy.ImplementationType)))
        {
            throw new ArgumentException("A concrete compatible implementation type is required.", nameof(item));
        }

        return copy;
    }

    #region Explicit Interface Implementation of Generic Overloads
    //----------------------------------------Transient----------------------------------------//

    /// <inheritdoc/>
    public IServiceCollection AddTransient(Type serviceType, Type implementationType)
    {
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));
        Guard.ArgumentNotNull(implementationType, nameof(implementationType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));
        Guard.ArgumentNotNull(implementationType, nameof(implementationType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));
        Guard.ArgumentNotNull(implementationType, nameof(implementationType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));

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
        Guard.ArgumentNotNull(serviceType, nameof(serviceType));
        Guard.ArgumentNotNull(implementationInstance, nameof(implementationInstance));

        return AddInstance(serviceType, implementationInstance, ServiceLifetime.Singleton);
    }

    /// <inheritdoc/>
    public IServiceCollection AddSingleton<TService>(TService implementationInstance) where TService : class
    {
        Guard.ArgumentNotNull(implementationInstance, nameof(implementationInstance));

        return AddSingleton(typeof(TService), implementationInstance);
    }
    #endregion

    #region IList Members
    /// <summary>
    /// Gets the number of service descriptors contained in the collection.
    /// </summary>
    public int Count { get { lock (_syncRoot) return _descriptors.Count; } }

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
            lock (_syncRoot) return Copy(_descriptors[index]);
        }
        set
        {
            ServiceDescriptor snapshot = Copy(value);
            lock (_syncRoot)
            {
                if (index < 0 || index >= _descriptors.Count) throw new ArgumentOutOfRangeException(nameof(index));
                int existingIndex = _descriptors.IndexOf(snapshot);
                if (existingIndex >= 0 && existingIndex != index) throw new ArgumentException("A service type may appear only once.", nameof(value));
                _descriptors[index] = snapshot;
            }
        }
    }

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    /// <param name="item">The service descriptor to locate.</param>
    /// <returns>The index of the item if found; otherwise, -1.</returns>
    public int IndexOf(ServiceDescriptor item)
    {
        lock (_syncRoot) return _descriptors.IndexOf(item);
    }

    /// <summary>
    /// Inserts a service descriptor at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The service descriptor to insert.</param>
    public void Insert(int index, ServiceDescriptor item)
    {
        item = Copy(item);
        lock (_syncRoot)
        {
            if (index < 0 || index > _descriptors.Count) throw new ArgumentOutOfRangeException(nameof(index));
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
        lock (_syncRoot) return _descriptors.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
    {
        Guard.ArgumentNotNull(array, nameof(array));

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        lock (_syncRoot)
        {
            if (arrayIndex > array.Length || _descriptors.Count > array.Length - arrayIndex)
            {
                throw new ArgumentException("Insufficient array capacity.", nameof(array));
            }

            for (int i = 0; i < _descriptors.Count; i++)
            {
                array[arrayIndex + i] = Copy(_descriptors[i]);
            }
        }
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
        lock (_syncRoot)
        {
            List<ServiceDescriptor> snapshot = new();
            foreach (ServiceDescriptor descriptor in _descriptors) snapshot.Add(Copy(descriptor));
            return snapshot.GetEnumerator();
        }
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
}
