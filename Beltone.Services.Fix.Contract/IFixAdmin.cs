using Beltone.Services.Fix.Contract.Entities.FromAdminMsgs;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Contract.Entities.ToAdminMsgs;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Beltone.Services.Fix.Contract
{
    [ServiceKnownType(typeof(subReq))]
    [ServiceKnownType(typeof(FixAdmin_TestResponse))]
    [ServiceContract(Namespace = "Beltone.Fix.Contract", SessionMode = SessionMode.Required, CallbackContract = typeof(IFixAdminCallback))]
    public interface IFixAdmin
    {
        [OperationContract]
        FixAdminMsg Subscribe(subReq login);
        [OperationContract]
        FixAdminMsg Resubscribe(ResupReq resub);
        [OperationContract]
        void Unsubscribe();
        [OperationContract]
        FixAdminMsg HandleMsg(IToAdminMsg msg);
        [OperationContract]
        void Ping();

    }
}
