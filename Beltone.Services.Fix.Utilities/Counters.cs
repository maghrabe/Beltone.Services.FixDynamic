using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Beltone.Services.Fix.Utilities
{
    public class CountersConstants
    {
        public const string FixSvcCategory = "Beltone.Services.Fix";
        public const string FixSvcCategoryHelp = "Beltone Fix Service";
        public const string FixSvcCategoryInstance = "Beltone.Services.Fix.Service";
        
        
        public const string ClientsNewOrderReqs = "Beltone.Services.Fix.ClientsNewOrderReqs";
        public const string ClientsNewOrderReqsHelp = "No. of new orders requests by clients";
        public const string ClientsCancelOrderReqs = "Beltone.Services.Fix.ClientsCancelOrderReqs";
        public const string ClientsCancelOrderReqsHelp = "No. of cancellation requests by clients";
        public const string ClientsReplaceOrderReqs = "Beltone.Services.Fix.ClientsReplaceOrderReqs";
        public const string ClientsReplaceOrderReqsHelp = "No. of replacement requests by clients";


        public const string RcvExecRspMsgs = "Beltone.Services.Fix.RcvExecRspMsgs";
        public const string RcvExecRspMsgsHelp = "No. of response execution messages"; 
        public const string NewExecRspMsgs = "Beltone.Services.Fix.NewExecRspMsgs";
        public const string NewExecRspMsgsHelp = "No. of accepted orders by bourse"; 
        public const string SuspendedExecRspMsgs = "Beltone.Services.Fix.SuspendedExecRspMsgs";    
        public const string SuspendedExecRspMsgsHelp = "No. of suspended orders by bourse";
        public const string FilledExecRspMsgs = "Beltone.Services.Fix.FilledExecRspMsgs";
        public const string FilledExecRspMsgsHelp = "No. of filled orders.";
        public const string PartialFilledExecRspMsgs = "Beltone.Services.Fix.PartialFilledExecRspMsgs";
        public const string PartialFilledExecRspMsgsHelp = "No. of partially filled orders.";
        public const string ReplacedExecRspMsgs = "Beltone.Services.Fix.ReplacedExecRspMsgs";
        public const string ReplacedExecRspMsgsHelp = "No. of modified orders by bourse";
        public const string RejectedExecRspMsgs = "Beltone.Services.Fix.RejectedExecRspMsgs";
        public const string RejectedExecRspMsgsHelp = "No. of rejected orders by bourse";
        public const string PendingReplaceExecRspMsgs = "Beltone.Services.Fix.PendingReplaceExecRspMsgs";
        public const string PendingReplaceExecRspMsgsHelp = "No. of pending replace response messages by bourse";
        public const string PendingCancelExecRspMsgs = "Beltone.Services.Fix.PendingCancelExecRspMsgs";
        public const string PendingCancelExecRspMsgsHelp = "No. of pending cancel response messages by bourse";
        public const string CanceledExecRspMsgs = "Beltone.Services.Fix.CanceledExecRspMsgs";
        public const string CanceledExecRspMsgsHelp = "No. of pending canceled orders response by bourse";
        public const string ExpiredExecRspMsgs = "Beltone.Services.Fix.ExpiredExecRspMsgs";
        public const string ExpiredExecRspMsgsHelp = "No. of expired orders response by bourse";
        public const string UnhandledExecRspMsgs = "Beltone.Services.Fix.UnhandledExecRspMsgs";
        public const string UnhandledExecRspMsgsHelp = "No. of unhandled execution messages responses by bourse";


        public const string BusinessRejectRspMsgs = "Beltone.Services.Fix.BusinessRejectRspMsgs";
        public const string BusinessRejectRspMsgsHelp = "No. of business reject messages responses by bourse";
        public const string OrderCancelRejectRspMsgs = "Beltone.Services.Fix.OrderCancelRejectRspMsgs";
        public const string OrderCancelRejectRspMsgsHelp = "No. of order cancel rejection messages responses by bourse";
        public const string RejectionMsgs = "Beltone.Services.Fix.RejectionMsgs";
        public const string RejectionMsgsHelp = "No. of rejection messages by bourse";


        public const string NotApplicableMessages = "Beltone.Services.Fix.NotApplicableMessages";
        public const string NotApplicableMessagesHelp = "No. of not defined type messages.";
        public const string ExceptionMessages = "Beltone.Services.Fix.ExceptionMessages";
        public const string ExceptionMessagesHelp = "No. of faulted messages";
        public const string SubmittedOrders = "Beltone.Services.Fix.SubmittedOrders";
        public const string SubmittedOrdersHelp = "No. of submitted orders";
        public const string DuplicatedOrders = "Beltone.Services.Fix.DuplicatedOrders";
        public const string DuplicatedOrdersHelp = "No. of duplicated orders";
        public const string SMSSentMessages = "Beltone.Services.Fix.SMSSent";
        public const string SMSSentMessagesHelp = "No. of posted cellular phone short messages to smtp server.";
        public const string EmailSentMessages = "Beltone.Services.Fix.EmailSent";
        public const string EmailSentMessagesHelp = "No. of posted emails to smtp server.";
    }


    public static class Counters
    {
        private static Dictionary<string, PerformanceCounter> m_sigleton = null;

        public static void Initialize()
        {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.Write("Initializing Performance Counter: ");

            //System.Diagnostics.PerformanceCounterCategory.Delete(CountersConstants.COECategory);

            if (!System.Diagnostics.PerformanceCounterCategory.Exists(CountersConstants.FixSvcCategory))
            {
                CounterCreationDataCollection CounterData = new CounterCreationDataCollection()
                    {
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsCancelOrderReqs, CounterHelp = CountersConstants.ClientsCancelOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsNewOrderReqs, CounterHelp = CountersConstants.ClientsNewOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsReplaceOrderReqs, CounterHelp = CountersConstants.ClientsReplaceOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.RcvExecRspMsgs, CounterHelp = CountersConstants.RcvExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.NewExecRspMsgs, CounterHelp = CountersConstants.NewExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SuspendedExecRspMsgs, CounterHelp = CountersConstants.SuspendedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.FilledExecRspMsgs, CounterHelp = CountersConstants.FilledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PartialFilledExecRspMsgs, CounterHelp = CountersConstants.PartialFilledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ReplacedExecRspMsgs, CounterHelp = CountersConstants.ReplacedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.RejectedExecRspMsgs, CounterHelp = CountersConstants.RejectedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.CanceledExecRspMsgs, CounterHelp = CountersConstants.CanceledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PendingCancelExecRspMsgs, CounterHelp = CountersConstants.PendingCancelExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PendingReplaceExecRspMsgs, CounterHelp = CountersConstants.PendingReplaceExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ExpiredExecRspMsgs, CounterHelp = CountersConstants.ExpiredExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.UnhandledExecRspMsgs, CounterHelp = CountersConstants.UnhandledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.BusinessRejectRspMsgs, CounterHelp = CountersConstants.BusinessRejectRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.OrderCancelRejectRspMsgs, CounterHelp = CountersConstants.OrderCancelRejectRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.RejectionMsgs, CounterHelp = CountersConstants.RejectionMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.NotApplicableMessages, CounterHelp =CountersConstants.NotApplicableMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SMSSentMessages, CounterHelp =CountersConstants.SMSSentMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.EmailSentMessages, CounterHelp =CountersConstants.EmailSentMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ExceptionMessages, CounterHelp =CountersConstants.ExceptionMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SubmittedOrders, CounterHelp =CountersConstants.SubmittedOrdersHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.DuplicatedOrders, CounterHelp =CountersConstants.DuplicatedOrdersHelp, CounterType = PerformanceCounterType.NumberOfItems64 }
                    };
                System.Diagnostics.PerformanceCounterCategory.Create(CountersConstants.FixSvcCategory, CountersConstants.FixSvcCategoryHelp, PerformanceCounterCategoryType.MultiInstance, CounterData);
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine("Done, Performance Counters have been creat3ed successfully !");
                //Console.WriteLine();

            }
            //else
            //{
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine("Done, Performance Counters Already Existed !");
                //Console.WriteLine();
            //}

            m_sigleton = new Dictionary<string, PerformanceCounter>();



            PerformanceCounter clientCancelOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsCancelOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ClientsCancelOrderReqs, clientCancelOrders);
            PerformanceCounter clientNewOrder = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsNewOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ClientsNewOrderReqs, clientNewOrder);
            PerformanceCounter clientReplaceOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsReplaceOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ClientsReplaceOrderReqs, clientReplaceOrders);


            PerformanceCounter newRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.NewExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.NewExecRspMsgs, newRsp);
            PerformanceCounter replaceRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ReplacedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ReplacedExecRspMsgs, replaceRsp);
            PerformanceCounter suspendedRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SuspendedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.SuspendedExecRspMsgs, suspendedRsp);
            PerformanceCounter filled = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.FilledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.FilledExecRspMsgs, filled);
            PerformanceCounter partialliFilled = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PartialFilledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.PartialFilledExecRspMsgs, partialliFilled);
            PerformanceCounter exec = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RcvExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.RcvExecRspMsgs, exec);
            PerformanceCounter rejectedRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RejectedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.RejectedExecRspMsgs, rejectedRsp);
            PerformanceCounter canceledRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.CanceledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.CanceledExecRspMsgs, canceledRsp);
            PerformanceCounter pendingCancelRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PendingCancelExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.PendingCancelExecRspMsgs, pendingCancelRsp);
            PerformanceCounter pendingReplaceRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PendingReplaceExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.PendingReplaceExecRspMsgs, pendingReplaceRsp);
            PerformanceCounter expiredRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ExpiredExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ExpiredExecRspMsgs, expiredRsp);
            PerformanceCounter unhandeledExecRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.UnhandledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.UnhandledExecRspMsgs, unhandeledExecRsp);

            PerformanceCounter businessRejectRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.BusinessRejectRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.BusinessRejectRspMsgs, businessRejectRsp);
            PerformanceCounter orderCancelRejectRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.OrderCancelRejectRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.OrderCancelRejectRspMsgs, orderCancelRejectRsp);
            PerformanceCounter rejectionMsg = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RejectionMsgs, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.RejectionMsgs, rejectionMsg);


            
            PerformanceCounter NotApplicableMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.NotApplicableMessages, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.NotApplicableMessages, NotApplicableMessages);
            PerformanceCounter ExceptionMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ExceptionMessages, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.ExceptionMessages, ExceptionMessages);
            PerformanceCounter SMSSentMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SMSSentMessages, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.SMSSentMessages, SMSSentMessages);
            PerformanceCounter EmailSentMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.EmailSentMessages, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.EmailSentMessages, EmailSentMessages);
            PerformanceCounter SubmittedOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SubmittedOrders, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.SubmittedOrders, SubmittedOrders);
            PerformanceCounter DuplicatedOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.DuplicatedOrders, CountersConstants.FixSvcCategoryInstance, false);
            m_sigleton.Add(CountersConstants.DuplicatedOrders, DuplicatedOrders);

            foreach (PerformanceCounter counter in m_sigleton.Values)
            {
                counter.ReadOnly = false;
            }

        }

        public static void IncrementCounter(string counterName)
        {
            try
            {
                if (m_sigleton.ContainsKey(counterName))
                {
                    m_sigleton[counterName].Increment();
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "IncrementCounter Error: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public static void DecrementCounter(string counterName)
        {
            try
            {
                if (m_sigleton.ContainsKey(counterName))
                {
                    m_sigleton[counterName].Decrement();
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "DecrementCounter Error: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public static Dictionary<string, PerformanceCounter> GetCounters()
        {
            return m_sigleton;
        }

        public static void ResetCounters()
        {
            try
            {
                foreach (PerformanceCounter counter in m_sigleton.Values)
                {
                    counter.RawValue = 0;
                }
            }
            catch (Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, "ResetCounters Error: " + ex.ToString(), ConsoleColor.Red, ConsoleColor.Black, true);
            }
        }

        public static void ReInitialize()
        {

            if (m_sigleton == null)
            {
                m_sigleton = new Dictionary<string, PerformanceCounter>();
            }

            lock (m_sigleton)
            {
                System.Diagnostics.PerformanceCounterCategory.Delete(CountersConstants.FixSvcCategory);

                if (!System.Diagnostics.PerformanceCounterCategory.Exists(CountersConstants.FixSvcCategory))
                {
                    CounterCreationDataCollection CounterData = new CounterCreationDataCollection()
                    {
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsCancelOrderReqs, CounterHelp = CountersConstants.ClientsCancelOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsNewOrderReqs, CounterHelp = CountersConstants.ClientsNewOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ClientsReplaceOrderReqs, CounterHelp = CountersConstants.ClientsReplaceOrderReqsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.RcvExecRspMsgs, CounterHelp = CountersConstants.RcvExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.NewExecRspMsgs, CounterHelp = CountersConstants.NewExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SuspendedExecRspMsgs, CounterHelp = CountersConstants.SuspendedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.FilledExecRspMsgs, CounterHelp = CountersConstants.FilledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PartialFilledExecRspMsgs, CounterHelp = CountersConstants.PartialFilledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ReplacedExecRspMsgs, CounterHelp = CountersConstants.ReplacedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.RejectedExecRspMsgs, CounterHelp = CountersConstants.RejectedExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.CanceledExecRspMsgs, CounterHelp = CountersConstants.CanceledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PendingCancelExecRspMsgs, CounterHelp = CountersConstants.PendingCancelExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.PendingReplaceExecRspMsgs, CounterHelp = CountersConstants.PendingReplaceExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ExpiredExecRspMsgs, CounterHelp = CountersConstants.ExpiredExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.UnhandledExecRspMsgs, CounterHelp = CountersConstants.UnhandledExecRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.BusinessRejectRspMsgs, CounterHelp = CountersConstants.BusinessRejectRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.OrderCancelRejectRspMsgs, CounterHelp = CountersConstants.OrderCancelRejectRspMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.RejectionMsgs, CounterHelp = CountersConstants.RejectionMsgsHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        
                        new CounterCreationData() { CounterName =  CountersConstants.NotApplicableMessages, CounterHelp =CountersConstants.NotApplicableMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SMSSentMessages, CounterHelp =CountersConstants.SMSSentMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.EmailSentMessages, CounterHelp =CountersConstants.EmailSentMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.ExceptionMessages, CounterHelp =CountersConstants.ExceptionMessagesHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.SubmittedOrders, CounterHelp =CountersConstants.SubmittedOrdersHelp, CounterType = PerformanceCounterType.NumberOfItems64 },
                        new CounterCreationData() { CounterName =  CountersConstants.DuplicatedOrders, CounterHelp =CountersConstants.DuplicatedOrdersHelp, CounterType = PerformanceCounterType.NumberOfItems64 }
                    };
                    System.Diagnostics.PerformanceCounterCategory.Create(CountersConstants.FixSvcCategory, CountersConstants.FixSvcCategoryHelp, PerformanceCounterCategoryType.MultiInstance, CounterData);

                }
                //else
                //{
                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.WriteLine("Done, Performance Counters Already Existed !");
                //    Console.WriteLine();
                //}


                PerformanceCounter clientCancelOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsCancelOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ClientsCancelOrderReqs, clientCancelOrders);
                PerformanceCounter clientNewOrder = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsNewOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ClientsNewOrderReqs, clientNewOrder);
                PerformanceCounter clientReplaceOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ClientsReplaceOrderReqs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ClientsReplaceOrderReqs, clientReplaceOrders);


                PerformanceCounter newRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.NewExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.NewExecRspMsgs, newRsp);
                PerformanceCounter replaceRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ReplacedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ReplacedExecRspMsgs, replaceRsp);
                PerformanceCounter suspendedRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SuspendedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.SuspendedExecRspMsgs, suspendedRsp);
                PerformanceCounter filled = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.FilledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.FilledExecRspMsgs, filled);
                PerformanceCounter partialliFilled = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PartialFilledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.PartialFilledExecRspMsgs, partialliFilled);
                PerformanceCounter exec = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RcvExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.RcvExecRspMsgs, exec);
                PerformanceCounter rejectedRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RejectedExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.RejectedExecRspMsgs, rejectedRsp);
                PerformanceCounter canceledRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.CanceledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.CanceledExecRspMsgs, canceledRsp);
                PerformanceCounter pendingCancelRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PendingCancelExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.PendingCancelExecRspMsgs, pendingCancelRsp);
                PerformanceCounter pendingReplaceRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.PendingReplaceExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.PendingReplaceExecRspMsgs, pendingReplaceRsp);
                PerformanceCounter expiredRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ExpiredExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ExpiredExecRspMsgs, expiredRsp);
                PerformanceCounter unhandeledExecRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.UnhandledExecRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.UnhandledExecRspMsgs, unhandeledExecRsp);

                PerformanceCounter businessRejectRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.BusinessRejectRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.BusinessRejectRspMsgs, businessRejectRsp);
                PerformanceCounter orderCancelRejectRsp = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.OrderCancelRejectRspMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.OrderCancelRejectRspMsgs, orderCancelRejectRsp);
                PerformanceCounter rejectionMsg = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.RejectionMsgs, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.RejectionMsgs, rejectionMsg);


                PerformanceCounter NotApplicableMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.NotApplicableMessages, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.NotApplicableMessages, NotApplicableMessages);
                PerformanceCounter ExceptionMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.ExceptionMessages, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.ExceptionMessages, ExceptionMessages);
                PerformanceCounter SMSSentMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SMSSentMessages, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.SMSSentMessages, SMSSentMessages);
                PerformanceCounter EmailSentMessages = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.EmailSentMessages, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.EmailSentMessages, EmailSentMessages);
                PerformanceCounter SubmittedOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.SubmittedOrders, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.SubmittedOrders, SubmittedOrders);
                PerformanceCounter DuplicatedOrders = new PerformanceCounter(CountersConstants.FixSvcCategory, CountersConstants.DuplicatedOrders, CountersConstants.FixSvcCategoryInstance, false);
                m_sigleton.Add(CountersConstants.DuplicatedOrders, DuplicatedOrders);



                foreach (PerformanceCounter counter in m_sigleton.Values)
                {
                    counter.ReadOnly = false;
                }
            }
        }
    }
}
