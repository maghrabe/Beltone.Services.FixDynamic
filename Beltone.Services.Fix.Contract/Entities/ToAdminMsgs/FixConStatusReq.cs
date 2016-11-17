using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.ToAdminMsgs
{
    [DataContract(Namespace = "Beltone.IToAdminMsg")]
    public class FixAdmin_FixConStatusReq : IToAdminMsg
    {
        [DataMember]
        public object[] Data { get; set; }
    }
}
