using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Enums;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IReplicationRequest")]
    public class ReplicationSessionSubscriptionRequest : IReplicationRequest
    {
        [DataMember(IsRequired = true)]
        public FixMsgsReplicationDicretion MsgsDir { get; set; }
        [DataMember(IsRequired = true)]
        public string CallbackQueuePath { get; set; }
        [DataMember(IsRequired=true)]
        public string[] SubscribedFixMsgsTypes { get; set; }
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime RequestDateTime { get; set; }
    }
}
