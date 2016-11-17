using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Constants;
using Beltone.Services.Fix.Contract.Entities;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public class OrderCancelRejectHandler : IResponseMessageHandler<QuickFix.Message>
    {
        private static int m_msgTypeTag;
        private static string m_MsgTypeTagValueToHandle;

        #region IResponseMessageHandler<IResponseMessage> Members

        public void Initialize(string msgTypeTagValueToHandle)
        {
            m_MsgTypeTagValueToHandle = msgTypeTagValueToHandle;
            m_msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));
        }

        public void Handle(QuickFix.Message msg)
        {

            long orderID = -1;
            // check message type tag, if execution report then push execution report update
            QuickFix.MsgType msgType = new QuickFix.MsgType();
            string msgTypeString = msg.getHeader().getField(m_msgTypeTag);
            if (msgTypeString == m_MsgTypeTagValueToHandle)
            {
                Counters.IncrementCounter(CountersConstants.OrderCancelRejectRspMsgs);

                //SystemLogger.WriteOnConsole(true, string.Format("new message recieved, Type: {0}, Message: '{1}'", msgTypeString, msg.ToXML()), ConsoleColor.Cyan, ConsoleColor.Black, false);
                SingleOrder order = null;
                // get order id
                if (msg.isSetField(11))
                {
                     orderID = long.Parse(msg.getField(11).Split(new char[] { '-' })[0]);
                    try
                    {
                        order = OrdersManager.GetOrder(orderID);
                    }
                    catch (Exception ex) // order not found
                    {
                        // get borse order  id
                        string bourseOrderID = msg.getField(37);
                        order = OrdersManager.GetOrder(bourseOrderID);
                        if (order == null)
                        {
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("order BourseOrderID {0} not found !", bourseOrderID), ConsoleColor.Red, ConsoleColor.Black, true);
                            return;
                        }
                    }
                } // then search by borse order  id
                else if (msg.isSetField(37))
                {
                    string bourseOrderID = msg.getField(37);
                    order = OrdersManager.GetOrder(bourseOrderID);
                    if (order == null)
                    {
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("order BourseOrderID {0} not found !", bourseOrderID), ConsoleColor.Red, ConsoleColor.Black, true);
                        return;
                    }
                }


                lock (order)
                {

                    string reason = string.Empty;
                    if (msg.isSetField(102))
                    {
                        try
                        {
                            reason = Lookups.GetOrderCancelRejectReasonsLookup(msg.getField(102)).MessageEn + " ,";
                        }
                        catch (Exception ex)
                        {
                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("Error getting order cancel reject reason: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, false);
                        }
                    }
                    if (msg.isSetField(58)) { reason += msg.getField(58) + " "; }
                    Nullable<DateTime> transactionDateTime = null;
                    if (msg.isSetField(60)) { transactionDateTime = msg.getUtcTimeStamp(60); }
                    LookupItem statusLookup = Lookups.GetOrderStatusLookup(msg.getField(39));
                    string status = statusLookup.CodeValue;

                    Guid requesterOrderID = (Guid)order[SingleOrderProperties.RequesterOrderID];
                    // find subscribed callbacks
                    //order[SingleOrderProperties.OrderStatus] = status;

                    bool isPendingCancel = false;
                    bool isPendingReplace = false;
                    string orderNote = string.Empty;
                    if (order[SingleOrderProperties.OrderStatus].ToString() == ORD_STATUS.PendingCancel)
                    {
                        isPendingCancel = true;
                        orderNote = "Order cancel has been refused by bourse: " + reason;
                    }
                    else if (order[SingleOrderProperties.OrderStatus].ToString() == ORD_STATUS.PendingReplace)
                    {
                        // handle diff allocation cancellation

                        isPendingReplace = true;
                        orderNote = "Order replace has been refused by bourse: " + reason;


                        #region Handle Mcsd Reset Allocation


                        try
                        {
                            if (order[SingleOrderProperties.IsMcsdAllocRequired] != null)
                            {
                                if (Convert.ToBoolean(order[SingleOrderProperties.IsMcsdAllocRequired]) == true)
                                {

                                    order[SingleOrderProperties.ActionOnAllocResponse] = ActionOnAllocResponse.DoNothing;
                                    int mcsdQty = Convert.ToInt32(order[SingleOrderProperties.McsdrAllocQty]);

                                    //  orderid = Convert.ToInt64(order[SingleOrderProperties.OrderID]);
                                    OrdersManager.HandleRejectedOrderAllocation(orderID, mcsdQty);
                                }
                            }
                        }
                        catch (Exception exp)
                        {
                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                            SystemLogger.LogErrorAsync(string.Format("Error, Reset MCSD allocation of Order {0}, Error : {1}", orderID, exp.ToString()));
                        }
                        #endregion
                   
                    
                    }
                    else
                    {
                        orderNote = "Order cancel/replace has been refused by bourse: " + reason;
                    }


                    // create IResponseMessage
                    string username = OrdersManager.GetOrdSessionIfAvailable((long)order[SingleOrderProperties.OrderID]);
                    if (username != null)
                    {
                        try
                        {
                            if (isPendingCancel)
                            {
                                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderCancelRejected() { Message = reason, RequesterOrderID = requesterOrderID, OrderStatus = order[SingleOrderProperties.OriginalOrderStatus].ToString() } });
                            }
                            else if (isPendingReplace)
                            {
                                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceRejected() { Message = reason, RequesterOrderID = requesterOrderID, OrderStatus = order[SingleOrderProperties.OriginalOrderStatus].ToString() } });
                            }
                            else
                            {
                                Sessions.Push(username, new IResponseMessage[] { new Fix_OrderReplaceCancelReject() { Message = reason, RequesterOrderID = requesterOrderID, OrderStatus = order[SingleOrderProperties.OriginalOrderStatus].ToString() } });
                            }
                        }
                        catch (Exception ex)
                        {
                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("error sending order cancel reject to clients: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                        }
                    }
                    // update databse
                    try
                    {
                        DatabaseMethods db = new DatabaseMethods();

                        order[SingleOrderProperties.OrderStatus] = order[SingleOrderProperties.OriginalOrderStatus];
                        order[SingleOrderProperties.IsPending] = false;
                        order[SingleOrderProperties.Note] =
                        order[SingleOrderProperties.Note2] = "Action Status: " + status;
                        order[SingleOrderProperties.ModifiedDateTime] = transactionDateTime == null ? DateTime.Now : transactionDateTime;


                        Dictionary<string, object> orders_Columns = new Dictionary<string, object>();


                        orders_Columns.Add(SingleOrderProperties.OrderStatus, order[SingleOrderProperties.OriginalOrderStatus]);
                        orders_Columns.Add(SingleOrderProperties.Note, order[SingleOrderProperties.Note]);
                        orders_Columns.Add(SingleOrderProperties.Note2, order[SingleOrderProperties.Note2]);
                        orders_Columns.Add(SingleOrderProperties.ActionOnAllocResponse, ActionOnAllocResponse.DoNothing);
                        orders_Columns.Add(SingleOrderProperties.ModifiedDateTime, transactionDateTime == null ? DateTime.Now : transactionDateTime);
                        orders_Columns.Add(SingleOrderProperties.IsPending, false);
                       

                        Dictionary<string, object> orders_Filters = new Dictionary<string, object>();
                        orders_Filters.Add(SingleOrderProperties.OrderID, order[SingleOrderProperties.OrderID]);

                        Dictionary<string, object> ordersDetails_Columns = new Dictionary<string, object>();
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderID, order[SingleOrderProperties.OrderID]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ClOrderID, order[SingleOrderProperties.ClOrderID]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrigClOrdID, order[SingleOrderProperties.OrigClOrdID]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutionMsgType, msgTypeString);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.AvgPrice, order[SingleOrderProperties.AvgPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentPrice, order[SingleOrderProperties.CurrentPrice]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecPrice, order[SingleOrderProperties.LastExecPrice]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.CurrentQuantity, order[SingleOrderProperties.CurrentQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutedQuantity, order[SingleOrderProperties.ExecutedQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.LastExecQuantity, order[SingleOrderProperties.LastExecQuantity]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.RemainingQuantity, order[SingleOrderProperties.RemainingQuantity]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecType, order[SingleOrderProperties.ExecType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderStatus, order[SingleOrderProperties.OrderStatus]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.Note, order[SingleOrderProperties.Note]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.OrderType, order[SingleOrderProperties.OrderType]);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.TimeInForce, order[SingleOrderProperties.TimeInForce]);

                        ordersDetails_Columns.Add(SingleOrdDetailsProps.IsResponse, true);


                        if (order[SingleOrderProperties.OrderStatus].ToString() == ORD_STATUS.PendingCancel)
                        {
                            ordersDetails_Columns.Add(SingleOrdDetailsProps.IsCancelResponse, true);
                        }
                        else if (order[SingleOrderProperties.OrderStatus].ToString() == ORD_STATUS.PendingReplace)
                        {
                            ordersDetails_Columns.Add(SingleOrdDetailsProps.IsModifyResponse, true);
                        }
                        else
                        {
                            ordersDetails_Columns.Add(SingleOrdDetailsProps.IsModifyResponse, true);
                            ordersDetails_Columns.Add(SingleOrdDetailsProps.IsCancelResponse, true);
                        }


                        ordersDetails_Columns.Add(SingleOrdDetailsProps.DateTime, DateTime.Now);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutionDate, transactionDateTime == null ? DateTime.Now : transactionDateTime);
                        ordersDetails_Columns.Add(SingleOrdDetailsProps.ExecutionRecievedDateTime, DateTime.Now);


                        db.UpdateOrderDetails(orders_Columns, orders_Filters, ordersDetails_Columns);




                        db = null;
                    }
                    catch (Exception ex)
                    {
                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("error updating order cancel reject into the db error: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                    }
                }
            }
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
