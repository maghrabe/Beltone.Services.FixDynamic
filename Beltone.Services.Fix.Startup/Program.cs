using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Service.Singletons;
using Beltone.Services.Fix.Utilities;
using Beltone.Services.Fix.Provider;
using System.ServiceModel;
using Beltone.Services.Fix.Service.OrdersService;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Delegates;
using Beltone.Services.Fix.Entities;
using Beltone.Services.Fix.Contract.Interfaces;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using System.Messaging;
using Beltone.Services.Fix.Contract.Entities.FromAdminMsgs;
using System.Diagnostics;
using System.Security.Principal;
using Beltone.Services.Fix.Service.GUI;
using Beltone.Services.Fix.Service;
using System.ServiceProcess;
using System.Configuration.Install;
using System.ComponentModel;
using System.IO;
using Beltone.Services.Fix.MCSD;
using Beltone.Services.ProcessorsRouter.Entities;
//using Beltone.Services.Fix.MCSD;

namespace Beltone.Services.Fix.Startup
{
    class Program : ServiceBase
    {
        internal static ServiceHost OrdSvcHost = null;
        internal static ServiceHost AdminSvcHost = null;
        private static FixSessionStatusChangedDelegate m_FixSessionStatusChangedDelegate;
        private static bool _resetSequenceNumber = false;

        public Program()
        {
            ServiceName = "Fix Orders Service";
        }

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("Arg[{0}] = [{1}]", i, args[i]);

                string param = args[i];

                if (param.ToLower() == "resetseq")
                {
                    _resetSequenceNumber = true;
                }
               
            }
            _resetSequenceNumber = true;
            
            //Console.Title = "Fix Orders Service (Fix Client)";
            SystemLogger.Initialize();
            SystemConfigurations.Initialize();
            NotificationSender.Initialize();

