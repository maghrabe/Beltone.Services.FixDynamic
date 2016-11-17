﻿using System;
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
using Beltone.Services.Fix.Entities.Configurations;
using Beltone.Services.Fix.Contract.Enums;
using System.Threading;
using System.Threading.Tasks;
using Beltone.Services.Fix.Service.Entities;

namespace Beltone.Services.Fix.Service.Singletons
{
    public class OrdersManagerTest
    {
        #region Variables
        //                      CallbackID, List RequesterOrderID
        private static Dictionary<Guid, List<Guid>> m_sessionKey_ReqOrdIDs = null;
        //                      RequesterOrderID, CallbackID
        //private static Dictionary<Guid, List<Guid>> m_ReqOrdID_subIDs = null;
        private static Dictionary<long, Guid> m_OrdID_sessionKey = null;
        //                      RequesterOrderID, ClOrderID
        private static Dictionary<Guid, long> m_ReqOrdID_OrdID = null;
        //                      OrderID, OrderDetails
        private static Dictionary<long, SingleOrder> m_orderID_ordersDetails = null;


        private static List<string> m_handledExecutionIDs = null;
        private static object m_syncObject = new object();
        #endregion Variables

        #region Constructors

        public static void Initialize()
        {
            // this line is to make sure that fix db conn is already created
            CmdOnPoolGenerator gen = new CmdOnPoolGenerator("FixOrdersDBConnectionString");
            DatabaseMethods db = new DatabaseMethods();
            m_orderID_ordersDetails = new Dictionary<long, SingleOrder>();
            m_ReqOrdID_OrdID = new Dictionary<Guid, long>();
            m_sessionKey_ReqOrdIDs = new Dictionary<Guid, List<Guid>>();
            //m_ReqOrdID_subIDs = new Dictionary<Guid, List<Guid>>();
            m_OrdID_sessionKey = new Dictionary<long, Guid>();
            m_handledExecutionIDs = new List<string>();
            // Fill the data 
            DataTable dtOrders = db.FillOrdersData();
            PropertiesColumnsSchemaItem[] schema = PropColMapper.GetTableSchema(SingleOrderProperties.TableName);

            foreach (DataRow row in dtOrders.Rows)
            {
                Guid sessionKey = ((Guid)row["SessionID"]);
                long ordID = (long)row["OrderID"];
                Guid ReqOrdID = (Guid)row["RequesterOrderID"];

                // prepare order object
                SingleOrder order = new SingleOrder();
                foreach (PropertiesColumnsSchemaItem item in schema)
                {
                    order.Data[item.ColumnName] = row[item.ColumnName];
                }

                m_orderID_ordersDetails.Add((long)row["OrderID"], order);
                m_ReqOrdID_OrdID.Add((Guid)row["RequesterOrderID"], (long)row["OrderID"]);
                if (!m_sessionKey_ReqOrdIDs.ContainsKey(sessionKey))
                    m_sessionKey_ReqOrdIDs.Add(sessionKey, new List<Guid>());
                m_sessionKey_ReqOrdIDs[sessionKey].Add(ReqOrdID);
                m_OrdID_sessionKey.Add(ordID, sessionKey);
            }


            foreach (string execID in db.GetExecutionIDs())
            {
                m_handledExecutionIDs.Add(execID);
            }
        }

        #endregion Constructors

        #region Place / Modify / Cancel Orders

