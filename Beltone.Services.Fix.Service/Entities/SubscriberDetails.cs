using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;

namespace Beltone.Services.Fix.Service.Entities
{
    public class SessionInfo
    {
        public Guid SessionKey { get; set; }
        public bool IsOnline { get; set; }
        public LoginInfo LoginInfo { get; set; }
        public IFixAdminCallback Callback { get; set; }
        public MessageQueue Queue { get; set; }
        public string QueueMachine { get; set; }
        public string QueueName { get; set; }
        public string QueuePath { get; set; }
        public bool FlushUpdatesOffline { get; set; }
    }
}
