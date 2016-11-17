using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class LogOnRequestHandler :  IRequestMessageHandler<IRequestMessage>
    {
        #region IRequestMessageHandler<IRequestMessage> Members

        public void Initialize()
        {
        }

        public bool Handle(IRequestMessage msg)
        {
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
