using System;
using System.Collections.Generic;

namespace DotNetFrameworkToolkit.Core
{
    /// <summary>
    /// Represents the result of validating a business or domain object, including property-level validation messages.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    [Serializable]
    public class ValidationResult<T>
    {
        /// <summary>
        /// Gets the validated object.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return (_validationMessages != null ? _validationMessages.Count : 0) == 0
                    && (_generalMessages != null ? _generalMessages.Count : 0) == 0;
            }
        }

        private readonly Dictionary<string, string[]> _validationMessages;
        private readonly List<string> _generalMessages;

        /// <summary>
        /// Gets the validation messages, keyed by property name.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string[]>> ValidationMessages
        {
            get
            {
                Dictionary<string, string[]> snapshot = [];
                foreach (KeyValuePair<string, string[]> pair in _validationMessages)
                {
                    snapshot.Add(pair.Key, (string[])pair.Value.Clone());
                }
                return snapshot;
            }
        }

        /// <summary>
        /// Gets the general (object-level) validation messages not associated with a specific property.
        /// </summary>
        public IEnumerable<string> GeneralMessages
        {
            get { return _generalMessages.AsReadOnly(); }
        }

        /// <summary>
        /// Initializes a successful validation result.
        /// </summary>
        /// <param name="value">The validated object.</param>
        public ValidationResult(T value) : this(value, new Dictionary<string, string[]>(), []) { }

        /// <summary>
        /// Initializes a failed validation result with property and/or general messages.
        /// </summary>
        /// <param name="value">The validated object.</param>
        /// <param name="validationMessages">Validation messages keyed by property name.</param>
        /// <param name="generalMessages">General validation messages not tied to a property.</param>
        public ValidationResult(T value, IDictionary<string, string[]> validationMessages, IList<string> generalMessages)
        {
            Value = value;
            _validationMessages = [];
            if (validationMessages != null)
            {
                foreach (KeyValuePair<string, string[]> pair in validationMessages)
                {
                    if (pair.Value != null && pair.Value.Length > 0)
                    {
                        _validationMessages.Add(pair.Key, (string[])pair.Value.Clone());
                    }
                }
            }
            _generalMessages = generalMessages != null ? [.. generalMessages] : [];
        }

        /// <summary>
        /// Returns a string that represents the current validation result, including general and property-level messages.
        /// </summary>
        public override string ToString()
        {
            List<string> lines = [.. _generalMessages];

            foreach (KeyValuePair<string, string[]> kvp in _validationMessages)
            {
                string property = kvp.Key;
                foreach (string message in kvp.Value)
                {
                    lines.Add(property + " - " + message);
                }
            }

            return string.Join("\n", [.. lines]);
        }
    }
}
