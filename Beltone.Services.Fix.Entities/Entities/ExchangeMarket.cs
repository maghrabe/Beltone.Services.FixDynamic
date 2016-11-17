﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class ExchangeMarket
    {
        public string ExchangeID { get; set; }
        public string MarketID { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public MarketGroup[] Groups { get; set; }
    }
}
