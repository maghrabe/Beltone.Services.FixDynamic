using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Contract.Interfaces
{
    public interface IReplicationRequest
    {
        Guid ClientKey { get; set; }
        DateTime RequestDateTime { get; set; }
    }
}
