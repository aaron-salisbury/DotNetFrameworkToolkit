using System;

namespace DotNetFrameworkToolkit.Modules.ComponentModel.Validation;

/// <summary>
/// Provides data for the <c>ErrorsChanged</c> event.
/// </summary>
public class DataErrorsChangedEventArgs : EventArgs
{
    private readonly string _propertyName;
    /// <summary>
    /// Gets the name of the property for which the errors have changed.
    /// </summary>
    public virtual string PropertyName
    {
        get { return _propertyName; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataErrorsChangedEventArgs"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that has errors.</param>
    public DataErrorsChangedEventArgs(string propertyName)
    {
        _propertyName = propertyName;
    }
}
