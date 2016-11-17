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
    public static class ReplicationSessionSubscribersList
    {
        private delegate void IReplicatedMessageDelegate(IReplicationResponse update);
        private static IReplicatedMessageDelegate m_iReplicatedMessageDelegate;
        private static Dictionary<Guid, ReplicatedSessionSubscriberDetails> m_sessions = null;
        private static Dictionary<string, List<Guid>> m_fixMsgType_SubscribersKeys = null;
        private static int m_msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));
        private static int m_fixMsgVersionTag = int.Parse(SystemConfigurations.GetAppSetting("FixMessageVersionTag"));
        private static object m_LockedObj = new object();
        private static int m_SessionReqResHeartBeatDiffInMilliSec = int.Parse(SystemConfigurations.GetAppSetting("ReplicationSessionReqResHeartBeatDiffInMilliSec"));
        private static int m_SessionsAliveChekerInMilliSec = int.Parse(SystemConfigurations.GetAppSetting("ReplicationSessionAliveChekerInMilliSec"));
        private static System.Timers.Timer m_sessionAliveChecker;

        public static void Initialize()
        {
            m_sessions = new Dictionary<Guid, ReplicatedSessionSubscriberDetails>();
            m_fixMsgType_SubscribersKeys = new Dictionary<string, List<Guid>>();
            m_iReplicatedMessageDelegate = new IReplicatedMessageDelegate(UpdateClientInternal);
            //foreach (KeyValuePair<Guid, string> kvp in OrdersManager.GetAllCallbacksQueues())
            foreach (ReplicatedSessionSubscriberDetails  sub in new DatabaseMethods().GetReplicationSessionsSubscribers())
            {
                sub.SessionRemoteQueue = new MessageQueue(sub.QueuePath);
                DateTime dtNow = DateTime.Now;
                sub.LastHeartBeatRequest = dtNow;
                sub.LastHeartBeatRequest = dtNow;
                m_sessions.Add(sub.SubscriberKey, sub);
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


            m_sessionAliveChecker = new System.Timers.Timer();
            m_sessionAliveChecker.Elapsed += new System.Timers.ElapsedEventHandler(m_callbacksAliveChecker_Elapsed);
            m_sessionAliveChecker.Interval = m_SessionsAliveChekerInMilliSec;
            m_sessionAliveChecker.Start();
        }

        static void m_callbacksAliveChecker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                CheckCallbacksAlive();
            }
            catch (Exception ex)
            {
                if (!m_sessionAliveChecker.Enabled)
                {
                    m_sessionAliveChecker.Start();
                }
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error m_callbacksAliveChecker_Elapsed, Error: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public static void AddSubscriber(Guid clientKey, string[] fixMsgsTypes, string callbackQueue)
        {
            lock (m_sessions)
            {
                if (!m_sessions.ContainsKey(clientKey))
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
                        m_sessions.Add(clientKey, new ReplicatedSessionSubscriberDetails() { SessionRemoteQueue = new MessageQueue(callbackQueue), SubscribedFixMsgsTypes = fixMsgsTypes, LastHeartBeatRequest = dtNow, LastHeartBeatResponse = dtNow, QueuePath = callbackQueue, SubscriberKey = clientKey, SubscriptionDateTime = dtNow });
                        ts.Complete();
                    }
                }
            }
        }

        public static ReplicatedSessionSubscriberDetails GetDetails(Guid clientKey)
        {
            return m_sessions[clientKey];
        }

        public static void PushUpdates(IReplicationResponse[] updateMessages)
        {
            foreach (IReplicationResponse update in updateMessages)
            {
                //GetDetails(update.ClientKey).Callback.Send(update);
                m_iReplicatedMessageDelegate.BeginInvoke(update, new AsyncCallback(OnClientUpdate), update);
            }
        }

        public static void BroadcastUpdates(IReplicationResponse[] updateMessages)
        {
            foreach (IReplicationResponse update in updateMessages)
            {
                foreach (KeyValuePair<Guid, ReplicatedSessionSubscriberDetails> callback in m_sessions)
                {
                    update.ClientKey = callback.Key;
                    m_iReplicatedMessageDelegate.BeginInvoke(update, new AsyncCallback(OnClientUpdate), update);
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
                ReplicatedSessionSubscriberDetails user = null;
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
                m_iReplicatedMessageDelegate.EndInvoke(res);
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error OnClientUpdate: {0}", ex.Message), ConsoleColor.Cyan, ConsoleColor.Black, false);
            }
        }

        private static void CheckCallbacksAlive()
        {
            lock (m_LockedObj)
            {
                List<Guid> callbacksToDelete = new List<Guid>();
                foreach (KeyValuePair<Guid, ReplicatedSessionSubscriberDetails> callback in m_sessions)
                {
                    // remove who didnt response for configured time
                    TimeSpan diff = callback.Value.LastHeartBeatResponse > callback.Value.LastHeartBeatRequest ? callback.Value.LastHeartBeatResponse - callback.Value.LastHeartBeatRequest : callback.Value.LastHeartBeatRequest - callback.Value.LastHeartBeatResponse;
                    if (diff.TotalMilliseconds > m_SessionReqResHeartBeatDiffInMilliSec)
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
                        RemoveCallback(callbackKey);
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

        internal static void RemoveCallback(Guid clientKey)
        {
            lock (m_LockedObj)
            {
                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {

                    DatabaseMethods db = new DatabaseMethods();
                    db.RemoveReplicationSessionSubscriber(clientKey);

                    if (m_sessions.ContainsKey(clientKey))
                    {
                        // remove subscriber from subscribed messages
                        string[] subscribedMsgs = m_sessions[clientKey].SubscribedFixMsgsTypes;
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
                        m_sessions.Remove(clientKey);
                    }
                    ts.Complete();
                }
            }
        }

        internal static MessageQueue GetCallback(Guid clientKey)
        {
            lock (m_LockedObj)
            {
                if (m_sessions.ContainsKey(clientKey))
                {
                    return m_sessions[clientKey].SessionRemoteQueue;
                }
                else
                {
                    return null;
                }
            }
        }

        internal static KeyValuePair<Guid, ReplicatedSessionSubscriberDetails> GetCallback(string queuePath)
        {
            lock (m_LockedObj)
            {
                return m_sessions.SingleOrDefault(b => b.Value.QueuePath == queuePath);
            }
        }

        private static bool IsSubscribed(Guid clientKey, string callbackQueue)
        {
            lock (m_LockedObj)
            {
                if (m_sessions.ContainsKey(clientKey))
                {
                    if (m_sessions[clientKey].QueuePath == callbackQueue)
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
            lock (m_LockedObj)
            {
                return m_sessions.ContainsKey(clientKey);
            }
        }

        internal static Guid HasSubscriberQueue(string callbackQueue)
        {
            lock (m_LockedObj)
            {
                //string[] path = callbackQueue.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);

                MessageQueue subQueue = new MessageQueue(callbackQueue);
                foreach (KeyValuePair<Guid, ReplicatedSessionSubscriberDetails> kvp in m_sessions)
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
            lock (m_LockedObj)
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
                lock (m_LockedObj)
                {
                    string msgTypeString = msg.getHeader().getField(m_msgTypeTag);
                    Guid[] subscribersKeys = GetFixMsgSubscribers(msgTypeString);
                    if (subscribersKeys != null && subscribersKeys.Length > 0)
                    {
                        foreach (Guid key in subscribersKeys)
                        {
                            PushUpdates(new IReplicationResponse[] { new ReplicatedFixMsg() { ClientKey = key, FixMessage = msg.ToString(), ResponseDateTime = DateTime.Now, FixVersion = msg.getHeader().getField(m_fixMsgVersionTag) } });
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
