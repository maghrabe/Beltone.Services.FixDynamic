using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;

namespace Beltone.Services.Fix.Contract
{
    public interface IOrdersCallback
    {
        [ServiceKnownType(typeof(Fix_OrderCancelRejected))]
        [ServiceKnownType(typeof(Fix_OrderReplaceRejected))]
        [ServiceKnownType(typeof(Fix_OrderRejectionResponse))]
        [ServiceKnownType(typeof(Fix_ExecutionReport))]
        [ServiceKnownType(typeof(Fix_OrderRefusedByService))]
        [ServiceKnownType(typeof(LogOnResponse))]
        [ServiceKnownType(typeof(LogOutResponse))]
        [ServiceKnownType(typeof(Fix_OrderReplaceCancelReject))]
        [ServiceKnownType(typeof(Fix_BusinessMessageReject))]
        [ServiceKnownType(typeof(Fix_OrderAcceptedResponse))]
        [ServiceKnownType(typeof(AreYouAlive))]
        [ServiceKnownType(typeof(Fix_OrderRefusedByService))]
        [ServiceKnownType(typeof(Fix_OrderReplacedResponse))]
        [ServiceKnownType(typeof(Fix_OrderStatusResponse))]
        [ServiceKnownType(typeof(Fix_OrderSuspensionResponse))]
        [ServiceKnownType(typeof(Fix_OrderCanceledResponse))]
        [ServiceKnownType(typeof(Fix_OrderExpiredResponse))]
        [ServiceKnownType(typeof(Fix_OrderReplaceRefusedByService))]
        [ServiceKnownType(typeof(Fix_OrderCancelRefusedByService))]
        [ServiceKnownType(typeof(Fix_PendingReplaceResponse))]
        [ServiceKnownType(typeof(Fix_PendingNewResponse))]
        [ServiceKnownType(typeof(Fix_PendingCancelResponse))]
        [ServiceKnownType(typeof(SubscriptionStatus))]
        [ServiceKnownType(typeof(SubscriberInitializationInfo))]
        [ServiceKnownType(typeof(Fix_SessionConnected))]
        [ServiceKnownType(typeof(Fix_SessionDisconnected))]
        [OperationContract(IsOneWay = true)]
        void PushUpdates(object msg);
    }
}
