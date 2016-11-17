using Beltone.Services.Fix.Entities.Constants;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.MCDR.Contract.Constants;
using Beltone.Services.MCDR.Contract.Entities.ResMsgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMcsdMessagesHandlers
   
{
    public class McsdExecutionResponseMessageHandler : IResponseMcsdMessageHandler<AllocRes>
    {
        public void Initialize()
        {
            //throw new NotImplementedException();
        }

        public void Handle(AllocRes msg)
        {
           
            if (msg != null)
            {
                switch (msg.ResType)
                {
                    case ALLOC_RES_TYPES.ALLOC_ACCEPTED_RES:
                    case ALLOC_RES_TYPES.ALLOC_UPDATED_RES:
                        HandleUpdatedAllocation(msg);
                        break;
                    case ALLOC_RES_TYPES.ALLOC_REFUSED_RES:
                    case ALLOC_RES_TYPES.ALLOC_UPDATE_REFUSED_RES:
                        HandleRejectAllocation(msg);
                      
                        break;

                    default:
                        // log here. 
                        SystemLogger.LogErrorAsync(string.Format("Unhandled message type received from MCSD : ", msg.ResType));

                        break;
                }
            }
        }

        
        private void HandleUpdatedAllocation(AllocRes msg)
        {
            // update might be new,increase or decrease update
            // search order
            SingleOrder foundOrd = OrdersManager.GetOrder(msg.ReqID);
            if (foundOrd == null)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Allocation Response not handled because Order ReqID [{0}] not found!"), ConsoleColor.Red, ConsoleColor.White, false);
                return;
            }
            lock (foundOrd)
            {
                // check status
                // switch btw
                // if PendingNew then send new order
                string actionOnAllocResponse = foundOrd.Data[SingleOrderProperties.ActionOnAllocResponse].ToString();

             

                switch(actionOnAllocResponse)
                {
                    case ActionOnAllocResponse.SendNewOrder :
                        OrdersManager.HandleAllocationNewOrderAccepted(foundOrd, Fix.Entities.McsdSourceMessage.McsdAccepted);
                        break;
                    case ActionOnAllocResponse.ModifyOrder:
                        OrdersManager.HandleAllocationUpdateOrderAccepted(foundOrd, Fix.Entities.McsdSourceMessage.McsdAccepted);
                        break;
                    case ActionOnAllocResponse.CancelOrder:
                        OrdersManager.HandleAllocationCancelOrderAccepted(foundOrd);
                        break;
                    case ActionOnAllocResponse.DoNothing:
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Do nothing Allocation Response not handled ReqID [{0}] ResponseType is {1}", msg.ReqID, msg.ResType), ConsoleColor.Red, ConsoleColor.White, false);
                        break;
                    default :
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Allocation Response not handled ReqID [{0}] ResponseType is {1} ", msg.ReqID,msg.ResType), ConsoleColor.Red, ConsoleColor.White, false);
                        break;
                }


                //if (status == ORD_STATUS.PendingNew)
                //    OrdersManager.HandleAllocationNewOrderAccepted(foundOrd, Fix.Entities.McsdSourceMessage.McsdAccepted);
              
                //else if (status == ORD_STATUS.Canceled)
                //    OrdersManager.HandleAllocationCancelOrderAccepted(foundOrd);
                //else if (isActive)
                //    OrdersManager.HandleAllocationUpdateOrderAccepted(foundOrd, Fix.Entities.McsdSourceMessage.McsdAccepted);
                //else
                //    SystemLogger.WriteOnConsoleAsync(true, string.Format("Allocation Response not handled ReqID [{0}] status [{1}]  IsActive [{2}]", msg.ReqID, status, isActive), ConsoleColor.Red, ConsoleColor.White, false);
            }
        }

        private void HandleRejectAllocation(AllocRes msg)
        {
            // update might be new,increase or decrease update
            // search order
            SingleOrder foundOrd = OrdersManager.GetOrder(msg.ReqID);
            if (foundOrd == null)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Allocation Response not handled because Order ReqID [{0}] not found!"), ConsoleColor.Red, ConsoleColor.White, false);
                return;
            }
            lock (foundOrd)
            {

                string actionOnAllocResponse = foundOrd.Data[SingleOrderProperties.ActionOnAllocResponse].ToString();



                switch (actionOnAllocResponse)
                {
                    case ActionOnAllocResponse.SendNewOrder:
                        OrdersManager.HandleAllocationNewOrderRefused(msg, foundOrd);
                        break;
                    case ActionOnAllocResponse.ModifyOrder:
                        OrdersManager.HandleAllocationUpdateOrderRefused(msg, foundOrd);
                        break;
                    case ActionOnAllocResponse.CancelOrder:
                        OrdersManager.HandleAllocationCancelOrderRefused(msg, foundOrd);
                        break;
                    case ActionOnAllocResponse.DoNothing:
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Do nothing Allocation Response not handled ReqID [{0}] ResponseType is {1}", msg.ReqID, msg.ResType), ConsoleColor.Red, ConsoleColor.White, false);
                        break;
                    default:
                        SystemLogger.WriteOnConsoleAsync(true, string.Format("Allocation Response not handled ReqID [{0}] ResponseType is {1} ", msg.ReqID, msg.ResType), ConsoleColor.Red, ConsoleColor.White, false);
                        break;
                }
            }
        }
     

       
        //private void HandleAllocationNewOrderRefused(AllocRes msg)
        //{
        //    OrdersManager.HandleAllocationNewOrderRefused(msg);
        //}
      
       
        // private void HandleAllocationUpdateOrCancelRefused(AllocRes msg)
        //{
        //    OrdersManager.HandleAllocationUpdateOrCancelRefused(msg);
        //}

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
