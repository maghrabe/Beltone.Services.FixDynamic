using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Messaging;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Service.Entities;
using System.Reflection;
using System.Threading;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class Sessions
    {
        #region Variables
        private delegate void IUpdateMessageDelegate(IResponseMessage update);
        //private static IUpdateMessageDelegate m_iUpdateMessageDelegate;
        private static Dictionary<Guid, SessionInfo> _key_Session = null;
        private static Dictionary<string, SessionInfo> _username_Session = null;
        static string todayDateTimeFomat = string.Format("{0}-{1}-{2}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
        private static bool m_ConsiderSysShutdown = bool.Parse(SystemConfigurations.GetAppSetting("ConsiderSysShutdown"));
        private static DateTime m_SysShutdownTime = DateTime.Parse(SystemConfigurations.GetAppSetting("SysShutdownTime"));
        private static int _checkEndOfSessionInMS = int.Parse(SystemConfigurations.GetAppSetting("CheckEndOfSessionInMilliSec"));
        static DateTime _currentDatetTime = DateTime.Now;
        static Thread _updater = null;
        #endregion Variables

        #region Constructor
        public static void Initialize()
        {
            _key_Session = new Dictionary<Guid, SessionInfo>();
            _username_Session = new Dictionary<string, SessionInfo>();
            //m_iUpdateMessageDelegate = new IUpdateMessageDelegate(UpdateClientInternal);
            DateTime dtNow = DateTime.Now;
            FixDbEntities dbContext = new FixDbEntities();
            // remove old sessions
            IQueryable<Session> sessionsToDelete = dbContext.Sessions.Select(s => s);
            foreach (Session s in sessionsToDelete)
                if (s.ConnectionDateTime.Date != DateTime.Now.Date)
                {
                    dbContext.SessionsHistories.Add(CreateHistory(s));
                    dbContext.Sessions.Remove(s);
                }
            dbContext.SaveChanges();

            // remove unsubscribed sessions
            IQueryable<Session> unSubSessions = dbContext.Sessions.Where(s => !s.IsSubscribed);
            foreach (Session s in unSubSessions)
            {
                dbContext.SessionsHistories.Add(CreateHistory(s));
                dbContext.Sessions.Remove(s);
            }
            dbContext.SaveChanges();

            IQueryable<Session> sessions = dbContext.Sessions.Where(s => s.IsSubscribed == true).OrderByDescending(d => d.ConnectionDateTime);
            foreach (Session sub in sessions)
            {
                try
                {
                    // if not today subscriber or there are a double record for same session key
                    if (_key_Session.ContainsKey(sub.SessionKey) || sub.ConnectionDateTime.Date != DateTime.Now.Date)
                    {
                        dbContext.SessionsHistories.Add(CreateHistory(sub));
                        dbContext.Sessions.Remove(sub);
                        continue;
                    }

                    // make sure that you have only one record for each session

                    sub.IsOnline = false;
                    SessionInfo details = new SessionInfo()
                    {
                        SessionKey = sub.SessionKey,
                        Callback = null, // waiting for reactivation
                        FlushUpdatesOffline = sub.FlushUpdatesOffline,
                        IsOnline = false,
                        QueueMachine = sub.QueueIP,
                        QueueName = sub.QueueName,
                        QueuePath = sub.QueuePath,
                        Queue = new MessageQueue(sub.QueuePath)
                    };



                    Login login = dbContext.Logins.SingleOrDefault(l => l.UserName == sub.UserName);
                    if (login == null)
                        continue;
                    details.LoginInfo = new LoginInfo() { Username = login.UserName, CanPlaceOrder = login.CanPlaceOrder, CanReplicate = login.CanReplicate };
                    _key_Session.Add(sub.SessionKey, details);
                    _username_Session.Add(login.UserName, details);
                }
                catch (Exception ex)
                {
                    SystemLogger.LogErrorAsync("Error while retriving subscribers from database: " + ex.ToString());
                }
            }
            dbContext.SaveChanges();

            _updater = new Thread(new ThreadStart(CheckEndOfSession));
            _updater.IsBackground = true;
            _updater.Start();
        }
        #endregion Constructor

        #region Sub / Resub / UnSub

        internal static bool Subscribe(LoginInfo login, Guid clientKey, IFixAdminCallback callback, string queueIP, string queueName, MessageQueue queue, bool pushOfflineUpdates)
        {
            lock (_key_Session)
            {
                SessionInfo details = new SessionInfo() { SessionKey = clientKey, LoginInfo = login, Queue = queue, QueueMachine = queueIP, QueueName = queueName, FlushUpdatesOffline = pushOfflineUpdates, Callback = callback, IsOnline = true, QueuePath = queue.Path };
                _key_Session.Add(clientKey, details);
                _username_Session.Add(login.Username, details);
            }
            return true;

        }

        internal static void DeactivateCallback(Guid clientKey)
        {
            if (!_key_Session.ContainsKey(clientKey))
                return;
            SessionInfo sub = _key_Session[clientKey];
            sub.IsOnline = false;
            sub.Callback = null;
        }

        internal static void Resubscribe(Guid sessionKey, IFixAdminCallback callBack, LoginInfo logInfo, bool flushUpdatesOffline, bool isNewQueue, string newQueueIP = null, string newQueueName = null, MessageQueue queue = null)
        {
            SessionInfo details = _key_Session[sessionKey];
            details.IsOnline = true;
            details.FlushUpdatesOffline = flushUpdatesOffline;
            details.Callback = callBack;
            details.LoginInfo = logInfo;
            if (isNewQueue)
            {
                // no need to validate data as IsNewQueue means that data already valid before passed here
                details.QueueMachine = newQueueIP;
                details.QueueName = newQueueName;
                details.QueuePath = queue.Path;
                details.Queue = null;
                details.Queue = queue;
            }
        }

        internal static void Unsubscribe(Guid sessionKey)
        {
            lock (_key_Session)
            {
                if (!_key_Session.ContainsKey(sessionKey))
                    return;
               var session = _key_Session[sessionKey];
               _username_Session.Remove(session.LoginInfo.Username);
               _key_Session.Remove(sessionKey);
            }
        }

        #endregion Sub / Resub / UnSub

        #region Pushing Updates

        public static void Push(string username, IResponseMessage[] msgs)
        {
            try
            {
                foreach (IResponseMessage update in msgs)
                {
                    try
                    {
                        UpdateClientInternal(username, update);
                        //m_iUpdateMessageDelegate.BeginInvoke(update, new AsyncCallback(OnClientUpdate), update);
                    }
                    catch (Exception inEx)
                    {
                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending msg {0} to ClientKey {1}, Error: {2}", update.GetType(), update.ClientKey, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error PushUpdates Error: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.White, true);
            }
        }

        public static void Push(string username, IFromAdminMsg[] msgs)
        {
            try
            {
                if (!_username_Session.ContainsKey(username))
                    return;
                SessionInfo session = _username_Session[username];
                if (session.Callback != null && session.IsOnline)
                    session.Callback.PushAdminMsg(msgs);
            }
            catch (Exception inEx)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.LogEventAsync(string.Format("Error sending admin msg to session {0}, Error: {1}", username, inEx.Message));
            }
        }

        public static void BroadcastAdminMsg(IFromAdminMsg[] msgs)
        {
            foreach (KeyValuePair<Guid, SessionInfo> callback in _key_Session)
            {
                try
                {
                    IFixAdminCallback cb = callback.Value.Callback;
                    if (cb == null || !callback.Value.IsOnline) // admin msgs dont need to be flushed as offline updates
                        continue;
                    cb.PushAdminMsg(msgs);
                }
                catch (Exception inex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error BroadcastAdminMsg  , Error: {0}", inex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private static void UpdateClientInternal(string username, IResponseMessage update)
        {
            byte trials = 3;
            while (trials > 0)
                try
                {
                    if (Session(username) != null)
                    {
                        SessionInfo user = _username_Session[username];
                        user.Queue.Send(update);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error UpdateClientInternal: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                }
                finally
                {
                    trials--;
                }
        }

        private static void OnClientUpdate(IAsyncResult res)
        {
            try
            {
                //m_iUpdateMessageDelegate.EndInvoke(res);
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error OnClientUpdate: {0}", ex.Message), ConsoleColor.Cyan, ConsoleColor.Black, false);
            }
        }

        #endregion Pushing Updates

        #region Queries

        internal static bool IsSubscribedToSendMsg(string username)
        {
            if (!_username_Session.ContainsKey(username))
                return false;
            if (!_username_Session[username].IsOnline)
                return false;
            //if (!session.IsOnline && !session.FlushUpdatesOffline)
            //    return false;
            return true;
        }

        internal static bool IsOnlineOrHasOfflineUpdates(string username)
        {
            if (!_username_Session.ContainsKey(username))
                return false;
            SessionInfo session = _username_Session[username];
            if (!session.IsOnline && !session.FlushUpdatesOffline)
                return false;
            return true;
        }

        internal static bool IsOnlineUser(string userName)
        {
            return _key_Session.Values.SingleOrDefault(cb => cb.LoginInfo.Username.ToLower() == userName.ToLower() && cb.IsOnline) != null;
        }

        internal static SessionInfo IsSubscribedQueue(string ip, string queueName)
        {
            return _key_Session.Values.SingleOrDefault(cb => cb.QueueMachine == ip && cb.QueueName.ToLower() == queueName.ToLower());
        }

        internal static SessionInfo Session(string username)
        {
            if (!_username_Session.ContainsKey(username))
                return null;
            return _username_Session[username];
        }

        internal static string GetUsername(Guid sessionKey)
        {
            return _key_Session.ContainsKey(sessionKey) ? _key_Session[sessionKey].LoginInfo.Username : null;
        }

        public static Guid GetSessionKey(string username)
        {
            if (!_username_Session.ContainsKey(username))
                return new Guid();
            return _username_Session[username].SessionKey;
        }
        #endregion Queries

        #region Helpers

        static SessionsHistory CreateHistory(Session session)
        {
            SessionsHistory history = new SessionsHistory();
            foreach (PropertyInfo sourcePropertyInfo in session.GetType().GetProperties())
            {
                PropertyInfo destPropertyInfo = history.GetType().GetProperty(sourcePropertyInfo.Name);
                destPropertyInfo.SetValue(history, sourcePropertyInfo.GetValue(session, null), null);
            }
            return history;
        }

        private static void CheckEndOfSession()
        {
            while (true)
            {
                try
                {
                    if (m_ConsiderSysShutdown && (DateTime.Now.TimeOfDay >= m_SysShutdownTime.TimeOfDay))
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "System is shutting down due to configured session end", ConsoleColor.Yellow, ConsoleColor.Red, true);
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, "Error! CheckEndOfSession: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
                }


                if (_currentDatetTime.Date != DateTime.Today.Date)
                {
                    FixExchangesInfo.RefreshActiveSessions();
                    _currentDatetTime = DateTime.Now;
                }


                Thread.Sleep(_checkEndOfSessionInMS);
            }
        }

        #endregion Helpers

        internal static List<SessionInfo> monitor_GetSession(bool onlyOnline = false)
        {
            if (!onlyOnline)
                return _key_Session.Values.ToList();
            else
                return _key_Session.Values.Where(s => s.IsOnline == true).ToList();
        }

        internal static bool monitor_SetSessionInActive(Guid sessionKey)
        {
            lock (_key_Session)
            {
                 DeactivateCallback(sessionKey);
                 return true;
            }
        }

        internal static void monitor_UnsubSession(Guid key)
        {
            lock (_key_Session)
            {
                Unsubscribe(key);
            }
        }
    }
}