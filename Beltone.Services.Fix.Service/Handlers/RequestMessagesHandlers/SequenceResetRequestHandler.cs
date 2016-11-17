using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Provider;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    class SequenceResetRequestHandler : IRequestMessageHandler<IRequestMessage>
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
                if (typeof(SequenceResetRequest) == msgType)
                {
                    SequenceResetRequest reset = (SequenceResetRequest)msg;
                    if (reset.IsZeroReset)
                    {
                        //FixGatewayManager.ResetSequence();
                    }
                    else
                    {
                        //FixGatewayManager.ResetSequence(reset.SeqNo);
                    }
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
