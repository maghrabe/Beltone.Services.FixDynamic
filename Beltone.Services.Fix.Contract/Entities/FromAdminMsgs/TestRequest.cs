using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.FromAdminMsgs
{
    [DataContract(Namespace = "Beltone.FromAdminMsgs")]
    public class FixAdmin_TestRequest : IFromAdminMsg
    {
        [DataMember(IsRequired = true)]
        public string TestKey { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