        public static void PlaceNewSingleOrder(Guid clientKey, Guid requesterOrderID, int clientID, string custodyID, string securityID,
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
                    DatabaseMethods db = new DatabaseMethods();
                    //orderID = db.AddNewSingleOrder(clientKey, requesterOrderID, null, null, clientID, custodyID, securityID, orderSide, 
                    //price, price, price, quantity, orderType, placementDateTime, ORD_STATUS.PendingNew, ORD_STATUS.PendingNew, "", false, "", timeInForce);


                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.CurrencyID).ColumnName, currency.ID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.GroupID).ColumnName, groupID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.MarketID).ColumnName, marketID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ExchangeID).ColumnName, exchangeID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.RequesterOrderID).ColumnName, requesterOrderID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ClientID).ColumnName, clientID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.CustodyID).ColumnName, custodyID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.SecurityCode).ColumnName, securityID);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderSide).ColumnName, orderSide);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.CurrentPrice).ColumnName, price);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalPrice).ColumnName, price);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.PlacementDateTime).ColumnName, placementDateTime);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalQuantity).ColumnName, quantity);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.RemainingQuantity).ColumnName, 0);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ExecutedQuantity).ColumnName, 0);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.LastExecQuantity).ColumnName, 0);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.LastExecPrice).ColumnName, 0);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.AvgPrice).ColumnName, 0);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.CurrentQuantity).ColumnName, quantity);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalOrderType).ColumnName, orderType);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderType).ColumnName, orderType);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderStatus).ColumnName, ORD_STATUS.PendingNew);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalOrderStatus).ColumnName, ORD_STATUS.PendingNew);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ExecType).ColumnName, EXEC_TYP.PendingNew);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderCreatedBySysDateTime).ColumnName, orderCreatedBySysDateTime);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderRecievedDateTime).ColumnName, DateTime.Now);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ModifiedDateTime).ColumnName, DateTime.Now);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.Note).ColumnName, "awaiting for acceptance response");
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsPending).ColumnName, true);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsActive).ColumnName, false);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsExecuted).ColumnName, false);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsCompleted).ColumnName, false);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.HasSystemError).ColumnName, false);
                    //data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ErrorMessage).ColumnName, string.Empty);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalTimeInForce).ColumnName, timeInForce);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.TimeInForce).ColumnName, timeInForce);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalHandleInst).ColumnName, handleValue);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.HandleInst).ColumnName, handleValue);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ExpirationDate).ColumnName, expiryDate.ToShortDateString());
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ExpirationDateTime).ColumnName, expiryDate);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.AON).ColumnName, hasAON);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalAON).ColumnName, hasAON);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OriginalMinQty).ColumnName, minQ);
                    data.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.MinQty).ColumnName, minQ);

                    Dictionary<string, object> dataDetails = new Dictionary<string, object>();
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentPrice).ColumnName, price);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentQuantity).ColumnName, quantity);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RemainingQuantity).ColumnName, quantity);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecutedQuantity).ColumnName, 0);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.AvgPrice).ColumnName, 0);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecPrice).ColumnName, 0);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.LastExecQuantity).ColumnName, 0);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderType).ColumnName, orderType);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderStatus).ColumnName, ORD_STATUS.PendingNew);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecType).ColumnName, EXEC_TYP.PendingNew);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.DateTime).ColumnName, DateTime.Now);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.Note).ColumnName, "New Order");
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.HasSystemError).ColumnName, false);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ErrorMessage).ColumnName, string.Empty);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.TimeInForce).ColumnName, timeInForce);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsNewOrderRequest).ColumnName, true);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsUserRequest).ColumnName, true);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.HandleInst).ColumnName, handleValue);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.AON).ColumnName, hasAON);
                    dataDetails.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.MinQty).ColumnName, minQ);



                    //orderID = db.AddNewSingleOrder(clientKey, data, dataDetails);

                    string clOrderID = string.Format("{0}-{1}", orderID.ToString(), "1");

                    SingleOrder order = new SingleOrder();
                    order.Data[SingleOrderProperties.OrderID] = orderID;
                    order.Data[SingleOrderProperties.ClOrderID] = clOrderID;
                    order.Data[SingleOrderProperties.GroupID] = groupID;
                    order.Data[SingleOrderProperties.MarketID] = marketID;
                    order.Data[SingleOrderProperties.ExchangeID] = exchangeID;
                    order.Data[SingleOrderProperties.CurrencyID] = currency.ID;
                    order.Data[SingleOrderProperties.RequesterOrderID] = requesterOrderID;
                    order.Data[SingleOrderProperties.ClientID] = clientID;
                    order.Data[SingleOrderProperties.CustodyID] = custodyID;
                    order.Data[SingleOrderProperties.SecurityCode] = securityID;
                    order.Data[SingleOrderProperties.OrderSide] = orderSide;

                    order.Data[SingleOrderProperties.IsActive] = false;
                    order.Data[SingleOrderProperties.IsPending] = true;
                    order.Data[SingleOrderProperties.IsCompleted] = false;
                    order.Data[SingleOrderProperties.IsExecuted] = false;

                    order.Data[SingleOrderProperties.CurrentPrice] = price;
                    order.Data[SingleOrderProperties.OriginalPrice] = price;
                    order.Data[SingleOrderProperties.AvgPrice] = 0;
                    order.Data[SingleOrderProperties.LastExecPrice] = 0;

                    order.Data[SingleOrderProperties.OriginalQuantity] = quantity;
                    order.Data[SingleOrderProperties.RemainingQuantity] = 0;
                    order.Data[SingleOrderProperties.ExecutedQuantity] = 0;
                    order.Data[SingleOrderProperties.LastExecQuantity] = 0;
                    order.Data[SingleOrderProperties.CurrentQuantity] = quantity;

                    order.Data[SingleOrderProperties.OriginalOrderType] = orderType;
                    order.Data[SingleOrderProperties.OrderType] = orderType;
                    order.Data[SingleOrderProperties.OrderStatus] = ORD_STATUS.PendingNew;
                    order.Data[SingleOrderProperties.OriginalOrderStatus] = ORD_STATUS.PendingNew;
                    order.Data[SingleOrderProperties.ExecType] = EXEC_TYP.PendingNew;
                    order.Data[SingleOrderProperties.OriginalTimeInForce] = timeInForce;
                    order.Data[SingleOrderProperties.TimeInForce] = timeInForce;
                    order.Data[SingleOrderProperties.OriginalHandleInst] = handleValue;
                    order.Data[SingleOrderProperties.HandleInst] = handleValue;

                    order.Data[SingleOrderProperties.AON] = hasAON;
                    order.Data[SingleOrderProperties.MinQty] = minQ;
                    order.Data[SingleOrderProperties.OriginalAON] = hasAON;
                    order.Data[SingleOrderProperties.OriginalMinQty] = minQ;

                    order.Data[SingleOrderProperties.PlacementDateTime] = placementDateTime;
                    order.Data[SingleOrderProperties.OrderCreatedBySysDateTime] = orderCreatedBySysDateTime;
                    order.Data[SingleOrderProperties.OrderRecievedDateTime] = DateTime.Now;
                    order.Data[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;
                    order.Data[SingleOrderProperties.Note] = "Awaiting For Acceptance Response";

                    order.Data[SingleOrderProperties.HasSystemError] = false;

                    // lock only shared entities as each method create its only copy of variable except shared entities
                    lock (m_syncObject)
                    {
                        if (!m_sessionKey_ReqOrdIDs.ContainsKey(clientKey))
                        {
                            m_sessionKey_ReqOrdIDs.Add(clientKey, new List<Guid>());
                        }
                        m_sessionKey_ReqOrdIDs[clientKey].Add((Guid)order.Data[SingleOrderProperties.RequesterOrderID]);
                        m_ReqOrdID_OrdID.Add((Guid)order.Data[SingleOrderProperties.RequesterOrderID], (long)order.Data[SingleOrderProperties.OrderID]);
                        m_orderID_ordersDetails.Add((long)order.Data[SingleOrderProperties.OrderID], order);
                        m_OrdID_sessionKey.Add(orderID, clientKey);
                        //if (!m_ReqOrdID_subIDs.ContainsKey((Guid)order.Data[SingleOrderProperties.RequesterOrderID]))
                        //{
                        //    m_ReqOrdID_subIDs.Add((Guid)order.Data[SingleOrderProperties.RequesterOrderID], new List<Guid>());
                        //}
                        //m_ReqOrdID_subIDs[(Guid)order.Data[SingleOrderProperties.RequesterOrderID]].Add(clientKey);
                    }

                    FixGatewayManager.PlaceNewSingleOrder(clOrderID, clientID.ToString(), securityID, quantity, price, custodyID, orderSideLookup.FixValue.ToCharArray()[0], orderTypeLookup.FixValue.ToCharArray()[0], currencyLookup.FixValue, exchangeLookup.FixValue, timeInForceLookup.FixValue.ToCharArray()[0], groupID, handleInstLookup.FixValue.ToCharArray()[0], expiryDate, hasAON, minQ);

                    ts.Complete();
                    db = null;
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                if (orderID > -1)
                { RemoveOrderRef(orderID); }

                try
                {
                    // Sessions.Push(new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = clientKey, RefuseMessage = "System Erorr", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inEx)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order to the client while placing order, ClientKey {0}, Req Order ID {1}, Error: {2}", clientKey, requesterOrderID, inEx.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }
        }

        internal static void ModifyOrder(Guid clientKey, Guid requesterOrderID, long orderID, int quantity, double price, string orderType, string timeInForce, Dictionary<string, object> optionalParam)
        {
            try
            {
                SingleOrder order = m_orderID_ordersDetails[orderID];

                string[] arr = order.Data[SingleOrderProperties.ClOrderID].ToString().Split(new char[] { '-' });
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


                    order.Data[SingleOrderProperties.ClOrderID] = newClOrdID;
                    order.Data[SingleOrderProperties.IsPending] = true;
                    order.Data[SingleOrderProperties.Note] = "Pending Modify Request";
                    order.Data[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;


                    Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                    orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ClOrderID).ColumnName, order.Data[SingleOrderProperties.ClOrderID]);

                    orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsPending).ColumnName, true);

                    orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.Note).ColumnName, order.Data[SingleOrderProperties.Note]);

                    orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ModifiedDateTime).ColumnName, order.Data[SingleOrderProperties.ModifiedDateTime]);

                    Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                    orders_Filters.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderID).ColumnName, order.Data[SingleOrderProperties.OrderID]);

                    Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderID).ColumnName, order.Data[SingleOrderProperties.OrderID]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ClOrderID).ColumnName, order.Data[SingleOrderProperties.ClOrderID]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrigClOrdID).ColumnName, order.Data[SingleOrderProperties.OrigClOrdID]);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.AvgPrice).ColumnName, order.Data[SingleOrderProperties.AvgPrice]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentPrice).ColumnName, order.Data[SingleOrderProperties.CurrentPrice]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecPrice).ColumnName, order.Data[SingleOrderProperties.LastExecPrice]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RequestedPrice).ColumnName, price);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentQuantity).ColumnName, order.Data[SingleOrderProperties.CurrentQuantity]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecutedQuantity).ColumnName, order.Data[SingleOrderProperties.ExecutedQuantity]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.LastExecQuantity).ColumnName, order.Data[SingleOrderProperties.LastExecQuantity]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RemainingQuantity).ColumnName, order.Data[SingleOrderProperties.RemainingQuantity]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RequestedQuantity).ColumnName, quantity);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.AON).ColumnName, hasAON);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RequestedMinQty).ColumnName, minQ);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecType).ColumnName, order.Data[SingleOrderProperties.ExecType]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderStatus).ColumnName, order.Data[SingleOrderProperties.OrderStatus]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.Note).ColumnName, order.Data[SingleOrderProperties.Note]);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsModifyRequest).ColumnName, true);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsUserRequest).ColumnName, true);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.DateTime).ColumnName, DateTime.Now);

                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderType).ColumnName, order.Data[SingleOrderProperties.OrderType]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RequestedOrderType).ColumnName, orderType);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.TimeInForce).ColumnName, order.Data[SingleOrderProperties.TimeInForce]);
                    ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RequestedTimeInForce).ColumnName, timeInForce);

                    db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);

                    FixGatewayManager.ModifyCancelOrder(order.Data[SingleOrderProperties.ClientID].ToString(), order.Data[SingleOrderProperties.SecurityCode].ToString(), string.Format("{0}-{1}", ordID, changeID), order.Data[SingleOrderProperties.OrigClOrdID].ToString(), quantity, price, Lookups.GetTimeInForceLookupByCodeValue(timeInForce).FixValue.ToCharArray()[0], Lookups.GetOrderTypeLookupByCodeValue(orderType).FixValue.ToCharArray()[0], Lookups.GetOrderSidesLookupByCodeValue(order.Data[SingleOrderProperties.OrderSide].ToString()).FixValue.ToCharArray()[0], hasAON, minQ);
                    ts.Complete();
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error modify order, ClientKey {0}, Error: {1}", clientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                try
                {
                    // Sessions.Push(new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
                }
                catch (Exception inex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending refused order modification to the client, ClientKey {0}, Error: {1}", clientKey, inex.Message), ConsoleColor.Red, ConsoleColor.White, false);
                }
            }

        }

        internal static void CancelOrder(Guid clientKey, Guid requesterOrderID, long orderID)
        {
            try
            {
                // Order Side

                SingleOrder order = m_orderID_ordersDetails[orderID];
                LookupItem orderSideLookup = Lookups.GetOrderSidesLookupByCodeValue(order.Data[SingleOrderProperties.OrderSide].ToString());
                lock (order)
                {
                    string[] arr = order.Data[SingleOrderProperties.ClOrderID].ToString().Split(new char[] { '-' });
                    string ordID = arr[0];
                    int changeID = int.Parse(arr[1]);
                    changeID++;

                    //string newOrigClOrdID = order.ClOrderID;
                    string newClOrdID = string.Format("{0}-{1}", ordID, changeID);


                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        DatabaseMethods db = new DatabaseMethods();
                        //db.UpdateOrderDetails(order.OrderID, newClOrdID, order.OrigClOrdID, order.CurrentQuantity, order.RemainingQuantity, order.ExecutedQuantity, order.CurrentPrice, order.CurrentPrice, order.CurrentPrice, order.OrderType, order.OrderStatus, order.ExecType, DateTime.Now, "Cancel Order Request", false, "", order.TimeInForce, null);


                        order.Data[SingleOrderProperties.ClOrderID] = newClOrdID;
                        order.Data[SingleOrderProperties.IsPending] = true;
                        order.Data[SingleOrderProperties.Note] = "Pending Cancel Request";
                        order.Data[SingleOrderProperties.ModifiedDateTime] = DateTime.Now;


                        Dictionary<string, object> orders_Columns = new Dictionary<string, object>();

                        orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ClOrderID).ColumnName, order.Data[SingleOrderProperties.ClOrderID]);

                        orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.IsPending).ColumnName, true);

                        orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.Note).ColumnName, order.Data[SingleOrderProperties.Note]);

                        orders_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.ModifiedDateTime).ColumnName, order.Data[SingleOrderProperties.ModifiedDateTime]);

                        Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                        orders_Filters.Add(PropColMapper.GetColumnByProperty(SingleOrderProperties.TableName, SingleOrderProperties.OrderID).ColumnName, order.Data[SingleOrderProperties.OrderID]);

                        Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderID).ColumnName, order.Data[SingleOrderProperties.OrderID]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ClOrderID).ColumnName, order.Data[SingleOrderProperties.ClOrderID]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrigClOrdID).ColumnName, order.Data[SingleOrderProperties.OrigClOrdID]);

                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.AvgPrice).ColumnName, order.Data[SingleOrderProperties.AvgPrice]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentPrice).ColumnName, order.Data[SingleOrderProperties.CurrentPrice]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecPrice).ColumnName, order.Data[SingleOrderProperties.LastExecPrice]);

                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.CurrentQuantity).ColumnName, order.Data[SingleOrderProperties.CurrentQuantity]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecutedQuantity).ColumnName, order.Data[SingleOrderProperties.ExecutedQuantity]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.LastExecQuantity).ColumnName, order.Data[SingleOrderProperties.LastExecQuantity]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.RemainingQuantity).ColumnName, order.Data[SingleOrderProperties.RemainingQuantity]);

                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.DateTime).ColumnName, DateTime.Now);


                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.ExecType).ColumnName, order.Data[SingleOrderProperties.ExecType]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderStatus).ColumnName, order.Data[SingleOrderProperties.OrderStatus]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.Note).ColumnName, order.Data[SingleOrderProperties.Note]);

                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsCancelRequest).ColumnName, true);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.IsUserRequest).ColumnName, true);

                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.OrderType).ColumnName, order.Data[SingleOrderProperties.OrderType]);
                        ordersDetails_Columns.Add(PropColMapper.GetColumnByProperty(SingleOrdDetailsProps.TableName, SingleOrdDetailsProps.TimeInForce).ColumnName, order.Data[SingleOrderProperties.TimeInForce]);


                        db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);




                        FixGatewayManager.CancelOrder(order.Data[SingleOrderProperties.ClientID].ToString(), order.Data[SingleOrderProperties.SecurityCode].ToString(), string.Format("{0}-{1}", ordID, changeID), order.Data[SingleOrderProperties.OrigClOrdID].ToString(), orderSideLookup.FixValue.ToCharArray()[0]);

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
                    // Sessions.Push(new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = clientKey, RefuseReason = "System Error", RequesterOrderID = requesterOrderID } });
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

        internal static Guid GetOrdSessionIfAvailable(long ordID)
        {
            if (m_OrdID_sessionKey.ContainsKey(ordID))
            {
                //Guid sessionKey = m_OrdID_sessionKey[ordID];
                //if (Sessions.IsOnlineOrHasOfflineUpdates(sessionKey))
                //    return sessionKey;
            }
            return Guid.Empty;
        }

        internal static bool CallbackHasReqOrdID(Guid callbackID, Guid requesterOrderID)
        {
            if (!m_sessionKey_ReqOrdIDs.ContainsKey(callbackID))
            {
                return false;
            }
            return m_sessionKey_ReqOrdIDs[callbackID].Contains(requesterOrderID);
        }

        #endregion Queries

        #region Helpers

        private static void RemoveOrderRef(long orderID)
        {
            lock (m_syncObject)
            {
                SingleOrder order = m_orderID_ordersDetails[orderID];
                Guid requesterOrderID = (Guid)order.Data[SingleOrderProperties.RequesterOrderID];
                Guid callbackID = m_OrdID_sessionKey[orderID];
                m_ReqOrdID_OrdID.Remove(requesterOrderID);
                m_sessionKey_ReqOrdIDs[callbackID].Remove(requesterOrderID);
                m_OrdID_sessionKey.Remove(orderID);
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

        //internal static void RemoveSubscriberOrdersReferences(Guid callbackKey)
        //{
        //    lock (m_syncObject)
        //    {
        //        if (m_subID_ReqOrdIDs.ContainsKey(callbackKey))
        //        {
        //            List<Guid> requesterOrderIDs = m_subID_ReqOrdIDs[callbackKey];
        //            m_subID_ReqOrdIDs.Remove(callbackKey);
        //            foreach (Guid reqOrdID in requesterOrderIDs)
        //            {
        //                if (m_ReqOrdID_subIDs.ContainsKey(reqOrdID))
        //                {
        //                    m_ReqOrdID_subIDs[reqOrdID].Remove(callbackKey);
        //                }
        //            }
        //        }
        //    }
        //}

        #endregion Helpers

        #region Monitor

        internal static List<SingleOrder> monitor_GetOrders(string side, string securityCode)
        {
            return m_orderID_ordersDetails.Values.Where(o => o.Data[SingleOrderProperties.OrderSide].ToString() == side && o.Data[SingleOrderProperties.SecurityCode].ToString() == securityCode).ToList();
        }


        #endregion Monitor
 
    }
}