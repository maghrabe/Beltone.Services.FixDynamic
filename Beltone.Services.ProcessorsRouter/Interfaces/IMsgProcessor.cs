using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.ProcessorsRouter.Interfaces
{
    public interface IMsgProcessor
    {
        void Initialize();
        void Process(object msg);
    }

}
