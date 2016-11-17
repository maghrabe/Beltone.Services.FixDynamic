using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.ToAdminMsgs
{
    [DataContract(Namespace = "Beltone.ToAdminMsgs")]
    public class FixAdmin_TestResponse :  IToAdminMsg
    {
        [DataMember(IsRequired=true)]
        public string TestKey { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
