using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Beltone.Services.Fix.Contract.Interfaces
{
    public interface IFromAdminMsg
    {
        object[] Data { get; set; }
    }
}
