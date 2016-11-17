using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using System.Data;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Provider;
using Beltone.Services.Fix.Contract.Entities;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Entities.Constants;
using Beltone.Services.Fix.Contract.Enums;
using System.Threading;
using System.Threading.Tasks;
using Beltone.Services.Fix.Service.Entities;
using Beltone.Services.Fix.MCSD;
using Beltone.Services.MCDR.Contract.Entities.ResMsgs;
using Beltone.Services.MCDR.Contract.Entities.Shared;
using Beltone.Services.MCDR.Contract.Constants;
using Beltone.Services.Fix.Entities;

namespace Beltone.Services.Fix.Service.Singletons
{
    public enum OpResult { OperationDone = 1, OperationFailed = 2, OrdNotFound = 3, PendingOrd = 4, InActiveOrd = 5 }

    public class OrdersManager
    {
        #region Variables
        //CallbackID, List RequesterOrderID
        private static Dictionary<string, List<Guid>> m_username_ReqOrdIDs = null;
        //                      RequesterOrderID, CallbackID
        //private static Dictionary<Guid, List<Guid>> m_ReqOrdID_subIDs = null;
        private static Dictionary<long, string> m_OrdID_username = null;
        //                      RequesterOrderID, ClOrderID
        private static Dictionary<Guid, long> m_ReqOrdID_OrdID = null;
        //                      OrderID, OrderDetails
        private static Dictionary<long, SingleOrder> m_orderID_ordersDetails = null;

        //                      OrderID, OrderDetails
        // private static Dictionary<long, SingleOrder> m_orderID_ordersDetails_MCSD = null;

        private static List<Guid> _dbReqIDs = null; // maghrabi

        private static int _MCSDcheckerIntervals = 5000;

        private static double _McsdTimeOutPeriod = 5000;


        private static List<string> m_handledExecutionIDs = null;
        private static object m_syncObject = new object();

        private static bool _AllowMcsdAllocation = false;
        private static string m_BrokerCode = string.Empty;
        private static List<string> _ActiveAllocationActions;
        private static List<string> _ActiveAllocationTypes;

        static Thread _threadMcsdTimeOutOrdersChecker;

        private static bool _ForceSendMcsdRejectedOrdersToMarket = false;

        private static object _syncObj = new object();

        #endregion Variables

        #region Constructors

        public static void Initialize()
        {

            _dbReqIDs = new List<Guid>();
            // this line is to make sure that fix db conn is already created
            CmdOnPoolGenerator gen = new CmdOnPoolGenerator(ConStrongKeys.FixDbCon);
            DatabaseMethods db = new DatabaseMethods();
            m_orderID_ordersDetails = new Dictionary<long, SingleOrder>();

            m_ReqOrdID_OrdID = new Dictionary<Guid, long>();
            m_username_ReqOrdIDs = new Dictionary<string, List<Guid>>();
            //m_ReqOrdID_subIDs = new Dictionary<Guid, List<Guid>>();
            m_OrdID_username = new Dictionary<long, string>();
            m_handledExecutionIDs = new List<string>();

            m_BrokerCode = SystemConfigurations.GetAppSetting("BrokerCode");
            // Fill the data 
            DataTable dtOrders = db.FillOrdersData();
            DataTable dtOrdersRequesIds = db.FillOrdersRequestIDs();


            foreach (DataRow row in dtOrders.Rows)// maghrabi 26 jan 2016
            {
                Guid ReqOrdID = (Guid)row["RequesterOrderID"];
                _dbReqIDs.Add(ReqOrdID);
            }

            foreach (DataRow row in dtOrders.Rows)
            {
                //Guid sessionKey = ((Guid)row["SessionID"]);
                string username = row["Username"].ToString();
                long ordID = (long)row["OrderID"];
                Guid ReqOrdID = (Guid)row["RequesterOrderID"];
                //  bool IsPendingcsd = (bool)row["IsPendingMcsd"];

                // prepare order object
                SingleOrder order = new SingleOrder();
                foreach (string colName in order.Data.Keys.ToArray())
                    order[colName] = row[colName];

                m_orderID_ordersDetails.Add((long)row["OrderID"], order);


                m_ReqOrdID_OrdID.Add((Guid)row["RequesterOrderID"], (long)row["OrderID"]);

                if (!m_username_ReqOrdIDs.ContainsKey(username))
                    m_username_ReqOrdIDs.Add(username, new List<Guid>());

                m_username_ReqOrdIDs[username].Add(ReqOrdID);


                m_OrdID_username.Add(ordID,username);
            }


            foreach (string execID in db.GetExecutionIDs())
            {
                m_handledExecutionIDs.Add(execID);
            }

            _AllowMcsdAllocation = Convert.ToBoolean(SystemConfigurations.GetAppSetting("AllowMcsdAllocation"));

            string activeAllocationActions = SystemConfigurations.GetAppSetting("ActiveAllocationsActions").ToString();
            string activeAllocationTyeps = SystemConfigurations.GetAppSetting("ActiveAllocationsTypes").ToString();

            _ActiveAllocationActions = activeAllocationActions.Split(',').ToList();
            _ActiveAllocationTypes = activeAllocationTyeps.Split(',').ToList();


            _MCSDcheckerIntervals = Convert.ToInt32(SystemConfigurations.GetAppSetting("McsdTimeOutCheckrIntervals"));

            _McsdTimeOutPeriod = Convert.ToDouble(SystemConfigurations.GetAppSetting("McsdTimeOutPeriod"));

            _ForceSendMcsdRejectedOrdersToMarket = Convert.ToBoolean(SystemConfigurations.GetAppSetting("ForceSendMcsdRejectedOrdersToMarket"));

            StartMcsdTimeOutChecker();

        }



        #endregion Constructors

        #region Place / Modify / Cancel Orders

