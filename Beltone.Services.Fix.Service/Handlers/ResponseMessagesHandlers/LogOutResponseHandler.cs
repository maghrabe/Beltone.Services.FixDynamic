using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Provider;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public class LogOutResponseHandler : IResponseMessageHandler<QuickFix.Message>
    {

        private static int m_msgTypeTag;
        private static string m_MsgTypeTagValueToHandle;

        #region IRequestMessageHandler<IRequestMessage> Members

        public void Initialize(string msgTypeTagValueToHandle)
        {
            m_MsgTypeTagValueToHandle = msgTypeTagValueToHandle;
            m_msgTypeTag = int.Parse(SystemConfigurations.GetAppSetting("MsgTypeTag"));
        }

        public void Handle(QuickFix.Message msg)
        {
            // check message type tag, if execution report then push execution report update
            QuickFix.MsgType msgType = new QuickFix.MsgType();
            string msgTypeString = msg.getHeader().getField(m_msgTypeTag);
            if (msgTypeString == m_MsgTypeTagValueToHandle)
            {
                //SystemLogger.WriteOnConsole(true, string.Format("new message recieved, Type: {0}, Message: '{1}'", msgTypeString, msg.ToXML()), ConsoleColor.Cyan, ConsoleColor.Black, false);
                
                //string reason = string.Empty;
                //if (msg.isSetField(58))
                //{
                //    reason = msg.getField(58);
                //}
                //int sequence = int.Parse(msg.getHeader().getField(34));
                //if (reason.ToLower().Contains("expected sequence") && reason.ToLower().Contains("upper"))
                //{
                //}
                //else if (reason.ToLower().Contains("expected sequence") && reason.ToLower().Contains("lower"))
                //{
                //}

                ////{8=FIX.4.29=10735=534=1149=CRDTEST52=20110127-08:07:04.64856=BELTONE58=MsgSeqNum too low, expecting 9 but received 110=251}
                //// create IResponseMessage
            }
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
