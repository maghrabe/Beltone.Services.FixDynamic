using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class DayOff
    {
        public string ExchangeID { get; set; }
        public string OccasionEn { get; set; }
        public string OccasionAr { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }
}
