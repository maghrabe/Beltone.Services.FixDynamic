using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Enums;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IRequestMessage")]
    public class NewSingleOrder : IRequestMessage
    {
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public Guid RequesterOrderID { get; set; }
        [DataMember(IsRequired = true)]
        public int ClientID { get; set; }
        [DataMember(IsRequired = true)]
        public string SecurityID { get; set; }
        [DataMember(IsRequired = true)]
        public int Quantity { get; set; }
        [DataMember(IsRequired = true)]
        public double Price { get; set; }
        [DataMember(IsRequired = true)]
        public string CustodyID { get; set; }
        [DataMember(IsRequired = true)]
        public string OrderType { get; set; }
        [DataMember(IsRequired = true)]
        public string OrderSide { get; set; }
        [DataMember(IsRequired = true)]
        public string TimeInForce { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime DateTime { get; set; }
        [DataMember(IsRequired = true)]
        public string ExchangeID { get; set; }
        [DataMember(IsRequired = true)]
        public HandleInstruction HandleInst { get; set; }
        private DateTime _ExpirationDateTime = DateTime.Now;
        [DataMember]
        public DateTime ExpirationDateTime
        {
            get { return _ExpirationDateTime; }
            set { _ExpirationDateTime = value; }
        }
        [DataMember]
        public Dictionary<string, object> OptionalParam { get; set; }
    }
}
