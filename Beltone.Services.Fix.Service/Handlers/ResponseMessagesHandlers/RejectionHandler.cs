using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Service.Singletons;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public class RejectionHandler : IResponseMessageHandler<QuickFix.Message>
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
                Counters.IncrementCounter(CountersConstants.RejectionMsgs);
                string txt = string.Empty;
                if (msg.isSetField(58)) { txt = msg.getField(58); }
                
                try
                {
                    if (msg.isSetField(373))
                    {
                        string rejectReasonValue = string.Empty;
                        rejectReasonValue = msg.getField(373);
                        txt += ", " + Lookups.GetSessionRejectReason(rejectReasonValue).MessageEn;
                    }
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error getting reason of rejected message: {0}", ex.Message), ConsoleColor.Red, ConsoleColor.Black, false);
                }
                // create IResponseMessage
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Order Rejected, Details {0} ,  Reason: {1} ", msg.ToXML(),txt), ConsoleColor.Red, ConsoleColor.White, false);
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
