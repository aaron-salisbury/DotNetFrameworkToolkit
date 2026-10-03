using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetFrameworkToolkit.Core;

// No user code runs under this monitor. Disposal waits for in-flight operations.
internal delegate void DisposalAction();

internal sealed class OperationLifetime
{
    private readonly object _sync = new();
    private readonly Dictionary<int, int> _threads = [];
    private int _active;
    private bool _closing;

    internal IDisposable Enter()
    {
        lock (_sync)
        {
            if (_closing)
            {
                throw new ObjectDisposedException("Service lifetime");
            }

            int id = Thread.CurrentThread.ManagedThreadId;
            _threads.TryGetValue(id, out int count);
            _threads[id] = count + 1;
            _active++;
            return new Lease(this, id);
        }
    }

    internal void Dispose(DisposalAction dispose)
    {
        int id = Thread.CurrentThread.ManagedThreadId;
        lock (_sync)
        {
            if (_threads.ContainsKey(id))
            {
                throw new InvalidOperationException("Cannot dispose a lifetime from one of its active operations.");
            }

            // Repeated concurrent disposal is a no-op, not another blocking dependency.
            if (_closing)
            {
                return;
            }

            _closing = true;

            while (_active != 0)
            {
                Monitor.Wait(_sync);
            }
        }
        dispose();
    }

    private sealed class Lease : IDisposable
    {
        private OperationLifetime _owner;
        private readonly int _id;

        internal Lease(OperationLifetime owner, int id)
        {
            this._owner = owner;
            this._id = id;
        }

        public void Dispose()
        {
            OperationLifetime current = Interlocked.Exchange(ref _owner, null);
            if (current == null)
            {
                return;
            }

            lock (current._sync)
            {
                if (--current._threads[_id] == 0)
                {
                    current._threads.Remove(_id);
                }

                current._active--;
                Monitor.PulseAll(current._sync);
            }
        }
    }
}
