using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IRequestMessage")]
    public class OrderStatusRequest : IRequestMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public Dictionary<string, object> OptionalParam { get; set; }
    }
}
