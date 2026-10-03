using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class LoggingTests
{
    [TestMethod]
    public void NamedAndRepeatedTemplatesPreserveProperties()
    {
        FormattedLogValues values = new("User {User} / {User} paid {Amount:N2}", "Aaron", 12.5);
        Assert.AreEqual("User Aaron / Aaron paid 12.50", values.ToString());
        Assert.AreEqual("Aaron", values.Properties["User"]);
        Assert.AreEqual(12.5, values.Properties["Amount"]);
        Assert.AreEqual("User {User} / {User} paid {Amount:N2}", values.Properties["{OriginalFormat}"]);
    }

    [TestMethod]
    public void FormattingIsInvariantAndArgumentsAreSnapshotted()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            object[] arguments = { 12.5 };
            FormattedLogValues values = new("Amount {Amount:N2}", arguments);
            arguments[0] = 99;
            Assert.AreEqual("Amount 12.50", values.ToString());
            Assert.AreEqual(12.5, values.Properties["Amount"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void MixedNumericAndNamedTemplatesAssignTheNextArgument()
    {
        FormattedLogValues values = new("{0} / {Name} / {Name}", "first", "second");
        Assert.AreEqual("first / second / second", values.ToString());
        Assert.AreEqual("first", values.Properties["0"]);
        Assert.AreEqual("second", values.Properties["Name"]);
    }

    [TestMethod]
    public void CompositeFormattingSupportsAlignmentAndEscapedBraces()
    {
        Assert.AreEqual("{value:    7}", new FormattedLogValues("{{value: {0,4}}}", 7).ToString());
        Assert.AreEqual("literal {no arguments}", new FormattedLogValues("literal {no arguments}").ToString());
    }

    [DataTestMethod]
    [DataRow("{Name} {Missing}")]
    [DataRow("Unclosed {Name")]
    [DataRow("Invalid {}")]
    public void MalformedOrMissingTemplateArgumentsThrow(string template)
    {
        Assert.ThrowsException<FormatException>(() => new FormattedLogValues(template, 1).ToString());
    }

    [TestMethod]
    public void LoggerFiltersNoneAndDoesNotFormatDisabledMessages()
    {
        InMemorySinkPNP sink = new();
        using LoggerPNP logger = new(LogLevel.Warning, sink);
        Assert.IsFalse(logger.IsEnabled(LogLevel.None));
        logger.LogInformation("{Missing}", Array.Empty<object>());
        logger.LogWarning("accepted");
        Assert.AreEqual(1, sink.Logs.Count);
        StringAssert.Contains(sink.Logs[0], "accepted");
    }

    [TestMethod]
    public void LoggerIncludesExceptionAndEventProperties()
    {
        CapturingSink sink = new();
        using LoggerPNP logger = new(LogLevel.Trace, sink);
        Exception error = new ApplicationException("diagnostic");
        logger.LogError(new EventId(42, "Failure"), error, "User {User}", "Aaron");
        Assert.AreEqual(42, sink.Last.EventId);
        Assert.AreEqual("Failure", sink.Last.ExtendedProperties["EventName"]);
        Assert.AreEqual("Aaron", sink.Last.ExtendedProperties["User"]);
        StringAssert.Contains(sink.Last.Message, "diagnostic");
    }

    [TestMethod]
    public void NestedScopesAllowOutOfOrderDisposalWithoutResurrection()
    {
        CapturingSink sink = new();
        using LoggerPNP logger = new(LogLevel.Information, sink);
        IDisposable outer = logger.BeginScope("Outer {Correlation}", 10);
        IDisposable inner = logger.BeginScope("Inner {Request}", 20);

        try
        {
            outer.Dispose();
            logger.LogInformation("first");
            Assert.IsFalse(sink.Last.ExtendedProperties.ContainsKey("Correlation"));
            Assert.AreEqual(20, sink.Last.ExtendedProperties["Request"]);
            inner.Dispose();
            logger.LogInformation("second");
            Assert.IsFalse(sink.Last.ExtendedProperties.ContainsKey("Scopes"));
        }
        finally
        {
            inner.Dispose();
            outer.Dispose();
        }
    }

    [TestMethod]
    [Timeout(15000)]
    public void ScopesDoNotFlowToWorkerThreadsAndRejectWorkerDisposal()
    {
        InMemorySinkPNP sink = new();
        using LoggerPNP logger = new(LogLevel.Information, sink);
        using IDisposable scope = logger.BeginScope("scope");
        Task.Run(() =>
        {
            Assert.IsNull(logger.CurrentScope);
            Assert.ThrowsException<InvalidOperationException>(() => scope.Dispose());
        }).GetAwaiter().GetResult();
        Assert.IsNotNull(logger.CurrentScope);
    }

    [TestMethod]
    public void MemorySinkReturnsReadOnlySnapshotsAndTrimsWhenLimitDrops()
    {
        InMemorySinkPNP sink = new(0);
        sink.Write("one");
        IList<string> snapshot = sink.Logs;
        sink.Write("two");
        sink.Write("three");
        Assert.AreEqual(1, snapshot.Count);
        Assert.ThrowsException<NotSupportedException>(() => snapshot.Clear());
        sink.MaxLogsCount = 2;
        CollectionAssert.AreEqual(new[] { "two", "three" }, sink.Logs.ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => sink.MaxLogsCount = -1);
    }

    [TestMethod]
    [Timeout(15000)]
    public void ConcurrentWritesKeepRetentionConsistent()
    {
        InMemorySinkPNP sink = new(25);
        Parallel.For(0, 500, i => sink.Write(i.ToString()));
        Assert.AreEqual(25, sink.Logs.Count);
        Assert.AreEqual(25, sink.Logs.Distinct().Count());
    }

    [TestMethod]
    public void FailingObserverDoesNotPreventRemainingObservers()
    {
        InMemorySinkPNP sink = new();
        int notified = 0;
        sink.LogEmitted += (sender, args) => throw new ApplicationException("observer");
        sink.LogEmitted += (sender, args) => notified++;
        using LoggerPNP logger = new(LogLevel.Information, sink);
        logger.LogInformation("message");
        Assert.AreEqual(1, notified);
        Assert.AreEqual(1, sink.Logs.Count);
    }

    [TestMethod]
    public void DisposedLoggerRejectsEnabledOperations()
    {
        LoggerPNP logger = new();
        logger.Dispose();
        logger.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => logger.LogInformation("message"));
        Assert.ThrowsException<ObjectDisposedException>(() => logger.BeginScope("scope"));
    }

    [DataTestMethod]
    [DataRow(null, "\r\n", "\r\n")]
    [DataRow("", "\r\n", "\r\n")]
    [DataRow("message", "\r\n", "message\r\n")]
    [DataRow(null, "\n", "\n")]
    public void MemorySinkWriteLinePreservesTerminatorForNullAndNonNullMessages(string message, string newLine, string expected)
    {
        using ConfigurableMemorySink sink = new(newLine);
        sink.WriteLine(message);
        CollectionAssert.AreEqual(new[] { expected }, sink.Logs.ToArray());
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void FileSinkShutdownFlushesReleasesFileAndRejectsFurtherWrites(int shutdownMode)
    {
        string path = Path.GetTempFileName();
        try
        {
            using FileSinkPNP sink = new(path);
            TraceSource source = new("FileSinkShutdown", SourceLevels.All);
            source.Listeners.Clear();
            source.Listeners.Add(sink);
            try
            {
                sink.WriteLine("message");
                switch (shutdownMode)
                {
                    case 0:
                        sink.Close();
                        break;
                    case 1:
                        source.Close();
                        break;
                    default:
                        sink.Dispose();
                        break;
                }

                // Exclusive access verifies shutdown released the file, not just its buffer.
                using (FileStream stream = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using StreamReader reader = new(stream);
                    Assert.AreEqual("message" + Environment.NewLine, reader.ReadToEnd());
                }

                sink.Close();
                sink.Dispose();
                Assert.ThrowsException<ObjectDisposedException>(() => sink.Write("late"));
                Assert.ThrowsException<ObjectDisposedException>(() => sink.WriteLine("late"));
                Assert.ThrowsException<ObjectDisposedException>(() => sink.Flush());
                Assert.ThrowsException<ObjectDisposedException>(() => sink.TraceData(null, "test", TraceEventType.Information, 0, "late"));
            }
            finally
            {
                source.Close();
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class ConfigurableMemorySink : InMemorySinkPNP
    {
        public ConfigurableMemorySink(string newLine)
        {
            CoreNewLine = newLine.ToCharArray();
        }
    }

    private sealed class CapturingSink : CustomTraceListener
    {
        public LogEntry Last { get; private set; }
        public override void Write(string message) { }
        public override void WriteLine(string message) { }
        public override void TraceData(TraceEventCache cache, string source, TraceEventType type, int id, object data)
        {
            Last = (LogEntry)data;
        }
    }
}