        public static void HandleNewSingleOrder(string username, Guid clientKey, Guid requesterOrderID, int clientID, string custodyID, string securityID,
            string orderSide, double price, int quantity, string orderType, DateTime placementDateTime, string timeInForce,
            CurrencyItem currency, string exchangeID, DateTime orderCreatedBySysDateTime, string groupID,
            string marketID, HandleInstruction handleInst, DateTime expiryDate, Dictionary<string, object> optionalParam)
        {

            try
            {

                int mcsdQuantity = 0;
                OrderActions orderAction = OrderActions.New;


                bool isMcsdAllocOrder = IsMCSDSellAllocationRequired(quantity, optionalParam, null, out mcsdQuantity, orderAction);

                if (isMcsdAllocOrder)
                {
                    PlaceNewSingleOrderMCSD(username, clientKey, requesterOrderID, clientID, custodyID, securityID, orderSide, price, quantity, orderType, placementDateTime, timeInForce, currency, exchangeID
                        , orderCreatedBySysDateTime, groupID, marketID, handleInst, expiryDate, optionalParam, mcsdQuantity);
                }
                else
                {
                    PlaceNewSingleOrder(username, clientKey, requesterOrderID, clientID, custodyID, securityID, orderSide, price, quantity, orderType, placementDateTime, timeInForce, currency, exchangeID
                       , orderCreatedBySysDateTime, groupID, marketID, handleInst, expiryDate, optionalParam);
                }
            }
            catch (Exception exp)
            {
                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = exp.Message, RequesterOrderID = requesterOrderID } });
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error HanldeNewOrder , ClientKey {0}, Error: {1}", clientKey, exp.Message), ConsoleColor.Red, ConsoleColor.White, true);
            }
        }

        private static void PlaceNewSingleOrder(string username, Guid clientKey, Guid requesterOrderID, int clientID, string custodyID, string securityID,
          string orderSide, double price, int quantity, string orderType, DateTime placementDateTime, string timeInForce,
          CurrencyItem currency, string exchangeID, DateTime orderCreatedBySysDateTime, string groupID,
          string marketID, HandleInstruction handleInst, DateTime expiryDate, Dictionary<string, object> optionalParam)
        {
            long orderID = -1;

            try
            {

                // Get Currency 
                LookupItem currencyLookup = Lookups.GetCurrencyLookupByCurrencyCode(currency.Code);
                // Get Destination Type
                LookupItem exchangeLookup = Lookups.GetExchangeDestinationByExchangeID(exchangeID);
                // order Type
                LookupItem orderTypeLookup = Lookups.GetOrderTypeLookupByCodeValue(orderType);
                // Order Side
                LookupItem orderSideLookup = Lookups.GetOrderSidesLookupByCodeValue(orderSide);
                // Time In Force
                LookupItem timeInForceLookup = Lookups.GetTimeInForceLookupByCode(timeInForce);
                // Handle Instruction
                string handleValue = Enum.GetName(typeof(HandleInstruction), handleInst);
                LookupItem handleInstLookup = Lookups.GetHandleInstTypeLookupByCodeValue(handleValue);
                bool hasAON = false;
                int minQ = 0;
                if (optionalParam != null)
                    if (optionalParam.ContainsKey("AON") && (bool)optionalParam["AON"])
                        hasAON = true;
                if (hasAON)// MinQty must be existed if AON = true as it's already been validated before
                    minQ = (int)optionalParam["MinQty"];
                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    string OrderStatus = string.Empty;
                    string OriginalOrderStatus = string.Empty;
                    string ExecType = string.Empty;
                    string Note = string.Empty;
                    bool IsPendingMcsd = false;


                    OrderStatus = ORD_STATUS.PendingNew;
                    OriginalOrderStatus = ORD_STATUS.PendingNew;
                    ExecType = EXEC_TYP.PendingNew;
                    Note = "awaiting for acceptance response";
                    IsPendingMcsd = false;


                    DatabaseMethods db = new DatabaseMethods();
                    //orderID = db.AddNewSingleOrder(clientKey, requesterOrderID, null, null, clientID, custodyID, securityID, orderSide, 
                    //price, price, price, quantity, orderType, placementDateTime, ORD_STATUS.PendingNew, ORD_STATUS.PendingNew, "", false, "", timeInForce);


                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add(SingleOrderProperties.CurrencyID, currency.ID);
                    data.Add(SingleOrderProperties.GroupID, groupID);
                    data.Add(SingleOrderProperties.MarketID, marketID);
                    data.Add(SingleOrderProperties.ExchangeID, exchangeID);
                    data.Add(SingleOrderProperties.RequesterOrderID, requesterOrderID);
                    data.Add(SingleOrderProperties.ClientID, clientID);
                    data.Add(SingleOrderProperties.CustodyID, custodyID);
                    data.Add(SingleOrderProperties.SecurityCode, securityID);
                    data.Add(SingleOrderProperties.OrderSide, orderSide);
                    data.Add(SingleOrderProperties.CurrentPrice, price);
                    data.Add(SingleOrderProperties.OriginalPrice, price);
                    data.Add(SingleOrderProperties.PlacementDateTime, placementDateTime);
                    data.Add(SingleOrderProperties.OriginalQuantity, quantity);
                    data.Add(SingleOrderProperties.RemainingQuantity, 0);
                    data.Add(SingleOrderProperties.ExecutedQuantity, 0);
                    data.Add(SingleOrderProperties.LastExecQuantity, 0);
                    data.Add(SingleOrderProperties.LastExecPrice, 0);
                    data.Add(SingleOrderProperties.AvgPrice, 0);
                    data.Add(SingleOrderProperties.CurrentQuantity, quantity);
                    data.Add(SingleOrderProperties.OriginalOrderType, orderType);
                    data.Add(SingleOrderProperties.OrderType, orderType);
                    data.Add(SingleOrderProperties.OrderStatus, OrderStatus);

                    data.Add(SingleOrderProperties.OriginalOrderStatus, OriginalOrderStatus);

                    data.Add(SingleOrderProperties.ExecType, ExecType);
                    data.Add(SingleOrderProperties.OrderCreatedBySysDateTime, orderCreatedBySysDateTime);
                    data.Add(SingleOrderProperties.OrderRecievedDateTime, DateTime.Now);
                    data.Add(SingleOrderProperties.ModifiedDateTime, DateTime.Now);
                    data.Add(SingleOrderProperties.Note, Note);
                    data.Add(SingleOrderProperties.IsPending, true);
                    // data.Add(SingleOrderProperties.IsPendingMCDR, true);
                    data.Add(SingleOrderProperties.IsActive, false);
                    data.Add(SingleOrderProperties.IsExecuted, false);
                    data.Add(SingleOrderProperties.IsCompleted, false);
                    data.Add(SingleOrderProperties.HasSystemError, false);
                    //data.Add(SingleOrderProperties.ErrorMessage, string.Empty);
                    data.Add(SingleOrderProperties.OriginalTimeInForce, timeInForce);
                    data.Add(SingleOrderProperties.TimeInForce, timeInForce);
                    data.Add(SingleOrderProperties.OriginalHandleInst, handleValue);
                    data.Add(SingleOrderProperties.HandleInst, handleValue);
                    data.Add(SingleOrderProperties.ExpirationDate, expiryDate.ToShortDateString());
                    data.Add(SingleOrderProperties.ExpirationDateTime, expiryDate);
                    data.Add(SingleOrderProperties.AON, hasAON);
                    data.Add(SingleOrderProperties.OriginalAON, hasAON);
                    data.Add(SingleOrderProperties.OriginalMinQty, minQ);
                    data.Add(SingleOrderProperties.MinQty, minQ);

                    data.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);// maghrabi
                    data.Add(SingleOrderProperties.RequestedPrice, price);// maghrabi
                    data.Add(SingleOrderProperties.RequestedQty, quantity);// maghrabi

                    //OrderDetails Table
                    Dictionary<string, object> dataDetails = new Dictionary<string, object>();
                    dataDetails.Add(SingleOrdDetailsProps.CurrentPrice, price);
                    dataDetails.Add(SingleOrdDetailsProps.CurrentQuantity, quantity);
                    dataDetails.Add(SingleOrdDetailsProps.RemainingQuantity, quantity);
                    dataDetails.Add(SingleOrdDetailsProps.ExecutedQuantity, 0);
                    dataDetails.Add(SingleOrdDetailsProps.AvgPrice, 0);
                    dataDetails.Add(SingleOrdDetailsProps.ExecPrice, 0);
                    dataDetails.Add(SingleOrdDetailsProps.LastExecQuantity, 0);
                    dataDetails.Add(SingleOrdDetailsProps.OrderType, orderType);
                    dataDetails.Add(SingleOrdDetailsProps.OrderStatus, OrderStatus);
                    dataDetails.Add(SingleOrdDetailsProps.ExecType, ExecType);
                    dataDetails.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    dataDetails.Add(SingleOrdDetailsProps.Note, Note);
                    dataDetails.Add(SingleOrdDetailsProps.HasSystemError, false);
                    dataDetails.Add(SingleOrdDetailsProps.ErrorMessage, string.Empty);
                    dataDetails.Add(SingleOrdDetailsProps.TimeInForce, timeInForce);
                    dataDetails.Add(SingleOrdDetailsProps.IsNewOrderRequest, true);
                    dataDetails.Add(SingleOrdDetailsProps.IsUserRequest, true);
                    dataDetails.Add(SingleOrdDetailsProps.HandleInst, handleValue);
                    dataDetails.Add(SingleOrdDetailsProps.AON, hasAON);
                    dataDetails.Add(SingleOrdDetailsProps.MinQty, minQ);



                    orderID = db.AddNewSingleOrder(clientKey, username, data, dataDetails);

                    string clOrderID = string.Format("{0}-{1}", orderID.ToString(), "1");

                    SingleOrder order = new SingleOrder();

                    order[SingleOrderProperties.OrderID] = orderID;
                    order[SingleOrderProperties.ClOrderID] = clOrderID;
                    order[SingleOrderProperties.GroupID] = groupID;
                    order[SingleOrderProperties.MarketID] = marketID;
                    order[SingleOrderProperties.ExchangeID] = exchangeID;
                    order[SingleOrderProperties.CurrencyID] = currency.ID;
                    order[SingleOrderProperties.RequesterOrderID] = requesterOrderID;
                    order[SingleOrderProperties.ClientID] = clientID;
                    order[SingleOrderProperties.CustodyID] = custodyID;
                    order[SingleOrderProperties.SecurityCode] = securityID;
                    order[SingleOrderProperties.OrderSide] = orderSide;

                    order[SingleOrderProperties.IsPendingMcsd] = false;
                    order[SingleOrderProperties.IsActive] = false;
                    order[SingleOrderProperties.IsPending] = true;
                    order[SingleOrderProperties.IsCompleted] = false;
                    order[SingleOrderProperties.IsExecuted] = false;

                    order[SingleOrderProperties.CurrentPrice] = price;
                    order[SingleOrderProperties.OriginalPrice] = price;
                    order[SingleOrderProperties.AvgPrice] = 0;
                    order[SingleOrderProperties.LastExecPrice] = 0;

                    order[SingleOrderProperties.OriginalQuantity] = quantity;
                    order[SingleOrderProperties.RemainingQuantity] = 0;
                    order[SingleOrderProperties.ExecutedQuantity] = 0;
                    order[SingleOrderProperties.LastExecQuantity] = 0;
                    order[SingleOrderProperties.CurrentQuantity] = quantity;

                    order[SingleOrderProperties.OriginalOrderType] = orderType;
                    order[SingleOrderProperties.OrderType] = orderType;
                    order[SingleOrderProperties.OrderStatus] = OrderStatus;
                    order[SingleOrderProperties.OriginalOrderStatus] = OriginalOrderStatus;
                    order[SingleOrderProperties.ExecType] = ExecType;
                    order[SingleOrderProperties.OriginalTimeInForce] = timeInForce;
                    order[SingleOrderProperties.TimeInForce] = timeInForce;
                    order[SingleOrderProperties.OriginalHandleInst] = handleValue;
                    order[SingleOrderProperties.HandleInst] = handleValue;
                    order[SingleOrderProperties.AON] = hasAON;
                    order[SingleOrderProperties.MinQty] = minQ;
                    order[SingleOrderProperties.OriginalAON] = hasAON;
                    order[SingleOrderProperties.OriginalMinQty] = minQ;

                    order[SingleOrderProperties.PlacementDateTime] = placementDateTime;
                    order[SingleOrderProperties.OrderCreatedBySysDateTime] = orderCreatedBySysDateTime;
                    order[SingleOrderProperties.OrderRecievedDateTime] = DateTime.Now;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;
                    order[SingleOrderProperties.Note] = Note;

                    order[SingleOrderProperties.HasSystemError] = false;

                    order[SingleOrderProperties.ExpirationDate] = expiryDate;  //maghrabi 11 Jan
                    order[SingleOrderProperties.RequestedPrice] = price;  //maghrabi 11 Jan
                    order[SingleOrderProperties.RequestedQty] = quantity;



                    // lock only shared entities as each method create its only copy of variable except shared entities
                    lock (m_syncObject)
                    {
                        if (!m_username_ReqOrdIDs.ContainsKey(username))
                        {
                            m_username_ReqOrdIDs.Add(username, new List<Guid>());
                        }


                        if (m_username_ReqOrdIDs[username].Contains(requesterOrderID) == false)
                            m_username_ReqOrdIDs[username].Add(requesterOrderID);


                        if (m_ReqOrdID_OrdID.ContainsKey(requesterOrderID) == false)
                            m_ReqOrdID_OrdID.Add(requesterOrderID, orderID);


                        if (m_orderID_ordersDetails.ContainsKey(orderID) == false)
                            m_orderID_ordersDetails.Add(orderID, order);


                        if (m_OrdID_username.ContainsKey(orderID) == false)
                            m_OrdID_username.Add(orderID, username);

                        _dbReqIDs.Add(requesterOrderID); // maghrabi 26 jan 2016

                    }


                    MarketFixClient.PlaceNewSingleOrder(clOrderID, clientID.ToString(), securityID, quantity, price, custodyID, orderSideLookup.FixValue.ToCharArray()[0], orderTypeLookup.FixValue.ToCharArray()[0], currencyLookup.FixValue, exchangeLookup.FixValue, timeInForceLookup.FixValue.ToCharArray()[0], groupID, handleInstLookup.FixValue.ToCharArray()[0], expiryDate, hasAON, minQ);


                    ts.Complete();
                    db = null;
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                if (orderID > -1)
                { RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }



        internal static void HandleModifyOrder(string username, Guid clientKey, Guid requesterOrderID, long orderID, int quantity, double price, string orderType, string timeInForce, Dictionary<string, object> optionalParam)
        {
            try
            {


                SingleOrder order = m_orderID_ordersDetails[orderID];

                OrderActions orderAction = OrderActions.Modify;
                bool isMcsdAllocRequired = false;
                int mcsdQuantity;
                isMcsdAllocRequired = IsMCSDSellAllocationRequired(quantity, optionalParam, order, out mcsdQuantity, orderAction);

                if (isMcsdAllocRequired)
                {
                    ModifyOrderMCSD(username, clientKey, requesterOrderID, orderID, quantity, price, orderType, timeInForce, optionalParam, mcsdQuantity);
                }

                else
                {
                    ModifyOrderFIXGatway(username, clientKey, requesterOrderID, orderID, quantity, price, orderType, timeInForce, optionalParam);
                }

            }
            catch (Exception exp)
            {
                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = exp.Message, RequesterOrderID = requesterOrderID } });
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error ModifyOrder , ClientKey {0}, Error: {1}", clientKey, exp.Message), ConsoleColor.Red, ConsoleColor.White, true);
            }
        }


        internal static void ModifyOrderFIXGatway(string username, Guid clientKey, Guid requesterOrderID, long orderID, int quantity, double price, string orderType, string timeInForce, Dictionary<string, object> optionalParam)
        {
            try
            {
                SingleOrder order = m_orderID_ordersDetails[orderID];
                string[] arr = order[SingleOrderProperties.ClOrderID].ToString().Split(new char[] { '-' });
                string ordID = arr[0];
                int changeID = int.Parse(arr[1]);
                changeID++;

                //string newOrigClOrdID = order.ClOrderID;
                string newClOrdID = string.Format("{0}-{1}", ordID, changeID);

                bool hasAON = false;
                int minQ = 0;
                if (optionalParam != null)
                    if (optionalParam.ContainsKey("AON") && (bool)optionalParam["AON"])
                        hasAON = true;
                if (hasAON)
                    minQ = (int)optionalParam["MinQty"];


                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    DatabaseMethods db = new DatabaseMethods();
                    /////db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, quantity, quantity - order.ExecutedQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Modify Order Request", false, "", timeInForce == string.Empty ? order.TimeInForce : timeInForce, null);


                    order[SingleOrderProperties.ClOrderID] = newClOrdID;
                    order[SingleOrderProperties.IsPending] = true;
                    order[SingleOrderProperties.IsPendingMcsd] = false;
                    order[SingleOrderProperties.Note] = "Pending Modify Request";
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;


                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(SingleOrderProperties.ClOrderID, order[SingleOrderProperties.ClOrderID]);

                    orders_Columns.Add(SingleOrderProperties.IsPending, true);

                    orders_Columns.Add(SingleOrderProperties.Note, order[SingleOrderProperties.Note]);

                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, false);

                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedPrice, price);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedQuantity, quantity);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.AON, hasAON);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedMinQty, minQ);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsModifyRequest, true);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsUserRequest, true);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedOrderType, orderType);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedTimeInForce, timeInForce);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    MarketFixClient.ModifyCancelOrder(order[SingleOrderProperties.ClientID].ToString(), order[SingleOrderProperties.SecurityCode].ToString(), string.Format("{0}-{1}", ordID, changeID), order[SingleOrderProperties.OrigClOrdID].ToString(), quantity, price, Lookups.GetTimeInForceLookupByCodeValue(timeInForce).FixValue.ToCharArray()[0], Lookups.GetOrderTypeLookupByCodeValue(orderType).FixValue.ToCharArray()[0], Lookups.GetOrderSidesLookupByCodeValue(order[SingleOrderProperties.OrderSide].ToString()).FixValue.ToCharArray()[0], hasAON, minQ);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }

        }

        internal static void HandleCancelOrder(string username, Guid clientKey, Guid requesterOrderID, long orderID, Dictionary<string, object> optionalParams)
        {
            try
            {

                CancelOrderFixGatway(username, clientKey, requesterOrderID, orderID);
                //// Canceled orders will hit the exchange first . if order canceled successfully then MCSD allocation will applied.
                //SingleOrder order = m_orderID_ordersDetails[orderID];
                //bool isMcsdAllocRequired = false;

                //int mcsdQuantity = 0;
                //OrderActions orderAction = OrderActions.Cancel;

                //isMcsdAllocRequired = IsMCSDSellAllocationRequired(0, optionalParams, order, out mcsdQuantity, orderAction);

                //if (isMcsdAllocRequired)
                //{
                //    CancelOrderFixGatway(username, clientKey, requesterOrderID, orderID, mcsdQuantity);
                //}

                //else // sell order doesn't need allocation from MCSD
                //{
                //    CancelOrderFixGatway(username, clientKey, requesterOrderID, orderID, 0);
                //}
            }
            catch (Exception exp)
            {
                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = exp.Message, RequesterOrderID = requesterOrderID } });
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error CancelOrder , ClientKey {0}, Error: {1}", clientKey, exp.Message), ConsoleColor.Red, ConsoleColor.White, true);
            }
        }

        internal static void CancelOrderFixGatway(string username, Guid clientKey, Guid requesterOrderID, long orderID)
        {
            try
            {
                // Order Side
                SingleOrder order = m_orderID_ordersDetails[orderID];
                LookupItem orderSideLookup = Lookups.GetOrderSidesLookupByCodeValue(order[SingleOrderProperties.OrderSide].ToString());
                lock (order)
                {
                    string[] arr = order[SingleOrderProperties.ClOrderID].ToString().Split(new char[] { '-' });
                    string ordID = arr[0];
                    int changeID = int.Parse(arr[1]);
                    changeID++;

                    //string newOrigClOrdID = order.ClOrderID;
                    string newClOrdID = string.Format("{0}-{1}", ordID, changeID);


                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        DatabaseMethods db = new DatabaseMethods();
                        //db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, order.CurrentQuantity, order.RemainingQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Cancel Order Request", false, "", order.TimeInForce, null);


                        order[SingleOrderProperties.ClOrderID] = newClOrdID;
                        order[SingleOrderProperties.IsPending] = true;
                        order[SingleOrderProperties.Note] = "Pending Cancel Request";
                        order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                        //   order[SingleOrderProperties.Alloc_McsdQuantity] = McsdQty;


                        Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                        orders_Columns.Add(SingleOrderProperties.ClOrderID, order[SingleOrderProperties.ClOrderID]);

                        orders_Columns.Add(SingleOrderProperties.IsPending, true);

                        orders_Columns.Add(SingleOrderProperties.Note, order[SingleOrderProperties.Note]);

                        orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                        // orders_Columns.Add(SingleOrderProperties.Alloc_McsdQuantity, McsdQty);

                        Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                        orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                        Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);


                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsCancelRequest, true);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsUserRequest, true);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);

                        db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                        MarketFixClient.CancelOrder(order[SingleOrderProperties.ClientID].ToString(), order[SingleOrderProperties.SecurityCode].ToString(), string.Format("{0}-{1}", ordID, changeID), order[SingleOrderProperties.OrigClOrdID].ToString(), orderSideLookup.FixValue.ToCharArray()[0]);

                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }

        }


        #endregion Place / Modify / Cancel Orders

        #region Queries

        public static SingleOrder GetOrder(long orderID)
        {
            return m_orderID_ordersDetails[orderID];
        }

        public static SingleOrder GetOrder(string bourseOrderID)
        {
            return m_orderID_ordersDetails.Values.SingleOrDefault(b => b.Data[SingleOrderProperties.BourseOrderID].ToString() == bourseOrderID);
        }

        public static SingleOrder GetOrder(string clOrdID, bool isOrig) // to avoid ambiguous call
        {
            return m_orderID_ordersDetails.Values.SingleOrDefault(b => b.Data[SingleOrderProperties.ClOrderID].ToString() == clOrdID);
        }

        internal static SingleOrder GetOrder(Guid requesterOrderID)
        {
            if (!m_ReqOrdID_OrdID.ContainsKey(requesterOrderID))
            {
                return null;
            }
            long clOrderID = m_ReqOrdID_OrdID[requesterOrderID];
            if (!m_orderID_ordersDetails.ContainsKey(clOrderID))
            {
                return null;
            }
            return m_orderID_ordersDetails[m_ReqOrdID_OrdID[requesterOrderID]];
        }

        internal static string GetOrdSessionIfAvailable(long ordID)
        {
            if (m_OrdID_username.ContainsKey(ordID))
            {
                string sessionKey = m_OrdID_username[ordID];
                if (Sessions.IsOnlineOrHasOfflineUpdates(sessionKey))
                    return sessionKey;
            }
            return null;
        }

        internal static bool CallbackHasReqOrdID(Guid requesterOrderID)
        {
            return m_ReqOrdID_OrdID.ContainsKey(requesterOrderID) || _dbReqIDs.Contains(requesterOrderID);
        }

        #endregion Queries

        #region Helpers
        private static void RemoveOrderRef(long orderID)
        {
            lock (m_syncObject)
            {
                SingleOrder order = m_orderID_ordersDetails[orderID];
                Guid requesterOrderID = (Guid)order[SingleOrderProperties.RequesterOrderID];
                string username = m_OrdID_username[orderID];
                m_ReqOrdID_OrdID.Remove(requesterOrderID);
                m_username_ReqOrdIDs[username].Remove(requesterOrderID);
                m_OrdID_username.Remove(orderID);
                m_orderID_ordersDetails.Remove(orderID);
            }
        }


        private static void RemoveOrderRef(long orderID, Guid requesterOrderID)
        {
            lock (m_syncObject)
            {


                m_ReqOrdID_OrdID.Remove(requesterOrderID);

                if (m_OrdID_username.ContainsKey(orderID))
                {
                    string username = m_OrdID_username[orderID];
                    m_username_ReqOrdIDs[username].Remove(requesterOrderID);
                }

                m_OrdID_username.Remove(orderID);

                m_orderID_ordersDetails.Remove(orderID);


            }
        }

        public static bool HasExecutionID(string execID)
        {
            return m_handledExecutionIDs.Contains(execID);
        }

        /// <summary>
        /// returns false if ExecID already existed
        /// </summary>
        /// <param name="execID"></param>
        /// <returns></returns>
        public static bool AddExecutionID(string execID)
        {
            lock (m_handledExecutionIDs)
            {
                if (!m_handledExecutionIDs.Contains(execID))
                {
                    m_handledExecutionIDs.Add(execID);
                    return true;
                }
                return false;
            }
        }

        public static void UpdateBourseOrderID(long clOrderID, string bourseOrderID)
        {
            m_orderID_ordersDetails[clOrderID].Data[SingleOrderProperties.BourseOrderID] = bourseOrderID;
        }



        #endregion Helpers

        #region Monitor

        internal static List<SingleOrder> monitor_GetOrders(string side, string securityCode)
        {
            return m_orderID_ordersDetails.Values.Where(o => o.Data[SingleOrderProperties.OrderSide].ToString() == side && o.Data[SingleOrderProperties.SecurityCode].ToString() == securityCode).ToList();
        }

        #endregion Monitor

        #region MCSD ALLOCATION

        #region MCSD Allocation Check
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private static bool _IsMCSDSellAllocationRequired(int requestedQuantity, Dictionary<string, object> optionaParam, SingleOrder order, out int mcsdQuantity, OrderActions orderAction)
        {
            bool isMcsdAlloc = false;
            mcsdQuantity = -1;

            if (optionaParam == null)
                return false;

            if (!optionaParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE))
            {
                return false;
            }
            else  // client requested to allocate order quantity in MCSD
            {
                if (orderAction == OrderActions.New)
                {
                    mcsdQuantity = requestedQuantity;
                    return isMcsdAlloc = true;
                }
                else if (orderAction == OrderActions.Modify)
                {
                    int CurrentQuantity = Convert.ToInt32(order[SingleOrderProperties.CurrentQuantity]);
                    if (CurrentQuantity == requestedQuantity)
                        return false; // no allocation required becasue there is no change in Quantity
                    int remainingQuantity = Convert.ToInt32(order[SingleOrderProperties.RemainingQuantity]);
                    mcsdQuantity = requestedQuantity - remainingQuantity;
                    if (mcsdQuantity == 0)
                        mcsdQuantity = remainingQuantity;
                    return isMcsdAlloc = true;
                }
                else if (orderAction == OrderActions.Cancel) //allocation for cancel order
                {
                    int remainingQuantity = (int)order[SingleOrderProperties.RemainingQuantity];
                    mcsdQuantity = -remainingQuantity;
                    return isMcsdAlloc = true;
                }
            }
            return isMcsdAlloc;
        }

        private static bool IsMCSDSellAllocationRequired(int requestedQuantity, Dictionary<string, object> optionaParam, SingleOrder order, out int mcsdQuantity, OrderActions orderAction)
        {
            bool isMcsdAlloc = false;
            mcsdQuantity = -1;


            if (_AllowMcsdAllocation == false)
                return false;  // Mcsd allocation is not enabled.



            //else  // client requested to allocate order quantity in MCSD
            #region Check MCSD is enabled on ations and mcsd tyeps

            string allocationType = string.Empty;
            string orderActionValue = orderAction.ToString();


            if (optionaParam != null)
            {

                if (optionaParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE))
                {
                    allocationType = optionaParam[ALLOC_REQ_FIELDS.ALLOC_TYPE].ToString().Trim();
                    if (_ActiveAllocationTypes.Contains(allocationType) == false)
                    {
                        throw new Exception(string.Format("Allocaion Type {0} is not allowed", allocationType));
                    }


                    if (_ActiveAllocationActions.Contains(orderActionValue.ToString()) == false)
                    {
                        if (orderActionValue != "Cancel")
                            throw new Exception(string.Format("Allocaion for action {0} is not allowed", orderActionValue));
                    }
                }
            }
            else   // order action is modify or cancel
            {
                // will check on order acion only
                if (_ActiveAllocationActions.Contains(orderActionValue.ToString()) == false)
                {
                    if (orderActionValue != "Cancel")
                        throw new Exception(string.Format("Allocaion for action {0} is not allowed", orderActionValue));
                }

            }

            #endregion


            if (orderAction == OrderActions.New)
            {
                if (optionaParam == null)
                    return false;

                if (!optionaParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE))
                    return false;


                mcsdQuantity = requestedQuantity;
                return isMcsdAlloc = true;
            }
            else if (orderAction == OrderActions.Modify)
            {
                if (order[SingleOrderProperties.IsMcsdAllocRequired] == null)
                    return false;

                if (Convert.ToBoolean(order[SingleOrderProperties.IsMcsdAllocRequired]) == false)
                    return false;

                // IsMcsdAllocRequired = true
                int CurrentQuantity = Convert.ToInt32(order[SingleOrderProperties.CurrentQuantity]);
                if (CurrentQuantity == requestedQuantity)
                    return false; // no allocation required becasue there is no change in Quantity
              //  int remainingQuantity = Convert.ToInt32(order[SingleOrderProperties.RemainingQuantity]);
                mcsdQuantity = requestedQuantity - CurrentQuantity;
                
                return isMcsdAlloc = true;
            }
            else if (orderAction == OrderActions.Cancel) //allocation for cancel order
            {
                if (order[SingleOrderProperties.IsMcsdAllocRequired] == null)
                    return false;

                if (Convert.ToBoolean(order[SingleOrderProperties.IsMcsdAllocRequired]) == false)
                    return false;
                // IsMcsdAllocRequired = true
                int remainingQuantity = (int)order[SingleOrderProperties.RemainingQuantity];
                mcsdQuantity = -remainingQuantity;
                return isMcsdAlloc = true;
            }

            return isMcsdAlloc;
        }

        private static bool IsMCSDSellAllocationRequired_TEST(int requestedQuantity, Dictionary<string, object> optionaParam, SingleOrder order, out int mcsdQuantity, OrderActions orderAction)
        {
            bool isMcsdAlloc = false;
            mcsdQuantity = -1;


            if (_AllowMcsdAllocation == false)
                return false;  // Mcsd allocation is not enabled.


            //else  // client requested to allocate order quantity in MCSD
            #region Check MCSD is enabled on ations and mcsd tyeps

            string allocationType = string.Empty;
            string orderActionValue = orderAction.ToString();


            if (optionaParam != null)
            {

                if (optionaParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE))
                {
                    allocationType = optionaParam[ALLOC_REQ_FIELDS.ALLOC_TYPE].ToString();
                    if (_ActiveAllocationTypes.Contains(allocationType) == false)
                    {
                        throw new Exception(string.Format("Allocaion Type {0} is not allowed", allocationType));
                    }

                    if (_ActiveAllocationActions.Contains(orderActionValue.ToString()) == false)
                    {
                        throw new Exception(string.Format("Allocaion for action {0} is not allowed", orderActionValue));
                    }
                }
            }
            else   // order action is modify or cancel
            {
                // will check on order acion only
                if (_ActiveAllocationActions.Contains(orderActionValue.ToString()) == false)
                {
                    throw new Exception(string.Format("Allocaion for action {0} is not allowed", orderActionValue));
                }

            }

            #endregion


            if (orderAction == OrderActions.New)
            {
                if (optionaParam == null)
                    return false;

                if (!optionaParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE))
                    return false;


                mcsdQuantity = requestedQuantity;
                return isMcsdAlloc = true;
            }
            else if (orderAction == OrderActions.Modify)
            {
                if (order[SingleOrderProperties.IsMcsdAllocRequired] == null)
                    return false;

                if (Convert.ToBoolean(order[SingleOrderProperties.IsMcsdAllocRequired]) == false)
                    return false;

                // IsMcsdAllocRequired = true
                int CurrentQuantity = Convert.ToInt32(order[SingleOrderProperties.CurrentQuantity]);
                if (CurrentQuantity == requestedQuantity)
                    return false; // no allocation required becasue there is no change in Quantity
                int remainingQuantity = Convert.ToInt32(order[SingleOrderProperties.RemainingQuantity]);
                mcsdQuantity = requestedQuantity - remainingQuantity;
                if (mcsdQuantity == 0)
                    mcsdQuantity = remainingQuantity;
                return isMcsdAlloc = true;
            }
            else if (orderAction == OrderActions.Cancel) //allocation for cancel order
            {
                if (order[SingleOrderProperties.IsMcsdAllocRequired] == null)
                    return false;

                if (Convert.ToBoolean(order[SingleOrderProperties.IsMcsdAllocRequired]) == false)
                    return false;
                // IsMcsdAllocRequired = true
                int remainingQuantity = (int)order[SingleOrderProperties.RemainingQuantity];
                mcsdQuantity = -remainingQuantity;
                return isMcsdAlloc = true;
            }

            return isMcsdAlloc;
        }
        #endregion

        #region MCSD Allocation Request

        private static void PlaceNewSingleOrderMCSD(string username, Guid clientKey, Guid requesterOrderID, int clientID, string custodyID, string securityID,
          string orderSide, double price, int quantity, string orderType, DateTime placementDateTime, string timeInForce,
          CurrencyItem currency, string exchangeID, DateTime orderCreatedBySysDateTime, string groupID,
          string marketID, HandleInstruction handleInst, DateTime expiryDate, Dictionary<string, object> optionalParam, int mcsdQty)
        {
            long orderID = -1;
            try
            {
                // Get Currency 
              //  LookupItem currencyLookup = Lookups.GetCurrencyLookupByCurrencyCode(currency.Code);
                //// Get Destination Type
              LookupItem exchangeLookup = Lookups.GetExchangeDestinationByExchangeID(exchangeID);
                //// order Type
                //LookupItem orderTypeLookup = Lookups.GetOrderTypeLookupByCodeValue(orderType);
                //// Order Side
                //LookupItem orderSideLookup = Lookups.GetOrderSidesLookupByCodeValue(orderSide);
                //// Time In Force
                //LookupItem timeInForceLookup = Lookups.GetTimeInForceLookupByCode(timeInForce);
                // Handle Instruction
                string handleValue = Enum.GetName(typeof(HandleInstruction), handleInst);
                LookupItem handleInstLookup = Lookups.GetHandleInstTypeLookupByCodeValue(handleValue);
                bool hasAON = false;
                int minQ = 0;
                if (optionalParam != null)
                    if (optionalParam.ContainsKey("AON") && (bool)optionalParam["AON"])
                        hasAON = true;
                if (hasAON)// MinQty must be existed if AON = true as it's already been validated before
                    minQ = (int)optionalParam["MinQty"];



             using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    string OrderStatus = string.Empty;
                    string OriginalOrderStatus = string.Empty;
                    string ExecType = string.Empty;
                    string Note = string.Empty;
                    bool IsPendingMcsd = false;


                    OrderStatus = ORD_STATUS.PendingNew;
                    OriginalOrderStatus = ORD_STATUS.PendingNew;
                    ExecType = EXEC_TYP.PendingNew;
                    Note = "awaiting for MCSD acceptance response";
                    IsPendingMcsd = true;

                    string allocType = string.Empty;
                    string allocUnifiedCode = string.Empty;
                    bool isAllocRequried = true;

                    try
                    {
                        allocType = optionalParam["ALLOC_TYPE"].ToString();
                    }

                    catch (Exception exp)
                    {
                        throw new Exception("ALLOC_TYPE is not valid");
                    }

                    try
                    {
                        allocUnifiedCode = optionalParam["UnifiedCode"].ToString();
                    }

                    catch (Exception exp)
                    {
                        throw new Exception("UnifiedCode is not valid");
                    }

                    DatabaseMethods db = new DatabaseMethods();

                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add(SingleOrderProperties.CurrencyID, currency.ID);
                    data.Add(SingleOrderProperties.GroupID, groupID);
                    data.Add(SingleOrderProperties.MarketID, marketID);
                    data.Add(SingleOrderProperties.ExchangeID, exchangeID);
                    data.Add(SingleOrderProperties.RequesterOrderID, requesterOrderID);
                    data.Add(SingleOrderProperties.ClientID, clientID);
                    data.Add(SingleOrderProperties.CustodyID, custodyID);
                    data.Add(SingleOrderProperties.SecurityCode, securityID);
                    data.Add(SingleOrderProperties.OrderSide, orderSide);
                    data.Add(SingleOrderProperties.CurrentPrice, price);
                    data.Add(SingleOrderProperties.OriginalPrice, price);
                    data.Add(SingleOrderProperties.PlacementDateTime, placementDateTime);
                    data.Add(SingleOrderProperties.OriginalQuantity, quantity);
                    data.Add(SingleOrderProperties.RemainingQuantity, 0);
                    data.Add(SingleOrderProperties.ExecutedQuantity, 0);
                    data.Add(SingleOrderProperties.LastExecQuantity, 0);
                    data.Add(SingleOrderProperties.LastExecPrice, 0);
                    data.Add(SingleOrderProperties.AvgPrice, 0);
                    data.Add(SingleOrderProperties.CurrentQuantity, quantity);
                    data.Add(SingleOrderProperties.OriginalOrderType, orderType);
                    data.Add(SingleOrderProperties.OrderType, orderType);
                    data.Add(SingleOrderProperties.OrderStatus, OrderStatus);

                    data.Add(SingleOrderProperties.OriginalOrderStatus, OriginalOrderStatus);

                    data.Add(SingleOrderProperties.ExecType, ExecType);
                    data.Add(SingleOrderProperties.OrderCreatedBySysDateTime, orderCreatedBySysDateTime);
                    data.Add(SingleOrderProperties.OrderRecievedDateTime, DateTime.Now);
                    data.Add(SingleOrderProperties.ModifiedDateTime, DateTime.Now);
                    data.Add(SingleOrderProperties.Note, Note);
                    data.Add(SingleOrderProperties.IsPending, false);
                    // data.Add(SingleOrderProperties.IsPendingMCDR, true);
                    data.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);// maghrabi
                    data.Add(SingleOrderProperties.IsActive, false);
                    data.Add(SingleOrderProperties.IsExecuted, false);
                    data.Add(SingleOrderProperties.IsCompleted, false);
                    data.Add(SingleOrderProperties.HasSystemError, false);
                    //data.Add(SingleOrderProperties.ErrorMessage, string.Empty);
                    data.Add(SingleOrderProperties.OriginalTimeInForce, timeInForce);
                    data.Add(SingleOrderProperties.TimeInForce, timeInForce);
                    data.Add(SingleOrderProperties.OriginalHandleInst, handleValue);
                    data.Add(SingleOrderProperties.HandleInst, handleValue);
                    data.Add(SingleOrderProperties.ExpirationDate, expiryDate.ToShortDateString());
                    data.Add(SingleOrderProperties.ExpirationDateTime, expiryDate);
                    data.Add(SingleOrderProperties.AON, hasAON);
                    data.Add(SingleOrderProperties.OriginalAON, hasAON);
                    data.Add(SingleOrderProperties.OriginalMinQty, minQ);
                    data.Add(SingleOrderProperties.MinQty, minQ);

                    data.Add(SingleOrderProperties.AllocType, allocType);
                    data.Add(SingleOrderProperties.AllocUnifiedCode, allocUnifiedCode);
                    data.Add(SingleOrderProperties.IsMcsdAllocRequired, isAllocRequried);
                    data.Add(SingleOrderProperties.McsdrAllocQty, mcsdQty);
                    data.Add(SingleOrderProperties.McsdrExpiryDate, expiryDate);
                    data.Add(SingleOrderProperties.ActionOnAllocResponse, ActionOnAllocResponse.SendNewOrder);
                    //  data.Add(SingleOrderProperties.Alloc_McsdQuantity, mcsdQty);


                    //OrderDetails Table
                    Dictionary<string, object> dataDetails = new Dictionary<string, object>();
                    dataDetails.Add(SingleOrdDetailsProps.CurrentPrice, price);
                    dataDetails.Add(SingleOrdDetailsProps.CurrentQuantity, quantity);
                    dataDetails.Add(SingleOrdDetailsProps.RemainingQuantity, quantity);
                    dataDetails.Add(SingleOrdDetailsProps.ExecutedQuantity, 0);
                    dataDetails.Add(SingleOrdDetailsProps.AvgPrice, 0);
                    dataDetails.Add(SingleOrdDetailsProps.ExecPrice, 0);
                    dataDetails.Add(SingleOrdDetailsProps.LastExecQuantity, 0);
                    dataDetails.Add(SingleOrdDetailsProps.OrderType, orderType);
                    dataDetails.Add(SingleOrdDetailsProps.OrderStatus, OrderStatus);
                    dataDetails.Add(SingleOrdDetailsProps.ExecType, ExecType);
                    dataDetails.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    dataDetails.Add(SingleOrdDetailsProps.Note, Note);
                    dataDetails.Add(SingleOrdDetailsProps.HasSystemError, false);
                    dataDetails.Add(SingleOrdDetailsProps.ErrorMessage, string.Empty);
                    dataDetails.Add(SingleOrdDetailsProps.TimeInForce, timeInForce);
                    dataDetails.Add(SingleOrdDetailsProps.IsNewOrderRequest, true);
                    dataDetails.Add(SingleOrdDetailsProps.IsUserRequest, true);
                    dataDetails.Add(SingleOrdDetailsProps.HandleInst, handleValue);
                    dataDetails.Add(SingleOrdDetailsProps.AON, hasAON);
                    dataDetails.Add(SingleOrdDetailsProps.MinQty, minQ);
                    dataDetails.Add(SingleOrdDetailsProps.IsMcdrRec, true);
                    dataDetails.Add(SingleOrdDetailsProps.RequestedQuantity, mcsdQty);




                    orderID = db.AddNewSingleOrder(clientKey, username, data, dataDetails);

                    string clOrderID = string.Format("{0}-{1}", orderID.ToString(), "1");

                    SingleOrder order = new SingleOrder();

                    order[SingleOrderProperties.OrderID] = orderID;
                    order[SingleOrderProperties.ClOrderID] = clOrderID;
                    order[SingleOrderProperties.GroupID] = groupID;
                    order[SingleOrderProperties.MarketID] = marketID;
                    order[SingleOrderProperties.ExchangeID] = exchangeID;
                    order[SingleOrderProperties.CurrencyID] = currency.ID;
                    order[SingleOrderProperties.RequesterOrderID] = requesterOrderID;
                    order[SingleOrderProperties.ClientID] = clientID;
                    order[SingleOrderProperties.CustodyID] = custodyID;
                    order[SingleOrderProperties.SecurityCode] = securityID;
                    order[SingleOrderProperties.OrderSide] = orderSide;

                    order[SingleOrderProperties.IsPendingMcsd] = IsPendingMcsd;
                    order[SingleOrderProperties.IsActive] = false;
                    order[SingleOrderProperties.IsPending] = false;
                    order[SingleOrderProperties.IsCompleted] = false;
                    order[SingleOrderProperties.IsExecuted] = false;

                    order[SingleOrderProperties.CurrentPrice] = price;
                    order[SingleOrderProperties.OriginalPrice] = price;
                    order[SingleOrderProperties.AvgPrice] = 0;
                    order[SingleOrderProperties.LastExecPrice] = 0;

                    order[SingleOrderProperties.OriginalQuantity] = quantity;
                    order[SingleOrderProperties.RemainingQuantity] = 0;
                    order[SingleOrderProperties.ExecutedQuantity] = 0;
                    order[SingleOrderProperties.LastExecQuantity] = 0;
                    order[SingleOrderProperties.CurrentQuantity] = quantity;

                    order[SingleOrderProperties.OriginalOrderType] = orderType;
                    order[SingleOrderProperties.OrderType] = orderType;
                    order[SingleOrderProperties.OrderStatus] = OrderStatus;
                    order[SingleOrderProperties.OriginalOrderStatus] = OriginalOrderStatus;
                    order[SingleOrderProperties.ExecType] = ExecType;
                    order[SingleOrderProperties.OriginalTimeInForce] = timeInForce;
                    order[SingleOrderProperties.TimeInForce] = timeInForce;
                    order[SingleOrderProperties.OriginalHandleInst] = handleValue;
                    order[SingleOrderProperties.HandleInst] = handleValue;
                    order[SingleOrderProperties.AON] = hasAON;
                    order[SingleOrderProperties.MinQty] = minQ;
                    order[SingleOrderProperties.OriginalAON] = hasAON;
                    order[SingleOrderProperties.OriginalMinQty] = minQ;

                    order[SingleOrderProperties.PlacementDateTime] = placementDateTime;
                    order[SingleOrderProperties.OrderCreatedBySysDateTime] = orderCreatedBySysDateTime;
                    order[SingleOrderProperties.OrderRecievedDateTime] = DateTime.Now;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;
                    order[SingleOrderProperties.Note] = Note;

                    order[SingleOrderProperties.HasSystemError] = false;

                    order[SingleOrderProperties.ExpirationDate] = expiryDate;  //maghrabi 11 Jan
                    order[SingleOrderProperties.ExpirationDateTime] = expiryDate;  //maghrabi 11 Jan
                    //  order[SingleOrderProperties.RequestedPrice] = price;  //maghrabi 11 Jan
                    // order[SingleOrderProperties.RequestedQty] = quantity;
                    //   order[SingleOrderProperties.Alloc_McsdQuantity] = mcsdQty;

                    order[SingleOrderProperties.AllocType] = allocType;
                    order[SingleOrderProperties.AllocUnifiedCode] = allocUnifiedCode;
                    order[SingleOrderProperties.IsMcsdAllocRequired] = isAllocRequried;

                    order[SingleOrderProperties.McsdrAllocQty] = mcsdQty;
                    order[SingleOrderProperties.McsdrExpiryDate] = expiryDate;
                    order[SingleOrderProperties.ActionOnAllocResponse] = ActionOnAllocResponse.SendNewOrder;

               

                    // lock only shared entities as each method create its only copy of variable except shared entities
                    lock (m_syncObject)
                    {
                        if (!m_username_ReqOrdIDs.ContainsKey(username))
                        {
                            m_username_ReqOrdIDs.Add(username, new List<Guid>());
                        }

                        if (m_username_ReqOrdIDs[username].Contains(requesterOrderID) == false)
                            m_username_ReqOrdIDs[username].Add(requesterOrderID);


                        if (m_ReqOrdID_OrdID.ContainsKey(requesterOrderID) == false)
                            m_ReqOrdID_OrdID.Add(requesterOrderID, orderID);


                        if (m_orderID_ordersDetails.ContainsKey(orderID) == false)
                            m_orderID_ordersDetails.Add(orderID, order);


                        if (m_OrdID_username.ContainsKey(orderID) == false)
                            m_OrdID_username.Add(orderID, username);

                        _dbReqIDs.Add(requesterOrderID); // maghrabi 26 jan 2016

                    }

                    // Alocate Quantity of sell order.
                    // string unifiedCode = optionalParam["UnifiedCode"].ToString();
                    // string allocationType = optionalParam["ALLOC_TYPE"].ToString();

                McsdGatwayManager.PlaceMcsdNewOrderAllocation(quantity, exchangeLookup.FixValue, custodyID, m_BrokerCode, allocUnifiedCode, securityID, requesterOrderID, allocType, expiryDate);
                 ts.Complete();
                   db = null;

               //    McsdGatwayManager.PlaceMcsdNewOrderAllocation(quantity, exchangeLookup.FixValue, custodyID, m_BrokerCode, allocUnifiedCode, securityID, requesterOrderID, allocType, expiryDate);
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error MCSD allocating order  , ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //if (orderID > -1)
                //{ RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr :" + ex.Message, RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }


        internal static void ModifyOrderMCSD(string username, Guid clientKey, Guid requesterOrderID, long orderID, int quantity, double
            price, string orderType, string timeInForce, Dictionary<string, object> optionalParam, int mcsdQuantity)
        {
            try
            {
                SingleOrder order = m_orderID_ordersDetails[orderID];
                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    DatabaseMethods db = new DatabaseMethods();
                    /////db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, quantity, quantity - order.ExecutedQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Modify Order Request", false, "", timeInForce == string.Empty ? order.TimeInForce : timeInForce, null);
                    // order[SingleOrderProperties.ClOrderID] = newClOrdID;
                    //order[SingleOrderProperties.IsPending] = true;
                    order[SingleOrderProperties.Note] = "Pending MCSD Modify Request";
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;
                    order[SingleOrderProperties.IsPendingMcsd] = true;
                    //order[SingleOrderProperties.OrderStatus] = "PendingEdit";

                    order[SingleOrderProperties.RequestedQty] = quantity;
                    order[SingleOrderProperties.RequestedPrice] = price;
                    order[SingleOrderProperties.RequestedOrderType] = orderType;
                    order[SingleOrderProperties.RequestedTimeInForce] = timeInForce;
                    order[SingleOrderProperties.ActionOnAllocResponse] = ActionOnAllocResponse.ModifyOrder;
                    //   order[SingleOrderProperties.Alloc_McsdQuantity] = mcsdQuantity;

                    // order.OptionalParams = optionalParam;
                    bool hasAON = false;
                    int minQty = 0;
                    if (optionalParam != null)
                        if (optionalParam.ContainsKey("AON") && (bool)optionalParam["AON"])
                            hasAON = true;
                    if (hasAON)
                        minQty = (int)optionalParam["MinQty"];


                    order[SingleOrderProperties.RequestedAON] = hasAON;
                    order[SingleOrderProperties.RequestedMinQty] = minQty;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                    order[SingleOrderProperties.McsdrAllocQty] = mcsdQuantity;

                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(SingleOrderProperties.ClOrderID, order[SingleOrderProperties.ClOrderID]);

                    orders_Columns.Add(SingleOrderProperties.IsPending, true);

                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, true);

                    orders_Columns.Add(SingleOrderProperties.Note, order[SingleOrderProperties.Note]);

                    orders_Columns.Add(SingleOrderProperties.OrderStatus, order[SingleOrderProperties.OrderStatus]);

                    orders_Columns.Add(SingleOrderProperties.ActionOnAllocResponse, ActionOnAllocResponse.ModifyOrder);
                   
                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);



                    // update allocation columns

                    orders_Columns.Add(SingleOrderProperties.RequestedQty, quantity);
                    orders_Columns.Add(SingleOrderProperties.RequestedPrice, price);
                    orders_Columns.Add(SingleOrderProperties.RequestedOrderType, orderType);
                    orders_Columns.Add(SingleOrderProperties.RequestedTimeInForce, timeInForce);
                    //  orders_Columns.Add(SingleOrderProperties.Alloc_McsdQuantity, mcsdQuantity);
                    orders_Columns.Add(SingleOrderProperties.RequestedAON, hasAON);
                    orders_Columns.Add(SingleOrderProperties.RequestedMinQty, minQty);
                    orders_Columns.Add(SingleOrderProperties.McsdrAllocQty, mcsdQuantity);


                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);

                    //ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                    // ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);
                    //  ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedPrice, price);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedQuantity, mcsdQuantity);

                    //ordersDetails_Columns.Add(SingleOrdDetailsProps.AON, hasAON);
                    // ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedMinQty, minQ);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsModifyRequest, true);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsUserRequest, true);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                    // ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedOrderType, orderType);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);
                    //  ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedTimeInForce, timeInForce);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    DateTime expirydate = Convert.ToDateTime(order[SingleOrderProperties.McsdrExpiryDate]);

                    McsdGatwayManager.PlaceMcsdModifyOrderAllocation(mcsdQuantity, requesterOrderID,expirydate);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error : " + ex.Message, RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }

        }

        internal static void CancelOrderMCSD(long orderID, int mcsdAllocQty)
        {
            Guid requesterOrderID = Guid.Empty;
            string username = string.Empty;
            try
            {
                // Order Side
                SingleOrder order = m_orderID_ordersDetails[orderID];
                requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
                username = m_OrdID_username[orderID];
                //order[SingleOrderProperties.OrderStatus] = "PendingCancelMCSD";
                lock (order)
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        DatabaseMethods db = new DatabaseMethods();
                        //db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, order.CurrentQuantity, order.RemainingQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Cancel Order Request", false, "", order.TimeInForce, null);

                        order[SingleOrderProperties.IsActive] = false;
                        order[SingleOrderProperties.IsPending] = false;
                        order[SingleOrderProperties.IsPendingMcsd] = true;
                        order[SingleOrderProperties.Note4] = "MCSD Clear order quantity request";
                        order[SingleOrderProperties.ActionOnAllocResponse] = ActionOnAllocResponse.CancelOrder;
                        order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;
                        

                        Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                        orders_Columns.Add(SingleOrderProperties.IsPending, false);
                        orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, true);

                        orders_Columns.Add(SingleOrderProperties.Note4, order[SingleOrderProperties.Note4]);

                        orders_Columns.Add(SingleOrderProperties.ActionOnAllocResponse,ActionOnAllocResponse.CancelOrder);
                        orders_Columns.Add(SingleOrderProperties.McsdrAllocQty, mcsdAllocQty);

                        orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                        Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                        orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                        Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                        // ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                        //  ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);

                        //  ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                        //     ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                        //      ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);


                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note4]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsCancelRequest, true);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsUserRequest, true);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                        //     ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.RequestedQuantity , mcsdAllocQty);

                        db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                        DateTime expirydate = Convert.ToDateTime(order[SingleOrderProperties.McsdrExpiryDate]);
                      
                        McsdGatwayManager.PlaceMcsdModifyOrderAllocation(mcsdAllocQty, requesterOrderID,expirydate);

                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {

                Guid clientKey = Sessions.GetSessionKey(username);
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //try
                //{
                //   // Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
                //}
                //catch (Exception inex)
                //{
                //    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                //    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //}
            }

        }

        #endregion

        #region MCSD Allocation Accepted

        internal static void HandleAllocationNewOrderAccepted(SingleOrder order,McsdSourceMessage sourceMessage)
        {
            long orderID = -1;
            string username = string.Empty; ;
            Guid requesterOrderID = new Guid();
            try
            {
                requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];
                //order = m_orderID_ordersDetails[orderID];  // get order from MCSD pending orders list
                #region valdite MCSD timeout
                // if !IsPendingMcsd anymore which means this allocation has timedout
                if (!Convert.ToBoolean(order[SingleOrderProperties.IsPendingMcsd]))
                {
                    return;
                }
                //object value = order[SingleOrderProperties.IsMcsdTimedOut];
                //bool isMcsdTimedOut = false;

                //bool result = bool.TryParse(value.ToString(), out isMcsdTimedOut);

                //if (result == true)
                //{
                //    if (isMcsdTimedOut)
                //    {
                //        return; // order is already rejecte due to timeout.
                //    }
                //}

                #region Get Lookups required parameters
                string securityID = order[SingleOrderProperties.SecurityCode].ToString();
                Stock stock = StocksDefinitions.GetStockByCode(securityID);
                CurrencyItem currency = Currencies.GetCurrencyByCode(stock.CurrencyCode);

                // Get ExchangeID
                string exchangeID = order[SingleOrderProperties.ExchangeID].ToString();
                string orderType = order[SingleOrderProperties.OrderType].ToString();
                string orderSide = order[SingleOrderProperties.OrderSide].ToString();
                string timeInForce = order[SingleOrderProperties.TimeInForce].ToString();
                string handleValue = order[SingleOrderProperties.HandleInst].ToString();
                #endregion

                // Get Currency 
                LookupItem currencyLookup = Lookups.GetCurrencyLookupByCurrencyCode(currency.Code);
                // Get Destination Type
                LookupItem exchangeLookup = Lookups.GetExchangeDestinationByExchangeID(exchangeID);
                // order Type
                LookupItem orderTypeLookup = Lookups.GetOrderTypeLookupByCodeValue(orderType);
                // Order Side
                LookupItem orderSideLookup = Lookups.GetOrderSidesLookupByCodeValue(orderSide);
                // Time In Force
                LookupItem timeInForceLookup = Lookups.GetTimeInForceLookupByCode(timeInForce);
                // Handle Instruction
                LookupItem handleInstLookup = Lookups.GetHandleInstTypeLookupByCodeValue(handleValue);

                string OrderStatus = ORD_STATUS.PendingNew;
                //  string OriginalOrderStatus = ORD_STATUS.PendingNew;
                string ExecType = ORD_STATUS.PendingNew;

                string Note = string.Empty;
                switch(sourceMessage)
                {
                    case McsdSourceMessage.McsdAccepted:
                        Note = "Sell order allocation accepted from MCSD";
                        break;
                    case McsdSourceMessage.McsdRejected:
                        Note = "Sell Order rejected from mcsd. rejected mcsd order will go to market";
                        break;
                    case McsdSourceMessage.McsdTimedOut:
                        Note = "Sell order timedout. mcsd timeout order will go to marekt";
                        break;
                    default:
                        Note = "No source message. ";
                        break;
                }

               
                bool IsPendingMcsd = false;

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    DatabaseMethods db = new DatabaseMethods();

                    order[SingleOrderProperties.OrderStatus] = OrderStatus;
                    //   order[SingleOrderProperties.OriginalOrderStatus] = OriginalOrderStatus;
                    order[SingleOrderProperties.ExecType] = ExecType;
                    order[SingleOrderProperties.Note2] = Note;
                    order[SingleOrderProperties.IsPendingMcsd] = IsPendingMcsd;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();



                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);
                    orders_Columns.Add(SingleOrderProperties.IsActive, false);
                    orders_Columns.Add(SingleOrderProperties.IsPending, true);
                    orders_Columns.Add(SingleOrderProperties.OrderStatus, ORD_STATUS.PendingNew);
                    // orders_Columns.Add(SingleOrderProperties.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                    //   orders_Columns.Add(SingleOrderProperties.IsPending, true);
                    orders_Columns.Add(SingleOrderProperties.Note2, order[SingleOrderProperties.Note2]);
                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note2]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsResponse, true);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);


                    ////clOrderID
                    //string clOrderID = string.Format("{0}-{1}", orderID.ToString(), "1");
                    string clOrderID = order[SingleOrderProperties.ClOrderID].ToString();
                    MarketFixClient.PlaceNewSingleOrder(clOrderID, order[SingleOrderProperties.ClientID].ToString(), securityID, (int)order[SingleOrderProperties.CurrentQuantity], (double)order[SingleOrderProperties.CurrentPrice], order[SingleOrderProperties.CustodyID].ToString(), orderSideLookup.FixValue.ToCharArray()[0], orderTypeLookup.FixValue.ToCharArray()[0], currencyLookup.FixValue, exchangeLookup.FixValue, timeInForceLookup.FixValue.ToCharArray()[0], order[SingleOrderProperties.GroupID].ToString(), handleInstLookup.FixValue.ToCharArray()[0], (DateTime)order[SingleOrderProperties.ExpirationDate], (bool)order[SingleOrderProperties.AON], (int)order[SingleOrderProperties.MinQty]);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {

                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                Guid clientKey = Sessions.GetSessionKey(username);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //if (orderID > -1)
                //{ RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        //internal static void HandleAllocationUpdateOrCacnelAccepted(AllocRes mcsdResponseMsg)
        //{
        //    long orderID = m_ReqOrdID_OrdID[mcsdResponseMsg.ReqID];
        //    SingleOrder order = m_orderID_ordersDetails[orderID];
        //    string orderStatus = order[SingleOrderProperties.OrderStatus].ToString();

        //    if (orderStatus == ORD_STATUS.Canceled)
        //    {
        //        HandleAllocationCancelOrderAccepted(mcsdResponseMsg);
        //    }
        //    else
        //    {
        //        HandleAllocationUpdateOrderAccepted(mcsdResponseMsg);
        //    }
        //}


        internal static void HandleAllocationUpdateOrderAccepted(SingleOrder order,McsdSourceMessage sourceMessage)
        {
            long orderID = -1;
            string username = string.Empty; ;

            Guid requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
            orderID = m_ReqOrdID_OrdID[requesterOrderID];
            username = m_OrdID_username[orderID];

            // if !IsPendingMcsd anymore which means this allocation has timedout
            if (!Convert.ToBoolean(order[SingleOrderProperties.IsPendingMcsd]))
            {

                return;
            }

            //#region valdite MCSD timeout
            //object value = order[SingleOrderProperties.IsMcsdTimedOut];
            //bool isMcsdTimedOut = false;
            //bool result = bool.TryParse(value.ToString(), out isMcsdTimedOut);

            //if (result == true)
            //{
            //    if (isMcsdTimedOut)
            //    {
            //        return; // order is already rejecte due to timeout.
            //    }
            //}

            //#endregion

            order[SingleOrderProperties.IsPendingMcsd] = false;

            int quantity = Convert.ToInt32(order[SingleOrderProperties.RequestedQty]);
            double price = Convert.ToDouble(order[SingleOrderProperties.RequestedPrice]);
            string orderType = Convert.ToString(order[SingleOrderProperties.RequestedOrderType]);
            string timeInForce = Convert.ToString(order[SingleOrderProperties.RequestedTimeInForce]);
            //int mcsdQuantit = Convert.ToInt32(order[SingleOrderProperties.Alloc_McsdQuantity]);

            bool hasAon = Convert.ToBoolean(order[SingleOrderProperties.RequestedAON]);
            int minQty = Convert.ToInt32(order[SingleOrderProperties.RequestedMinQty]);

            Dictionary<string, object> optionalParams = new Dictionary<string, object>();
            optionalParams.Add("AON", hasAon);
            optionalParams.Add("MinQty", minQty);

            using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
            {
                DatabaseMethods db = new DatabaseMethods();


                string Note = string.Empty;
                switch (sourceMessage)
                {
                    case McsdSourceMessage.McsdAccepted:
                        Note = "Sell order allocation accepted from MCSD";
                        break;
                    case McsdSourceMessage.McsdRejected:
                        Note = "Sell Order rejected from mcsd. rejected mcsd order will go to market";
                        break;
                    case McsdSourceMessage.McsdTimedOut:
                        Note = "Sell order timedout. mcsd timeout order will go to marekt";
                        break;
                    default:
                        Note = "No source message. ";
                        break;
                }

                order[SingleOrderProperties.Note3] = Note;
                order[SingleOrderProperties.IsPendingMcsd] = false;
                order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, false);
                orders_Columns.Add(SingleOrderProperties.Note3, order[SingleOrderProperties.Note3]);
                orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();

                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note3]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsResponse, true);

                db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);
                ts.Complete();

                Guid sessionKey = Sessions.GetSessionKey(username);
                ModifyOrderFIXGatway(username, sessionKey, requesterOrderID, orderID, quantity, price, orderType, timeInForce, optionalParams);
            }
        }

        internal static void HandleAllocationCancelOrderAccepted(SingleOrder order)
        {
            //string username = string.Empty; ;
            //SingleOrder order = null;
            Guid requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
            using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
            {
                //username = m_OrdID_username[orderID];

                DatabaseMethods db = new DatabaseMethods();

                //string Note = string.Empty;
                //switch (sourceMessage)
                //{
                //    case McsdSourceMessage.McsdAccepted:
                //        Note = "Sell order allocation accepted from MCSD";
                //        break;
                //    case McsdSourceMessage.McsdRejected:
                //        Note = "Sell Order rejected from mcsd.";
                //        break;
                //    case McsdSourceMessage.McsdTimedOut:
                //        Note = "Sell order timedout. mcsd timeout order will go to marekt";
                //        break;
                //    default:
                //        Note = "No source message. ";
                //        break;
                //}

                order[SingleOrderProperties.Note3] = "Sell Order Accepted form MCSD";
                order[SingleOrderProperties.IsPendingMcsd] = false;
                order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, false);
                orders_Columns.Add(SingleOrderProperties.Note3, order[SingleOrderProperties.Note3]);
                orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();

                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note3]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsResponse, true);


                db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                ts.Complete();
            }
        }

                #endregion



        #endregion

        #region MCSD Allocation Rejected

        internal static void HandleAllocationNewOrderRefused(AllocRes mcsdResponseMsg, SingleOrder order)
        {

            if (_ForceSendMcsdRejectedOrdersToMarket)
            {
                HandleAllocationNewOrderAccepted(order, Fix.Entities.McsdSourceMessage.McsdRejected);
                return;
            }
            long orderID = -1;
            string username = string.Empty; ;
            //SingleOrder order = null;
            Guid requesterOrderID = new Guid();
            try
            {
                string rejectMessage = string.Empty;
                try
                {
                    OpVal[] opVals = mcsdResponseMsg.Fields;
                    if (mcsdResponseMsg.IsFieldSet(ALLOC_RES_FIELDS.ERR_MSG))
                        rejectMessage = mcsdResponseMsg[ALLOC_RES_FIELDS.ERR_MSG].ToString();
                    else
                        rejectMessage = "No MCSD Specified Error";
                }
                catch (Exception exp)
                {
                    rejectMessage = "No MCSD Specified Error";
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Couldn't get MCSD reject message. Error Deatails : {0}", exp.ToString()), ConsoleColor.Red, ConsoleColor.White, true);
                }

                requesterOrderID = mcsdResponseMsg.ReqID;
                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];
                order = m_orderID_ordersDetails[orderID];  // get order from MCSD pending orders list

                string orderType = order[SingleOrderProperties.OrderType].ToString();

                string timeInForce = order[SingleOrderProperties.TimeInForce].ToString();

                string OrderStatus = ORD_STATUS.Rejected, ExecType = ORD_STATUS.Rejected;
                //string OriginalOrderStatus = ORD_STATUS.PendingNew;
                string Note = string.Format("Sell order allocation rejected from MCSD : {0}", rejectMessage);
                bool IsPendingMcsd = false;

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    DatabaseMethods db = new DatabaseMethods();

                    order[SingleOrderProperties.OrderStatus] = OrderStatus;
                    order[SingleOrderProperties.IsPendingMcsd] = IsPendingMcsd;
                    //  order[SingleOrderProperties.ExecType] = ExecType;
                    //  order[SingleOrderProperties.Note] = Note;
                    //  order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(SingleOrderProperties.OrderStatus, OrderStatus);
                    // orders_Columns.Add(SingleOrderProperties.OriginalOrderStatus, OriginalOrderStatus);
                    orders_Columns.Add(SingleOrderProperties.ExecType, ExecType);
                    orders_Columns.Add(SingleOrderProperties.IsActive, false);
                    orders_Columns.Add(SingleOrderProperties.IsPending, false);
                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);
                    orders_Columns.Add(SingleOrderProperties.Note2, Note);
                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, ExecType);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, OrderStatus);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, Note);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    //Update orders dictionaries.
                    // remove order from MCSD dictioanry
                    //  m_orderID_ordersDetails_MCSD.Remove(orderID);
                    // RemoveOrderRef(orderID, requesterOrderID);

                    ts.Complete();

                    Guid clientKey = Sessions.GetSessionKey(username);
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRejectionResponse() { ClientKey = clientKey, RejectionReason = rejectMessage, OrderStatus = OrderStatus, RequesterOrderID = requesterOrderID } });

                }
            }
            catch (Exception ex)
            {

                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                Guid clientKey = Sessions.GetSessionKey(username);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error in method HandleAllocationNewOrderRefused(), ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                if (orderID > -1)
                { RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        //internal static void HandleAllocationUpdateOrCancelRefused(AllocRes mcsdResponseMsg)
        //{

        //    try
        //    {

        //        long orderID = m_ReqOrdID_OrdID[mcsdResponseMsg.ReqID];

        //        SingleOrder order = m_orderID_ordersDetails[orderID];

        //        if (order[SingleOrderProperties.OrderStatus].ToString() == ORD_STATUS.Canceled)
        //        {
        //            HandleAllocationCancelOrderRefused(mcsdResponseMsg);
        //        }
        //        else
        //        {
        //            HandleAllocationUpdateOrderRefused(mcsdResponseMsg);
        //        }
        //    }
        //    catch (Exception exp)
        //    {

        //        SystemLogger.WriteOnConsoleAsync(true, string.Format("Error in method HandleAllocationUpdateOrCancelRefused(). Error Deatails : {0}", exp.ToString()), ConsoleColor.Red, ConsoleColor.White, true);
        //    }
        //}

        internal static void HandleAllocationUpdateOrderRefused(AllocRes mcsdResponseMsg, SingleOrder order)
        {

            if (_ForceSendMcsdRejectedOrdersToMarket)
            {
                HandleAllocationUpdateOrderAccepted(order, Fix.Entities.McsdSourceMessage.McsdRejected);
                return;
            }

            long orderID = -1;
            string username = string.Empty; ;
            //  SingleOrder order = null;
            Guid requesterOrderID = new Guid();
            try
            {
                string rejectMessage = string.Empty;
                try
                {
                    OpVal[] opVals = mcsdResponseMsg.Fields;
                    if (mcsdResponseMsg.IsFieldSet(ALLOC_RES_FIELDS.ERR_MSG))
                        rejectMessage = mcsdResponseMsg[ALLOC_RES_FIELDS.ERR_MSG].ToString();
                    else
                        rejectMessage = "No MCSD Specified Error";
                }
                catch (Exception exp)
                {
                    rejectMessage = "No MCSD Specified Error";
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Couldn't get MCSD reject message. Error Deatails : {0}", exp.ToString()), ConsoleColor.Red, ConsoleColor.White, true);
                }

                requesterOrderID = mcsdResponseMsg.ReqID;
                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];
                //  order = m_orderID_ordersDetails[orderID];


                string Note = string.Format("Sell order modfiy allocation rejected from MCSD : {0} ", rejectMessage);
                bool IsPendingMcsd = false;

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {

                    order[SingleOrderProperties.IsPendingMcsd] = false;
                    order[SingleOrderProperties.IsActive] = true;
                    order[SingleOrderProperties.IsPending] = false;
                    order[SingleOrderProperties.OrderStatus] = ORD_STATUS.Rejected;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;


                    DatabaseMethods db = new DatabaseMethods();



                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();


                    orders_Columns.Add(SingleOrderProperties.Note4, Note);
                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);
                    orders_Columns.Add(SingleOrderProperties.IsPending, false);
                    orders_Columns.Add(SingleOrderProperties.IsActive, true);
                    orders_Columns.Add(SingleOrderProperties.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);


                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    Guid clientKey = Sessions.GetSessionKey(username);

                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRejectionResponse() { ClientKey = clientKey, RejectionReason = rejectMessage, OrderStatus = order[SingleOrderProperties.OrderStatus].ToString(), RequesterOrderID = requesterOrderID } });

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {

                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                Guid clientKey = Sessions.GetSessionKey(username);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error in method HandleAllocationUpdateOrderRefused(), ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                if (orderID > -1)
                { RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        internal static void HandleAllocationCancelOrderRefused(AllocRes mcsdResponseMsg, SingleOrder order)
        {
            long orderID = -1;
            string username = string.Empty;
            // SingleOrder order = null;


            Guid requesterOrderID = mcsdResponseMsg.ReqID;

            using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
            {

                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];

                //  order = m_orderID_ordersDetails[orderID];
                order[SingleOrderProperties.IsPendingMcsd] = false;

                DatabaseMethods db = new DatabaseMethods();

                string rejectMessage = string.Empty;
                try
                {
                    OpVal[] opVals = mcsdResponseMsg.Fields;
                    if (mcsdResponseMsg.IsFieldSet(ALLOC_RES_FIELDS.ERR_MSG))
                        rejectMessage = mcsdResponseMsg[ALLOC_RES_FIELDS.ERR_MSG].ToString();
                    else
                        rejectMessage = "No MCSD Specified Error";
                }
                catch (Exception exp)
                {
                    rejectMessage = "No MCSD Specified Error";
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Couldn't get MCSD reject message. Error Deatails : {0}", exp.ToString()), ConsoleColor.Red, ConsoleColor.White, true);
                }


                order[SingleOrderProperties.Note3] = string.Format("allocation clearance refused by MCSD : {0}", rejectMessage);
                order[SingleOrderProperties.IsPendingMcsd] = false;
                order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;


                Dictionary<string, object> orders_Columns = new Dictionary<string, object>();


                orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, false);
                orders_Columns.Add(SingleOrderProperties.IsActive, false);
                orders_Columns.Add(SingleOrderProperties.IsPending, false);
                orders_Columns.Add(SingleOrderProperties.Note3, order[SingleOrderProperties.Note3]);
                orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();

                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.Note3, order[SingleOrderProperties.Note3]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                ts.Complete();
            }
        }
        #endregion

        #region MCSD time out

        private static void StartMcsdTimeOutChecker()
        {
            try
            {
                bool enableMcsdTimeOutChecker = Convert.ToBoolean(SystemConfigurations.GetAppSetting("EnableMcsdTimeOutChecker"));
                if (enableMcsdTimeOutChecker)
                {
                    _threadMcsdTimeOutOrdersChecker = new Thread(new ThreadStart(CheckMcsdTimerOutOrders));
                    _threadMcsdTimeOutOrdersChecker.IsBackground = true;
                    _threadMcsdTimeOutOrdersChecker.Start();
                }
            }
            catch (Exception exp)
            {
                SystemLogger.LogErrorAsync("Error in start Mcsd timed out ourders thread. error details  : " + exp.ToString());
            }
        }

        private static void CheckMcsdTimerOutOrders()
        {

            try
            {
                while (true)
                {
                    HanldeMcsdTimedOutOrders();
                    System.Threading.Thread.Sleep(_MCSDcheckerIntervals);
                }
            }
            catch (Exception exp)
            {
                SystemLogger.LogErrorAsync("Error in CheckMcsdTimerOutOrders(). Error : " + exp.ToString());
            }

        }

        private static void HanldeMcsdTimedOutOrders()
        {
            try
            {
                lock (m_syncObject)
                {
                    foreach (long orderid in m_ReqOrdID_OrdID.Values)
                    {

                        SingleOrder order = GetOrder(orderid);
                        lock (order)
                        {
                            bool isPendingMcsd = Convert.ToBoolean(order[SingleOrderProperties.IsPendingMcsd]);

                            object value = order[SingleOrderProperties.IsMcsdTimedOut];

                            bool isMcsdTimedOut = Convert.ToBoolean(value);


                            if (isPendingMcsd == true && isMcsdTimedOut == false)
                            {
                                DateTime orderTime = Convert.ToDateTime(order[SingleOrderProperties.ModifiedDateTime]);
                                DateTime currentTime = DateTime.Now;

                                TimeSpan tspan = currentTime - orderTime;

                                double orderTimeMilliseconds = tspan.TotalMilliseconds;

                                if (orderTimeMilliseconds > _McsdTimeOutPeriod)
                                {
                                    PorcessMcsdTimedOutOrder(order);
                                }
                            }
                        }// end lock
                    }// end loop
                }
            }
            catch (Exception exp)
            {
                SystemLogger.LogErrorAsync("Error in HanldeMcsdTimedOutOrders(). Error : " + exp.ToString());
            }
        }

        private static void PorcessMcsdTimedOutOrder(SingleOrder order)
        {
            try
            {
                string orderStatus = order[SingleOrderProperties.OrderStatus].ToString();
                switch (orderStatus)
                {
                    case ORD_STATUS.PendingNew:
                        HandleAllocationNewOrderTimeOut(order);
                        break;
                    case ORD_STATUS.PendingReplace:
                        HandleAllocationUpdateOrderTimeOut(order);
                        break;
                    default:
                        {
                            SystemLogger.LogErrorAsync(string.Format("Timed out order {0} of status {1} will not processed ",order.Data[SingleOrderProperties.OrderID],orderStatus));
                            break;
                        }
                }
            }
            catch (Exception exp)
            {
                SystemLogger.LogErrorAsync("Error in PorcessMcsdTimedOutOrder(). Error : " + exp.ToString());
            }
        }

        internal static void HandleAllocationUpdateOrderTimeOut(SingleOrder order)
        {

            if (_ForceSendMcsdRejectedOrdersToMarket)
            {
                HandleAllocationUpdateOrderAccepted(order, Fix.Entities.McsdSourceMessage.McsdTimedOut);
                return;
            }

            long orderID = Convert.ToInt64(order[SingleOrderProperties.OrderID]);
            string username = string.Empty;
            Guid requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());

            try
            {

                //  requesterOrderID = mcsdResponseMsg.ReqID;
                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];
                order = m_orderID_ordersDetails[orderID];


                string Note = string.Format("Order is timedout no response from MCSD");
                bool IsPendingMcsd = false;

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {

                    order[SingleOrderProperties.IsPendingMcsd] = false;
                    //   order[SingleOrderProperties.IsActive] = true;
                    order[SingleOrderProperties.IsPending] = false;
                    order[SingleOrderProperties.OrderStatus] = ORD_STATUS.Rejected;
                    order[SingleOrderProperties.IsMcsdTimedOut] = true;
                    order[SingleOrderProperties.IsRejected] = true;
                    order[SingleOrderProperties.RejectionReason] = "MCSD Timedout";
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                    DatabaseMethods db = new DatabaseMethods();
                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(SingleOrderProperties.Note4, Note);
                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);
                    orders_Columns.Add(SingleOrderProperties.IsPending, false);
                    //    orders_Columns.Add(SingleOrderProperties.IsActive, true);
                    orders_Columns.Add(SingleOrderProperties.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                    orders_Columns.Add(SingleOrderProperties.IsMcsdTimedOut, order[SingleOrderProperties.IsMcsdTimedOut]);
                    orders_Columns.Add(SingleOrderProperties.RejectionReason, order[SingleOrderProperties.RejectionReason]);
                    orders_Columns.Add(SingleOrderProperties.IsRejected, order[SingleOrderProperties.IsRejected]);
                    orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);

                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);


                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    Guid clientKey = Sessions.GetSessionKey(username);

                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRejectionResponse() { ClientKey = clientKey, RejectionReason = "MCSD timeout", OrderStatus = order[SingleOrderProperties.OrderStatus].ToString(), RequesterOrderID = requesterOrderID } });

                    ts.Complete();
                }
            }
            catch (Exception ex)
            {

                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                Guid clientKey = Sessions.GetSessionKey(username);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error in method HandleAllocationUpdateOrderTimeOut(), ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //if (orderID > -1)
                //{RemoveOrderRef(orderID, requesterOrderID); }

                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        internal static void HandleAllocationNewOrderTimeOut(SingleOrder order)
        {

            if (_ForceSendMcsdRejectedOrdersToMarket)
            {
                HandleAllocationNewOrderAccepted(order, Fix.Entities.McsdSourceMessage.McsdTimedOut);
                return;
            }

            string username = string.Empty; ;

            long orderID = -1; //Convert.ToInt64(order[SingleOrderProperties.OrderID]);
            Guid requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
            try
            {

                //  requesterOrderID = mcsdResponseMsg.ReqID;
                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];
                order = m_orderID_ordersDetails[orderID];  // get order from MCSD pending orders list

                string orderType = order[SingleOrderProperties.OrderType].ToString();

                string timeInForce = order[SingleOrderProperties.TimeInForce].ToString();

                string OrderStatus = ORD_STATUS.Rejected, ExecType = EXEC_TYP.Rejected;
                //string OriginalOrderStatus = ORD_STATUS.PendingNew;
                string Note = string.Format("Sell order allocation is timed out");
                bool IsPendingMcsd = false;

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    DatabaseMethods db = new DatabaseMethods();

                    order[SingleOrderProperties.OrderStatus] = OrderStatus;
                    order[SingleOrderProperties.IsPendingMcsd] = IsPendingMcsd;
                    order[SingleOrderProperties.IsActive] = false;
                    order[SingleOrderProperties.IsMcsdTimedOut] = true;
                    order[SingleOrderProperties.IsRejected] = true;
                    order[SingleOrderProperties.RejectionReason] = "MCSD Timedout";
                    //  order[SingleOrderProperties.ExecType] = ExecType;
                    //  order[SingleOrderProperties.Note] = Note;
                    order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(SingleOrderProperties.OrderStatus, OrderStatus);
                    // orders_Columns.Add(SingleOrderProperties.OriginalOrderStatus, OriginalOrderStatus);
                    orders_Columns.Add(SingleOrderProperties.ExecType, ExecType);
                    orders_Columns.Add(SingleOrderProperties.IsActive, false);
                    orders_Columns.Add(SingleOrderProperties.IsPending, false);
                    orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, IsPendingMcsd);
                    orders_Columns.Add(SingleOrderProperties.Note2, Note);
                    orders_Columns.Add(SingleOrderProperties.IsMcsdTimedOut, order[SingleOrderProperties.IsMcsdTimedOut]);
                    orders_Columns.Add(SingleOrderProperties.IsMcsdTimedOut, order[SingleOrderProperties.IsMcsdTimedOut]);
                    orders_Columns.Add(SingleOrderProperties.RejectionReason, order[SingleOrderProperties.RejectionReason]);
                    orders_Columns.Add(SingleOrderProperties.IsRejected, order[SingleOrderProperties.IsRejected]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, ExecType);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, OrderStatus);
                    // ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, OrderStatus);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, Note);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                    ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    //Update orders dictionaries.
                    // remove order from MCSD dictioanry
                    //  m_orderID_ordersDetails_MCSD.Remove(orderID);
                    // RemoveOrderRef(orderID, requesterOrderID);

                    ts.Complete();
                    Guid clientKey = Sessions.GetSessionKey(username);
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRejectionResponse() { ClientKey = clientKey, RejectionReason = "Allocation Timeout", OrderStatus = OrderStatus, RequesterOrderID = requesterOrderID } });

                }
            }
            catch (Exception ex)
            {

                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                Guid clientKey = Sessions.GetSessionKey(username);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error in method HandleAllocationNewOrderTimeOut(), ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //if (orderID > -1)
                //{ RemoveOrderRef(orderID, requesterOrderID); }
                try
                {
                    Sessions.Push(username, new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        internal static void HandleAllocationCancelOrderTimeOut(SingleOrder singleOrder)
        {
            long orderID = -1;
            string username = string.Empty; ;
            SingleOrder order = null;

            Guid requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());

            using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
            {

                orderID = m_ReqOrdID_OrdID[requesterOrderID];
                username = m_OrdID_username[orderID];

                order = m_orderID_ordersDetails[orderID];
                order[SingleOrderProperties.IsPendingMcsd] = false;

                DatabaseMethods db = new DatabaseMethods();

                order[SingleOrderProperties.Note3] = string.Format("MCSD Allocation timeout");
                order[SingleOrderProperties.IsPendingMcsd] = false;
                order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, false);
                //   orders_Columns.Add(SingleOrderProperties.IsActive, false);
                orders_Columns.Add(SingleOrderProperties.IsPending, false);
                orders_Columns.Add(SingleOrderProperties.Note3, order[SingleOrderProperties.Note3]);
                orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();

                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.Note3, order[SingleOrderProperties.Note3]);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                ts.Complete();
            }
        }
        #endregion

        #region Clear Allocated Quantity

        internal static void HandleRejectedOrderAllocation(long orderID, int mcsdAllocQty)
        {
            Guid requesterOrderID = Guid.Empty;
            string username = string.Empty;
            try
            {
             
                
                    mcsdAllocQty = mcsdAllocQty * -1;
                
                
                // Order Side
                SingleOrder order = m_orderID_ordersDetails[orderID];
                requesterOrderID = new Guid(order[SingleOrderProperties.RequesterOrderID].ToString());
                username = m_OrdID_username[orderID];
                //order[SingleOrderProperties.OrderStatus] = "PendingCancelMCSD";
                lock (order)
                {
                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        DatabaseMethods db = new DatabaseMethods();
                        //db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, order.CurrentQuantity, order.RemainingQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Cancel Order Request", false, "", order.TimeInForce, null);

                        //order[SingleOrderProperties.IsActive] = false;
                        //order[SingleOrderProperties.IsPending] = false;
                      //  order[SingleOrderProperties.IsPendingMcsd] = true;

                        order[SingleOrderProperties.ActionOnAllocResponse] = ActionOnAllocResponse.DoNothing;

                        string note = string.Empty;

                        if(mcsdAllocQty > 0)
                        {
                            note = "Order Rjeceted from Excahnge, Restore Cleard Allocation";
                        }
                        else
                        {
                            note = "Order Rjeceted from Excahnge, Clear Allocation Quantity";
                        }

                        order[SingleOrderProperties.Note4] = note;
                        order[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;

                        Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                        //orders_Columns.Add(SingleOrderProperties.IsPending, false);
                       // orders_Columns.Add(SingleOrderProperties.IsPendingMcsd, true);

                        orders_Columns.Add(SingleOrderProperties.Note4, order[SingleOrderProperties.Note4]);

                        orders_Columns.Add(SingleOrderProperties.ActionOnAllocResponse,ActionOnAllocResponse.DoNothing);

                        orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, order[SingleOrderProperties.ModifiedDateTime]);

                        Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                        orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                        Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                        // ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                        //  ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);

                        //  ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                        //     ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                        //      ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);


                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note4]);

                       ordersDetails_Columns.Add(SingleOrdDetailsProps.IsCancelRequest, false);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsUserRequest, false);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                        //     ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsMcdrRec, true);

                        db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                        DateTime expirydate = Convert.ToDateTime(order[SingleOrderProperties.McsdrExpiryDate]);
                        McsdGatwayManager.PlaceMcsdModifyOrderAllocation(mcsdAllocQty, requesterOrderID, expirydate);

                        ts.Complete();
                    }
                }
            }
            catch (Exception ex)
            {

                Guid clientKey = Sessions.GetSessionKey(username);
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //try
                //{
                //   // Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
                //}
                //catch (Exception inex)
                //{
                //    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                //    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                //}
            }

        }

        #endregion
        #endregion
    }
}

