using Beltone.Services.ProcessorsRouter.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.ProcessorsRouter.Interfaces
{
    public interface IEventLogger
    {
        void Initialzie();
        void LogEvent(LOG_TYP logTyp,string msg, params string[] args);
    }
}
