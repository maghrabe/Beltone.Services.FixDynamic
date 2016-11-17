using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public interface IResponseMessageHandler<T> : IDisposable where T : QuickFix.Message
    {
        void Initialize(string msgTypeTagValueToHandle);
        void Handle(T msg);
    }
}
