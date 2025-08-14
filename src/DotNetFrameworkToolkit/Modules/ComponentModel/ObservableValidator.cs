using DotNetFrameworkToolkit.Modules.ComponentModel.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DotNetFrameworkToolkit.Modules.ComponentModel;

/// <summary>
/// Provides a base class for objects that support property change notification and validation error reporting.
/// Implements <see cref="INotifyDataErrorInfo"/> and <see cref="IDataErrorInfo"/> for data validation scenarios.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>WinForms needs an ErrorProvider to be created and passed the control and its message via .UpdateError().</item>
/// <item>WPF uses IDataErrorInfo when ValidatesOnDataErrors is set to true and the REAL INotifyDataErrorInfo when ValidatesOnNotifyDataErrors is set to true.</item>
/// <item>ASP.NET MVC uses a model binder (the DefaultModelBinder) to detect whether or not a class implements the IDataErrorInfo interface.</item>
/// </list>
/// </remarks>
public abstract class ObservableValidator : ObservableObject, IDataErrorInfo, INotifyDataErrorInfo
{
    /// <summary>
    /// Stores non-property specific validation errors.
    /// </summary>
    protected readonly List<string> EntityLevelErrors = []; // Non-property specific validation errors.

    /// <summary>
    /// Stores validation errors for each property by property name.
    /// </summary>
    protected readonly Dictionary<string, List<string>> ErrorsByPropertyNames = [];

    /// <summary>
    /// Raises the <see cref="ObservableObject.PropertyChanged"/> event and validates the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    public void RaisePropertyChangedWithValidation(string propertyName)
    {
        PropertyIsValid(propertyName);
        base.RaisePropertyChanged(propertyName);
    }

    /// <summary>
    /// Gets the list of validation errors for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A list of validation error messages for the property.</returns>
    public List<string> GetErrorsForProperty(string propertyName)
    {
        return ErrorsByPropertyNames.ContainsKey(propertyName)
            ? ErrorsByPropertyNames[propertyName]
            : [];
    }

    /// <summary>
    /// Sets the validation errors for the specified property and raises the <see cref="ErrorsChangedCore"/> event if errors have changed.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errors">The list of validation errors for the property.</param>
    protected void SetErrorsForProperty(string propertyName, List<string> errors)
    {
        List<string> startingErrorsForProperty = ErrorsByPropertyNames.ContainsKey(propertyName)
            ? ErrorsByPropertyNames[propertyName]
            : [];

        if (errors.Count > 0)
        {
            ErrorsByPropertyNames[propertyName] = errors;
        }
        else
        {
            if (ErrorsByPropertyNames.ContainsKey(propertyName))
            {
                ErrorsByPropertyNames.Remove(propertyName);
            }
        }

        if (!ErrorsHaventChanged(startingErrorsForProperty, errors, null))
        {
            OnErrorsChanged(this, propertyName);
        }
    }

    /// <summary>
    /// Determines whether two collections of errors are equal.
    /// </summary>
    /// <param name="first">The first collection of errors.</param>
    /// <param name="second">The second collection of errors.</param>
    /// <param name="comparer">The equality comparer to use, or <c>null</c> to use the default comparer.</param>
    /// <returns><c>true</c> if the collections are equal; otherwise, <c>false</c>.</returns>
    private static bool ErrorsHaventChanged(IEnumerable<string> first, IEnumerable<string> second, IEqualityComparer<string> comparer)
    {
        comparer ??= EqualityComparer<string>.Default;

        using IEnumerator<string> enumerator = first.GetEnumerator();
        using IEnumerator<string> enumerator2 = second.GetEnumerator();
        do
        {
            if (!enumerator.MoveNext())
            {
                return !enumerator2.MoveNext();
            }

            if (!enumerator2.MoveNext())
            {
                return false;
            }
        }
        while (comparer.Equals(enumerator.Current, enumerator2.Current));

        return false;
    }

