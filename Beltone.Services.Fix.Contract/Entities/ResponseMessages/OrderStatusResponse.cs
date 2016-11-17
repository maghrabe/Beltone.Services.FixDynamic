using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Enums;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_OrderStatusResponse : IResponseMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid ReqOrdID { get; set; }
        [DataMember]
        public bool IsExisted { get; set; }
        [DataMember]
        public int CurrQty { get; set; }
        [DataMember]
        public int TotExecQty { get; set; }
        [DataMember]
        public int LastExecQty { get; set; }
        [DataMember]
        public int RemQty { get; set; }
        [DataMember]
        public double CurrPrc { get; set; }
        [DataMember]
        public double ExecPrc { get; set; }
        [DataMember]
        public double AvgPrc { get; set; }
        [DataMember]
        public string OrdStatus { get; set; }
        [DataMember]
        public string OrdTyp { get; set; }
        [DataMember]
        public string TIF { get; set; }
        [DataMember]
        public bool IsActive { get; set; }
        [DataMember]
        public bool IsPending { get; set; }
        [DataMember]
        public bool IsExecuted { get; set; }
        [DataMember]
        public bool IsCompleted { get; set; }
        [DataMember]
        public string Note { get; set; }
        [DataMember]
        public DateTime PlacementTime { get; set; }
        [DataMember]
        public DateTime LastUpdate { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
