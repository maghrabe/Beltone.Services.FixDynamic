using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.DataLayer
{
    public class DBResponse
    {
        public int RequestID { get; set; }
        public bool IsSucceeded { get; set; }
        public int AffectedRows { get; set; }
        public string ErrorMsg { get; set; }
    }
}
