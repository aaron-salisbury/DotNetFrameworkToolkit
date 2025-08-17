using System;

namespace DotNetFrameworkToolkit.Modules.DependencyInjection;

/// <summary>
/// Describes a service with its service type, implementation, and lifetime.
/// </summary>
public class ServiceDescriptor : IEquatable<ServiceDescriptor>
{
    /// <summary>
    /// Gets or sets the lifetime of the service.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the type of the service.
    /// </summary>
    public Type ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the type that implements the service.
    /// </summary>
    public Type ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the instance that implements the service.
    /// </summary>
    public object ImplementationInstance { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return Equals(obj as ServiceDescriptor);
    }

    /// <summary>
    /// Determines whether the specified <see cref="ServiceDescriptor"/> is equal to the current <see cref="ServiceDescriptor"/>.
    /// </summary>
    /// <param name="other">The <see cref="ServiceDescriptor"/> to compare with the current <see cref="ServiceDescriptor"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="ServiceDescriptor"/> is equal to the current; otherwise, <c>false</c>.</returns>
    public bool Equals(ServiceDescriptor other)
    {
        return other != null && ServiceType == other.ServiceType;
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return ServiceType.GetHashCode();
    }
}
