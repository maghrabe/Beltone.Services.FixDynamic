using Beltone.Services.ProcessorsRouter.Enums;
using Beltone.Services.ProcessorsRouter.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beltone.Services.ProcessorsRouter.Entities
{
    public class Router
    {
        #region Private Variable
        Dictionary<object, int> _key_processorID;
        List<MsgProcessor> _processors;
        Dictionary<int, MsgProcessor> _processorID_processorObj;
        Dictionary<int, int> _processorID_HandledKeysCount;
        Task _incomingMsgsReader = null;
        ProcessQueue _routerQueue = null;
        IEventLogger _logger = null;
        int _nextprocessorID = 0;
        object _lockObj;
        #endregion Private Variable

        #region Constructors
        public Router(Type processorTyp) : this(processorTyp, 10, null){}
        public Router(Type processorTyp, IEventLogger logger = null) : this(processorTyp, 10, logger) { }
        public Router(Type processorTyp, int processorsNum = 10) : this(processorTyp, processorsNum, null) { }
        public Router(Type processorTyp, int processorsNum = 10, IEventLogger logger = null)
        {
            _lockObj = new object();
            _routerQueue = new ProcessQueue();
            _logger = logger;
            if (processorsNum <= 0)
                processorsNum = 10;
            _key_processorID = new Dictionary<object, int>();
            _processorID_processorObj = new Dictionary<int, MsgProcessor>();
            _processorID_HandledKeysCount = new Dictionary<int, int>();
            _processors = new List<MsgProcessor>();
            _incomingMsgsReader = new Task(() => { RecievingMsgs(); });
            _incomingMsgsReader.Start();
            for (int i = 0; i < processorsNum; i++)
            {
                MsgProcessor q = new MsgProcessor((IMsgProcessor)Activator.CreateInstance(processorTyp), logger);
                _processorID_processorObj.Add(q.ID, q);
                _processorID_HandledKeysCount.Add(q.ID, 0);
                _processors.Add(q);
            }
            _nextprocessorID = _processors[0].ID;
        }
        #endregion Constructors

        #region Published Methods
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
            lock (_lockObj)
                _routerQueue.Enqueue(new object[] { key, msg });
        }
        public void PushMessage(object msg)
        {
            lock (_lockObj)
                _routerQueue.Enqueue(new object[] { null, msg });
        }
        public void ReplaceLogger(IEventLogger logger)
        {
            lock (_lockObj)
            {
                _logger = logger;
                foreach (var p in _processors)
                    p.ReplaceLogger(logger);
            }
        }
        #endregion Published Methods

        #region Private Methods

        void RecievingMsgs()
        {
            while (true)
            {
                try
                {
                    object[] msg = (object[])_routerQueue.Dequeue();
                    if (msg == null || msg.Length == 0)
                    {
                        LogEvent(LOG_TYP.ERROR, "Error while reading msg: Message has no data!");
                        continue;
                    }
                    lock (_lockObj)
                    {

                        Route(msg[0], msg[1]);
                    }
                }
                catch (Exception ex)
                {
                    LogEvent(LOG_TYP.ERROR, "Error while routing msg: {0}", ex.Message);
                }
            }
        }
        void LogEvent(LOG_TYP logTyp, string msg, params string[] args)
        {
            try
            {
                lock (_lockObj)
                {
                    if (_logger == null)
                        return;
                    _logger.LogEvent(LOG_TYP.ERROR, msg, args);
                }
            }
            catch
            { }
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
            lock (_lockObj)
                processor.Push(msgObj);
        }
        MsgProcessor GetProcessor(int id)
        {
            lock (_lockObj)
                return _processorID_processorObj[id];
        }
        void NextProcessor()
        {
            lock (_lockObj)
            {
                int totCount = _processorID_HandledKeysCount.Values.Sum();
                if (totCount == 0)
                    return;
                _nextprocessorID = _processorID_HandledKeysCount.OrderBy(q => q.Value).First().Key;
            }
        }
        #endregion Private Methods

        #region Inner Helper Classes

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
            IEventLogger _logger = null;
            object _lockObj = new object();

            public MsgProcessor(IMsgProcessor msgProcessor, IEventLogger eventLogger)
            {
                lock (_lockObj)
                {
                    _msgProcessor = msgProcessor;
                    _msgProcessor.Initialize();
                    _queue = new ProcessQueue();
                    _worker = new Task(() => { ReadingPushedMsgs(); });
                    _logger = eventLogger;
                    _id = _worker.Id;
                    _worker.Start();
                }
            }

            public void Push(object msg)
            {
                lock (_lockObj)
                    _queue.Enqueue(msg);
            }

            private void ReadingPushedMsgs()
            {
                while (true)
                {
                    try
                    {
                        object msg = _queue.Dequeue();
                        lock (_lockObj)
                            _msgProcessor.Process(msg);
                    }
                    catch (Exception ex)
                    {
                        LogEvent(LOG_TYP.ERROR, "Error reading msg Error: {0}", ex.Message);
                    }
                }
            }

            void LogEvent(LOG_TYP logTyp, string msg, params string[] args)
            {
                try
                {
                    lock (_lockObj)
                    {
                        if (_logger == null)
                            return;
                        _logger.LogEvent(LOG_TYP.ERROR, msg, args);
                    }
                }
                catch
                { }
            }

            internal void ReplaceLogger(IEventLogger logger)
            {
                lock (_lockObj)
                    _logger = logger;
            }
        }

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
        #endregion Inner Helper Classes
    }

}
