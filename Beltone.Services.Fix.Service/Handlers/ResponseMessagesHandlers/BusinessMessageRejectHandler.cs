using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Utilities;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public class BusinessMessageRejectHandler : IResponseMessageHandler<QuickFix.Message>
    {
        private static int m_msgTypeTag;
        private static string m_MsgTypeTagValueToHandle;

        #region IResponseMessageHandler<IResponseMessage> Members

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
                Counters.IncrementCounter(CountersConstants.BusinessRejectRspMsgs);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("new message recieved, Type: {0}, Message: '{1}'", msgTypeString, msg.ToXML()), ConsoleColor.Cyan, ConsoleColor.Black, false);
                // create IResponseMessage
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
