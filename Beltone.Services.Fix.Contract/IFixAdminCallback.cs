using Beltone.Services.Fix.Contract.Entities.FromAdminMsgs;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Beltone.Services.Fix.Contract
{
    [ServiceKnownType(typeof(FixAdmin_SessionUp))]
    [ServiceKnownType(typeof(FixAdmin_MarketStatus))]
    [ServiceKnownType(typeof(FixAdminMsg))]
    public interface IFixAdminCallback
    {
        [OperationContract(IsOneWay = true)]
        void PushAdminMsg(IFromAdminMsg[] msgs);
    }
}
