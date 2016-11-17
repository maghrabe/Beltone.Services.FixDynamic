using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Messaging;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class RepSessions
    {
        private delegate void RepMsgDel(IReplicationResponse update);
        private static RepMsgDel _repMsgDel;
        private static Dictionary<Guid, RepSession> _sessions = null;
        private static Dictionary<string, List<Guid>> m_fixMsgType_SubscribersKeys = null;
        private static int _msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));
        private static int _fixMsgVersionTag = int.Parse(SystemConfigurations.GetAppSetting("FixMessageVersionTag"));
        private static object _LockedObj = new object();
        private static int _SessionReqResHeartBeatDiffInMilliSec = int.Parse(SystemConfigurations.GetAppSetting("ReplicationSessionReqResHeartBeatDiffInMilliSec"));
        private static int _SessionsAliveChekerInMilliSec = int.Parse(SystemConfigurations.GetAppSetting("ReplicationSessionAliveChekerInMilliSec"));
        private static System.Timers.Timer _sessionAliveChecker;

        public static void Initialize()
        {
            _sessions = new Dictionary<Guid, RepSession>();
            m_fixMsgType_SubscribersKeys = new Dictionary<string, List<Guid>>();
            _repMsgDel = new RepMsgDel(UpdateClientInternal);
            //foreach (KeyValuePair<Guid, string> kvp in OrdersManager.GetAllCallbacksQueues())
            foreach (RepSession  sub in new DatabaseMethods().GetReplicationSessionsSubscribers())
            {
                sub.SessionRemoteQueue = new MessageQueue(sub.QueuePath);
                DateTime dtNow = DateTime.Now;
                sub.LastHeartBeatRequest = dtNow;
                sub.LastHeartBeatRequest = dtNow;
                _sessions.Add(sub.SubscriberKey, sub);
                foreach (string msgType in sub.SubscribedFixMsgsTypes)
                {
                    if (!m_fixMsgType_SubscribersKeys.ContainsKey(msgType))
                    {
                        m_fixMsgType_SubscribersKeys.Add(msgType, new List<Guid>());
                    }
                    if (!m_fixMsgType_SubscribersKeys[msgType].Contains(sub.SubscriberKey))
                    {
                        m_fixMsgType_SubscribersKeys[msgType].Add(sub.SubscriberKey);
                    }
                }
            }


            _sessionAliveChecker = new System.Timers.Timer();
            _sessionAliveChecker.Elapsed += new System.Timers.ElapsedEventHandler(_callbacksAliveChecker_Elapsed);
            _sessionAliveChecker.Interval = _SessionsAliveChekerInMilliSec;
            _sessionAliveChecker.Start();
        }

        static void _callbacksAliveChecker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                CheckCallbacksAlive();
            }
            catch (Exception ex)
            {
                if (!_sessionAliveChecker.Enabled)
                {
                    _sessionAliveChecker.Start();
                }
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error m_callbacksAliveChecker_Elapsed, Error: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public static void Subscribe(Guid clientKey, string[] fixMsgsTypes, string callbackQueue)
        {
            lock (_sessions)
            {
                if (!_sessions.ContainsKey(clientKey))
                {
                    string msgTypesString = string.Empty;
                    foreach (string msgType in fixMsgsTypes)
                    {
                        msgTypesString += string.Format("{0},", msgType);
                        if (!m_fixMsgType_SubscribersKeys.ContainsKey(msgType))
                        {
                            m_fixMsgType_SubscribersKeys.Add(msgType, new List<Guid>());
                        }
                        if (!m_fixMsgType_SubscribersKeys[msgType].Contains(clientKey))
                        {
                            m_fixMsgType_SubscribersKeys[msgType].Add(clientKey);
                        }
                    }
                    msgTypesString = msgTypesString.Remove(msgTypesString.Length - 1, 1);
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        DateTime dtNow = DateTime.Now;
                        DatabaseMethods db = new DatabaseMethods();
                        db.AddReplicationSessionSubscriber(clientKey, msgTypesString, callbackQueue, dtNow);
                        _sessions.Add(clientKey, new RepSession() { SessionRemoteQueue = new MessageQueue(callbackQueue), SubscribedFixMsgsTypes = fixMsgsTypes, LastHeartBeatRequest = dtNow, LastHeartBeatResponse = dtNow, QueuePath = callbackQueue, SubscriberKey = clientKey, SubscriptionDateTime = dtNow });
                        ts.Complete();
                    }
                }
            }
        }

        public static RepSession GetDetails(Guid clientKey)
        {
            return _sessions[clientKey];
        }

        public static void PushUpdates(IReplicationResponse[] updateMessages)
        {
            foreach (IReplicationResponse update in updateMessages)
            {
                //GetDetails(update.ClientKey).Callback.Send(update);
                _repMsgDel.BeginInvoke(update, new AsyncCallback(OnClientUpdate), update);
            }
        }

        public static void BroadcastUpdates(IReplicationResponse[] updateMessages)
        {
            foreach (IReplicationResponse update in updateMessages)
            {
                foreach (KeyValuePair<Guid, RepSession> callback in _sessions)
                {
                    update.ClientKey = callback.Key;
                    _repMsgDel.BeginInvoke(update, new AsyncCallback(OnClientUpdate), update);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private static void UpdateClientInternal(IReplicationResponse update)
        {
            //for (int i = 0; i < 10; i++)
            //{
            try
            {
                RepSession user = null;
                try
                {
                    user = GetDetails(update.ClientKey);
                }
                catch (Exception inex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error UpdateClientInternal {0} , Error: {1}, Retrying to send, you will get the same error if retrial failed", update.ClientKey.ToString(), inex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                    user = GetDetails(new Guid(update.ClientKey.ToString()));
                }
                user.SessionRemoteQueue.Send(update);
                //break;
                //SystemLogger.WriteOnConsole(true, string.Format("Message sent of type ({0}) to client {1}", update.GetType().ToString(), update.ClientKey), ConsoleColor.Cyan, ConsoleColor.Black, false);
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error UpdateClientInternal: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
            }
            // }
        }

        private static void OnClientUpdate(IAsyncResult res)
        {
            try
            {
                _repMsgDel.EndInvoke(res);
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error OnClientUpdate: {0}", ex.Message), ConsoleColor.Cyan, ConsoleColor.Black, false);
            }
        }

        private static void CheckCallbacksAlive()
        {
            lock (_LockedObj)
            {
                List<Guid> callbacksToDelete = new List<Guid>();
                foreach (KeyValuePair<Guid, RepSession> callback in _sessions)
                {
                    // remove who didnt response for configured time
                    TimeSpan diff = callback.Value.LastHeartBeatResponse > callback.Value.LastHeartBeatRequest ? callback.Value.LastHeartBeatResponse - callback.Value.LastHeartBeatRequest : callback.Value.LastHeartBeatRequest - callback.Value.LastHeartBeatResponse;
                    if (diff.TotalMilliseconds > _SessionReqResHeartBeatDiffInMilliSec)
                    {
                        callbacksToDelete.Add(callback.Key);
                    }

                    // now check the callbacks alive
                    if (!callbacksToDelete.Contains(callback.Key))
                    {
                        try
                        {
                            callback.Value.SessionRemoteQueue.Send(new ReplicationSessionAreYouAlive() { ClientKey = callback.Key, ResponseDateTime = DateTime.Now });
                            callback.Value.LastHeartBeatRequest = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            callback.Value.LastHeartBeatRequest = DateTime.Now;
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending HeartBeat to callback : {0}, Error: {1}", callback.Key, ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                    }
                }

                if (callbacksToDelete.Count > 0)
                {
                    foreach (Guid callbackKey in callbacksToDelete)
                    {
                        Remove(callbackKey);
                        SystemLogger.WriteOnConsoleAsync(true, "replication session removed " + callbackKey.ToString(), ConsoleColor.Magenta, ConsoleColor.Black, false); 
                    }
                }
            }
            //lock (m_callbacks)
            //{
            //    foreach (IETF_Callback callback in m_callbacks.Values.Select(b => b.Callback))
            //    {
            //        ICommunicationObject commObj = callback as ICommunicationObject;
            //        if (commObj != null && (commObj.State == CommunicationState.Closed || commObj.State == CommunicationState.Faulted))
            //        {
            //            // Code to remove callback channel from any internal lists you might have, other processing
            //        }
            //    }
            //}
        }

        internal static void Remove(Guid clientKey)
        {
            lock (_LockedObj)
            {
                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {

                    DatabaseMethods db = new DatabaseMethods();
                    db.RemoveReplicationSessionSubscriber(clientKey);

                    if (_sessions.ContainsKey(clientKey))
                    {
                        // remove subscriber from subscribed messages
                        string[] subscribedMsgs = _sessions[clientKey].SubscribedFixMsgsTypes;
                        foreach (string msg in subscribedMsgs)
                        {
                            if (m_fixMsgType_SubscribersKeys.ContainsKey(msg))
                            {
                                m_fixMsgType_SubscribersKeys[msg].Remove(clientKey);
                                if (m_fixMsgType_SubscribersKeys[msg].Count == 0)
                                {
                                    m_fixMsgType_SubscribersKeys.Remove(msg);
                                }
                            }
                        }
                        // remove subscriiber from subscribers list
                        _sessions.Remove(clientKey);
                    }
                    ts.Complete();
                }
            }
        }

        internal static MessageQueue GetQueue(Guid clientKey)
        {
            lock (_LockedObj)
            {
                if (_sessions.ContainsKey(clientKey))
                {
                    return _sessions[clientKey].SessionRemoteQueue;
                }
                else
                {
                    return null;
                }
            }
        }

        internal static KeyValuePair<Guid, RepSession> GetCallback(string queuePath)
        {
            lock (_LockedObj)
            {
                return _sessions.SingleOrDefault(b => b.Value.QueuePath == queuePath);
            }
        }

        private static bool IsSubscribed(Guid clientKey, string callbackQueue)
        {
            lock (_LockedObj)
            {
                if (_sessions.ContainsKey(clientKey))
                {
                    if (_sessions[clientKey].QueuePath == callbackQueue)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }

        private static bool IsSubscribed(Guid clientKey)
        {
            lock (_LockedObj)
            {
                return _sessions.ContainsKey(clientKey);
            }
        }

        internal static Guid HasSubscriberQueue(string callbackQueue)
        {
            lock (_LockedObj)
            {
                //string[] path = callbackQueue.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);

                MessageQueue subQueue = new MessageQueue(callbackQueue);
                foreach (KeyValuePair<Guid, RepSession> kvp in _sessions)
                {
                    if (kvp.Value.SessionRemoteQueue.MachineName == subQueue.MachineName && kvp.Value.SessionRemoteQueue.QueueName == subQueue.QueueName)
                    {
                        return kvp.Key;
                    }
                }
                return Guid.Empty;


                //return m_sessions.SingleOrDefault(b => b.Value.QueuePath.ToLower() == callbackQueue.ToLower());
            }
        }

        internal static Guid[] GetFixMsgSubscribers(string msgTypeString)
        {
            lock (_LockedObj)
            {
                if (!m_fixMsgType_SubscribersKeys.ContainsKey(msgTypeString))
                {
                    return null;
                }
                return m_fixMsgType_SubscribersKeys[msgTypeString].ToArray();
            }

        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        internal static void ReplicateFixMessage(QuickFix.Message msg)
        {
            try
            {
                lock (_LockedObj)
                {
                    string msgTypeString = msg.getHeader().getField(_msgTypeTag);
                    Guid[] subscribersKeys = GetFixMsgSubscribers(msgTypeString);
                    if (subscribersKeys != null && subscribersKeys.Length > 0)
                    {
                        foreach (Guid key in subscribersKeys)
                        {
                            PushUpdates(new IReplicationResponse[] { new ReplicatedFixMsg() { ClientKey = key, FixMessage = msg.ToString(), ResponseDateTime = DateTime.Now, FixVersion = msg.getHeader().getField(_fixMsgVersionTag) } });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("ReplicateFixMessage, Error: {0} ", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        internal static void HandleSubscriberRequest(IReplicationRequest req)
        {
            try
            {
                Type msgType = req.GetType();
                if (typeof(IReplicationRequest).IsAssignableFrom(msgType))
                {
                    if (typeof(ReplicationSessionAmAlive) == msgType)
                    {
                        ReplicationSessionAmAlive alive = (ReplicationSessionAmAlive)req;
                        GetDetails(alive.ClientKey).LastHeartBeatResponse = DateTime.Now;
                        //SystemLogger.WriteOnConsole(true, string.Format("Client {0} is alive @ {1} el7amdolelah!", alive.ClientKey, DateTime.Now), ConsoleColor.Gray, ConsoleColor.Black, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error setting client LastHeartBeatResponse, Error: {0} ", ex.Message), ConsoleColor.Cyan, ConsoleColor.Black, true);
            }
        }
    }

}
