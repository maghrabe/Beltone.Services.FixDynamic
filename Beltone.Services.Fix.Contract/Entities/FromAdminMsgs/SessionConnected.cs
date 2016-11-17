using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities.FromAdminMsgs
{
    [DataContract(Namespace = "Beltone.IFromAdminMsg")]
    public class FixAdmin_MarketStatus : IFromAdminMsg
    {
        [DataMember]
        public bool IsConnected { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
