using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Delegates;
using Beltone.Services.Fix.Routers;

namespace Beltone.Services.Fix.Provider
{
    internal class Application : MessageCracker, QuickFix.Application
    {
        internal static SessionID m_sessionID;
        //private static Session m_session;
        //private int m_orderID;
        //private int m_execID;
        private SocketInitiator m_SocketInitiator;
        private static FixSessionStatusChangedDelegate m_FixSessionStatusChangedDelegate;
        private static IRouter _router;
        private static int m_msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));

        public Application(SocketInitiator socketInitiator, IRouter router , FixSessionStatusChangedDelegate sessionStatusDel)
        {
            m_SocketInitiator = socketInitiator;
            _router = router;
            m_FixSessionStatusChangedDelegate = sessionStatusDel;
        }

        public void onCreate(SessionID sessionID)
        {
            m_sessionID = sessionID;
            //Console.WriteLine("onCreate: session id " + sessionID.ToString());
            //Console.WriteLine();
            //Console.WriteLine();
        }
        public void onLogon(SessionID sessionID)
        {
            m_FixSessionStatusChangedDelegate(Beltone.Services.Fix.Entities.FixSessionStatus.Connected);
            SystemLogger.WriteOnConsoleAsync(true, "Logged In: session id " + sessionID.ToString(), ConsoleColor.Green, ConsoleColor.Yellow, false);
        }
        public void onLogout(SessionID sessionID)
        {
            m_FixSessionStatusChangedDelegate(Beltone.Services.Fix.Entities.FixSessionStatus.Disconnected);
            SystemLogger.WriteOnConsoleAsync(true, "Logged Out: session id " + sessionID.ToString(), ConsoleColor.Red, ConsoleColor.Yellow, false);
            //Console.WriteLine("onLogout: session id " + sessionID.ToString());
        }
        public void toAdmin(Message message, SessionID sessionID)
        {
            //Type type = message.GetType();
            //if (type == typeof(QuickFix42.Logon))
            //{
            //    DatabaseMethods db = new DatabaseMethods();
            //    if (!db.IsTodaySequenceReset())
            //    {
            //        message.getHeader().setField(34, "1");
            //        db.UpdateTodaySequenceReset();
            //        Session.sendToTarget(new QuickFix42.SequenceReset(new NewSeqNo(1)), m_sessionID);
            //    }
            //    db = null;
            //}
            //SystemLogger.LogEventAsync("toAdmin:" + message.ToString() + Environment.NewLine);
            //SystemLogger.WriteOnConsole(true, "toAdmin:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
        }
        public void toApp(Message message, SessionID sessionID)
        {
            // get seqNumber and ClOrder ID in case of new order
            // then update order in db with generated seq num
            //string msgTypeString = message.getHeader().getField(m_msgTypeTag);
            //if (msgTypeString == "D")
            //{
            //    if (message.isSetField(11))
            //    {
                    //MsgSeqNum msgSeqNum = new MsgSeqNum();
                    //message.getHeader().getField(msgSeqNum);
            
            //        long orderID = long.Parse(message.getField(11).Split(new char[] { '-' })[0]);
            //        // now update the database
            //    }
            //}


            //SystemLogger.LogEventAsync("toApp:" + message.ToString() + Environment.NewLine);
            //SystemLogger.WriteOnConsole(true, "toApp:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
        }
        public void fromAdmin(Message message, SessionID sessionID)
        {
            Route(message);
            //SystemLogger.LogEventAsync("fromAdmin:" + message.ToString() + Environment.NewLine);
            //SystemLogger.WriteOnConsole(true, "fromAdmin:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
        }
        public void fromApp(Message message, SessionID sessionID)
        {
            //crack(message, sessionID);
            Route(message);
            //SystemLogger.WriteOnConsole(true, "fromApp:" + message.ToXML() + Environment.NewLine, ConsoleColor.Yellow, ConsoleColor.Black, false);
            //SystemLogger.LogEventAsync("fromApp:" + message.ToString() + Environment.NewLine);
        }

        private void Route(Message message)
        {
            try
            {
                string msgTypeString = message.getHeader().getField(m_msgTypeTag);
                // if you can extract the key (ClOrdID here)
                // then let specific processor to handle this msg by orderid
                if (message.isSetField(11))
                {
                    string orderID = message.getField(11).Split(new char[] { '-' })[0];
                    _router.PushMessage(orderID, message);
                }
                else // if not then send to next processor
                {
                    _router.PushMessage(message);
                }
                //else if (msgTypeString == "9" || msgTypeString == "8")
                //{
                //    string orderID = message.getField(11).Split(new char[] { '-' })[0];
                //    _router.PushMessage(orderID, message);
                //}
            }
            catch(Exception ex)
            {
                _router.PushMessage(message);
                SystemLogger.WriteOnConsoleAsync(true, "error while routing response: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        //public override void onMessage(QuickFix40.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();
        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix40.ExecutionReport executionReport = new QuickFix40.ExecutionReport
        //        (GenerateOrderID(),
        //          GenerateExecID(),
        //          new ExecTransType(ExecTransType.NEW),
        //          new OrdStatus(OrdStatus.FILLED),
        //          symbol,
        //          side,
        //          orderQty,
        //          new LastShares(orderQty.getValue()),
        //          new LastPx(price.getValue()),
        //          new CumQty(orderQty.getValue()),
        //          new AvgPx(price.getValue()));

        //    executionReport.set(clOrdID);

        //    if (order.isSetAccount())
        //        executionReport.set(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}

        //public override void onMessage(QuickFix41.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();

        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix41.ExecutionReport executionReport = new QuickFix41.ExecutionReport
        //        (GenerateOrderID(),
        //          GenerateExecID(),
        //          new ExecTransType(ExecTransType.NEW),
        //          new ExecType(ExecType.FILL),
        //          new OrdStatus(OrdStatus.FILLED),
        //          symbol,
        //          side,
        //          orderQty,
        //          new LastShares(orderQty.getValue()),
        //          new LastPx(price.getValue()),
        //          new LeavesQty(0),
        //          new CumQty(orderQty.getValue()),
        //          new AvgPx(price.getValue()));

        //    executionReport.set(clOrdID);

        //    if (order.isSetAccount())
        //        executionReport.set(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}

        //public override void onMessage(QuickFix42.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();

        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix42.ExecutionReport executionReport = new QuickFix42.ExecutionReport
        //                                            (GenerateOrderID(),
        //                                              GenerateExecID(),
        //                                              new ExecTransType(ExecTransType.NEW),
        //                                              new ExecType(ExecType.FILL),
        //                                              new OrdStatus(OrdStatus.FILLED),
        //                                              symbol,
        //                                              side,
        //                                              new LeavesQty(0),
        //                                              new CumQty(orderQty.getValue()),
        //                                              new AvgPx(price.getValue()));

        //    executionReport.set(clOrdID);
        //    executionReport.set(orderQty);
        //    executionReport.set(new LastShares(orderQty.getValue()));
        //    executionReport.set(new LastPx(price.getValue()));

        //    if (order.isSetAccount())
        //        executionReport.set(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}

        //public override void onMessage(QuickFix43.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();

        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix43.ExecutionReport executionReport = new QuickFix43.ExecutionReport
        //                                            (GenerateOrderID(),
        //                                              GenerateExecID(),
        //                                              new ExecType(ExecType.FILL),
        //                                              new OrdStatus(OrdStatus.FILLED),
        //                                              side,
        //                                              new LeavesQty(0),
        //                                              new CumQty(orderQty.getValue()),
        //                                              new AvgPx(price.getValue()));

        //    executionReport.set(clOrdID);
        //    executionReport.set(symbol);
        //    executionReport.set(orderQty);
        //    executionReport.set(new LastQty(orderQty.getValue()));
        //    executionReport.set(new LastPx(price.getValue()));

        //    if (order.isSetAccount())
        //        executionReport.set(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}

        //public override void onMessage(QuickFix44.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();

        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix44.ExecutionReport executionReport = new QuickFix44.ExecutionReport
        //      (GenerateOrderID(),
        //      GenerateExecID(),
        //      new ExecType(ExecType.FILL),
        //      new OrdStatus(OrdStatus.FILLED),
        //      side,
        //      new LeavesQty(0),
        //      new CumQty(orderQty.getValue()),
        //      new AvgPx(price.getValue()));

        //    executionReport.set(clOrdID);
        //    executionReport.set(symbol);
        //    executionReport.set(orderQty);
        //    executionReport.set(new LastQty(orderQty.getValue()));
        //    executionReport.set(new LastPx(price.getValue()));

        //    if (order.isSetAccount())
        //        executionReport.setField(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}

        //public override void onMessage(QuickFix50.NewOrderSingle order, SessionID sessionID)
        //{
        //    Symbol symbol = new Symbol();
        //    Side side = new Side();
        //    OrdType ordType = new OrdType();
        //    OrderQty orderQty = new OrderQty();
        //    Price price = new Price();
        //    ClOrdID clOrdID = new ClOrdID();

        //    order.get(ordType);

        //    if (ordType.getValue() != OrdType.LIMIT)
        //        throw new IncorrectTagValue(ordType.getField());

        //    order.get(symbol);
        //    order.get(side);
        //    order.get(orderQty);
        //    order.get(price);
        //    order.get(clOrdID);

        //    QuickFix50.ExecutionReport executionReport = new QuickFix50.ExecutionReport
        //     (GenerateOrderID(),
        //      GenerateExecID(),
        //      new ExecType(ExecType.FILL),
        //      new OrdStatus(OrdStatus.FILLED),
        //      side,
        //      new LeavesQty(0),
        //      new CumQty(orderQty.getValue()));

        //    executionReport.set(clOrdID);
        //    executionReport.set(symbol);
        //    executionReport.set(orderQty);
        //    executionReport.set(new LastQty(orderQty.getValue()));
        //    executionReport.set(new LastPx(price.getValue()));
        //    executionReport.set(new AvgPx(price.getValue()));

        //    if (order.isSetAccount())
        //        executionReport.setField(order.getAccount());

        //    try
        //    {
        //        Session.sendToTarget(executionReport, sessionID);
        //    }
        //    catch (SessionNotFound) { }
        //}



        //private OrderID GenerateOrderID()
        //{
        //    return new OrderID((++m_orderID).ToString());
        //}

        //private ExecID GenerateExecID()
        //{
        //    return new ExecID((++m_execID).ToString());
        //}

       
    }


}
