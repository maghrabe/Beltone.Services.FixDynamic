using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class LookupItem : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public string ConstName { get; set; }
        [DataMember]
        public string CodeValue { get; set; }
        [DataMember]
        public string FixValue { get; set; }
        [DataMember]
        public string MessageEn { get; set; }
        [DataMember]
        public string MessageAr { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
