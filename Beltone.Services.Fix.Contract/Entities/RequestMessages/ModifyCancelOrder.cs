using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IRequestMessage")]
    public class ModifyCancelOrder : IRequestMessage
    {
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public Guid RequesterOrderID { get; set; }
        [DataMember(IsRequired = true)]
        public double Price { get; set; }
        [DataMember(IsRequired = true)]
        public int Quantity { get; set; }
        [DataMember(IsRequired = true)]
        public string OrderType { get; set; }
        [DataMember(IsRequired = true)]
        public string TimeInForce { get; set; }
        [DataMember]
        public Dictionary<string, object> OptionalParam { get; set; }
    }
}
