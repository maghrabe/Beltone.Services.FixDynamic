using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Contract.Entities.FromAdminMsgs;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Contract.Entities.ToAdminMsgs;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Constants;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Provider;
using Beltone.Services.Fix.Service.Entities;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FixAdminSvc : IFixAdmin
    {
        #region Static Variables
        static string todayDateTimeFomat = string.Format("{0}-{1}-{2}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
        static int _allowableAccessRetry = int.Parse(SystemConfigurations.GetAppSetting("AccessRetry"));
        static int _awaitTestResInMS = int.Parse(SystemConfigurations.GetAppSetting("AwaitTestResInMS"));
        static object _lockObj = new object();
        #endregion Static Variables
        
        #region Session Variables
        IFixAdminCallback _callBack = null;
        Guid _sessionKey = Guid.NewGuid();
        string _testKey = Guid.NewGuid().ToString();
        FixDbEntities _dbContext = new FixDbEntities();
        string _requestType = "Subscribe";
        Guid _resubscribedSessionKey = Guid.Empty;
        bool _isAuth = false;
        bool _isAwaitTestRes = false;
        bool _isSessionKilledBySvc = false;
        bool _isSessionTerminated = false;
        System.Diagnostics.Stopwatch _swAwaitTestKeyRes = new System.Diagnostics.Stopwatch();
        LoginInfo _loginInfo = null;
        Session _session = null;
        MessageQueue _queue = null;
        int _AccessRetry = 0;
        #endregion Session Variables

        #region Constructors
        public FixAdminSvc()
        {
            try
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Client {0} Opened Session", _sessionKey), ConsoleColor.Cyan, ConsoleColor.Black, false);

                // create session callback
                _callBack = OperationContext.Current.GetCallbackChannel<IFixAdminCallback>();
                OperationContext.Current.Channel.Faulted += new EventHandler(Channel_Faulted);
                OperationContext.Current.Channel.Closed += new EventHandler(Channel_Closed);

                _session = _dbContext.Sessions.Add(new Session()
                {
                    SessionKey = _sessionKey,
                    RequestType = "NewSession",
                    ConnectionDateTime = DateTime.Now,
                    IsSubscribed = false,
                    IsTestResponded = false,
                    IsTestRequested = false,
                    IsValidTestKey = false, 
                    IsOnline = false,
                    IsUnsubscribed = false,
                    FlushUpdatesOffline = true,
                    NewQueue = false, 
                    IsSessionFaulted  = false,
                    RequestedSubscription = false
                });
                try
                {
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "client session initialization error : " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                KillSession(GeneralCodeConstants.OperationFailed, "Kindly contact system administrator");
            }
        }
        #endregion Constructors

        #region IFixAdmin methods

        public FixAdminMsg Subscribe(subReq loginRequest)
        {
            if(_isSessionTerminated)
                return ReturnMsg(GeneralCodeConstants.SessionNotFound, "Session Terminated!");

            try
            {
                // static locking object to make sure that only one instance can subscribe at a time
                lock (_lockObj)
                {
                    //try
                    //{
                    if (_session.IsSubscribed)
                    {
                        return ReturnMsg(GeneralCodeConstants.AlreadyOnlineUser, "You are already registered and online!");
                    }

                    if (_isAwaitTestRes && !_session.IsTestResponded)
                    {
                        return ReturnMsg(GeneralCodeConstants.AwaitingTestKey, "Waiting for test key response");
                    }

                    _AccessRetry++;

                    if (loginRequest == null)
                        return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData);

                    _session.RequestedSubscription = true;
                    _session.RequestType = "Subscribe";
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    _requestType = "Subscribe";
                    //1- client send sub request
                    //2- svc sends test request to make sure queue existance and awaits for test response for specific time, ex: 5 sec
                    //3- client replies with test response
                    //4- svc creates session and sends subscription key & initilization info
                    //5- client starts sending requests
                    // if client didn't respond for test request within configured period, session will be terminated
                    // dont add key to callback unless client send valid test response
                    #region Authentication

                    // check authentication
                    if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
                    {
                        _session.ErrMsg = "Bad Data : Username/Password";
                        _session.UserName = loginRequest.Username;
                        _session.Password = loginRequest.Password;
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    loginRequest.Username = loginRequest.Username.Trim().ToLower();
                    loginRequest.Password = loginRequest.Password.Trim();

                    if (loginRequest.Username.Length > 20 || loginRequest.Password.Length > 20)
                    {
                        _session.ErrMsg = "Bad Data : Username/Password Exceeded max .allowable length '20 char'";
                        _session.UserName = loginRequest.Username;
                        _session.Password = loginRequest.Password;
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        //KillSession(AdminMsgTyp.ReqBadData, "Bad Provided Data.");
                        return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    // if already online user
                    if (Sessions.IsOnlineUser(loginRequest.Username))
                    {
                        _session.UserName = loginRequest.Username;
                        _session.Password = loginRequest.Password;
                        _session.ErrMsg = "Already Online User! Session has been terminated.";
                        _session.ErrCode = GeneralCodeConstants.AlreadyOnlineUser;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updating db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.AlreadyOnlineUser, _session.ErrMsg);
                    }

                    Login dbLogin = _dbContext.Logins.SingleOrDefault(l => l.UserName == loginRequest.Username && l.Password == loginRequest.Password);
                    if (dbLogin == null)
                    {
                        _session.UserName = loginRequest.Username;
                        _session.Password = loginRequest.Password;
                        _session.ErrMsg = "Access Denied";
                        _session.ErrCode = GeneralCodeConstants.AccessDenied;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updating db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AccessDenied, "Access Denied");
                    }

                    // authenticated
                    _session.UserName = loginRequest.Username;
                    _session.Password = loginRequest.Password;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    _isAuth = true;
                    _loginInfo = new LoginInfo() { Username = dbLogin.UserName, CanPlaceOrder = dbLogin.CanPlaceOrder, CanReplicate = dbLogin.CanReplicate };

                    #endregion Authentication

                    if (string.IsNullOrEmpty(loginRequest.QueueIP) || string.IsNullOrEmpty(loginRequest.QueueName))
                    {
                        //SystemLogger.WriteOnConsoleAsync(true, "SubscribeSession Error: Invalid Queue Path (Null or Empty)", ConsoleColor.Red, ConsoleColor.Black, true);
                        _session.ErrMsg = "Invalid Queue Data";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        _session.QueueIP = loginRequest.QueueIP;
                        _session.QueueName = loginRequest.QueueName;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    loginRequest.QueueName = loginRequest.QueueName.Trim().ToLower();

                    if (loginRequest.QueueName.Length > 50)
                    {
                        _session.ErrMsg = "QueueName exceeded max. allowable length";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    // only ip or .(localhost) are allowed
                    if (loginRequest.QueueIP != ".")
                    {
                        IPAddress ipAdd;

                        if (!IPAddress.TryParse(loginRequest.QueueIP, out ipAdd))
                        {
                            _session.ErrMsg = "Invalid IP, only valid ip 'xxx.xxx.xxx.xxx' or '.' are allowed";
                            _session.ErrCode = GeneralCodeConstants.ReqBadData;
                            _session.QueueIP = loginRequest.QueueIP;
                            _session.QueueName = loginRequest.QueueName;
                            try
                            {
                                _dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                            }
                            KillSession(GeneralCodeConstants.ReqBadData, "Invalid IP");
                        }
                    }

                    string callbackQueuePath = string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", loginRequest.QueueIP, loginRequest.QueueName);
                    _session.QueueIP = loginRequest.QueueIP;
                    _session.QueueName = loginRequest.QueueName;
                    _session.QueuePath = callbackQueuePath;
                    _session.FlushUpdatesOffline = loginRequest.FlushUpdatesOffline;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }

                    if (Sessions.IsSubscribedQueue(loginRequest.QueueIP, loginRequest.QueueName) != null)
                    {
                        _session.ErrMsg = "Already subscribed queue";
                        _session.ErrCode = GeneralCodeConstants.AlreadyRegisteredQueue;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.AlreadyRegisteredQueue, "Queue info is already subscribed for another session, try 'Resubscribe' method");
                    }

                    try
                    {
                        _queue = new MessageQueue(callbackQueuePath);
                        _queue.Send(new FixAdmin_TestRequest() { TestKey = _testKey });
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("error sending msg for client queue {0} : {1}", _sessionKey, ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                        KillSession(GeneralCodeConstants.QueueNotWorking, "Service cannot reach provided queue");
                    }
                    _session.IsTestRequested = true;
                    _session.TestReqDateTime = DateTime.Now;
                    _session.ReqTestKey = _testKey;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    _isAwaitTestRes = true;
                    _swAwaitTestKeyRes.Start();
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AwaitingTestKey, "test key sent to validate the provided queue.");
                }// end lock
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "client session subscription error : " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                KillSession(GeneralCodeConstants.OperationFailed, "Kindly contact system administrator");
                return ReturnMsg(GeneralCodeConstants.OperationFailed, "Kindly contact system administrator");
            }
        }

        public FixAdminMsg Resubscribe_(ResupReq reSubReq)
        {
            if (_isSessionTerminated)
                return ReturnMsg(GeneralCodeConstants.SessionNotFound, "Session Terminated!");

            // static locking object to make sure that only one instance can subscribe at a time
            // if same queue then just re activate session if not then send test key
            lock (_lockObj)
            {
                //try
                //{
                if (_session.IsSubscribed)
                {
                    return ReturnMsg(GeneralCodeConstants.AlreadyOnlineUser, "You are already registered and online!");
                }

                if (_isAwaitTestRes && !_session.IsTestResponded)
                {
                    return ReturnMsg(GeneralCodeConstants.AwaitingTestKey, "Waiting for test key response");
                }

                _AccessRetry++;

                if (reSubReq == null)
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData);

                _session.RequestType = "Resubscribe";
                _session.RequestedSubscription = true;
                _session.NewQueue = reSubReq.NewQueue;
                _session.ResubSessionKey = reSubReq.SessionKey;
                try
                {
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }

                _requestType = "Resubscribe";


                #region Session Existance
                if (reSubReq.SessionKey == Guid.NewGuid())
                    KillSession(GeneralCodeConstants.BadSessionKey);



                SessionInfo subDetails = Sessions.Session(_loginInfo.Username);
                if (subDetails == null)
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Resubscribe : session not found for key " + reSubReq.SessionKey.ToString();
                    _session.ErrCode = GeneralCodeConstants.SessionNotFound;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.SessionNotFound);
                }



                #endregion Session Existance

                #region Authentication
                // check authentication
                if (reSubReq == null || string.IsNullOrEmpty(reSubReq.Username) || string.IsNullOrEmpty(reSubReq.Password))
                {
                    _session.ErrMsg = "Bad Data : Username/Password";
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrCode = GeneralCodeConstants.ReqBadData;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                }
                // trim and lower case
                reSubReq.Username = reSubReq.Username.Trim().ToLower();
                reSubReq.Password = reSubReq.Password.Trim();

                if (reSubReq.Username.Length > 20 || reSubReq.Password.Length > 20)
                {
                    _session.ErrMsg = "Bad Data : Username/Password Exceeded max .allowable length '20 char'";
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrCode = GeneralCodeConstants.ReqBadData;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    //KillSession(AdminMsgTyp.ReqBadData, "Bad Provided Data.");
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                }

                // if already online user
                if (Sessions.IsOnlineUser(reSubReq.Username))
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Already Online User! Session has been terminated.";
                    _session.ErrCode = GeneralCodeConstants.AlreadyOnlineUser;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.AlreadyOnlineUser, _session.ErrMsg);
                }

                Login dbLogin = _dbContext.Logins.SingleOrDefault(l => l.UserName == reSubReq.Username && l.Password == reSubReq.Password);
                if (dbLogin == null)
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Resubscribe : Access Denied";
                    _session.ErrCode = GeneralCodeConstants.AccessDenied;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AccessDenied, "Access Denied");
                }
                // authenticated
                _session.UserName = reSubReq.Username;
                _session.Password = reSubReq.Password;
                try
                {
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }
                _isAuth = true;
                _loginInfo = new LoginInfo() { Username = dbLogin.UserName, CanPlaceOrder = dbLogin.CanPlaceOrder, CanReplicate = dbLogin.CanReplicate };
                #endregion Authentication

                if (!reSubReq.NewQueue)
                {
                    // first : remove old session before changing current session key to the old one as database wont accept this
                    Session oldSessionRecord = _dbContext.Sessions.SingleOrDefault(s => s.SessionKey == reSubReq.SessionKey);
                    _dbContext.SessionsHistories.Add(CreateHistory(oldSessionRecord));
                    _dbContext.Sessions.Remove(oldSessionRecord);
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    // now update current session key with old one
                    string callbackQueuePath = string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", subDetails.QueueMachine, subDetails.QueueName);
                    subDetails.IsOnline = true;
                    _sessionKey = reSubReq.SessionKey;
                    _session.SessionKey = reSubReq.SessionKey;
                    _session.IsOnline = true;
                    _session.QueueIP = subDetails.QueueMachine;
                    _session.QueueName = subDetails.QueueName;
                    _session.QueuePath = callbackQueuePath;
                    _session.Note = string.Format("client resubscribed session {0}", _session.SessionKey);
                    _queue = subDetails.Queue;
                    _session.IsSubscribed = true;
                    _session.SubscriptionDateTime = DateTime.Now;
                    _session.SubscriptionDateTimeString = todayDateTimeFomat;
                    _session.FlushUpdatesOffline = reSubReq.FlushUpdatesOffline;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    Sessions.Resubscribe(_sessionKey, _callBack, _loginInfo, reSubReq.FlushUpdatesOffline, false);
                    _AccessRetry = 0;
                    OperationContext.Current.OperationCompleted+=ResubConStatus_OperationCompleted;
                    return ReturnMsg(GeneralCodeConstants.Subscribed);
                }
                else // if new queue
                {
                    if (string.IsNullOrEmpty(reSubReq.QueueIP) || string.IsNullOrEmpty(reSubReq.QueueName))
                    {
                        //SystemLogger.WriteOnConsoleAsync(true, "SubscribeSession Error: Invalid Queue Path (Null or Empty)", ConsoleColor.Red, ConsoleColor.Black, true);
                        _session.ErrMsg = "Invalid Queue Data";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        _session.QueueIP = reSubReq.QueueIP;
                        _session.QueueName = reSubReq.QueueName;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    reSubReq.QueueName = reSubReq.QueueName.Trim().ToLower();

                    if (reSubReq.QueueName.Length > 50)
                    {
                        _session.ErrMsg = "QueueName exceeded max. allowable length";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    // only ip or .(localhost) are allowed
                    if (reSubReq.QueueIP != ".")
                    {
                        IPAddress ipAdd;

                        if (!IPAddress.TryParse(reSubReq.QueueIP, out ipAdd))
                        {
                            _session.ErrMsg = "Invalid IP";
                            _session.ErrCode = GeneralCodeConstants.ReqBadData;
                            _session.QueueIP = reSubReq.QueueIP;
                            _session.QueueName = reSubReq.QueueName;
                            try
                            {
                                _dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                            }
                            KillSession(GeneralCodeConstants.ReqBadData, "Invalid IP");
                        }
                    }

                    string callbackQueuePath = string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", reSubReq.QueueIP, reSubReq.QueueName);
                    _session.QueueIP = reSubReq.QueueIP;
                    _session.QueueName = reSubReq.QueueName;
                    _session.QueuePath = callbackQueuePath;
                    _session.FlushUpdatesOffline = reSubReq.FlushUpdatesOffline;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }


                    if (Sessions.IsSubscribedQueue(reSubReq.QueueIP, reSubReq.QueueName) != null)
                    {
                        _session.ErrMsg = "Already subscribed queue";
                        _session.ErrCode = GeneralCodeConstants.AlreadyRegisteredQueue;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.AlreadyRegisteredQueue, "Queue info is already subscribed by another session");
                    }


                    try
                    {
                        _queue = new MessageQueue(callbackQueuePath);
                        //Task.Factory.StartNew(() => { _queue.Send(new FixAdmin_TestRequest() { TestKey = _testKey }); });
                        _queue.Send(new FixAdmin_TestRequest() { TestKey = _testKey });
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("error sending msg for client queue {0} : {1}", _sessionKey, ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                        KillSession(GeneralCodeConstants.QueueNotWorking, "Service cannot reach provided queue");
                    }

                    _resubscribedSessionKey = reSubReq.SessionKey;
                    _session.IsTestRequested = true;
                    _session.TestReqDateTime = DateTime.Now;
                    _session.ReqTestKey = _testKey;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    _isAwaitTestRes = true;
                    _swAwaitTestKeyRes.Start();

                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AwaitingTestKey, "test key sent to validate the provided queue.");
                }
            }// end lock
        }

        public FixAdminMsg Resubscribe(ResupReq reSubReq)
        {
            if (_isSessionTerminated)
                return ReturnMsg(GeneralCodeConstants.SessionNotFound, "Session Terminated!");

            // static locking object to make sure that only one instance can subscribe at a time
            // if same queue then just re activate session if not then send test key
            lock (_lockObj)
            {
                //try
                //{
                if (_session.IsSubscribed)
                {
                    return ReturnMsg(GeneralCodeConstants.AlreadyOnlineUser, "You are already registered and online!");
                }

                if (_isAwaitTestRes && !_session.IsTestResponded)
                {
                    return ReturnMsg(GeneralCodeConstants.AwaitingTestKey, "Waiting for test key response");
                }

                _AccessRetry++;

                if (reSubReq == null)
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData);

                _session.RequestType = "Resubscribe";
                _session.RequestedSubscription = true;
                _session.NewQueue = reSubReq.NewQueue;
                _session.ResubSessionKey = reSubReq.SessionKey;
                try
                {
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }

                _requestType = "Resubscribe";


                #region Session Existance
                if (reSubReq.SessionKey == Guid.NewGuid())
                    KillSession(GeneralCodeConstants.BadSessionKey);

                // maghrabe
                Login _dbLogin = _dbContext.Logins.SingleOrDefault(l => l.UserName == reSubReq.Username && l.Password == reSubReq.Password);
                _loginInfo = new LoginInfo() { Username = _dbLogin.UserName, CanPlaceOrder = _dbLogin.CanPlaceOrder, CanReplicate = _dbLogin.CanReplicate };


                SessionInfo subDetails = Sessions.Session(_loginInfo.Username);
                if (subDetails == null)
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Resubscribe : session not found for key " + reSubReq.SessionKey.ToString();
                    _session.ErrCode = GeneralCodeConstants.SessionNotFound;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.SessionNotFound);
                }



                #endregion Session Existance

                #region Authentication
                // check authentication
                if (reSubReq == null || string.IsNullOrEmpty(reSubReq.Username) || string.IsNullOrEmpty(reSubReq.Password))
                {
                    _session.ErrMsg = "Bad Data : Username/Password";
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrCode = GeneralCodeConstants.ReqBadData;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                }
                // trim and lower case
                reSubReq.Username = reSubReq.Username.Trim().ToLower();
                reSubReq.Password = reSubReq.Password.Trim();

                if (reSubReq.Username.Length > 20 || reSubReq.Password.Length > 20)
                {
                    _session.ErrMsg = "Bad Data : Username/Password Exceeded max .allowable length '20 char'";
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrCode = GeneralCodeConstants.ReqBadData;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    //KillSession(AdminMsgTyp.ReqBadData, "Bad Provided Data.");
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                }

                // if already online user
                if (Sessions.IsOnlineUser(reSubReq.Username))
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Already Online User! Session has been terminated.";
                    _session.ErrCode = GeneralCodeConstants.AlreadyOnlineUser;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.AlreadyOnlineUser, _session.ErrMsg);
                }

                Login dbLogin = _dbContext.Logins.SingleOrDefault(l => l.UserName == reSubReq.Username && l.Password == reSubReq.Password);
                if (dbLogin == null)
                {
                    _session.UserName = reSubReq.Username;
                    _session.Password = reSubReq.Password;
                    _session.ErrMsg = "Resubscribe : Access Denied";
                    _session.ErrCode = GeneralCodeConstants.AccessDenied;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AccessDenied, "Access Denied");
                }
                // authenticated
                _session.UserName = reSubReq.Username;
                _session.Password = reSubReq.Password;
                try
                {
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }
                _isAuth = true;
                _loginInfo = new LoginInfo() { Username = dbLogin.UserName, CanPlaceOrder = dbLogin.CanPlaceOrder, CanReplicate = dbLogin.CanReplicate };
                #endregion Authentication

                if (!reSubReq.NewQueue)
                {
                    // first : remove old session before changing current session key to the old one as database wont accept this
                    Session oldSessionRecord = _dbContext.Sessions.SingleOrDefault(s => s.SessionKey == reSubReq.SessionKey);
                    _dbContext.SessionsHistories.Add(CreateHistory(oldSessionRecord));
                    _dbContext.Sessions.Remove(oldSessionRecord);
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    // now update current session key with old one
                    string callbackQueuePath = string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", subDetails.QueueMachine, subDetails.QueueName);
                    subDetails.IsOnline = true;
                    _sessionKey = reSubReq.SessionKey;
                    _session.SessionKey = reSubReq.SessionKey;
                    _session.IsOnline = true;
                    _session.QueueIP = subDetails.QueueMachine;
                    _session.QueueName = subDetails.QueueName;
                    _session.QueuePath = callbackQueuePath;
                    _session.Note = string.Format("client resubscribed session {0}", _session.SessionKey);
                    _queue = subDetails.Queue;
                    _session.IsSubscribed = true;
                    _session.SubscriptionDateTime = DateTime.Now;
                    _session.SubscriptionDateTimeString = todayDateTimeFomat;
                    _session.FlushUpdatesOffline = reSubReq.FlushUpdatesOffline;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    Sessions.Resubscribe(_sessionKey, _callBack, _loginInfo, reSubReq.FlushUpdatesOffline, false);
                    _AccessRetry = 0;
                    OperationContext.Current.OperationCompleted += ResubConStatus_OperationCompleted;
                    return ReturnMsg(GeneralCodeConstants.Subscribed);
                }
                else // if new queue
                {
                    if (string.IsNullOrEmpty(reSubReq.QueueIP) || string.IsNullOrEmpty(reSubReq.QueueName))
                    {
                        //SystemLogger.WriteOnConsoleAsync(true, "SubscribeSession Error: Invalid Queue Path (Null or Empty)", ConsoleColor.Red, ConsoleColor.Black, true);
                        _session.ErrMsg = "Invalid Queue Data";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        _session.QueueIP = reSubReq.QueueIP;
                        _session.QueueName = reSubReq.QueueName;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    reSubReq.QueueName = reSubReq.QueueName.Trim().ToLower();

                    if (reSubReq.QueueName.Length > 50)
                    {
                        _session.ErrMsg = "QueueName exceeded max. allowable length";
                        _session.ErrCode = GeneralCodeConstants.ReqBadData;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.ReqBadData, _session.ErrMsg);
                    }

                    // only ip or .(localhost) are allowed
                    if (reSubReq.QueueIP != ".")
                    {
                        IPAddress ipAdd;

                        if (!IPAddress.TryParse(reSubReq.QueueIP, out ipAdd))
                        {
                            _session.ErrMsg = "Invalid IP";
                            _session.ErrCode = GeneralCodeConstants.ReqBadData;
                            _session.QueueIP = reSubReq.QueueIP;
                            _session.QueueName = reSubReq.QueueName;
                            try
                            {
                                _dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                            }
                            KillSession(GeneralCodeConstants.ReqBadData, "Invalid IP");
                        }
                    }

                    string callbackQueuePath = string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", reSubReq.QueueIP, reSubReq.QueueName);
                    _session.QueueIP = reSubReq.QueueIP;
                    _session.QueueName = reSubReq.QueueName;
                    _session.QueuePath = callbackQueuePath;
                    _session.FlushUpdatesOffline = reSubReq.FlushUpdatesOffline;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }


                    if (Sessions.IsSubscribedQueue(reSubReq.QueueIP, reSubReq.QueueName) != null)
                    {
                        _session.ErrMsg = "Already subscribed queue";
                        _session.ErrCode = GeneralCodeConstants.AlreadyRegisteredQueue;
                        try
                        {
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        KillSession(GeneralCodeConstants.AlreadyRegisteredQueue, "Queue info is already subscribed by another session");
                    }


                    try
                    {
                        _queue = new MessageQueue(callbackQueuePath);
                        //Task.Factory.StartNew(() => { _queue.Send(new FixAdmin_TestRequest() { TestKey = _testKey }); });
                        _queue.Send(new FixAdmin_TestRequest() { TestKey = _testKey });
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("error sending msg for client queue {0} : {1}", _sessionKey, ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                        KillSession(GeneralCodeConstants.QueueNotWorking, "Service cannot reach provided queue");
                    }

                    _resubscribedSessionKey = reSubReq.SessionKey;
                    _session.IsTestRequested = true;
                    _session.TestReqDateTime = DateTime.Now;
                    _session.ReqTestKey = _testKey;
                    try
                    {
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    _isAwaitTestRes = true;
                    _swAwaitTestKeyRes.Start();

                    return ReturnMsgWithCheckAccessRetry(GeneralCodeConstants.AwaitingTestKey, "test key sent to validate the provided queue.");
                }
            }// end lock
        }

        public void Unsubscribe()
        {
            if (_isSessionTerminated)
                return;

                if (!_session.IsSubscribed)
                    KillSession(GeneralCodeConstants.NotSubscribed);
                Sessions.Unsubscribe(_sessionKey);
                _session.ErrCode = GeneralCodeConstants.ClientUnsubscribed;
                _session.IsUnsubscribed = true;
                _session.IsOnline = false;
                _session.UnsubscriptionDateTime0 = DateTime.Now;
                _dbContext.SaveChanges();
                _dbContext.SessionsHistories.Add(CreateHistory(_session));
                _dbContext.Sessions.Remove(_session);
                _dbContext.SaveChanges();
                KillSession(GeneralCodeConstants.ClientUnsubscribed, "client unsubscribed");
        }

        public FixAdminMsg HandleMsg(IToAdminMsg msg)
        {
            if (_isSessionTerminated)
                return ReturnMsg(GeneralCodeConstants.SessionNotFound, "Session Terminated!");

            // dont check subscription status before switch case. it depends on the msg type
            // TestResponse msg doesnt need session to be subscribed
            if (!_isAuth)
                return ReturnMsg(GeneralCodeConstants.NotSubscribed);
            if (msg == null)
                return ReturnMsg(GeneralCodeConstants.ReqBadData);

            #region TestResponse
            if (msg is FixAdmin_TestResponse)
            {
                if (_session.IsSubscribed)
                    return ReturnMsg(GeneralCodeConstants.Subscribed);
                if (!_isAwaitTestRes)
                    return ReturnMsg(GeneralCodeConstants.NotIdentified);
                FixAdmin_TestResponse res = (FixAdmin_TestResponse)msg;
                if (string.IsNullOrEmpty(res.TestKey))
                    return ReturnMsg(GeneralCodeConstants.ReqBadData);

                if (res.TestKey == _testKey)
                {
                    OperationContext.Current.OperationCompleted += SendKey_OperationCompleted;
                    _isAwaitTestRes = false;
                    _session.IsTestResponded = true;
                    _session.TestResDateTime = DateTime.Now;
                    _session.IsSubscribed = true;
                    _session.IsOnline = true;
                    _session.IsValidTestKey = true;
                    _session.ResTestKey = res.TestKey;
                    _session.SubscriptionDateTime = DateTime.Now;
                    _session.SubscriptionDateTimeString = todayDateTimeFomat;
                    _dbContext.SaveChanges();
                    if (_requestType == "Subscribe")
                        Sessions.Subscribe(_loginInfo, _sessionKey, _callBack, _session.QueueIP, _session.QueueName, _queue, _session.FlushUpdatesOffline);
                    else
                    {
                        try
                        {
                            Session oldSessionRecord = _dbContext.Sessions.SingleOrDefault(s => s.SessionKey == _resubscribedSessionKey);
                            _dbContext.SessionsHistories.Add(CreateHistory(oldSessionRecord));
                            _dbContext.Sessions.Remove(oldSessionRecord);
                            _dbContext.SaveChanges();
                            _sessionKey = _resubscribedSessionKey;
                            _session.SessionKey = _resubscribedSessionKey;
                            _dbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, "Handle Msg Err, Valid TestResp error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                        Sessions.Resubscribe(_sessionKey, _callBack, _loginInfo, _session.FlushUpdatesOffline, _session.NewQueue, _session.QueueIP, _session.QueueName, _queue);
                    }

                    _AccessRetry = 0;
                    return ReturnMsg(GeneralCodeConstants.Subscribed);
                }
                else
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Admin Err Msg Client {0}, Err : {1} ", _sessionKey, "Invalid Test Key"), ConsoleColor.Magenta, ConsoleColor.Black, true);
                    try
                    {
                        _session.IsTestResponded = true;
                        _session.TestResDateTime = DateTime.Now;
                        _session.IsSubscribed = false;
                        _session.IsOnline = false;
                        _session.IsValidTestKey = false;
                        _session.ErrMsg = "Invalid Test Key";
                        _session.ResTestKey = res.TestKey;
                        _dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "Handle Msg Err, Invalid TestResp error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.InvalidTestKey);
                }
            }
            #endregion TestResponse
            else if (msg is FixAdmin_FixConStatusReq)
            {
                if (!_session.IsSubscribed)
                    return ReturnMsg(GeneralCodeConstants.NotSubscribed);
                OperationContext.Current.OperationCompleted += ResubConStatus_OperationCompleted;
                return ReturnMsg(GeneralCodeConstants.OperationDone);
            }
            return ReturnMsg(GeneralCodeConstants.NotIdentified);
        }

        public void Ping()
        {
            if (_isSessionTerminated)
                return;

            //SystemLogger.WriteOnConsoleAsync(true, "ping", ConsoleColor.Yellow, ConsoleColor.Green, false);
            if (_isAwaitTestRes || !_session.IsSubscribed)
            {
                if (_swAwaitTestKeyRes.ElapsedMilliseconds > _awaitTestResInMS)
                {
                    string errMsg = string.Format("Client didn't respond to test request within {0} ms", _awaitTestResInMS);
                    SystemLogger.WriteOnConsoleAsync(true, _session.ErrMsg, ConsoleColor.Yellow, ConsoleColor.Green, false);
                    try
                    {
                        _session.ErrMsg = errMsg;
                        _dbContext.SaveChanges();
                        _swAwaitTestKeyRes.Stop();
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, "client session Ping error updaing db: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                    KillSession(GeneralCodeConstants.TestKeyResponseTimeout, errMsg);
                }
            }
        }
        
        #endregion IFixAdmin methods

        #region Helper Methods

        SessionsHistory CreateHistory(Session session)
        {
            SessionsHistory history = new SessionsHistory();
            foreach (PropertyInfo sourcePropertyInfo in session.GetType().GetProperties())
            {
                PropertyInfo destPropertyInfo = history.GetType().GetProperty(sourcePropertyInfo.Name);
                destPropertyInfo.SetValue(history, sourcePropertyInfo.GetValue(session, null), null);
            }
            return history;
        }

        private void Dispose()
        {
            _swAwaitTestKeyRes = null;
            _loginInfo = null;
            _session = null;
            _dbContext = null;
            _queue = null;
        }

        void SendKey_OperationCompleted(object sender, EventArgs e)
        {
            try
            {
                _callBack.PushAdminMsg(new IFromAdminMsg[] { new FixAdmin_SessionUp() { SessionKey = _sessionKey } });
                _callBack.PushAdminMsg(new IFromAdminMsg[] { new FixAdmin_MarketStatus() { IsConnected = MarketFixClient.IsLoggedOn() } });
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending session key : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        void ResubConStatus_OperationCompleted(object sender, EventArgs e)
        {
            try
            {
                _callBack.PushAdminMsg(new IFromAdminMsg[] { new FixAdmin_MarketStatus() { IsConnected = MarketFixClient.IsLoggedOn() } });
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending session key : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        FixAdminMsg ReturnMsgWithCheckAccessRetry(string code, string note = "", object[]  data = null)
        {
            if (_AccessRetry >= _allowableAccessRetry)
            {
                string noteToSend = note ?? string.Empty;
                noteToSend += noteToSend != string.Empty ? " / " : "";
                noteToSend += " Exceeded max access trials! session has been terminated.";
                KillSession(code, noteToSend);
            }
            return new FixAdminMsg() { Code = code, Note = note, Data = data };
        }

        FixAdminMsg ReturnMsg(string code, string note = "", object[] data = null)
        {
            try
            {
                return new FixAdminMsg() { Code = code, Note = note, Data = data };
            }
            catch(Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error returning message : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                return new FixAdminMsg() { Code = GeneralCodeConstants.OperationFailed };
            }
        }

        void KillSession(string code, string note = "")
        {
            return;
            
            if (_isSessionTerminated)
                return;

            try
            {
                _isSessionTerminated = true;
                _session.IsOnline = false;
                _session.ErrCode = code;
                _session.Note = "Session Faulted";
                _dbContext.SaveChanges();
                _isSessionKilledBySvc = true;
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error Ending Session while setting session killed in db : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
            }
            SystemLogger.WriteOnConsoleAsync(true, string.Format("Ending Session, Code : {0} ", code), ConsoleColor.Magenta, ConsoleColor.Black, false);
            string expMsg = string.Format("CODE {0}", code);
            Dispose();
            if (!string.IsNullOrEmpty(note))
                expMsg += string.Format("| {0}", note);
            throw new Exception(expMsg);
        }

        void Channel_Faulted(object sender, EventArgs e)
        {
            _isSessionTerminated = true;
            SystemLogger.WriteOnConsoleAsync(true, string.Format("Channel_Faulted Session {0}", _sessionKey), ConsoleColor.Cyan, ConsoleColor.Black, false);
            // if not ended by system ... which means connection interrupted or client closed
            if (!_isSessionKilledBySvc)
            {
                try
                {
                    Sessions.DeactivateCallback(_sessionKey);
                    _session.IsOnline = false;
                    _session.ErrCode = GeneralCodeConstants.SessionFaulted;
                    _session.Note = "Session Faulted";
                    _session.IsSessionFaulted = true;
                    _session.SessionFaultDateTime = DateTime.Now;
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error Ending Session while setting session faulted in db : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }

        void Channel_Closed(object sender, EventArgs e)
        {
            _isSessionTerminated = true;            
            SystemLogger.WriteOnConsoleAsync(true, string.Format("Channel_Closed  Session {0}", _sessionKey), ConsoleColor.Cyan, ConsoleColor.Black, false);
            // if not ended by system ... which means connection interrupted or client closed
            if (!_isSessionKilledBySvc)
            {
                try
                {
                    Sessions.DeactivateCallback(_sessionKey);
                    _session.IsOnline = false;
                    _session.ErrCode = GeneralCodeConstants.SessionFaulted;
                    _session.Note = "Session Faulted";
                    _session.IsSessionFaulted = true;
                    _session.SessionFaultDateTime = DateTime.Now;
                    _dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error Ending Session while setting session closed in db : {0} ", ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }
        
        #endregion Helper Methods
    }
}
