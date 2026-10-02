using DotNetFrameworkToolkit.Core.Extensions;
using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class UtilityTests
{
    [TestMethod]
    public void TimestampIncludesHourAndIgnoresCurrentCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            DateTime time = new(2026, 10, 2, 13, 4, 5);
            Assert.AreEqual("2026.10.02.13.04.05", DateTimeExtensions.ToTimeStamp(time));
            Assert.AreNotEqual(DateTimeExtensions.ToTimeStamp(time), DateTimeExtensions.ToTimeStamp(time.AddHours(1)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [DataTestMethod]
    [DataRow("HelloWorld", "Hello World", "Hello")]
    [DataRow("HTTPServer", "HTTP Server", "HTTP")]
    [DataRow("Hello", "Hello", "Hello")]
    public void StringWordHelpersSplitExpectedBoundaries(string input, string split, string first)
    {
        Assert.AreEqual(split, StringExtensions.SplitPascalCase(input));
        Assert.AreEqual(first, StringExtensions.GetFirstWord(input));
    }

    [TestMethod]
    public void FirstWordPreservesNullAndEmptyAndByteHelperRemainsAscii()
    {
        Assert.IsNull(StringExtensions.GetFirstWord(null));
        Assert.AreEqual(string.Empty, StringExtensions.GetFirstWord(string.Empty));
        CollectionAssert.AreEqual(new byte[] { 65, 63 }, StringExtensions.ToBytes("Aä"));
    }

    [TestMethod]
    public void EventIdentityUsesIdRatherThanName()
    {
        EventId first = new(42, "first");
        EventId second = new(42, "second");
        Assert.IsTrue(first == second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        Assert.AreEqual("first", first.ToString());
        Assert.AreEqual("42", new EventId(42).ToString());
    }

}
