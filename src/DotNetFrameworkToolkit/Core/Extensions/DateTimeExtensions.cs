using System;

namespace DotNetFrameworkToolkit.Core.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="DateTime"/> instances.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to a timestamp string in the format "yyyy.MM.dd.mm.ss".
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
    /// <returns>A string representation of the timestamp.</returns>
    public static string ToTimeStamp(DateTime dateTime)
    {
        return dateTime.ToString("yyyy.MM.dd.mm.ss");
    }
}
