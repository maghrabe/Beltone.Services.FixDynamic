using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Contract.Entities.RequestMessages
{
    [DataContract(Namespace = "Beltone.IRequestMessage")]
    public class ConnectionStatusRequest : IRequestMessage
    {
        [DataMember]
        public Guid ClientKey { get; set; }
        [DataMember]
        public Dictionary<string, object> OptionalParam { get; set; }
    }
}
