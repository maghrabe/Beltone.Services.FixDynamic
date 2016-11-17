using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class LoginInfo
    {
        public string Username { get; set; }
        public bool CanPlaceOrder { get; set; }
        public bool CanReplicate { get; set; }
    }
}
