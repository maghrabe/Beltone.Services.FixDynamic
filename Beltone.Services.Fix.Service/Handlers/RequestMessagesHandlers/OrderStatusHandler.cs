using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Entities.Constants;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class OrderInfoHandler : IRequestMessageHandler<IRequestMessage>
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
                if (typeof(OrderStatusRequest) == msgType)
                {
                    OrderStatusRequest statusReq = (OrderStatusRequest)msg;
                    SingleOrder order = OrdersManager.GetOrder(statusReq.RequesterOrderID);
                    if (order != null)
                    {
                        lock (order)
                        {
                            try
                            {
                                Fix_OrderStatusResponse status = new Fix_OrderStatusResponse();
                                {
                                    status.ClientKey = statusReq.ClientKey;
                                    status.ReqOrdID = statusReq.RequesterOrderID;
                                    status.OrdStatus = order[SingleOrderProperties.OrderStatus].ToString();
                                    status.IsExisted = true;
                                    status.AvgPrc = Convert.ToDouble(order[SingleOrderProperties.AvgPrice]);
                                    status.CurrPrc = Convert.ToDouble(order[SingleOrderProperties.CurrentPrice]);
                                    status.ExecPrc = Convert.ToDouble(order[SingleOrderProperties.LastExecPrice]);
                                    status.CurrQty = (int)order[SingleOrderProperties.CurrentQuantity];
                                    status.LastExecQty = (int)order[SingleOrderProperties.LastExecQuantity];
                                    status.RemQty = (int)order[SingleOrderProperties.RemainingQuantity];
                                    status.TotExecQty = (int)order[SingleOrderProperties.ExecutedQuantity];
                                    status.IsActive = Convert.ToBoolean(order[SingleOrderProperties.IsActive]);
                                    status.IsPending = Convert.ToBoolean(order[SingleOrderProperties.IsPending]);
                                    status.IsCompleted = Convert.ToBoolean(order[SingleOrderProperties.IsCompleted]);
                                    status.IsExecuted = Convert.ToBoolean(order[SingleOrderProperties.IsExecuted]);
                                    status.LastUpdate = (DateTime)order[SingleOrderProperties.ModifiedDateTime];
                                    status.PlacementTime = (DateTime)order[SingleOrderProperties.PlacementDateTime];
                                    status.OrdTyp = order[SingleOrderProperties.OrderType].ToString();
                                    status.TIF = order[SingleOrderProperties.TimeInForce].ToString();
                                    status.Note = order[SingleOrderProperties.Note].ToString();
                                }

                                Dictionary<string, object> data = new Dictionary<string, object>();
                                bool aon = (bool)order[SingleOrderProperties.AON];
                                if (aon)
                                {
                                    data.Add("AON", aon);
                                    data.Add("MinQty", (int)order[SingleOrderProperties.MinQty]);
                                }
                                if (HasValue(order[SingleOrderProperties.ExpirationDate]))
                                    data.Add("ExpirationDate", (DateTime)order[SingleOrderProperties.ExpirationDate]);
                                if (HasValue(order[SingleOrderProperties.ExpirationDateTime]))
                                    data.Add("ExpirationDateTime", (DateTime)order[SingleOrderProperties.ExpirationDateTime]);


                                Sessions.Push(Sessions.GetUsername(status.ClientKey), new IResponseMessage[] { status });
                            }
                            catch (Exception ex)
                            {
                                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending order status to the client, ClientKey {0}, Error: {1}", statusReq, ex.Message), ConsoleColor.Red, ConsoleColor.White, true);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            Sessions.Push(Sessions.GetUsername(statusReq.ClientKey), new IResponseMessage[] { new Fix_OrderStatusResponse() 
                             { ClientKey = statusReq.ClientKey, ReqOrdID = statusReq.RequesterOrderID , IsExisted = false} });
                        }
                        catch (Exception ex)
                        {
                            Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                            SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending order status to the client, ClientKey {0}, Error: {1}", statusReq, ex.Message), ConsoleColor.Red, ConsoleColor.White, true);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        bool HasValue(object obj)
        {
            return obj != null && obj != DBNull.Value && obj.ToString() != string.Empty;
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
