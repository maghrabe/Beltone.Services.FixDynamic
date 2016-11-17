using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;

namespace Beltone.Services.Fix.Contract
{
    //[ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IOrdersCallback))]
    [ServiceKnownType(typeof(NewSingleOrder))]
    [ServiceKnownType(typeof(CancelSingleOrder))]
    [ServiceKnownType(typeof(ModifyCancelOrder))]
    [ServiceKnownType(typeof(DontKnowTrade))]
    [ServiceKnownType(typeof(OrderStatusRequest))]
    [ServiceKnownType(typeof(Beltone.Services.Fix.Contract.Enums.HandleInstruction))]
    [ServiceContract]
    public interface IOrders
    {
        [OperationContract(IsOneWay = true)]
        void Handle(IRequestMessage msg);
    }
}
