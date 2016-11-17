using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Entities.Constants;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class OrderCancelRequestHandler : IRequestMessageHandler<IRequestMessage>
    {
        #region IRequestMessageHandler<IRequestMessage> Members

        public void Initialize()
        {
        }

        public bool Handle(IRequestMessage msg)
        {
            Type msgType = msg.GetType();
            if (typeof(IRequestMessage).IsAssignableFrom(msgType))
            {
                if (typeof(CancelSingleOrder) == msgType)
                {

                    CancelSingleOrder cancelOrder = (CancelSingleOrder)msg;

                    if (cancelOrder.ClientKey == Guid.Empty || cancelOrder.ClientKey == null)
                    {
                        SystemLogger.LogEventAsync(string.Format("ClientKey[0] not valid! ", cancelOrder.ClientKey.ToString()));
                        return true;
                    }
                    string username = Sessions.GetUsername(cancelOrder.ClientKey);
                    if (!Sessions.IsSubscribedToSendMsg(username))
                    {
                        SystemLogger.LogEventAsync(string.Format("ClientKey not subscribed", cancelOrder.ClientKey));
                        return true;
                    }
                    SingleOrder order = OrdersManager.GetOrder(cancelOrder.RequesterOrderID);
                    if (order == null)
                    {
                        Sessions.Push(Sessions.GetUsername(cancelOrder.ClientKey), new IResponseMessage[] { new Fix_OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = "order not found", RequesterOrderID = cancelOrder.RequesterOrderID, OrderStatus = "Order Not Found" } });
                        SystemLogger.LogEventAsync(string.Format("Order Not Found RequesterID {0}  ", cancelOrder.RequesterOrderID));
                        return true;
                    }

                    lock (order)
                    {
                        string status = order[SingleOrderProperties.OrderStatus].ToString();

                        // check ispending before isactive becoz a pendingnew order might be not isactive but ispending
                        bool isPending, isMcsdPending = false;
                        isPending = Convert.ToBoolean(order[SingleOrderProperties.IsPending]);
                        isMcsdPending = Convert.ToBoolean(order[SingleOrderProperties.IsPendingMcsd]);
                        if (bool.TryParse(order[SingleOrderProperties.IsPending].ToString(), out isPending))
                        {
                            if (isPending || isMcsdPending)
                            {
                                Sessions.Push(Sessions.GetUsername(cancelOrder.ClientKey), new IResponseMessage[] { new Fix_OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = !isMcsdPending ? "Order is in pending request" : "Awaiting for allocation response", RequesterOrderID = cancelOrder.RequesterOrderID, OrderStatus = status } });
                                return true;
                            }
                        }

                        bool isActive = false;
                        if (bool.TryParse(order[SingleOrderProperties.IsActive].ToString(), out isActive))
                        {
                            if (!isActive)
                            {
                                Sessions.Push(Sessions.GetUsername(cancelOrder.ClientKey), new IResponseMessage[] { new Fix_OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = "Order not active anymore", RequesterOrderID = cancelOrder.RequesterOrderID, OrderStatus = status } });
                                return true;
                            }
                        }

                        try
                        {
                            OrdersManager.HandleCancelOrder(username, cancelOrder.ClientKey, (Guid)order[SingleOrderProperties.RequesterOrderID], (long)order[SingleOrderProperties.OrderID], msg.OptionalParam);
                            Counters.IncrementCounter(CountersConstants.ClientsCancelOrderReqs);
                        }
                        catch (Exception ex)
                        {
                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                            SystemLogger.LogEventAsync(string.Format("Error cancelling order {0}, Error: {1}", order[SingleOrderProperties.OrderID], ex.ToString()));
                        }
                        return true;
                    }
                }
            }
            return false;
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





//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Beltone.Services.Fix.Contract.Interfaces;
//using Beltone.Services.Fix.Contract.Entities.RequestMessages;
//using Beltone.Services.Fix.Utilities;
//using Beltone.Services.Fix.Service.Singletons;
//using Beltone.Services.Fix.Entities.Entities;
//using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
//using Beltone.Services.Fix.Entities.Constants;

//namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
//{
//    public class OrderCancelRequestHandler : IRequestMessageHandler<IRequestMessage>
//    {
//        #region IRequestMessageHandler<IRequestMessage> Members

//        public void Initialize()
//        {
//        }

