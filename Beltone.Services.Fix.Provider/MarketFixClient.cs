using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;
using Beltone.Services.Fix.Utilities;
using System.Threading;
using Beltone.Services.Fix.Entities.Delegates;
using Beltone.Services.Fix.Entities;
using Beltone.Services.ProcessorsRouter.Entities;

namespace Beltone.Services.Fix.Provider
{
    public static class MarketFixClient
    {
        private static SocketInitiator _initiator = null;
        private static FixSessionStatusChangedDelegate _FixSessionStatusChangedDelegate;
        private static SessionID _sessionID;
        private static Router _router;
        private static string _senderCompID = string.Empty;
        private static string _targetCompID = string.Empty;
        private static string _msgSplit = SystemConfigurations.GetAppSetting("MsgSplit");
        private static string _wcfPrefixCode = SystemConfigurations.GetAppSetting("WcfMsgPrefix");
        private static string _fixPrefixCode = SystemConfigurations.GetAppSetting("FixMsgPrefix");
        private static string _wcfMsgPrefix = string.Format("{0}{1}", SystemConfigurations.GetAppSetting("WcfMsgPrefix"), _msgSplit);
        private static string _fixMsgPrefix = string.Format("{0}{1}", SystemConfigurations.GetAppSetting("FixMsgPrefix"), _msgSplit);


        public static void Initialize(string settingsFilePath, Router router, FixSessionStatusChangedDelegate sessionStatus)
        {
            _FixSessionStatusChangedDelegate = sessionStatus;
            _router = router;
            SessionSettings settings = new SessionSettings(settingsFilePath);
            ClientApp application = new ClientApp();
            FileStoreFactory storeFactory = new FileStoreFactory(settings);
            //logFactory = new ScreenLogFactory(settings);
            FileLogFactory logFactory = new FileLogFactory(settings);
            MessageFactory messageFactory = new DefaultMessageFactory();
            _initiator = new SocketInitiator(application, storeFactory, settings, logFactory, messageFactory);
        }

        public static void Logon()
        {
            _initiator.start();
        }

        public static void Logout()
        {
            _initiator.stop();
        }

