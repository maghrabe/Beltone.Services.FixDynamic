using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_OrderRejectionResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public string OrderStatus { get; set; }
        [DataMember]
        public string RejectionReason { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }

    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Mcsd_CancelRejectionResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public string OrderStatus { get; set; }
        [DataMember]
        public string RejectionReason { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }

    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Mcsd_AllocTimeoutResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public string OrderStatus { get; set; }
        [DataMember]
        public string RejectionReason { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
