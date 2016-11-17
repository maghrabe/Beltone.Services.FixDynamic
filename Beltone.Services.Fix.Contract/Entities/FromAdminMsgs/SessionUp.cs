using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.FromAdminMsgs
{
    [DataContract(Namespace = "Beltone.IFromAdminMsg")]
    public class FixAdmin_SessionUp : IFromAdminMsg
    {
        [DataMember]
        public Guid SessionKey { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