        public static bool IsLoggedOn()
        {
            return _initiator.isLoggedOn();
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static void PlaceNewSingleOrder(string _clOrderID, string _clientID, string _securityCode, int _quantity, double _price, string _custodyCode,
            char _orderSide, char _orderType, string currencyCode, string exDestination, char _timeInForce, string groupID,
            char handleInst, DateTime expiration, bool hasAON, int minQty)
        {
            QuickFix44.NewOrderSingle order = new QuickFix44.NewOrderSingle();

            #region Order Details

            QuickFix.OrderQty orderQty = new QuickFix.OrderQty(_quantity); order.setField(orderQty);
            QuickFix.Symbol symbol = new QuickFix.Symbol(_securityCode); order.setField(symbol);
            QuickFix.SecurityID secID = new QuickFix.SecurityID(_securityCode); order.setField(secID);
            QuickFix.Side side = new QuickFix.Side(_orderSide); order.setField(side);
            QuickFix.OrdType ordType = new QuickFix.OrdType(_orderType); order.setField(ordType);
            QuickFix.Price price = new QuickFix.Price(_price); order.setField(price);
            Currency currency = new Currency(currencyCode); order.setField(currency);
            Account acc = new Account(_clientID); order.setField(acc);
            //QuickFix.ClearingFirm custody = new ClearingFirm(_custodyCode);order.setField(custody);
            QuickFix.PartyID custody = new PartyID(_custodyCode); order.setField(custody);
            //QuickFix.PartyRole pr = new PartyRole(PartyRole.CUSTODIAN); order.setField(pr);
            //QuickFix.NoPartyIDs npid = new NoPartyIDs(1); order.setField(npid);
            //QuickFix.PartyIDSource pid = new PartyIDSource(PartyIDSource.PROPRIETARY); order.setField(pid);
            TimeInForce tif = new TimeInForce(_timeInForce); order.setField(tif);
            IDSource ids = new IDSource("4"); order.setField(ids);
            TransactTime tt = new TransactTime(DateTime.Now); order.setField(tt);
            //SenderSubID ss = new SenderSubID("05095a"); order.setField(ss);
            if (_timeInForce == QuickFix.TimeInForce.GOOD_TILL_DATE)
            {
                ExpireDate ed = new ExpireDate(expiration.ToString("yyyyMMdd")); order.setField(ed);
                //ExpireTime et = new ExpireTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour + 4, 0, 0)); order.setField(et);
                ExpireTime et = new ExpireTime(expiration); order.setField(et);
            }
            if (hasAON)
            {
                order.setField(new ExecInst(ExecInst.ALL_OR_NONE.ToString()));
                order.setField(new MinQty(minQty));
            }
            #endregion Order Details

            #region Fix Order Message IDs

            QuickFix.ClOrdID clOrdID = new QuickFix.ClOrdID(string.Format("{0}{1}", _wcfMsgPrefix, _clOrderID));
            order.setField(clOrdID);

            #endregion Fix Order Message IDs

            #region Reporting

            //QuickFix.HandlInst handlInst = new QuickFix.HandlInst(QuickFix.HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE_NO_BROKER_INTERVENTION); order.setField(handlInst);
            QuickFix.HandlInst handlInst = new QuickFix.HandlInst(handleInst); order.setField(handlInst);

            #endregion Reporting

            #region Exchange

            ExDestination exd = new ExDestination(exDestination); order.setField(exd);
            TradingSessionID tradSession = new TradingSessionID(groupID); order.setField(tradSession);
            #endregion Exchange

            Session.sendToTarget(order, _sessionID);
        }

        //public static void ResetSequence()
        //{
        //    QuickFix44.SequenceReset resetSeq = new QuickFix44.SequenceReset();
        //    Session.sendToTarget(resetSeq, Application.m_sessionID);
        //}

        //public static void ResetSequence(int seqNo)
        //{
        //    QuickFix44.SequenceReset resetSeq = new QuickFix44.SequenceReset(new NewSeqNo(seqNo)); 
        //    Session.sendToTarget(resetSeq, Application.m_sessionID);
        //}

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static void ModifyCancelOrder(string account, string securityCode, string clOrderID, string OrigClOrdID, int quantity, double price, char timeInForce, char orderType, char orderSide, bool hasAON, int minQty)
        {
            QuickFix44.OrderCancelReplaceRequest modify = new QuickFix44.OrderCancelReplaceRequest();
            modify.setField(new Account(account));
            modify.setField(new QuickFix.ClOrdID(string.Format("{0}{1}", _wcfMsgPrefix, clOrderID)));
            modify.setField(new OrigClOrdID(string.Format("{0}{1}", _wcfMsgPrefix, OrigClOrdID)));
            modify.setField(new Symbol(securityCode));
            modify.setField(new OrderQty((int)quantity));
            modify.setField(new Price((double)price));
            modify.setField(new TimeInForce((char)timeInForce));
            modify.setField(new QuickFix.Side(orderSide));
            modify.setField(new QuickFix.OrdType(orderType));
            modify.setField(new TransactTime(DateTime.Now));
            modify.setField(new QuickFix.HandlInst(QuickFix.HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE_NO_BROKER_INTERVENTION));
            if (hasAON)
            {
                modify.setField(new ExecInst(ExecInst.ALL_OR_NONE.ToString()));
                modify.setField(new MinQty(minQty));
            }
            Session.sendToTarget(modify, _sessionID);
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static void CancelOrder(string account, string securityCode, string clOrderID, string OrigClOrdID, char orderSide)
        {
            QuickFix44.OrderCancelRequest cancel = new QuickFix44.OrderCancelRequest();
            cancel.setField(new Account(account));
            //cancel.setField(new ClOrdID(clOrderID));
            cancel.setField(new QuickFix.ClOrdID(string.Format("{0}{1}", _wcfMsgPrefix, clOrderID)));
            cancel.setField(new OrigClOrdID(string.Format("{0}{1}", _wcfMsgPrefix, OrigClOrdID)));
            cancel.setField(new Symbol(securityCode));
            cancel.setField(new TransactTime(DateTime.Now));
            cancel.setField(new QuickFix.Side(orderSide));
            Session.sendToTarget(cancel, _sessionID);
        }

        public static void ResetSequence()
        {
            QuickFix44.SequenceReset reset = new QuickFix44.SequenceReset(new NewSeqNo(4));
            reset.setField(new GapFillFlag(true));
            reset.setField(new PossDupFlag(true));
            reset.setField(new MsgType("4"));
            Session.sendToTarget(reset, _sessionID);
        }

        public static void SendDirectFix(Message msg)
        {
            // replace SenderCompID and TargetCompID
            msg.removeField(49);
            msg.removeField(56);
            //msg.setField(49, _senderCompID);
            //msg.setField(56, _targetCompID);
            Session.sendToTarget(msg, _sessionID);
        }

        class ClientApp :  QuickFix.Application
        {
            private static int _msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));

            public ClientApp()
            {
            }



            public void onCreate(SessionID sessionID)
            {
                _sessionID = sessionID;
                //Console.WriteLine("onCreate: session id " + sessionID.ToString());
                //Console.WriteLine();
                //Console.WriteLine();
            }
            public void onLogon(SessionID sessionID)
            {
                _senderCompID = sessionID.getSenderCompID();
                _targetCompID = sessionID.getTargetCompID();
                _FixSessionStatusChangedDelegate(Beltone.Services.Fix.Entities.FixSessionStatus.Connected);
                SystemLogger.WriteOnConsoleAsync(true, "Logged In: session id " + sessionID.ToString(), ConsoleColor.Green, ConsoleColor.Yellow, false);
            }
            public void onLogout(SessionID sessionID)
            {
                _FixSessionStatusChangedDelegate(Beltone.Services.Fix.Entities.FixSessionStatus.Disconnected);
                SystemLogger.WriteOnConsoleAsync(true, "Logged Out: session id " + sessionID.ToString(), ConsoleColor.Red, ConsoleColor.Yellow, false);
                //Console.WriteLine("onLogout: session id " + sessionID.ToString());
            }
            public void toAdmin(Message message, SessionID sessionID)
            {
                //SystemLogger.WriteOnConsole(true, "toAdmin:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
            }
            public void toApp(Message message, SessionID sessionID)
            {
                //SystemLogger.WriteOnConsole(true, "toApp:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
            }
            public void fromAdmin(Message message, SessionID sessionID)
            {
                //Route(message);
                //SystemLogger.WriteOnConsole(true, "fromAdmin:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
            }
            public void fromApp(Message message, SessionID sessionID)
            {
                //crack(message, sessionID);
                // reply from exchange.
                Route(message);
                //SystemLogger.WriteOnConsole(true, "fromApp:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
            }


            private void Route(Message message)
            {
                // Route To WCF Client or FIX Client
                try
                {
                    // if you can extract the OrdID (Tag 11)
                    if (message.isSetField(11))
                    {
                        bool isWcf = true;
                        string[] msgArr = message.getField(11).Split(new string[] { _msgSplit }, StringSplitOptions.None);
                        string prefix = msgArr[0];
                        if (prefix == _fixPrefixCode)
                            isWcf = false;
                        //else(prefix == _wcfPrefixCode)

                        message.removeField(11);
                        if (!isWcf)
                        {
                            // Ex: F-A-101
                            string sessionCode = msgArr[1];
                            string ordID = msgArr[2];
                            message.setField(11, ordID);
                            RouteDirectFixClient(sessionCode, ordID, message);
                        }
                        else
                        {
                            // Ex: F-101
                            string ordID = msgArr[1];
                            message.setField(11, ordID);
                            SvcFixServer.Replicate(message);
                            RouteToWcfClient(ordID, message);
                        }
                    }
                    else // if not then send to next processor
                        _router.PushMessage(message);
                }
                catch (Exception ex)
                {
                    //_router.PushMessage(message);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("error while routing response OrdIDID [{0}] : {1}", message.isSetField(11) ? message.getField(11) : "Not Found In Fix Msg", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }

            private void RouteToWcfClient(string ordID, Message message)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Response to WCF Client OrderID [{0}]", ordID), ConsoleColor.Blue, ConsoleColor.Yellow, false);
                _router.PushMessage(ordID, message);
            }

            private void RouteDirectFixClient(string sessionCode, string ordID, Message message)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Response to FIX Client OrderID [{0}]", ordID), ConsoleColor.Blue, ConsoleColor.White, false);
                SvcFixServer.RouteFixMessageToClient(sessionCode, ordID, message);
            }
        }
    }
}