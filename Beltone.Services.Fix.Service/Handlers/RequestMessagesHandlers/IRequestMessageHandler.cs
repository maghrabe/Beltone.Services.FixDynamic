using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public interface IRequestMessageHandler<T> : IDisposable where T : IRequestMessage
    {
        void Initialize();
        bool Handle(T msg);
    }
}
