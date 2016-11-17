using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_OrderRestatedResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid ReqOrdID { get; set; }
        [DataMember]
        public string OrdStatus { get; set; }
        [DataMember]
        public double CurrPrc { get; set; }
        [DataMember]
        public double LastExecPrc { get; set; }
        [DataMember]
        public double AvgPrc { get; set; }
        [DataMember]
        public int CurrQty { get; set; }
        [DataMember]
        public int RemQty { get; set; }
        [DataMember]
        public int ExecQty { get; set; }
        [DataMember]
        public int LastExecQty { get; set; }
        [DataMember]
        public string Msg { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
