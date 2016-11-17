using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Messaging;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class RepSession
    {
        public Guid SubscriberKey { get; set; }
        public string QueuePath { get; set; }
        public MessageQueue SessionRemoteQueue { get; set; }
        public string[] SubscribedFixMsgsTypes { get; set; }
        public DateTime LastHeartBeatRequest { get; set; }
        public DateTime LastHeartBeatResponse { get; set; }
        public DateTime SubscriptionDateTime { get; set; }
    }
}
