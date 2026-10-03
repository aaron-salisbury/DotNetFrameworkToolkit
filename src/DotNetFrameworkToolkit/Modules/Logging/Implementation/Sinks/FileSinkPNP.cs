using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging.Formatters;
using Microsoft.Practices.EnterpriseLibrary.Logging.TraceListeners;
using System;
using System.Diagnostics;

namespace DotNetFrameworkToolkit.Modules.Logging;

/// <summary>
/// Writes Patterns & Practices Enterprise Library log entries to a file by leveraging a <see cref="TextWriterTraceListener"/>.
/// </summary>
[ConfigurationElementType(typeof(CustomTraceListenerData))]
public class FileSinkPNP : CustomTraceListener
{
    private readonly TextWriterTraceListener _fileListener;
    private readonly object _syncLock = new();
    private bool _disposed;

    /// <inheritdoc/>
    public override bool IsThreadSafe => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSinkPNP"/> class.
    /// using the specified file as recipient of the tracing or debugging output.
    /// </summary>
    /// <param name="filePath">The output file path used by the underlying file trace listener.</param>
    /// <param name="formatter">The formatter used for <see cref="LogEntry"/> instances. If <see langword="null"/>, a default formatter is used.</param>
    public FileSinkPNP(string filePath, ILogFormatter formatter = null)
    {
        if (filePath == null || filePath.Trim().Length == 0)
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        _fileListener = new TextWriterTraceListener(filePath);

        Formatter = formatter ?? DefaultFormatter();
    }

    /// <summary>
    /// Writes a message to this instance's listener without a line terminator.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public override void Write(string message)
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();
            _fileListener.Write(message);
        }
    }

    /// <summary>
    /// Writes a message to this instance's listener followed by a line terminator.
    /// The default line terminator is a carriage return followed by a line feed (\r\n).
    /// </summary>
    /// <param name="message">The message to write.</param>
    public override void WriteLine(string message)
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();
            _fileListener.WriteLine(message);
        }
    }

    /// <summary>
    /// Traces structured data to the listener.
    /// </summary>
    /// <param name="eventCache">The event cache that contains contextual trace data.</param>
    /// <param name="source">The source that emitted the trace event.</param>
    /// <param name="eventType">The type of trace event.</param>
    /// <param name="id">The numeric identifier for the event.</param>
    /// <param name="data">The event data to trace.</param>
    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();

            if (Formatter is not null && data is LogEntry logEntry)
            {
                string message = Formatter.Format(logEntry);
                _fileListener.WriteLine(message);
            }
            else
            {
                _fileListener.TraceData(eventCache, source, eventType, id, data);
            }
        }
    }

    private static ILogFormatter DefaultFormatter()
    {
        string textFormatterTemplate = "{timestamp(yyyy-MM-dd HH:mm:ss.ff)} UTC [{category}] {message}";

        return new TextFormatter(textFormatterTemplate);
    }

    /// <summary>
    /// Flushes the output buffer for the underlying file listener.
    /// </summary>
    public override void Flush()
    {
        lock (_syncLock)
        {
            ThrowIfDisposed();
            _fileListener.Flush();
        }
        base.Flush();
    }

    /// <summary>
    /// Flushes and closes the underlying file listener. Repeated calls are harmless.
    /// </summary>
    public override void Close()
    {
        Dispose();
    }

    /// <summary>
    /// Releases managed resources used by this listener.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; otherwise, <see langword="false"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
            return;
        }

        lock (_syncLock)
        {
            if (_disposed)
            {
                return;
            }

            _fileListener.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FileSinkPNP));
        }
    }
}
