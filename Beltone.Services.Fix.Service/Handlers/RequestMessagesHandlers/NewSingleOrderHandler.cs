using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Provider;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Service.Singletons;
using QuickFix;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Entities.Constants;
using Beltone.Services.Fix.Contract.Entities;
using Beltone.Services.MCDR.Contract.Constants;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class NewSingleOrderHandler : IRequestMessageHandler<IRequestMessage>
    {
        #region IRequestMessageHandler<IRequestMessage> Members

        int m_allowableOrderDelayInMilliSeconds = int.Parse(SystemConfigurations.GetAppSetting("AllowableOrderDelayInMilliSeconds"));
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
                    if (typeof(NewSingleOrder) == msgType)
                    {
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        Counters.IncrementCounter(CountersConstants.ClientsNewOrderReqs);

                        NewSingleOrder order = (NewSingleOrder)msg;

                        #region validate client key
                        if (order.ClientKey == Guid.Empty || order.ClientKey == null)
                        {
                            SystemLogger.LogEventAsync(string.Format("ClientKey [{0}] not valid! ", order.ClientKey.ToString()));
                            return true;
                        }
                        string username = Sessions.GetUsername(order.ClientKey);
                        if (!Sessions.IsSubscribedToSendMsg(username))
                        {
                            SystemLogger.LogEventAsync(string.Format("ClientKey [{0}] not subscribed", order.ClientKey));
                            return true;
                        }
                        #endregion validate client key

                        #region validate stock group
                        Stock stock = StocksDefinitions.GetStockByCode(order.SecurityID);
                        if(stock == null)
                            Sessions.Push(Sessions.GetUsername(order.ClientKey), new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = order.ClientKey, RefuseMessage = "Stock Not Found!", RequesterOrderID = order.RequesterOrderID } });
                        #endregion validate stock group

                        #region validate currency
                        CurrencyItem currency = Currencies.GetCurrencyByCode(stock.CurrencyCode);
                        #endregion validate currency

                        #region validate order details
                        string validation = ValidateOrder(username, order);
                        #endregion validate order details

                        #region not valid order

                        if (validation != "valid")
                        {
                            try
                            {
                                SystemLogger.LogEventAsync(string.Format("OrderRefusedByService RequesterOrderID: {0}, Reason: {1}", order.RequesterOrderID, validation));
                                Sessions.Push(Sessions.GetUsername(order.ClientKey), new IResponseMessage[] { new Fix_OrderRefusedByService() { ClientKey = order.ClientKey, RefuseMessage = validation, RequesterOrderID = order.RequesterOrderID } });
                            }
                            catch (Exception ex)
                            {
                                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                                SystemLogger.LogEventAsync(string.Format("Error sending refused order to the client, ClientKey {0}, Error: {1}", order.ClientKey, ex.Message));
                            }
                            return true;
                        }
                        #endregion not valid order
  
                        OrdersManager.HandleNewSingleOrder(username, order.ClientKey, order.RequesterOrderID, order.ClientID, order.CustodyID, order.SecurityID, order.OrderSide, Math.Round(order.Price, m_orderPriceDigitsRounding), order.Quantity, order.OrderType, DateTime.Now, order.TimeInForce, currency, order.ExchangeID, order.DateTime, stock.GroupID, stock.MarketID, order.HandleInst, order.ExpirationDateTime, order.OptionalParam);
                        sw.Stop();
                        SystemLogger.LogEventAsync(string.Format("new order handled in {0} ms ", sw.ElapsedMilliseconds));
                        sw = null;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.LogErrorAsync("NewSingleOrderHandler Error: " + ex.Message);
                return true;
            }
        }

        private string ValidateOrder(string username, NewSingleOrder order)
        {
            if (OrdersManager.CallbackHasReqOrdID(order.RequesterOrderID))
            {
                return "Requested Order ID already existed for this session.";
            }
            if (order.DateTime.Date != DateTime.Today.Date)
            {
                return "Invalid DateTime";
            }
            if (DateTime.Now.Subtract(order.DateTime).Milliseconds > m_allowableOrderDelayInMilliSeconds)
            {
                return "Delayed Order";
            }
            Dictionary<string, object> optionalParam = order.OptionalParam;
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
                    if (minq > order.Quantity)
                        return "Min. quantity should be equal or less than order's quantity";
                }

                if (optionalParam.ContainsKey(ALLOC_REQ_FIELDS.ALLOC_TYPE) && order.OrderSide == "Buy")
                {
                    bool isAllowBuyAllocation = Convert.ToBoolean(SystemConfigurations.GetAppSetting("AllowBuyAllocation"));
                    if(isAllowBuyAllocation == false)
                    {
                        return "Mcsd Allocation for buy orders is not allowed";
                    }
                }
            }

            return "valid";
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





