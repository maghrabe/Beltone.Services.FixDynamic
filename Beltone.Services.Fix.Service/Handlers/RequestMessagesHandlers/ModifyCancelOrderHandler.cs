using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Provider;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Entities.Constants;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class ModifyCancelOrderHandler : IRequestMessageHandler<IRequestMessage>
    {
        #region IRequestMessageHandler<IRequestMessage> Members

        int m_orderPriceDigitsRounding = int.Parse(SystemConfigurations.GetAppSetting("OrderPriceDigitsRounding"));

        public void Initialize()
        {
        }

        public bool Handle(IRequestMessage msg)
        {
            try
            {
                Type msgType = msg.GetType();
                if (typeof(IRequestMessage).IsAssignableFrom(msgType))
                {
                    if (typeof(ModifyCancelOrder) == msgType)
                    {
                        Counters.IncrementCounter(CountersConstants.ClientsReplaceOrderReqs);


                        ModifyCancelOrder modifyOrder = (ModifyCancelOrder)msg;
                        SingleOrder order = OrdersManager.GetOrder(modifyOrder.RequesterOrderID);

                        #region order existanse validation
                        if (order == null)
                        {
                            Sessions.Push(Sessions.GetUsername(modifyOrder.ClientKey), new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseReason = "Order Not Found!", RequesterOrderID = modifyOrder.RequesterOrderID } });
                            SystemLogger.LogEventAsync(string.Format("Order Not Found RequesterID {0}  ", modifyOrder.RequesterOrderID));
                            return true;
                        }
                        #endregion order existanse validation

                        lock (order)
                        {

                            #region check order activation
                            // check ispending before isactive becoz a pendingnew order might be not isactive but ispending
                            bool isPending, isMcsdPending = false;
                            isPending = Convert.ToBoolean(order[SingleOrderProperties.IsPending]);
                            isMcsdPending = Convert.ToBoolean(order[SingleOrderProperties.IsPendingMcsd]);

                            if (isPending || isMcsdPending)
                            {
                                Sessions.Push(Sessions.GetUsername(modifyOrder.ClientKey), new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseReason = !isMcsdPending ? "Order is in pending request" : "Awaiting for allocation response", RequesterOrderID = modifyOrder.RequesterOrderID } });
                                return true;
                            }

                            bool isActive = false;
                            isActive = Convert.ToBoolean(order[SingleOrderProperties.IsActive]);
                            if (!isActive)
                            {
                                Sessions.Push(Sessions.GetUsername(modifyOrder.ClientKey), new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseReason = "Order not active any more", RequesterOrderID = modifyOrder.RequesterOrderID } });
                                return true;
                            }


                            #endregion check order activation

                            #region order validation
                            // ValidateOrder
                            string validation = ValidateOrder(modifyOrder, order);
                            if (validation != "valid")
                            {
                                Sessions.Push(Sessions.GetUsername(modifyOrder.ClientKey), new IResponseMessage[] { new Fix_OrderReplaceRefusedByService() { ClientKey = modifyOrder.ClientKey, RefuseReason = validation, RequesterOrderID = modifyOrder.RequesterOrderID } });
                                return true;
                            }
                            #endregion order validation

                            // handle ClOrderID and OrigClOrdID
                            OrdersManager.HandleModifyOrder(Sessions.GetUsername(modifyOrder.ClientKey), modifyOrder.ClientKey, modifyOrder.RequesterOrderID, (long)order[SingleOrderProperties.OrderID], modifyOrder.Quantity, Math.Round(modifyOrder.Price, m_orderPriceDigitsRounding), modifyOrder.OrderType, modifyOrder.TimeInForce, modifyOrder.OptionalParam);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.LogErrorAsync("ModifyCancelOrderHandler Error: " + ex.Message);
                return true;
            }
        }

        private string ValidateOrder(ModifyCancelOrder modify, SingleOrder order)
        {
            if (modify.ClientKey == Guid.Empty || modify.ClientKey == null)
            {
                return "Invalid ClientKey";
            }
            string username = Sessions.GetUsername(modify.ClientKey);
            if (!Sessions.IsSubscribedToSendMsg(username))
            {
                return "Client not subscribed";
            }
            Dictionary<string, object> optionalParam = modify.OptionalParam;
            if (optionalParam != null)
            {
                bool aon = false;
                if (optionalParam.ContainsKey("AON") && (optionalParam["AON"] == null || !bool.TryParse(optionalParam["AON"].ToString(), out aon)))
                    return "Invalid AON";
                if (aon)
                {
                    if (!optionalParam.ContainsKey("MinQty"))
                        return "Missing Min. Qty";
                    int minq = 0;
                    if (optionalParam["MinQty"] == null || !int.TryParse(optionalParam["MinQty"].ToString(), out minq))
                        return "Invalid Min. Qty";
                    if (minq <= 0)
                        return "Invalid Min. Qty";
                    if (minq > modify.Quantity)
                        return "Min. quantity should be equal or less than order's quantity";
                }
            }
            return "valid";
        }


        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}



   //private string ValidateOrder(ModifyCancelOrder modify, SingleOrder order)
   //     {
   //         if (modify.ClientKey == Guid.Empty || modify.ClientKey == null)
   //         {
   //             return "Invalid ClientKey";
   //         }
   //         if (!CallbackList.IsSubscribed(modify.ClientKey))
   //         {
   //             return "Client not subscribed";
   //         }
   //         if (!Lookups.GetOrderTypes().Contains(modify.OrderType))
   //         {
   //             return "Order Type Error !";
   //         }
   //         if (!Lookups.GetTimeInForceTypes().Contains(modify.TimeInForce))
   //         {
   //             return "Invalid TimeInForce !";
   //         }
   //         if (modify.Quantity <= 0 || modify.Quantity > int.MaxValue)
   //         {
   //             return "Invalid Quantity!";
   //         }
   //         if (modify.Price <= 0 || modify.Price > double.MaxValue)
   //         {
   //             return "Invalid Price!";
   //         }
   //         if (modify.Quantity < (int)order[SingleOrderProperties.ExecutedQuantity])
   //         {
   //             return string.Format("Modified quantity should be greater than executed quantity [{0}]", (int)order[SingleOrderProperties.ExecutedQuantity]);
   //         }
   //         if((int)order[SingleOrderProperties.CurrentQuantity] == modify.Quantity
   //             && (double)order[SingleOrderProperties.CurrentPrice] == modify.Price
   //             && order[SingleOrderProperties.TimeInForce].ToString() == modify.TimeInForce
   //             && order[SingleOrderProperties.OrderType].ToString() == modify.OrderType)
   //         {
   //             return "No change in order details!";
   //         }

   //         return "valid";
   //     }
