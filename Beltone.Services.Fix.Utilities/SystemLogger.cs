using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace Beltone.Services.Fix.Utilities
{
    public static class SystemLogger
    {

        private delegate void ConsoleLoggerDelegate(bool newLine, string txt, ConsoleColor fontColor, ConsoleColor backgroundColor, bool logAsError);
        private static ConsoleLoggerDelegate m_ConsoleLoggerDelegate;
        private delegate void EventLoggerDelegate(string msg);
        private static EventLoggerDelegate m_EventLoggerDelegate;
        private delegate void ErrorLoggerDelegate(string msg);
        private static ErrorLoggerDelegate m_ErrorLoggerDelegate;
        private static NLog.Logger m_Filelogger;
        private static NLog.Logger m_Consolelogger;

        public static void Initialize()
        {
            m_Consolelogger = LogManager.GetLogger("ConsoleLogger");
            m_Filelogger = LogManager.GetLogger("TempLogger");
            m_EventLoggerDelegate = new EventLoggerDelegate(LogEventInternal);
            m_ErrorLoggerDelegate = new ErrorLoggerDelegate(LogErrorInternal);
            m_ConsoleLoggerDelegate = new ConsoleLoggerDelegate(LogProcess);
        }

        public static void LogErrorAsync(string error)
        {
            m_ErrorLoggerDelegate.BeginInvoke(error, new AsyncCallback(ErrorLogProcess), null);
        }

        private static void LogErrorInternal(string error)
        {
            try
            {
                m_Filelogger.Warn(" Error | " + error);
                //m_Consolelogger.Trace(error);
            }
            catch (Exception ex)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Cannot log error, Error: {0} ", ex.Message);
                //Console.ResetColor();
            }
        }

        public static void LogEventAsync(string _event)
        {
            m_EventLoggerDelegate.BeginInvoke(_event, new AsyncCallback(EventLogProcess), null);

        }

        private static void EventLogProcess(IAsyncResult res)
        {
            m_EventLoggerDelegate.EndInvoke(res);
        }

        private static void ErrorLogProcess(IAsyncResult res)
        {
            m_ErrorLoggerDelegate.EndInvoke(res);
        }

        private static void LogEventInternal(string _event)
        {
            try
            {
                m_Filelogger.Warn(" Event | " + _event);
                //m_Consolelogger.Trace(_event);
            }
            catch (Exception ex)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Cannot log Event, Error: {0} ", ex.Message);
                //Console.ResetColor();
            }
        }

        public static void WriteOnConsoleAsync(bool newLine, string txt, ConsoleColor fontColor, ConsoleColor backgroundColor, bool logAsError)
        {
            //LogProcess(newLine, txt, fontColor, backgroundColor, logAsError);
            m_ConsoleLoggerDelegate.BeginInvoke(newLine, txt, fontColor, backgroundColor, logAsError, new AsyncCallback(OnLogProcess), null);
        }

        private static void OnLogProcess(IAsyncResult res)
        {
            m_ConsoleLoggerDelegate.EndInvoke(res);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private static void LogProcess(bool newLine, string txt, ConsoleColor fontColor, ConsoleColor backgroundColor, bool logAsError)
        {
            try
            {
                Console.ForegroundColor = fontColor;
                Console.BackgroundColor = backgroundColor;
                if (newLine)
                {
                    Console.WriteLine(txt);
                }
                else
                {
                    Console.Write(txt);
                }
                if (logAsError)
                {
                    LogErrorAsync(txt);
                }
                else
                {
                    LogEventAsync(txt);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("logger error: " + ex.ToString());
            }
        }

    }
}
