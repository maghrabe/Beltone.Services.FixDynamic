using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;

namespace Beltone.Services.Fix.Utilities
{
    public class CustomizedQueue : Queue
    {
        private bool open;

        public CustomizedQueue(ICollection col) : base(col) { open = true; }

        public CustomizedQueue(int capacity, float increasedBy) : base(capacity, increasedBy) { open = true; }

        public CustomizedQueue(int capacity) : base(capacity) { open = true; }

        public CustomizedQueue() : base() { open = true; }

        ~CustomizedQueue() { Close(); }

        public override void Clear() { lock (base.SyncRoot) { base.Clear(); } }

        public void Close()
        {
            lock (base.SyncRoot)
            {
                open = false;
                base.Clear();
                Monitor.PulseAll(base.SyncRoot);    // resume any waiting threads
            }
        }

        public override object Dequeue() { return Dequeue(Timeout.Infinite); }

        public object Dequeue(TimeSpan timeout) { return Dequeue(timeout.Milliseconds); }

        public object Dequeue(int timeout)
        {
            lock (base.SyncRoot)
            {
                while (open && (base.Count == 0))
                {
                    if (!Monitor.Wait(base.SyncRoot, timeout))
                        throw new InvalidOperationException("Timeout");
                }
                if (open)
                    return base.Dequeue();
                else
                    throw new InvalidOperationException("Queue Closed");
            }

        }

        public override void Enqueue(object obj)
        {
            lock (base.SyncRoot)
            {
                base.Enqueue(obj);
                Monitor.Pulse(base.SyncRoot);
            }
        }

        public void Open() { lock (base.SyncRoot) { open = true; } }

        public bool Closed { get { return !open; } }
    }

}