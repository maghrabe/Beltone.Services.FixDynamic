using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class GroupSession
    {
        public string GroupID { get; set; }
        public string ExchangeID { get; set; }
        public string MarketID { get; set; }
        public string WorkingDays { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
    }
}
