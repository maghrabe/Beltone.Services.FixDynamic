﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class MarketGroup
    {
        public string GroupID { get; set; }
        public string ExchangeID { get; set; }
        public string MarketID { get; set; }
        public GroupSession[] Sessions { get; set; }
    }
}
