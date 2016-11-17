using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities.ResponseMessages
{
    [DataContract(Namespace = "Beltone.IReplicationResponse")]
    public class ReplicatedFixMsg : IReplicationResponse
    {
        [DataMember(IsRequired = true)]
        public Guid ClientKey { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime ResponseDateTime { get; set; }
        [DataMember(IsRequired = true)]
        public string FixMessage { get; set; }
        [DataMember(IsRequired = true)]
        public string FixVersion { get; set; }
    }
}
