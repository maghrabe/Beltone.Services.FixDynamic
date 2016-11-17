using Beltone.Services.MCDR.Contract.Entities.ResMsgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMcsdMessagesHandlers
{
    public interface IResponseMcsdMessageHandler<T> : IDisposable where T : AllocRes
    {
        void Initialize();
        void Handle(T msg);
    }
}


