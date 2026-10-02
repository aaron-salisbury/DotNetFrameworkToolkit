using DotNetFrameworkToolkit.Modules.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetFrameworkToolkit.Tests;

[TestClass]
public class LoggingContractTests
{
    [TestMethod]
    public void DirectScopeConstructionRejectsDisposedLoggerWithoutInstallingScope()
    {
        LoggerPNP logger = new(LogLevel.None);
        logger.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(() => new LoggerPNPScope(logger, "direct"));
        Assert.ThrowsException<ObjectDisposedException>(() => logger.BeginScope(new object()));
        Assert.IsNull(logger.CurrentScope);
        Assert.ThrowsException<ArgumentNullException>(() => new LoggerPNPScope(null, "direct"));

        // Filtering is a configuration query and disabled writes remain harmless.
        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        logger.LogInformation("{Missing}", 1);
    }

    [TestMethod]
    public void DirectScopesShareNestingAndAllowNullState()
    {
        CapturingSink sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            using (LoggerPNPScope outer = new(logger, "outer"))
            {
                using (LoggerPNPScope inner = new(logger, null))
                {
                    Assert.AreSame(outer, inner.Parent);
                    logger.LogInformation("entry");
                    CollectionAssert.AreEqual(new[] { "outer", string.Empty }, (string[])sink.Last.ExtendedProperties["Scopes"]);
                }
                Assert.AreSame(outer, logger.CurrentScope);
            }
            Assert.IsNull(logger.CurrentScope);
        }
    }

    [TestMethod]
    public void ThreeScopesSkipDisposedAncestorsAndRepeatedDisposal()
    {
        CapturingSink sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            IDisposable outer = logger.BeginScope("outer");
            IDisposable middle = logger.BeginScope("middle");
            IDisposable inner = logger.BeginScope("inner");
            try
            {
                middle.Dispose();
                middle.Dispose();
                logger.LogInformation("first");
                CollectionAssert.AreEqual(new[] { "outer", "inner" }, (string[])sink.Last.ExtendedProperties["Scopes"]);
                outer.Dispose();
                inner.Dispose();
                logger.LogInformation("second");
                Assert.IsNull(logger.CurrentScope);
                Assert.IsFalse(sink.Last.ExtendedProperties.ContainsKey("Scopes"));
            }
            finally
            {
                inner.Dispose();
                middle.Dispose();
                outer.Dispose();
            }
        }
    }

    [TestMethod]
    public void ScopeDisposalAfterLoggerDisposalDoesNotRestoreAncestors()
    {
        LoggerPNP logger = new(LogLevel.None);
        IDisposable outer = logger.BeginScope("outer");
        IDisposable inner = logger.BeginScope("inner");
        logger.Dispose();
        inner.Dispose();
        outer.Dispose();
        inner.Dispose();
        Assert.IsNull(logger.CurrentScope);
    }

    [TestMethod]
    public void ListenerShutdownFailureStillClearsScopeAndClosesAdmission()
    {
        ApplicationException original = new("listener shutdown");
        LoggerPNP logger = new(LogLevel.Information, new ThrowingShutdownSink(original));
        IDisposable scope = logger.BeginScope("scope");
        try
        {
            ApplicationException actual = Assert.ThrowsException<ApplicationException>(() => logger.Dispose());
            Assert.AreSame(original, actual);
            Assert.IsNull(logger.CurrentScope);
            Assert.ThrowsException<ObjectDisposedException>(() => new LoggerPNPScope(logger, "late"));
        }
        finally
        {
            scope.Dispose();
            logger.Dispose();
        }
    }

    [TestMethod]
    public void ScopesOfDifferentLoggersAreIndependent()
    {
        using (LoggerPNP first = new(LogLevel.None))
        {
            using (LoggerPNP second = new(LogLevel.None))
            {
                using (IDisposable scope = first.BeginScope("first"))
                {
                    Assert.IsNull(second.CurrentScope);
                    second.Dispose();
                    Assert.IsNotNull(first.CurrentScope);
                }
                Assert.IsNull(first.CurrentScope);
            }
        }
    }

    [TestMethod]
    [Timeout(15000)]
    public void WorkerOwnsCleanupAfterLoggerShutdown()
    {
        LoggerPNP logger = new(LogLevel.None);
        using (ManualResetEventSlim ready = new())
        {
            using (ManualResetEventSlim release = new())
            {
                Exception failure = null;
                Thread worker = new(() =>
                {
                    try
                    {
                        using (IDisposable outer = logger.BeginScope("worker outer"))
                        {
                            using (IDisposable inner = logger.BeginScope("worker inner"))
                            {
                                ready.Set();
                                Assert.IsTrue(release.Wait(5000), "Owner thread was not released.");
                                Assert.IsNotNull(logger.CurrentScope);
                                Assert.ThrowsException<ObjectDisposedException>(() => new LoggerPNPScope(logger, "late"));
                            }
                        }
                        Assert.IsNull(logger.CurrentScope, "Worker retained the logger after scope cleanup.");
                    }
                    catch (Exception error)
                    {
                        failure = error;
                        ready.Set();
                    }
                });
                worker.IsBackground = true;
                worker.Start();
                try
                {
                    Assert.IsTrue(ready.Wait(5000), "Worker did not create scopes.");
                    Assert.IsNull(logger.CurrentScope, "Worker scope leaked onto the shutdown thread.");
                    logger.Dispose();
                }
                finally
                {
                    release.Set();
                    Assert.IsTrue(worker.Join(5000), "Worker did not clean up.");
                    logger.Dispose();
                }
                Assert.IsNull(failure, failure == null ? string.Empty : failure.ToString());
            }
        }
    }

    [TestMethod]
    [Timeout(15000)]
    public void ShutdownDrainsFormatterAndRejectsBothScopeEntryPoints()
    {
        CapturingSink sink = new();
        LoggerPNP logger = new(LogLevel.Information, sink);
        using (ManualResetEventSlim entered = new())
        {
            using (ManualResetEventSlim release = new())
            {
                Task write = Task.Run(() => logger.Log(LogLevel.Information, 0, "entry", null, (state, error) =>
                {
                    entered.Set();
                    if (!release.Wait(5000))
                    {
                        throw new TimeoutException("Formatter was not released.");
                    }
                    return state;
                }));
                Task shutdown = null;
                try
                {
                    Assert.IsTrue(entered.Wait(5000), "Formatter did not start.");
                    shutdown = Task.Run(() => logger.Dispose());
                    Assert.IsTrue(SpinWait.SpinUntil(() => IsClosing(logger), 5000), "Shutdown did not close admission.");
                    Assert.ThrowsException<ObjectDisposedException>(() => new LoggerPNPScope(logger, "direct"));
                    Assert.ThrowsException<ObjectDisposedException>(() => logger.BeginScope("factory"));
                    Assert.IsFalse(shutdown.IsCompleted, "Shutdown failed to wait for the write.");
                }
                finally
                {
                    release.Set();
                    Assert.IsTrue(write.Wait(5000), "Write did not drain.");
                    if (shutdown != null)
                    {
                        Assert.IsTrue(shutdown.Wait(5000), "Shutdown did not finish.");
                    }
                    logger.Dispose();
                }
                Assert.AreEqual("entry", sink.Last.Message);
            }
        }
    }

    [TestMethod]
    public void FormatterCannotDisposeItsActiveLogger()
    {
        InMemorySinkPNP sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            logger.Log(LogLevel.Information, 0, "entry", null, (state, error) =>
            {
                Assert.ThrowsException<InvalidOperationException>(() => logger.Dispose());
                return state;
            });
            logger.LogInformation("still open");
            Assert.AreEqual(2, sink.Logs.Count);
        }
    }

    [DataTestMethod]
    [DataRow("{Name} / {0} / {Next} / {Name}", "first / first / second / first")]
    [DataRow("{1} / {Name} / {0} / {Name}", "second / third / first / third")]
    [DataRow("{Name} / {name} / {Name}", "first / second / first")]
    public void MixedTemplatesUseEncounterOrderAndCaseSensitiveNames(string template, string expected)
    {
        FormattedLogValues values = new(template, "first", "second", "third");
        Assert.AreEqual(expected, values.ToString());
    }

    [TestMethod]
    public void RepeatedNamesCanUseDifferentFormattingWithoutConsumingArguments()
    {
        FormattedLogValues values = new("{Amount:N2} / {Amount,8:N1}", 12.5);
        Assert.AreEqual("12.50 /     12.5", values.ToString());
        Assert.AreEqual(12.5, values.Properties["Amount"]);
    }

    [DataTestMethod]
    [DataRow("{2}")]
    [DataRow("{Name} }")]
    [DataRow("{Name,invalid}")]
    [DataRow("{Name:Q}")]
    public void InvalidFormattingWritesNothingAndDoesNotCloseLogger(string template)
    {
        InMemorySinkPNP sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            Assert.ThrowsException<FormatException>(() => logger.LogInformation(template, 7));
            Assert.AreEqual(0, sink.Logs.Count);
            logger.LogInformation("recovery");
            Assert.AreEqual(1, sink.Logs.Count);
        }
    }

    [TestMethod]
    public void FormatterAndScopeStringFailuresPreserveOriginalExceptionAndWriteNothing()
    {
        InMemorySinkPNP sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            ApplicationException original = new("formatter");
            ApplicationException actual = Assert.ThrowsException<ApplicationException>(() => logger.Log(LogLevel.Information, 0, "entry", null, (state, error) =>
            {
                throw original;
            }));
            Assert.AreSame(original, actual);
            using (IDisposable scope = logger.BeginScope(new ThrowingState(original)))
            {
                actual = Assert.ThrowsException<ApplicationException>(() => logger.LogInformation("entry"));
                Assert.AreSame(original, actual);
            }
            Assert.AreEqual(0, sink.Logs.Count);
            logger.LogInformation("recovery");
            Assert.AreEqual(1, sink.Logs.Count);
        }
    }

    [TestMethod]
    public void InnerScopeAndEntryPropertiesOverrideOuterValues()
    {
        CapturingSink sink = new();
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            using (IDisposable outer = logger.BeginScope("outer {Key}", "outer"))
            {
                using (IDisposable inner = logger.BeginScope("inner {Key}", "inner"))
                {
                    logger.LogInformation("entry");
                    Assert.AreEqual("inner", sink.Last.ExtendedProperties["Key"]);
                    logger.LogInformation("entry {Key}", "entry");
                    Assert.AreEqual("entry", sink.Last.ExtendedProperties["Key"]);
                    Assert.AreEqual("entry {Key}", sink.Last.ExtendedProperties["{OriginalFormat}"]);
                }
            }
        }
    }

    [TestMethod]
    [Timeout(15000)]
    public void SinkObserversSeeCommittedBufferAndCanWriteFromAnotherThread()
    {
        InMemorySinkPNP sink = new();
        int calls = 0;
        sink.LogEmitted += (sender, args) =>
        {
            Assert.AreEqual("message", sink.Logs.Single());
            Task writer = Task.Run(() => sink.Write("worker"));
            Assert.IsTrue(writer.Wait(5000), "Observer ran while the buffer lock was held.");
            calls++;
        };
        sink.TraceData(null, "test", TraceEventType.Information, 0, "message");
        Assert.AreEqual(1, calls, "Observer assertion was swallowed by the best-effort event contract.");
        CollectionAssert.AreEqual(new[] { "message", "worker" }, sink.Logs.ToArray());
    }

    [TestMethod]
    public void FailingObserversCannotReplaceOriginalExceptionOrOtherObserverData()
    {
        InMemorySinkPNP sink = new();
        Exception original = new ApplicationException("operation");
        LogEvent observed = null;
        sink.LogEmitted += (sender, args) =>
        {
            args.LogEvent.Message = "mutated";
            args.LogEvent.Exception = null;
            throw new ApplicationException("observer");
        };
        sink.LogEmitted += (sender, args) =>
        {
            observed = args.LogEvent;
        };
        using (LoggerPNP logger = new(LogLevel.Information, sink))
        {
            logger.LogError(original, "entry");
            Assert.IsNotNull(observed);
            Assert.AreSame(original, observed.Exception);
            StringAssert.Contains(observed.Message, "entry");
            Assert.AreNotEqual("mutated", observed.Message);
            Assert.AreEqual(1, sink.Logs.Count);
        }
    }

    private static bool IsClosing(LoggerPNP logger)
    {
        try
        {
            using (IDisposable probe = logger.BeginScope("probe"))
            {
                return false;
            }
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private sealed class ThrowingState
    {
        private readonly Exception _error;

        public ThrowingState(Exception error)
        {
            _error = error;
        }

        public override string ToString()
        {
            throw _error;
        }
    }

    private sealed class ThrowingShutdownSink : TraceListener
    {
        private readonly Exception _error;

        public ThrowingShutdownSink(Exception error)
        {
            _error = error;
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void Close()
        {
            throw _error;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                throw _error;
            }
            base.Dispose(disposing);
        }
    }

    private sealed class CapturingSink : CustomTraceListener
    {
        public LogEntry Last { get; private set; }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void TraceData(TraceEventCache cache, string source, TraceEventType type, int id, object data)
        {
            Last = (LogEntry)data;
        }
    }
}
