using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IReplicationResponse")]
    public class ReplicationSessionAreYouAlive : IReplicationResponse
    {
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime ResponseDateTime { get; set; }
    }
}