    /// <summary>
    /// Raises the <see cref="ErrorsChangedCore"/> event for the specified property.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="propertyName">The name of the property whose errors changed.</param>
    protected virtual void OnErrorsChanged(object sender, string propertyName)
    {
        // So classes that derive from this and want to implement System.ComponentModel.INotifyDataErrorInfo can wire the ErrorsChanged events together.
        ErrorsChangedCore?.Invoke(sender, new DataErrorsChangedEventArgs(propertyName));
    }

    #region IDataErrorInfo Members
    /// <summary>
    /// Gets a message that describes any non-property specific validation errors for this object.
    /// </summary>
    public string Error
    {
        get
        {
            if (EntityLevelErrors.Count == 0)
            {
                return string.Empty;
            }
            else if (EntityLevelErrors.Count == 1)
            {
                return EntityLevelErrors[0];
            }

            return string.Join(Environment.NewLine, [.. EntityLevelErrors]);
        }
    }

    /// <summary>
    /// Gets the error message for the property with the given name.
    /// </summary>
    /// <param name="columnName">The name of the property to retrieve validation errors for.</param>
    /// <returns>A string containing the validation error messages for the property.</returns>
    public string this[string columnName]
    {
        get
        {
            PropertyIsValid(columnName);
            return string.Join(Environment.NewLine, [.. GetErrorsForProperty(columnName)]);
        }
    }
    #endregion

    #region INotifyDataErrorInfo Members
    /// <summary>
    /// Occurs when the validation errors have changed for a property or for the entire object.
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChangedCore;

    /// <summary>
    /// Gets the validation errors for a specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve validation errors for.</param>
    /// <returns>An <see cref="IEnumerable"/> of errors for the specified property.</returns>
    public IEnumerable GetErrors(string propertyName)
    {
        return GetErrorsForProperty(propertyName);
    }

    /// <summary>
    /// Gets a value indicating whether the object has any validation errors.
    /// </summary>
    public bool HasErrors
    {
        get { return ErrorsByPropertyNames.Values.Count > 0; }
    }
    #endregion

    /// <summary>
    /// Validates all properties of the object and returns whether the object is valid.
    /// </summary>
    /// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this))
        {
            ValidateProperty(property);
        }

        return !HasErrors;
    }

    /// <summary>
    /// Validates the specified property and updates its error collection.
    /// </summary>
    /// <param name="propertyName">The name of the property to validate.</param>
    /// <returns><c>true</c> if the property is valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown if the property does not exist.</exception>
    public bool PropertyIsValid(string propertyName)
    {
        PropertyDescriptor propertyDescriptor = null;

        if (!string.IsNullOrEmpty(propertyName))
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(this))
            {
                if (string.Equals(propertyName, property.Name))
                {
                    propertyDescriptor = property;
                }
            }
        }

        if (propertyDescriptor == null)
        {
            throw new ArgumentException("Property doesn't exist to validate for the name given.");
        }

        List<string> errors = ValidateProperty(propertyDescriptor);
        SetErrorsForProperty(propertyName, errors); // This must be called to keep the errors collection correct and to trigger the ErrorsChanged event as needed.

        return errors.Count == 0;
    }

    /// <summary>
    /// When overridden in a derived class, validates the specified property and returns a list of validation errors.
    /// </summary>
    /// <param name="property">The property descriptor to validate.</param>
    /// <returns>A list of validation error messages for the property.</returns>
    /// <exception cref="NotImplementedException">Thrown if not implemented in a derived class.</exception>
    public virtual List<string> ValidateProperty(PropertyDescriptor property)
    {
        // *** PSEUDO CODE ***
        //List<string> errorsForProperty = [];

        //// Foreach validation...
        //if (false) // Property failed a validation.
        //{
        //    errorsForProperty.Add("Some validation message.");
        //}

        //return errorsForProperty;

        throw new NotImplementedException();
    }
}
