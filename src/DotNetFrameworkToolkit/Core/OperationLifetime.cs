using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetFrameworkToolkit.Core;

// No user code runs under this monitor. Disposal waits for in-flight operations.
internal delegate void DisposalAction();

internal sealed class OperationLifetime
{
    private readonly object sync = new();
    private readonly Dictionary<int, int> threads = new();
    private int active, disposingThread;
    private bool closing, closed;
    internal IDisposable Enter()
    {
        lock (sync)
        {
            if (closing) throw new ObjectDisposedException("Service lifetime");
            int id = Thread.CurrentThread.ManagedThreadId;
            threads.TryGetValue(id, out int count); threads[id] = count + 1; active++;
            return new Lease(this, id);
        }
    }
    internal void Dispose(DisposalAction dispose)
    {
        int id = Thread.CurrentThread.ManagedThreadId;
        lock (sync)
        {
            if (threads.ContainsKey(id)) throw new InvalidOperationException("Cannot dispose a lifetime from one of its active operations.");
            if (closing)
            {
                if (disposingThread == id) return;
                while (!closed) Monitor.Wait(sync);
                return;
            }
            closing = true; disposingThread = id;
            while (active != 0) Monitor.Wait(sync);
        }
        try { dispose(); }
        finally { lock (sync) { closed = true; Monitor.PulseAll(sync); } }
    }
    private sealed class Lease : IDisposable
    {
        private OperationLifetime owner;
        private readonly int id;
        internal Lease(OperationLifetime owner, int id) { this.owner = owner; this.id = id; }
        public void Dispose()
        {
            OperationLifetime current = Interlocked.Exchange(ref owner, null);
            if (current == null) return;
            lock (current.sync)
            {
                if (--current.threads[id] == 0) current.threads.Remove(id);
                current.active--; Monitor.PulseAll(current.sync);
            }
        }
    }
}
