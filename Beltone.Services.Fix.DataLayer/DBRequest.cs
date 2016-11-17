using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.DataLayer
{
    public class DBRequest
    {
        public int RequestID { get; set; }
        public string QueryString { get; set; }
        public string ConnectionString { get; set; }
    }
}
