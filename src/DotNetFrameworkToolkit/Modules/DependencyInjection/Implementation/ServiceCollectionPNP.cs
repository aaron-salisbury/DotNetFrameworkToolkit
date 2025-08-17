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
    private readonly List<ServiceDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCollectionPNP"/> class.
    /// </summary>
    public ServiceCollectionPNP()
    {
        _descriptors = [];
    }

    /// <summary>
    /// Adds a service descriptor with the specified service type, implementation type, and lifetime.
    /// </summary>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationType">The implementation type of the service.</param>
    /// <param name="lifetime">The lifetime of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
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

    /// <summary>
    /// Adds a service descriptor with the specified service type, implementation instance, and lifetime.
    /// </summary>
    /// <param name="serviceType">The type of the service to register.</param>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <param name="lifetime">The lifetime of the service.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
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

    /// <summary>
    /// Creates an <see cref="IServiceProvider"/> containing services from the current collection.
    /// </summary>
    /// <returns>The <see cref="IServiceProvider"/>.</returns>
    public IServiceProvider BuildServiceProvider()
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

    /// <summary>
    /// Removes the service descriptor at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        _descriptors.RemoveAt(index);
    }

    /// <summary>
    /// Adds a service descriptor to the end of the collection.
    /// </summary>
    /// <param name="item">The service descriptor to add.</param>
    public void Add(ServiceDescriptor item)
    {
        Insert(_descriptors.Count, item);
    }

    /// <summary>
    /// Removes all service descriptors from the collection.
    /// </summary>
    public void Clear()
    {
        _descriptors.Clear();
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
        return _descriptors.Remove(item);
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
