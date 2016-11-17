using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;
using Beltone.Services.Fix.Utilities;
using System.Threading;
using Beltone.Services.Fix.Entities.Delegates;
using Beltone.Services.Fix.Entities;
using Beltone.Services.Fix.Routers;

namespace Beltone.Services.Fix.Provider
{
    public static class FixGatewayManager
    {
        private static SocketInitiator m_initiator = null;
        private static string m_filePath;
        private static CustomizedQueue m_queue;
        private static SessionSettings settings;
        private static Application application;
        private static FileStoreFactory storeFactory;
        //private static ScreenLogFactory logFactory;
        private static MessageFactory messageFactory;
        private static FixSessionStatusChangedDelegate m_FixSessionStatusChangedDelegate;


        public static void Initialize(string settingsFilePath, IRouter router, FixSessionStatusChangedDelegate sessionStatus)
        {
            m_FixSessionStatusChangedDelegate = sessionStatus;
            m_filePath = settingsFilePath;
            settings = new SessionSettings(m_filePath);
            application = new Application(m_initiator, router, m_FixSessionStatusChangedDelegate);
            storeFactory = new FileStoreFactory(settings);
            //logFactory = new ScreenLogFactory(settings);
            FileLogFactory logFactory = new FileLogFactory(settings);
            messageFactory = new DefaultMessageFactory();
            m_initiator = new SocketInitiator(application, storeFactory, settings, logFactory, messageFactory);
            //m_initiator = new SocketInitiator(application, storeFactory, settings, messageFactory);
        }

        //public static void ResetSequence(int seqNo)
        //{
        //    Session.sendToTarget(new QuickFix44.SequenceReset(new NewSeqNo(seqNo)), Application.m_sessionID);
        //}


        public static void Logon()
        {
            m_initiator.start();
        }

        public static void Logout()
        {
            //m_initiator.start();
        }

        public static bool IsLoggedOn()
        {
            return m_initiator.isLoggedOn();
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

            QuickFix.ClOrdID clOrdID = new QuickFix.ClOrdID(_clOrderID);
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

            Session.sendToTarget(order, Application.m_sessionID);
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
            modify.setField(new ClOrdID(clOrderID));
            modify.setField(new OrigClOrdID(OrigClOrdID));
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
            Session.sendToTarget(modify, Application.m_sessionID);
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public static void CancelOrder(string account, string securityCode, string clOrderID, string OrigClOrdID, char orderSide)
        {
            QuickFix44.OrderCancelRequest cancel = new QuickFix44.OrderCancelRequest();
            cancel.setField(new Account(account));
            cancel.setField(new ClOrdID(clOrderID));
            cancel.setField(new OrigClOrdID(OrigClOrdID));
            cancel.setField(new Symbol(securityCode));
            cancel.setField(new TransactTime(DateTime.Now));
            cancel.setField(new QuickFix.Side(orderSide));
            Session.sendToTarget(cancel, Application.m_sessionID);
        }

        public static void ResetSequence()
        {
            QuickFix44.SequenceReset reset = new QuickFix44.SequenceReset(new NewSeqNo(4));
            reset.setField(new GapFillFlag(true));
            reset.setField(new PossDupFlag(true));
            reset.setField(new MsgType("4"));
            Session.sendToTarget(reset, Application.m_sessionID);
        }



    }
}
