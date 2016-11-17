using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Beltone.Services.Fix.Contract.Entities;

namespace Beltone.Services.Fix.Contract.Interfaces
{
    public interface IResponseMessage
    {
        Guid ClientKey { get; set; }
        OptionalFields OptionalFields { get; set; }
    }
}
