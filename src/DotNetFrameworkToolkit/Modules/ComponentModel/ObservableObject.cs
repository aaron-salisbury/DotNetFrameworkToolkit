using System.Collections.Generic;
using System.ComponentModel;

namespace DotNetFrameworkToolkit.Modules.ComponentModel;

/// <summary>
/// Provides a base class that implements <see cref="INotifyPropertyChanged"/> to simplify property change notification.
/// </summary>
/// <remarks>
/// Inspired by <see href="https://stackoverflow.com/a/1316417">this answer</see> by Marc Gravell.
/// </remarks>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Occurs when a static property value changes.
    /// </summary>
    public static event PropertyChangedEventHandler StaticPropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the field to the specified value and raises the <see cref="PropertyChanged"/> event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">A reference to the field to set.</param>
    /// <param name="newValue">The new value for the field.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <returns><c>true</c> if the value was changed; otherwise, <c>false</c>.</returns>
    protected bool SetField<T>(ref T field, T newValue, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        RaisePropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// Raises the <see cref="StaticPropertyChanged"/> event for the specified static property.
    /// </summary>
    /// <param name="propertyName">The name of the static property that changed.</param>
    protected static void StaticRaisePropertyChanged(string propertyName)
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
    }
}
