# Logging contracts

## Lifetime and scopes

| Operation | Contract |
| --- | --- |
| Enabled log write | Admitted while the logger is open. Shutdown rejects new writes and waits for admitted writes to finish. Formatter, state conversion, and property enumeration failures propagate before the entry is written. |
| Disabled log write | A no-op, including after shutdown; its template and state are not formatted. |
| `IsEnabled` | A configuration query, not a liveness check. It remains callable after shutdown. `None` and values outside the logging range are disabled. |
| `BeginScope` or direct `new LoggerPNPScope(logger, state)` | Both use the same lifetime admission. A closing/disposed logger throws `ObjectDisposedException`; rejected construction installs no scope. A direct scope accepts null state. |
| Scope disposal | Must run on the creating thread. A wrong-thread call throws without completing the scope. Repeated disposal on the owner thread is harmless. Disposed ancestors are skipped, so out-of-order disposal cannot resurrect them. |
| Logger disposal | Owns and disposes its configured listeners. Clears the scope slot on the disposing thread, even if writer disposal throws. It does not complete outstanding scope objects or clear another thread's slot. |
| Worker cleanup | Scopes do not flow to workers. Each worker must dispose its own scopes before leaving that thread, including when another thread has already disposed the logger. This releases the worker's scope slot and its reference to the logger. |
| Reentrant shutdown | Disposing the logger from its active formatter or sink callback throws `InvalidOperationException`. Do not synchronously wait for another thread to dispose the logger from these callbacks: shutdown waits for the callback's write. |

Argument validation can precede lifetime admission. The string-template overloads parse placeholders before attempting the operation, so a malformed template may throw `FormatException` even when the logger is already disposed. No scope or entry is installed on failure.

Keep scopes in `using` blocks and dispose them before their logger whenever possible. Scopes are synchronous context on .NET Framework 2.0, not asynchronous context propagation.

```csharp
using (LoggerPNP logger = new(LogLevel.Information))
{
    using (IDisposable scope = logger.BeginScope("Request {RequestId}", requestId))
    {
        logger.LogInformation("Handled {Count} items", count);
    }
}
```

## Templates and properties

| Input | Behavior |
| --- | --- |
| Named placeholders | Names are case-sensitive and use encounter order. A repeated name reuses its first argument, even with another alignment/format suffix. |
| Numeric placeholders | Tokens that parse as nonnegative Int32 indices address arguments directly. After a numeric index, the next new name uses at least the following index. Earlier named assignments stay unchanged. |
| Mixed placeholders | `{Name} / {0} / {Next}` with two arguments renders argument 0 twice, then argument 1. `{1} / {Name} / {0}` with three arguments renders arguments 1, 2, then 0. Named and numeric references can intentionally share an argument. |
| Formatting | Uses invariant culture and standard composite-format alignment/suffixes, including escaped braces. The argument array is copied; objects inside it are not deep-copied. |
| No arguments | Treats the whole message as literal text, including braces. A null message becomes `string.Empty`. |
| Invalid formatting | Missing arguments, unclosed/invalid placeholders, invalid alignment, or unsupported value formats throw `FormatException` when parsing/rendering. No partial entry is written. Arbitrary user formatter exceptions retain their original identity. |
| Extra arguments | Unreferenced arguments are ignored. |
| Scope properties | Applied from outer to inner scope; inner values override outer values. Entry properties are applied last and override scope properties. |
| Metadata keys | `{OriginalFormat}` holds a template; `Scopes` holds outer-to-inner scope strings; `EventName` comes from the event ID. These share the property dictionary with supplied state. Avoid using metadata names as application property names. |

This is a small custom parser, not a replacement for every modern structured logging provider. It does not implement destructuring operators or deep object snapshots.

## In-memory sink notifications

`InMemorySinkPNP.Logs` returns a read-only snapshot. A positive `MaxLogsCount` retains the newest entries; zero means unlimited retention. Reducing the limit trims immediately.

`LogEmitted` runs synchronously after the formatted message is stored, outside the buffer lock. A failing observer is best-effort: its exception is swallowed and the remaining observers still run. Each observer receives separate event data, so changing one event's fields does not rewrite the buffer or another observer's event. Exception objects referenced by event data are shared, not cloned.

Observers may inspect snapshots or write to the sink. They must not dispose the active logger or wait for its shutdown. Direct `Write`/`WriteLine` calls store text without emitting `LogEmitted`. Formatter/listener failures are separate from observer exceptions; the logger does not promise transactional rollback across multiple listeners.
