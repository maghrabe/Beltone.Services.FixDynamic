using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Entities.Entities
{
    public class Stock
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Reuter { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public string ExchangeID { get; set; }
        public string MarketID { get; set; }
        public string CurrencyCode { get; set; }
        public string GroupID { get; set; }
    }
}
