using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Beltone.Services.Fix.Contract.Interfaces
{
    public interface IRequestMessage
    {
        Guid ClientKey { get; set; }
        Dictionary<string, object> OptionalParam { get; set; }
    }
}
