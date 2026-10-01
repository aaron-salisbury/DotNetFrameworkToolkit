using System;
using System.Globalization;

namespace DotNetFrameworkToolkit.Core.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="DateTime"/> instances.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts the specified <see cref="DateTime"/> to an invariant timestamp with seconds precision.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
    /// <returns>A string representation of the timestamp.</returns>
    public static string ToTimeStamp(DateTime dateTime)
    {
        return dateTime.ToString("yyyy.MM.dd.HH.mm.ss", CultureInfo.InvariantCulture);
    }
}
