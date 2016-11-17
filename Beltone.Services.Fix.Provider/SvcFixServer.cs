using Beltone.Services.Fix.Utilities;
using QuickFix;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Provider
{
    public class SvcFixServer : QuickFix.Application
    {
        private static object _lockObj = new object();
        private static SocketAcceptor _acceptor = null;
        private static Dictionary<string, string> _targetCompID_SessionCode = null;
        //private static Dictionary<string, string> _sessionCode_targetCompID = null;
        private static Dictionary<string, SessionInfo> _sessionCode_sessionDetails = null;
        private static Dictionary<string, string> _sessionID_sessionCode = null;
        private static List<string> _sessionsCodesToRep = null;
        //private static Dictionary<string, List<string>> _sessionCode_MsgKeys = null;
        //private static Dictionary<string, string> _mapKey_msgKey = null;M

        private static string _msgSplit = SystemConfigurations.GetAppSetting("MsgSplit");
        
        private static string _fixPrefix = string.Format("{0}{1}", SystemConfigurations.GetAppSetting("FixMsgPrefix"), _msgSplit);

        class SessionInfo
        {
            public SessionID Session { get; set; }
            public string TargetCode { get; set; }
        }

        public static void InitializeServer(string settingsPath)
        {
            try
            {
                _targetCompID_SessionCode = new Dictionary<string, string>();
                _sessionCode_sessionDetails = new Dictionary<string, SessionInfo>();
                _sessionID_sessionCode = new Dictionary<string, string>();
                _sessionsCodesToRep = new List<string>();

                using (DataLayer.DatabaseMethods db = new DataLayer.DatabaseMethods())
                {
                    foreach (DataRow row in db.GetTable("FixSessionCode").Rows)
                    {
                        string targetCompID = row["TargetCompID"].ToString();
                        string code = row["Code"].ToString();
                        bool isRep = Convert.ToBoolean(row["IsRep"]);
                        _targetCompID_SessionCode.Add(targetCompID, code);
                        if (isRep)
                            _sessionsCodesToRep.Add(code);
                    }
                }
                SessionSettings settings = new SessionSettings(settingsPath);
                _acceptor = new SocketAcceptor(new SvcFixServer(), new FileStoreFactory(settings), settings, new FileLogFactory(settings), new DefaultMessageFactory());
                _acceptor.start();
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Fix Server Started Up !"), ConsoleColor.Green, ConsoleColor.Black, false);
            }
            catch (Exception e)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("InitializeServer Error, Error {0}", e.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        private static bool IsExistedSession(SessionID sessionID)
        {
            return Session.doesSessionExist(sessionID);
        }

        internal static void Replicate(Message message, string exceptSessionCode = null)
        {
            List<string> repSessionCodes = null;
            if(exceptSessionCode != null)
                repSessionCodes = _sessionsCodesToRep.Where(y => y != exceptSessionCode).ToList();
            else
                repSessionCodes = _sessionsCodesToRep.ToList();// new instance

            repSessionCodes.ForEach(code =>
            {
                SessionID session = _sessionCode_sessionDetails[code].Session;
                message.removeField(49);
                message.removeField(56);
                message.removeField(34);
                message.removeField(10);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Replicating Fix Message To Client {0}", session.ToString()), ConsoleColor.Blue, ConsoleColor.White, false);
                Session.sendToTarget(message, session);
            });
            repSessionCodes.Clear();
            repSessionCodes = null;
        }

        internal static void RouteFixMessageToClient(string sessionCode, string ordID, Message message)
        {
            try
            {
                // get fix client session by registered ClientAllocID
                // overwrite sender and target with clients data
                // overwrite ClientAllocId (Tag 11)
                // route to client
                lock (_lockObj)
                {
                    if (!_sessionCode_sessionDetails.ContainsKey(sessionCode))
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Error routing fix msg! Session {0} not found!", sessionCode), ConsoleColor.Red, ConsoleColor.White, false);
                        return;
                    }

                    // get rep session except the session that need it as a reply
                    SessionID session = _sessionCode_sessionDetails[sessionCode].Session;
                    message.removeField(49);
                    message.removeField(56);
                    message.removeField(34);
                    message.removeField(10);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Routing Fix Message To Client {0}", session.ToString()), ConsoleColor.Blue, ConsoleColor.White, false);
                    Session.sendToTarget(message, session);
                    Replicate(message, sessionCode);
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error routing fix msg ClientAllocID {0} : {1}", ordID, ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public void onCreate(SessionID sessionID)
        {
            SystemLogger.WriteOnConsoleAsync(true, string.Format("new sessionID: {0} ", sessionID.ToString()), ConsoleColor.Cyan, ConsoleColor.Black, false);
            lock (_lockObj)
            {
                if (!_sessionCode_sessionDetails.ContainsKey(sessionID.ToString()))
                {
                    if (!_targetCompID_SessionCode.ContainsKey(sessionID.getTargetCompID()))
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("not added sessionID: {0} because not found in database", sessionID.ToString()), ConsoleColor.White, ConsoleColor.Red, false);
                        return;
                    }
                    _sessionCode_sessionDetails.Add(_targetCompID_SessionCode[sessionID.getTargetCompID()], new SessionInfo() { Session = sessionID, TargetCode = sessionID.getTargetCompID() });
                    _sessionID_sessionCode.Add(sessionID.ToString(), _targetCompID_SessionCode[sessionID.getTargetCompID()]);
                    //SystemLogger.WriteOnConsoleAsync(true, string.Format("new added sessionID: {0} ", sessionID.ToString()), ConsoleColor.Cyan, ConsoleColor.Black, false);
                }
            }
        }

        public void onLogon(SessionID sessionID)
        {

            lock (_lockObj)
            {
                if (!_targetCompID_SessionCode.ContainsKey(sessionID.getTargetCompID()))
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("kicked sessionID: {0} because not found in database", sessionID.ToString()), ConsoleColor.White, ConsoleColor.Red, false);
                    Session.lookupSession(sessionID).logout();
                    return;
                }

                //sessionID.getSenderCompID();
                //FixSessions.Add(sessionID.ToString(), sessionID);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("loggedin sessionID: {0} ", sessionID.ToString()), ConsoleColor.DarkMagenta, ConsoleColor.White, false);
            }
            // test send
            //SendFixMessageToAllConnections(new QuickFix44.ExecutionReport(new OrderID("324"),new ExecID("234"), new ExecType(ExecType.FILL), new OrdStatus(OrdStatus.FILLED), new Side(Side.BUY), new LeavesQty(324), new CumQty(123), new AvgPx(12)));
        }

        public void onLogout(SessionID sessionID)
        {
            lock (_lockObj)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///// you should keep sending the messages even if the initiator logged out, so quickfix will send the previous message in case re-logged in
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //m_sessionIDs.Remove(sessionID);
                //SystemLogger.WriteOnConsoleAsync(true, string.Format("removed sessionID: {0} ", sessionID.ToString()), ConsoleColor.Magenta, ConsoleColor.Black, false);
            }

            SystemLogger.WriteOnConsoleAsync(true, string.Format("loggedout sessionID: {0} ", sessionID.ToString()), ConsoleColor.DarkYellow, ConsoleColor.White, false);
        }

        public void toAdmin(Message message, SessionID sessionID)
        {
        }

        public void toApp(Message message, SessionID sessionID)
        {
        }

        public void fromAdmin(Message message, SessionID sessionID)
        {
        }

        public void fromApp(Message message, SessionID sessionID)
        {
            //SystemLogger.WriteOnConsoleAsync(true, string.Format("fromApp: {0} ", message.ToString()), ConsoleColor.Green, ConsoleColor.Black, false);
            //SystemLogger.LogEventAsync(string.Format("fromApp: {0} ", message.ToString()));

            //crack(message, sessionID);

            // extract clientAllocID
            // check max char length
            // prefix clientAllocID with "F" stand for "Fix Connection"
            // add to current cache
            // update db
            // send to MCDR
            try
            {
                lock (_lockObj)
                {
                    if (message.isSetField(11))
                    {
                        string target = sessionID.getTargetCompID();
                        if (!_targetCompID_SessionCode.ContainsKey(target))
                        {
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("Error recieveing fix msg from Target {0} Not FOund! : ", target), ConsoleColor.White, ConsoleColor.Red, true);
                            return;
                        }
                        string msgKey = message.getField(11);
                        string sessionCode = _targetCompID_SessionCode[target];
                        string mapKey = string.Format("{0}{1}{2}{3}", _fixPrefix, sessionCode, _msgSplit, msgKey);

                        message.removeField(11);
                        message.setField(11, mapKey);

                        MarketFixClient.SendDirectFix(message);
                    }
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "Error recieveing fix msg from Target : " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }
    }
}
