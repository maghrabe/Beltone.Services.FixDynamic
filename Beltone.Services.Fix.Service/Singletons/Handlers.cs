using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers;
using Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers;
using Beltone.Services.Fix.Utilities;
using System.Threading;
using Beltone.Services.Fix.Service.Handlers.ResponseMcsdMessagesHandlers;
using Beltone.Services.MCDR.Contract.Entities.ResMsgs;
using Beltone.Services.ProcessorsRouter.Interfaces;

namespace Beltone.Services.Fix.Service.Singletons
{
    public class RequestsProcessor : IMsgProcessor
    {
        private List<IRequestMessageHandler<IRequestMessage>> m_requestHandlers = null;

        public RequestsProcessor()
        {
            LoadRequestMessagesHandlers();
        }

        private void LoadRequestMessagesHandlers()
        {
            RequestMessagesHandlers handlers = (RequestMessagesHandlers)ConfigurationManager.GetSection("RequestMessagesHandlers");
            if (handlers == null || handlers.RequestMessagesHandlersList.Count == 0)
            {
                throw new ArgumentNullException("Messages Handlers Not Found");
            }
            m_requestHandlers = new List<IRequestMessageHandler<IRequestMessage>>();
            int count = 1;
            foreach (RequestMessageHandler handler in handlers.RequestMessagesHandlersList)
            {
                Type handlerTyp = Type.GetType(handler.Type, true, true);
                if (typeof(IRequestMessageHandler<IRequestMessage>).IsAssignableFrom(handlerTyp))
                {
                    IRequestMessageHandler<IRequestMessage> newHandlerInstance =
                        (IRequestMessageHandler<IRequestMessage>)Activator.CreateInstance(handlerTyp);
                    m_requestHandlers.Add(newHandlerInstance);
                    newHandlerInstance.Initialize();
                }
                else
                {
                    throw new Exception("Handler Not Defined");
                }
                count++;
            }
        }

        public void Process(object msg)
        {
            foreach (IRequestMessageHandler<IRequestMessage> handler in m_requestHandlers)
            {
                try
                {
                    if (handler.Handle((IRequestMessage)msg)) { break; }
                    //SystemLogger.WriteOnConsole(true, string.Format("Couldn't find handler for message type", msg.GetType()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error handling request message of type {0}, Error: {1}", msg.GetType(), ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }


        public void Initialize()
        {
        }
    }

    public class ResponsesProcessor : IMsgProcessor
    {

        private List<IResponseMessageHandler<QuickFix.Message>> m_responseHandlers = null;

        public ResponsesProcessor()
        {
            LoadResponseMessagesHandlers();
        }

        private void LoadResponseMessagesHandlers()
        {
            ResponseMessagesHandlers handlers = (ResponseMessagesHandlers)ConfigurationManager.GetSection("ResponseMessagesHandlers");
            if (handlers == null || handlers.ResponseMessagesHandlersList.Count == 0)
            {
                throw new ArgumentNullException("Messages Handlers Not Found");
            }
            m_responseHandlers = new List<IResponseMessageHandler<QuickFix.Message>>();
            int count = 1;
            foreach (ResponseMessageHandler handler in handlers.ResponseMessagesHandlersList)
            {
                Type handlerTyp = Type.GetType(handler.Type, true, true);
                if (typeof(IResponseMessageHandler<QuickFix.Message>).IsAssignableFrom(handlerTyp))
                {
                    IResponseMessageHandler<QuickFix.Message> newHandlerInstance =
                        (IResponseMessageHandler<QuickFix.Message>)Activator.CreateInstance(handlerTyp);
                    m_responseHandlers.Add(newHandlerInstance);
                    newHandlerInstance.Initialize(handler.MsgTypeValue);
                }
                else
                {
                    throw new Exception("Handler Not Defined");
                }
                count++;
            }
        }

        public void Process(object msg)
        {
            //RepSessions.ReplicateFixMessage((QuickFix.Message)msg);
            foreach (IResponseMessageHandler<QuickFix.Message> handler in m_responseHandlers)
            {
                try
                {
                    handler.Handle((QuickFix.Message)msg);
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error handling response message of type {0}, Error: {1}", msg.GetType(), ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }

        public void Initialize()
        {
        }
    }

    #region MCSD
    public class ResponsesProcessorMcsd : IMsgProcessor
    {

        private List<IResponseMcsdMessageHandler<AllocRes>> m_McsdresponseHandlers = null;

        public ResponsesProcessorMcsd()
        {
            LoadResponseMessagesHandlers();
        }

        private void LoadResponseMessagesHandlers()
        {
            McsdResponseMessagesHandlers handlers = (McsdResponseMessagesHandlers)ConfigurationManager.GetSection("McsdResponseMessagesHandlers");
            if (handlers == null || handlers.McsdResponseMessagesHandlersList.Count == 0)
            {
                throw new ArgumentNullException("MCSD Messages Handlers Not Found");
            }
            m_McsdresponseHandlers = new List<IResponseMcsdMessageHandler<AllocRes>>();
            //int count = 1;
            foreach (McsdResponseMessageHandler handler in handlers.McsdResponseMessagesHandlersList)
            {
                Type handlerTyp = Type.GetType(handler.Type, true, true);
                if (typeof(IResponseMcsdMessageHandler<AllocRes>).IsAssignableFrom(handlerTyp))
                {
                    IResponseMcsdMessageHandler<AllocRes> newHandlerInstance =
                        (IResponseMcsdMessageHandler<AllocRes>)Activator.CreateInstance(handlerTyp);
                    m_McsdresponseHandlers.Add(newHandlerInstance);
                    newHandlerInstance.Initialize();
                }
                else
                {
                    throw new Exception("Handler Not Defined");
                }
                //count++;
            }
        }

        public void Process(object msg)
        {
            //ReplicationSessionSubscribersList.ReplicateFixMessage((QuickFix.Message)msg);
            foreach (IResponseMcsdMessageHandler<AllocRes> handler in m_McsdresponseHandlers)
            {
                try
                {
                    handler.Handle((AllocRes)msg);
                }
                catch (Exception ex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, string.Format("Error handling mcsd response message of type {0}, Error: {1}", msg.GetType(), ex.ToString()), ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
        }

        public void Initialize()
        {
        }
    }
    #endregion
}