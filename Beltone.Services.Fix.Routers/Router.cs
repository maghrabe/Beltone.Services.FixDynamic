using Beltone.Services.Fix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.Routers
{
    public interface IMsgProcessor
    {
        void Process(object msg);
    }

    public interface IRouter
    {
        /// <summary>
        /// sending msg with already existed key means that a specific 
        /// processor will handle this message
        /// </summary>
        /// <param name="key">each processor handles set of key's</param>
        /// <param name="msg">a message object to handle</param>
        void PushMessage(object key, object msg);
        /// <summary>
        /// handling message without key means that each message 
        /// will be passed for next available processor
        /// </summary>
        /// <param name="key">each processor handles set of key's</param>
        /// <param name="msg">a message object to handle</param>
        void PushMessage(object msg);
        void RemoveKey(object key);
    }

    public class Router : IRouter
    {
        Dictionary<object, int> _key_processorID;
        List<MsgProcessor> _processors;
        Dictionary<int, MsgProcessor> _processorID_processorObj;
        Dictionary<int, int> _processorID_HandledKeysCount;
        CustomizedQueue _routerQueue;
        Task _incomingMsgsReader = null;
        int _nextprocessorID = 0;
        object _lockObj;

        public Router(IMsgProcessor msgProcessor, int processorNum = 10)
        {
            _lockObj = new object();
            if (processorNum <= 0)
                processorNum = 10;
            _key_processorID = new Dictionary<object, int>();
            _processorID_processorObj = new Dictionary<int, MsgProcessor>();
            _processorID_HandledKeysCount = new Dictionary<int, int>();
            _routerQueue = new CustomizedQueue();
            _processors = new List<MsgProcessor>();
            _incomingMsgsReader = new Task(() => { RecievingMsgs(); });
            _incomingMsgsReader.Start();
            for (int i = 0; i < processorNum; i++)
            {
                MsgProcessor q = new MsgProcessor((IMsgProcessor)Activator.CreateInstance(msgProcessor.GetType()));
                _processorID_processorObj.Add(q.ID, q);
                _processorID_HandledKeysCount.Add(q.ID, 0);
                _processors.Add(q);
            }
            _nextprocessorID = _processors[0].ID;
        }

        public void RemoveKey(object key)
        {
            lock (_lockObj)
            {
                if (!_key_processorID.ContainsKey(key))
                    return;
                int queueID = _key_processorID[key];
                if (_processorID_HandledKeysCount[queueID] > 0)
                    _processorID_HandledKeysCount[queueID]--;
                _key_processorID.Remove(key);
            }
        }

        public void PushMessage(object key, object msg)
        {
            _routerQueue.Enqueue(new object[] { key, msg });
        }
        public void PushMessage(object msg)
        {
            _routerQueue.Enqueue(new object[] { null, msg });
        }

        void RecievingMsgs()
        {
            while (true)
            {
                try
                {
                    object[] msg = (object[])_routerQueue.Dequeue();
                    Route(msg[0], msg[1]);
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, "Reading Routed Msg Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
                }
                finally
                {
                }
            }
        }
        void Route(object key, object msg)
        {
            lock (_lockObj)
            {
                if (key == null)
                {
                    ProcessMsg(key, msg, GetProcessor(_nextprocessorID));
                    NextProcessor();
                }
                else if (!_key_processorID.ContainsKey(key))
                {
                    _key_processorID.Add(key, _nextprocessorID);
                    _processorID_HandledKeysCount[_nextprocessorID]++;
                    ProcessMsg(key, msg, GetProcessor(_nextprocessorID));
                    NextProcessor();
                }
                else
                {
                    int processorID = _key_processorID[key];
                    ProcessMsg(key, msg, GetProcessor(processorID));
                }
            }
        }
        void ProcessMsg(object msgKey, object msgObj, MsgProcessor processor)
        {
            processor.Push(msgObj);
        }
        MsgProcessor GetProcessor(int id)
        {
            return _processorID_processorObj[id];
        }
        void NextProcessor()
        {
            int totCount = _processorID_HandledKeysCount.Values.Sum();
            if (totCount == 0)
                return;
            _nextprocessorID = _processorID_HandledKeysCount.OrderBy(q => q.Value).First().Key;
        }

        /// <summary>
        /// internal class
        /// </summary>
        class MsgProcessor
        {
            private ProcessQueue _queue = null;
            private Task _worker = null;
            private int _id;
            public int ID { get { return _id; } }
            public int UnProcessedMsgsCount { get { return _queue.Count; } }
            IMsgProcessor _msgProcessor;

            public MsgProcessor(IMsgProcessor msgProcessor)
            {
                _msgProcessor = msgProcessor;
                _queue = new ProcessQueue();
                _worker = new Task(() => { ReadingPushedMsgs(); });
                _id = _worker.Id;
                _worker.Start();
            }

            public void Push(object msg)
            {
                _queue.Enqueue(msg);
            }

            private void ReadingPushedMsgs()
            {
                while (true)
                {
                    try
                    {
                        object msg = _queue.Dequeue();
                        _msgProcessor.Process(msg);
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error reading msg Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    finally
                    {

                    }
                }
            }

            /// <summary>
            /// internal class
            /// </summary>
            class ProcessQueue : Queue
            {
                private bool open;

                public ProcessQueue(ICollection col) : base(col) { open = true; }

                public ProcessQueue(int capacity, float increasedBy) : base(capacity, increasedBy) { open = true; }

                public ProcessQueue(int capacity) : base(capacity) { open = true; }

                public ProcessQueue() : base() { open = true; }

                ~ProcessQueue() { Close(); }

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
    }
}
