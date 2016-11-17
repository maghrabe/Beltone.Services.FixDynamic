using Beltone.Services.Fix.Contract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Entities.ToAdminMsgs
{
    [DataContract(Namespace = "Beltone.ToAdminMsgs")]
    public class ResupReq : IToAdminMsg
    {
        [DataMember]
        public Guid SessionKey { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public bool NewQueue { get; set; }
        [DataMember]
        public string QueueName { get; set; }
        [DataMember]
        public string QueueIP { get; set; }
        [DataMember]
        public bool FlushUpdatesOffline { get; set; }
        [DataMember]
        public object[] Data { get; set; }
    }
}