            Program prog = new Program();
            if (Environment.UserInteractive)
            {
                prog.OnStart(args);
                while (true)
                    Console.ReadKey();
            }
            else
            {
                ServiceBase.Run(prog);
            }
        }

        protected override void OnStart(string[] args)
        {
            OnStartInternally(args);
        }
        
        protected void OnStartInternally(object arguments)
        {

            string[] args = (string[]) arguments;

            try
            {

                /*if (!IsAdministrator())
                {
                    // Restart program and run as admin
                    var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                    startInfo.Verb = "runas";
                    System.Diagnostics.Process.Start(startInfo);
                    Environment.Exit(0);
                    return;
                }*/



                // check multiple processes
                //commented by khazbak to avoid requirement to start in admin mode
                /*
                if (bool.Parse(SystemConfigurations.GetAppSetting("CheckMultipleInstances")))
                {
                    string currProcName = Process.GetCurrentProcess().ProcessName;
                    int currProcID = Process.GetCurrentProcess().Id;
                    Process[] processes = Process.GetProcessesByName(currProcName);
                    if (processes.Length > 1)
                    {
                        foreach (Process p in processes)
                        {
                            if (p.Id != currProcID)
                            {
                                int id = p.Id;
                                p.Kill();
                                SystemLogger.WriteOnConsoleAsync(true, "Process has been killed ID: " + id, ConsoleColor.Gray, ConsoleColor.White, false);
                            }
                        }
                    }
                }*/

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                SystemLogger.LogEventAsync("Starting FIX Service...");

                try
                {
                    if (bool.Parse(SystemConfigurations.GetAppSetting("ReinitializeCounters")))
                    {
                        Counters.ReInitialize();
                    }
                    else
                    {
                        Counters.Initialize();
                    }
                }
                catch (Exception ex)
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Error initializing counters, Error: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
                }


                DatabaseMethods db = new DatabaseMethods();
                if (!db.IsTodaySequenceReset()) // reset the counters only if the service starts for first time today
                {
                    Counters.ResetCounters();
                }
                // Check queue existance
                if (bool.Parse(SystemConfigurations.GetAppSetting("CheckQueueExistance")))
                {
                    CheckAndSetPermissionsToQueues();
                }
                FixExchangesInfo.Initialize();
                Currencies.Initialize();
                StocksDefinitions.Initialize();
                Lookups.Initialize();
                string seqFilePath = SystemConfigurations.GetAppSetting("SequenceFilePath");
                if (!db.IsTodaySequenceReset())
                {
                    SystemLogger.LogEventAsync("Resetting FIX sequence..");
                    db.UpdateTodaySequenceReset();
                    SystemLogger.LogEventAsync("Sequence reset successfully");
                    //try
                    //{
                    //    System.IO.File.Delete(seqFilePath);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    //    SystemLogger.WriteOnConsoleAsync(true, "Deleting Sequence File Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
                    //}
                }


                Router responsesRouter = new Router(typeof(ResponsesProcessor), int.Parse(SystemConfigurations.GetAppSetting("ResponsesRouterProcessorsNum")));
                
                m_FixSessionStatusChangedDelegate = new FixSessionStatusChangedDelegate(OnFixStatusChange);


                bool AllowMcsdAllocation = Convert.ToBoolean(SystemConfigurations.GetAppSetting("AllowMcsdAllocation"));  // maghrabi

                if (AllowMcsdAllocation) //MCSD
                {
                    Router mcsdResponsesRouter = new Router(typeof(ResponsesProcessorMcsd), int.Parse(SystemConfigurations.GetAppSetting("McsdResponsesRouterProcessorsNum")));
                    McsdGatwayManager.Initialize(mcsdResponsesRouter); // maghrabi
                    McsdGatwayManager.LoginToMCSD(); // maghrabi

                }

                string fixClientSettings = Environment.CurrentDirectory + @"\" + SystemConfigurations.GetAppSetting("FixClientSettingsFile");
                string fixServerSettings = Environment.CurrentDirectory + @"\" + SystemConfigurations.GetAppSetting("FixServerSettingsFile");
                MarketFixClient.Initialize(fixClientSettings, responsesRouter, m_FixSessionStatusChangedDelegate);
                SvcFixServer.InitializeServer(fixServerSettings);
                OrdersManager.Initialize();
                Sessions.Initialize();
                //RepSessions.Initialize();
                InitializeService();

                if (_resetSequenceNumber)
                {
                  //  MarketFixClient.ResetSequence();
                }

                MarketFixClient.Logon();

              


                SystemLogger.WriteOnConsoleAsync(true, "Awaiting for Fix server response ...", ConsoleColor.Yellow, ConsoleColor.Black,false);
                System.Windows.Forms.Application.Run(new frmMonitor()); //maghrabi
                //while (true) { Console.ReadKey(); }
                //StopService();
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, "Main Error: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);

                try
                {
                    string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                    NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "Fix Service Down", "Fix Service Down",
                        string.Format("Service startup error state on machine {0} at {1}, Error : {2}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString(), ex.ToString()), null);
                }
                catch (Exception inex)
                {
                    Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                    SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + inex.Message, ConsoleColor.Red, ConsoleColor.Black, false);
                }

                //Console.ReadKey();
            }
        }

        protected override void OnStop()
        {
            if (AdminSvcHost != null)
            {
                AdminSvcHost.Close();
                AdminSvcHost = null;
            }
            if (OrdSvcHost != null)
            {
                OrdSvcHost.Close();
                OrdSvcHost = null;
            }
        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void StopService()
        {
            if (OrdSvcHost != null && OrdSvcHost.State != CommunicationState.Closed)
                OrdSvcHost.Close();
        }

        private static void InitializeService()
        {

            AdminSvcHost = new ServiceHost(typeof(FixAdminSvc));
            AdminSvcHost.Closed += new EventHandler(shAdmin_Closed);
            AdminSvcHost.Faulted += new EventHandler(shAdmin_Faulted);
            AdminSvcHost.Open();
            SystemLogger.WriteOnConsoleAsync(true, "Admin Service started up successfully!", ConsoleColor.Green, ConsoleColor.Black, false);

            OrdSvcHost = new ServiceHost(typeof(FixOrdSvc));
            OrdSvcHost.Opened += new EventHandler(sh_Opened);
            OrdSvcHost.Closed += new EventHandler(sh_Closed);
            OrdSvcHost.Faulted += new EventHandler(sh_Faulted);
            OrdSvcHost.UnknownMessageReceived += new EventHandler<UnknownMessageReceivedEventArgs>(sh_UnknownMessageReceived);
            OrdSvcHost.Open();
            SystemLogger.WriteOnConsoleAsync(true, "Orders Service started up successfully!", ConsoleColor.Green, ConsoleColor.Black, false);
        }

        static void shAdmin_Faulted(object sender, EventArgs e)
        {
            try
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "ETF Service Faulted", "ETF Service Faulted",
                    string.Format("Fix Admin Service in faulted state on machine {0} at {1}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString()), null);
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        static void shAdmin_Closed(object sender, EventArgs e)
        {
            try
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "ETF Service Closed", "ETF Service Closed",
                    string.Format("Fix Admin Service has been closed on machine {0} at {1}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString()), null);
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        private static void OnFixStatusChange(FixSessionStatus fixSessionStatus)
        {
            if (fixSessionStatus == FixSessionStatus.Connected)
            {
                Sessions.BroadcastAdminMsg(new IFromAdminMsg[] { new FixAdmin_MarketStatus { IsConnected = true } });
            }
            else if (fixSessionStatus == FixSessionStatus.Disconnected)
            {
                Sessions.BroadcastAdminMsg(new IFromAdminMsg[] { new FixAdmin_MarketStatus { IsConnected = false } });
            }
        }

        static void sh_Opened(object sender, EventArgs e)
        {
            try
            {
                string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                if (!NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "Fix Orders Service Startup", "Fix Orders Service Startup",
                    string.Format("Fix Orders Service has been started up successfully on machine {0} at {1}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString()), null))
                {
                    SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error", ConsoleColor.Red, ConsoleColor.Black, true);
                }
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        static void sh_UnknownMessageReceived(object sender, UnknownMessageReceivedEventArgs e)
        {

        }

        static void sh_Faulted(object sender, EventArgs e)
        {
            try
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "Fix Orders Service Faulted", "Fix Orders Service Faulted",
                    string.Format("Fix Orders Service in faulted state on machine {0} at {1}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString()), null);
            }
            catch (Exception ex)
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        static void sh_Closed(object sender, EventArgs e)
        {
            try
            {
                Counters.IncrementCounter(CountersConstants.ExceptionMessages);
                string[] cc = SystemConfigurations.GetAppSetting("SupportMailCC").Split(',');
                NotificationSender.Send(true, true, false, SystemConfigurations.GetAppSetting("SupportMailFrom"), SystemConfigurations.GetAppSetting("SupportMailTo"), cc, "Fix Orders Service Closed", "Fix Orders Service Closed",
                    string.Format("Fix Orders Service has been closed on machine {0} at {1}", SystemConfigurations.GetMachineIP(), DateTime.Now.ToString()), null);
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "Sending Mail Error: " + ex.Message, ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        private static void CheckAndSetPermissionsToQueues()
        {
            SystemLogger.WriteOnConsoleAsync(true, string.Format("Checking Service Queue Existance: "), ConsoleColor.Yellow, ConsoleColor.Black, false);
            string RecievedMessagesQueueName = SystemConfigurations.GetAppSetting("RecievedMessagesQueueName");
            bool IsTransactionalQueue = bool.Parse(SystemConfigurations.GetAppSetting("IsTransactionalQueue"));
            if (!MessageQueue.Exists(RecievedMessagesQueueName))
            {
                MessageQueue queue = MessageQueue.Create(RecievedMessagesQueueName, IsTransactionalQueue);
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Done, Queue {0} has been created successfully !", queue.Path), ConsoleColor.Green, ConsoleColor.Black, false);
            }
            else
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Done ,Queue Already Existed!"), ConsoleColor.Green, ConsoleColor.Black, false);
            }


            SystemLogger.WriteOnConsoleAsync(true, string.Format("Setting Permission to queue: "), ConsoleColor.Yellow, ConsoleColor.Black, false);
            MessageQueue m = new MessageQueue(RecievedMessagesQueueName);
            m.SetPermissions(SystemConfigurations.GetAppSetting("FixQueueAuthenticatedUser"), MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            SystemLogger.WriteOnConsoleAsync(true, string.Format("Done"), ConsoleColor.Green, ConsoleColor.Black, false);


        }

    }


    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            service = new ServiceInstaller();
            service.ServiceName = "Fix Orders Service";
            Installers.Add(process);
            Installers.Add(service);
        }
    }



}
