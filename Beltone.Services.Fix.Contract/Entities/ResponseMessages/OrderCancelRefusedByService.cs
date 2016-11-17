using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_OrderCancelRefusedByService : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public string OrderStatus { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
