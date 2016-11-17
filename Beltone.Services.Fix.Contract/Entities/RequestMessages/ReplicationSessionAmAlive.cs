using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IReplicationRequest")]
    public class ReplicationSessionAmAlive : IReplicationRequest
    {
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime RequestDateTime { get; set; }
    }
}
