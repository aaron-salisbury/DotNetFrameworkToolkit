using System.Text;
using System.Text.RegularExpressions;

namespace DotNetFrameworkToolkit.Core.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="string"/> instances.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts the specified string to a byte array using ASCII encoding.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>A byte array representing the ASCII-encoded string.</returns>
    public static byte[] ToBytes(string str)
    {
        return Encoding.ASCII.GetBytes(str);
    }

    /// <summary>
    /// Inserts spaces into a PascalCase or camelCase string, splitting words at uppercase letters and numbers.
    /// </summary>
    /// <param name="str">The PascalCase or camelCase string to split.</param>
    /// <returns>A string with spaces inserted between words.</returns>
    /// <remarks>
    /// Inspired by <see href="https://stackoverflow.com/a/3216204">this answer</see> by chilltemp.
    /// </remarks>
    public static string SplitPascalCase(string str)
    {
        return Regex.Replace(str, @"(?<=[A-Za-z])(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[0-9]?[A-Z])", " ");
    }

    /// <summary>
    /// Gets the first word from the specified string, splitting PascalCase if necessary.
    /// </summary>
    /// <param name="str">The string from which to extract the first word.</param>
    /// <returns>The first word in the string, or the original string if it contains no spaces.</returns>
    /// <remarks>
    /// Inspired by <see href="https://stackoverflow.com/a/3607316">this answer</see> by Jamiec.
    /// </remarks>
    public static string GetFirstWord(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        str = SplitPascalCase(str);

        return str.IndexOf(" ") > -1 ? str.Substring(0, str.IndexOf(" ")) : str;
    }
}
