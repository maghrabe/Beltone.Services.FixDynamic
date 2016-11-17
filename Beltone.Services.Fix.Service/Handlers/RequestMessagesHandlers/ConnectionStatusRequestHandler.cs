using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    class ConnectionStatusRequestHandler : IRequestMessageHandler<IRequestMessage>
    {
        #region IRequestMessageHandler<IRequestMessage> Members

        public void Initialize()
        {
        }

        public bool Handle(IRequestMessage msg)
        {
            Type msgType = msg.GetType();
            if (typeof(IRequestMessage).IsAssignableFrom(msgType))
            {
                if (typeof(ConnectionStatusRequest) == msgType)
                {
                    ConnectionStatusRequest cs = (ConnectionStatusRequest)msg;
                    Sessions.Push( Sessions.GetUsername(cs.ClientKey), new IResponseMessage[] { new ConnectionStatusResponse(){ ClientKey = cs.ClientKey, IsConnected = true }});
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
