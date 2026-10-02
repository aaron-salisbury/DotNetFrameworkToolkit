using DotNetFrameworkToolkit.Core;
using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using ToolkitAggregateException = DotNetFrameworkToolkit.Core.AggregateException;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class ResultTests
{
    public enum Outcome
    {
        None,
        NotFound
    }

    [TestMethod]
    public void SuccessfulFalseDescribesOperationSuccess()
    {
        ProcessResult<bool> result = ProcessResult<bool>.Success(false);
        Assert.IsTrue(result);
        Assert.IsFalse(result.Value);
        Assert.IsTrue(result.TryGet(out bool value));
        Assert.IsFalse(value);
    }

    [TestMethod]
    public void SuccessfulNullIsStillSuccessful()
    {
        ProcessResult<string> result = ProcessResult<string>.Success(null);
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsTrue(result.TryGet(out string value));
        Assert.IsNull(value);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void FailurePreservesOriginalExceptionAndStackAcrossRepeatedReads()
    {
        Exception original;
        try
        {
            ThrowOriginal();
            Assert.Fail("Expected fixture exception.");
            return;
        }
        catch (ApplicationException error)
        {
            original = error;
        }
        string stack = original.StackTrace;
        ProcessResult<int> result = ProcessResult<int>.Failure(original);
        Assert.IsFalse(result);
        Assert.IsFalse(result.TryGet(out int value));
        Assert.AreEqual(0, value);
        for (int i = 0; i < 2; i++)
        {
            InvalidOperationException access = Assert.ThrowsException<InvalidOperationException>(() =>
            {
                int ignored = result.Value;
            });
            Assert.AreSame(original, access.InnerException);
            Assert.AreEqual(stack, original.StackTrace);
        }
    }

    [TestMethod]
    public void NullExceptionIsRejected()
    {
        Assert.ThrowsException<ArgumentNullException>(() => ProcessResult<int>.Failure(null));
    }

    [TestMethod]
    public void NullResultConvertsToFalse()
    {
        ProcessResult<int> exceptionResult = null;
        ProcessResult<int, Outcome> expectedResult = null;
        Assert.IsFalse(exceptionResult);
        Assert.IsFalse(expectedResult);
    }

    [TestMethod]
    public void ExpectedFailureRejectsSuccessSentinelAndHasNoValue()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ProcessResult<int, Outcome>.Failure(Outcome.None));
        ProcessResult<int, Outcome> result = ProcessResult<int, Outcome>.Failure(Outcome.NotFound);
        Assert.IsFalse(result);
        Assert.AreEqual(Outcome.NotFound, result.Error);
        Assert.IsFalse(result.TryGet(out int value));
        Assert.AreEqual(0, value);
        Assert.ThrowsException<InvalidOperationException>(() => { int ignored = result.Value; });
    }

    [TestMethod]
    public void ValueAndErrorCanUseTheSameEnumType()
    {
        ProcessResult<Outcome, Outcome> success = ProcessResult<Outcome, Outcome>.Success(Outcome.NotFound);
        Assert.IsTrue(success);
        Assert.AreEqual(Outcome.NotFound, success.Value);
        Assert.AreEqual(Outcome.None, success.Error);
        Assert.IsFalse(ProcessResult<Outcome, Outcome>.Failure(Outcome.NotFound));
    }

    [TestMethod]
    public void ForwardedExceptionRetainsOriginalEvenWithLoggingDisabled()
    {
        using LoggerPNP logger = new(LogLevel.None);
        Exception original = new ApplicationException("original");
        ProcessResult<int> result = ProcessResult<int>.LogAndForwardException("context", original, logger);
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual("context", result.Error.Message);
        Assert.AreSame(original, result.Error.InnerException);
    }

    [TestMethod]
    public void ValidationResultCopiesInputAndReturnedMessages()
    {
        string[] messages = { "required" };
        Dictionary<string, string[]> properties = new() { { "Name", messages } };
        List<string> general = ["entity"];
        ValidationResult<object> result = new(new object(), properties, general);
        messages[0] = "changed";
        properties.Clear();
        general.Clear();
        result.ValidationMessages.Single().Value[0] = "returned mutation";
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("required", result.ValidationMessages.Single().Value[0]);
        Assert.AreEqual("entity", result.GeneralMessages.Single());
    }

    [TestMethod]
    public void EmptyValidationMessagesDoNotMakeResultInvalid()
    {
        ValidationResult<int> result = new(5, new Dictionary<string, string[]> { { "Name", Array.Empty<string>() } }, null);
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(5, result.Value);
    }

    [TestMethod]
    public void AggregateCopiesFlattensAndReportsUnhandledExceptions()
    {
        Exception first = new ApplicationException("first");
        Exception second = new InvalidOperationException("second");
        List<Exception> input = [first, new ToolkitAggregateException(second)];
        ToolkitAggregateException aggregate = new(input);
        input.Clear();
        CollectionAssert.AreEqual(new[] { first, second }, aggregate.Flatten().InnerExceptions.ToArray());
        ToolkitAggregateException unhandled = Assert.ThrowsException<ToolkitAggregateException>(() => aggregate.Flatten().Handle(error => ReferenceEquals(error, first)));
        Assert.AreSame(second, unhandled.InnerExceptions.Single());
        StringAssert.Contains(aggregate.ToString(), "first");
        StringAssert.Contains(aggregate.ToString(), "second");
    }

    [TestMethod]
    public void AggregateRejectsNullElementsAndRoundTripsSerialization()
    {
        Assert.ThrowsException<ArgumentException>(() => new ToolkitAggregateException([null]));
        ToolkitAggregateException original = new("context", new ApplicationException("inner"));
        using MemoryStream stream = new();
        BinaryFormatter formatter = new();
        // Only this trusted in-memory test object is deserialized.
        formatter.Serialize(stream, original);
        stream.Position = 0;
        ToolkitAggregateException copy = (ToolkitAggregateException)formatter.Deserialize(stream);
        Assert.AreEqual("context", copy.Message);
        Assert.AreEqual("inner", copy.InnerExceptions.Single().Message);
    }

    private static void ThrowOriginal()
    {
        throw new ApplicationException("original");
    }
}
