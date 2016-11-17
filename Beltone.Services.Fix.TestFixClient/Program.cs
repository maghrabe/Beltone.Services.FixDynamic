using Beltone.Services.Fix.Utilities;
using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.TestFixClient
{
    class Program
    {
        public delegate void OnConnectionChanged(bool connected);
        static SocketInitiator _initiator = null;
        static ClientApp _app = null;

        static void Main(string[] args)
        {
            SystemLogger.Initialize();
            SystemConfigurations.Initialize();
            Task.Factory.StartNew(() => { OpenFixConn(); });

            while (true)
            {
                Console.ReadLine();
                PlaceNewSingleOrder();
            }
        }

        public static void PlaceNewSingleOrder()
        {
            QuickFix44.NewOrderSingle order = new QuickFix44.NewOrderSingle();

            #region Order Details

            QuickFix.OrderQty orderQty = new QuickFix.OrderQty(10); order.setField(orderQty);
            QuickFix.Symbol symbol = new QuickFix.Symbol("EGS48031C016"); order.setField(symbol);
            QuickFix.SecurityID secID = new QuickFix.SecurityID("EGS48031C016"); order.setField(secID);
            QuickFix.Side side = new QuickFix.Side(Side.SELL); order.setField(side);
            QuickFix.OrdType ordType = new QuickFix.OrdType(OrdType.LIMIT); order.setField(ordType);
            QuickFix.Price price = new QuickFix.Price(10); order.setField(price);
            Currency currency = new Currency("EGP"); order.setField(currency);
            Account acc = new Account("1003"); order.setField(acc);
            QuickFix.PartyID custody = new PartyID("5004"); order.setField(custody);
            TimeInForce tif = new TimeInForce(TimeInForce.DAY); order.setField(tif);
            IDSource ids = new IDSource("4"); order.setField(ids);
            TransactTime tt = new TransactTime(DateTime.Now); order.setField(tt);
            //SenderSubID ss = new SenderSubID("05095a"); order.setField(ss);
            #endregion Order Details

            #region Fix Order Message IDs

            QuickFix.ClOrdID clOrdID = new ClOrdID(Guid.NewGuid().ToString());
            order.setField(clOrdID);

            #endregion Fix Order Message IDs

            #region Exchange
            ExDestination exd = new ExDestination("CA"); order.setField(exd);
            TradingSessionID tradSession = new TradingSessionID("NOPL"); order.setField(tradSession);
            #endregion Exchange

            Session.sendToTarget(order, _app.SessionID);
        }

        static void OpenFixConn()
        {
            try
            {
                string fixClientConf = Environment.CurrentDirectory + @"\" + SystemConfigurations.GetAppSetting("FixClientConfPath");
                SessionSettings settings = new SessionSettings(fixClientConf);
                OnConnectionChanged d = new OnConnectionChanged(FixConnChanged);
                _app = new ClientApp(d);
                //_app.OnStatusChanged+=  
                FileStoreFactory storeFactory = new FileStoreFactory(settings);
                FileLogFactory logFactory = new FileLogFactory(settings);
                MessageFactory messageFactory = new DefaultMessageFactory();
                _initiator = new SocketInitiator(_app, storeFactory, settings, logFactory, messageFactory);
                _initiator.start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void FixConnChanged(bool connected)
        {
            if (!connected)
                _initiator.start();
        }


        public class ClientApp : QuickFix.Application
        {
            public SessionID SessionID { get { return _sessionID; } }
            SessionID _sessionID = null;
            OnConnectionChanged _delConChanged = null;
            public ClientApp(OnConnectionChanged del)
            {
                _delConChanged = del;
            }

            public void onCreate(QuickFix.SessionID value)
            {
                //Console.WriteLine("Message OnCreate" + value.toString());
            }

            public void onLogon(QuickFix.SessionID value)
            {
                //_loggedOn = true;
                _sessionID = value;
                SystemLogger.WriteOnConsoleAsync(true, "Client Logged In" + value.ToString(), ConsoleColor.Green, ConsoleColor.Yellow, false);
                //Console.WriteLine("OnLogon" + value.toString());
            }

            public void onLogout(QuickFix.SessionID value)
            {
                SystemLogger.WriteOnConsoleAsync(true, "Client Logged Out" + value.ToString(), ConsoleColor.Red, ConsoleColor.White, false);
                // Console.WriteLine("Log out Session" + value.toString());
            }

            public void toAdmin(QuickFix.Message value, QuickFix.SessionID session)
            {
                //SystemLogger.WriteOnConsoleAsync(true, "To Admin: " + value.ToString(), ConsoleColor.Black, ConsoleColor.White, false);
            }

            public void toApp(QuickFix.Message value, QuickFix.SessionID session)
            {
                //  Console.WriteLine("Called toApp :" + value.ToString());
            }

            public void fromAdmin(QuickFix.Message value, SessionID session)
            {
                //SystemLogger.WriteOnConsoleAsync(true, "From Admin: " + value.ToString(), ConsoleColor.Gray, ConsoleColor.White, false);
            }

            public void fromApp(QuickFix.Message value, SessionID session)
            {
                SystemLogger.WriteOnConsoleAsync(true, value.ToString(), ConsoleColor.DarkGray, ConsoleColor.White, false);
                Handle(value);
            }



            public bool Handle(QuickFix.Message msg)
            {
                return true;
            }
        }



    }
}
