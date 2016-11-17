using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Contract.Interfaces
{
    public interface IReplicationResponse
    {
        Guid ClientKey { get; set; }
        DateTime ResponseDateTime { get; set; }
    }
}
