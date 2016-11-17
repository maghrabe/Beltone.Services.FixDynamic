using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Service.Singletons;
using System.Messaging;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.ProcessorsRouter.Entities;

namespace Beltone.Services.Fix.Service.OrdersService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class FixOrdSvc : IOrders
    {
        static Router _router;
        public FixOrdSvc()
        {
            int _requestsRouterProcessorsNum = int.Parse(SystemConfigurations.GetAppSetting("RequestsRouterProcessorsNum"));
            _router = new Router(typeof(RequestsProcessor), _requestsRouterProcessorsNum);
        }

        public void Handle(IRequestMessage msg)
        {
            //_callback.PushUpdates(new SubscriptionStatus() { ClientKey = _clientKey, IsSubscribed = false, Message = "You do not have any subscribed session before" });
            //SystemLogger.WriteOnConsole(true, string.Format("recieved {0} message from client {1} ", msg.GetType().ToString(), msg.ClientKey), ConsoleColor.Cyan, ConsoleColor.Black, false);
            
            if(msg.GetType() == typeof(NewSingleOrder))
            {
                _router.PushMessage(((NewSingleOrder)msg).RequesterOrderID, msg);
            }
            else if (msg.GetType() == typeof(CancelSingleOrder))
            {
                _router.PushMessage(((CancelSingleOrder)msg).RequesterOrderID, msg);
            }
            else if (msg.GetType() == typeof(ModifyCancelOrder))
            {
                _router.PushMessage(((ModifyCancelOrder)msg).RequesterOrderID, msg);
            }
            else
            {
                _router.PushMessage(msg);
            }
         
        }
    }
}