//        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
//        public bool Handle(IRequestMessage msg)
//        {
//            Type msgType = msg.GetType();
//            if (typeof(IRequestMessage).IsAssignableFrom(msgType))
//            {
//                if (typeof(CancelSingleOrder) == msgType)
//                {

//                    Counters.IncrementCounter(CountersConstants.ClientsCancelOrderReqs);

//                    CancelSingleOrder cancelOrder = (CancelSingleOrder)msg;

//                    if (cancelOrder.ClientKey == Guid.Empty || cancelOrder.ClientKey == null)
//                    {
//                        SystemLogger.WriteOnConsoleAsync(true, string.Format("ClientKey[0] not valid! ", cancelOrder.ClientKey.ToString()), ConsoleColor.Red, ConsoleColor.White, false);
//                        return true;
//                    }
//                    if (!CallbackList.IsSubscribed(cancelOrder.ClientKey))
//                    {
//                        SystemLogger.WriteOnConsoleAsync(true, string.Format("ClientKey not subscribed", cancelOrder.ClientKey), ConsoleColor.Red, ConsoleColor.White, false);
//                        return true;
//                    }
//                    SingleOrder order = OrdersManager.GetOrder(cancelOrder.RequesterOrderID);
//                    if (order == null)
//                    {
//                        try
//                        {
//                            CallbackList.PushUpdates(new IResponseMessage[] { new Fix_OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = "order not found", RequesterOrderID = cancelOrder.RequesterOrderID , OrderStatus = "Order Not Found"} });
//                            //CallbackList.PushUpdates(new IResponseMessage[] { new OrderRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseMessage = validation, RequesterOrderID = modifyOrder.RequesterOrderID } });
//                        }
//                        catch (Exception ex)
//                        {
//                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
//                            SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending cancel refused to the client, ClientKey {0}, Error: {1}", cancelOrder.ClientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
//                        }
//                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Order Not Found RequesterID {0}  ", cancelOrder.RequesterOrderID), ConsoleColor.Red, ConsoleColor.Black, true);
//                        return true;
//                    }

//                    string status = order[SingleOrderProperties.OrderStatus].ToString();
//                    bool isPending = false;
//                    if (bool.TryParse(order[SingleOrderProperties.IsPending].ToString(), out isPending))
//                    {
//                        if (isPending)
//                        {
//                            try
//                            {
//                                CallbackList.PushUpdates(new IResponseMessage[] { new Fix_OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = "Order Status: " + status, RequesterOrderID = cancelOrder.RequesterOrderID, OrderStatus = status } });
//                                //CallbackList.PushUpdates(new IResponseMessage[] { new OrderRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseMessage = validation, RequesterOrderID = modifyOrder.RequesterOrderID } });
//                            }
//                            catch (Exception ex)
//                            {
//                                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
//                                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending cancel refused to the client, ClientKey {0}, Error: {1}", cancelOrder.ClientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
//                            }
//                            return true;
//                        }
//                    }

                    
//                    //if (!OrdersManager.CanModifyCancelOrder(order[SingleOrderProperties.OrderStatus].ToString()))
//                    //{
//                    //    try
//                    //    {
//                    //        CallbackList.PushUpdates(new IResponseMessage[] { new OrderCancelRefusedByService() { ClientKey = cancelOrder.ClientKey, Message = "Order Status: " + order[SingleOrderProperties.OrderStatus], RequesterOrderID = cancelOrder.RequesterOrderID } });
//                    //        //CallbackList.PushUpdates(new IResponseMessage[] { new OrderRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseMessage = validation, RequesterOrderID = modifyOrder.RequesterOrderID } });
//                    //    }
//                    //    catch (Exception ex)
//                    //    {
//                    //        SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending cancel refused to the client, ClientKey {0}, Error: {1}", cancelOrder.ClientKey, ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
//                    //    }
//                    //    return true;
//                    //}

//                    try
//                    {
//                        OrdersManager.CancelOrder(cancelOrder.ClientKey , (Guid)order[SingleOrderProperties.RequesterOrderID], (long)order[SingleOrderProperties.OrderID]);
//                    }
//                    catch (Exception ex)
//                    {
//                        Counters.IncrementCounter(CountersConstants.ExceptionMessages);
//                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Error cancelling order {0}, Error: {1}", order[SingleOrderProperties.OrderID], ex.Message), ConsoleColor.Red, ConsoleColor.White, false);
//                    }

//                    return true;
//                }
//            }
//            return false;
//        }

//        #endregion

//        #region IDisposable Members

//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }
//}
