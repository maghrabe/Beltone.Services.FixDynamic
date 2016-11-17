using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.FromAdminMsgs
{
    [DataContract(Namespace = "Beltone.FromAdminMsgs")]
    public class FixAdminMsg : IFromAdminMsg
    {
        [DataMember]
        public string Code { get; set; }
        [DataMember]
        public string Note { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
