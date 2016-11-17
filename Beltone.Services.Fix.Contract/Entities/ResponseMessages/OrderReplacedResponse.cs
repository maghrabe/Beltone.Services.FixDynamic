using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_OrderReplacedResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid ReqOrdID { get; set; }
        [DataMember]
        public int Qty { get; set; }
        [DataMember]
        public int ExecQty { get; set; }
        [DataMember]
        public int RemQty { get; set; }
        [DataMember]
        public double Prc { get; set; }
        [DataMember]
        public string OrdTyp { get; set; }
        [DataMember]
        public string OrdStatus { get; set; }
        [DataMember]
        public string Side { get; set; }
        [DataMember]
        public string TIF { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
