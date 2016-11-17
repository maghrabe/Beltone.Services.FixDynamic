using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IResponseMessage")]
    public class Fix_ExecutionReport : IResponseMessage
    {
        [DataMember] 
        public Guid ClientKey { get; set; }
        [DataMember]
        public Guid RequesterOrderID { get; set; }
        [DataMember]
        public int CurrentQty { get; set; }
        [DataMember]
        public int TotalExecutedQuantity { get; set; }
        [DataMember]
        public int TradeExecutedQuantity { get; set; }
        [DataMember]
        public int RemainingQuantity { get; set; }
        [DataMember]
        public double CurrPrice { get; set; }
        [DataMember]
        public double ExecPrice { get; set; }
        [DataMember]
        public double AvgPrice { get; set; }
        [DataMember]
        public string OrderStatus { get; set; }
        [DataMember]
        public bool IsActive { get; set; }
        [DataMember]
        public bool IsExecuted { get; set; }
        [DataMember]
        public bool IsCompleted { get; set; }
        [DataMember]
        public OptionalFields OptionalFields { get; set; }
    }
}
