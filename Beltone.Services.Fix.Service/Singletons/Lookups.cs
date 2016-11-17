using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Contract.Entities;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class Lookups
    {

        private static Dictionary<string, LookupItem> m_orderExecutionMessages = null;
        private static Dictionary<string, LookupItem> m_orderStatusMessages = null;
        private static Dictionary<string, LookupItem> m_rejectionMessages = null;
        private static Dictionary<string, LookupItem> m_orderTypes = null;
        private static Dictionary<string, LookupItem> m_orderSides = null;
        private static Dictionary<string, LookupItem> m_timeInForceTypes = null;
        private static Dictionary<string, LookupItem> m_currencyTypes = null;
        private static Dictionary<string, LookupItem> m_exDestinationTypes = null;
        private static Dictionary<string, LookupItem> m_sessionRejectReason = null;
        private static Dictionary<string, LookupItem> m_orderCancelRejectReasons = null;
        private static Dictionary<string, LookupItem> m_ordHandleInstType = null;


        public static void Initialize()
        {
            DatabaseMethods db = new DatabaseMethods();
            Dictionary<string,Dictionary<string,LookupItem>> lookups = db.GetLookups();
            m_orderExecutionMessages = lookups["Lookup_ExecutionTypeMessages"];
            m_rejectionMessages = lookups["Lookup_RejectionReasonMessages"];
            m_orderTypes = lookups["Lookup_OrderTypes"];
            m_orderSides = lookups["Lookup_OrderSides"];
            m_timeInForceTypes = lookups["Lookup_OrderTimeInForceType"];
            m_orderStatusMessages = lookups["Lookup_OrderStatusTypeMessages"];
            m_currencyTypes = lookups["Lookup_CurrencyTypes"];
            m_exDestinationTypes = lookups["Lookup_ExchangeDestinationTypes"];
            m_sessionRejectReason = lookups["Lookup_SessionRejectReason"];
            m_orderCancelRejectReasons = lookups["Lookup_FixOrderCancelRejectReasons"];
            m_ordHandleInstType = lookups["Lookup_OrderHandleInstTypes"];
            db = null;
        }


        public static LookupItem GetExecTypeLookup(string fixValue)
        {
            return m_orderExecutionMessages[fixValue];
        }

        public static LookupItem GetHandleInstTypeLookup(string fixValue)
        {
            return m_ordHandleInstType[fixValue];
        }
        public static LookupItem GetHandleInstTypeLookupByCodeValue(string codeValue)
        {
            return m_ordHandleInstType.Values.SingleOrDefault(b => b.CodeValue == codeValue);
        }
        
        public static LookupItem GetSessionRejectReason(string fixValue)
        {
            return m_sessionRejectReason[fixValue];
        }

        public static LookupItem GetOrderTypeLookup(string fixValue)
        {
            return m_orderTypes[fixValue];
        }
        
        public static LookupItem GetOrderStatusLookup(string fixValue)
        {
            return m_orderStatusMessages[fixValue];
        }
        
        public static LookupItem GetOrderSideLookup(string fixValue)
        {
            return m_orderSides[fixValue];
        }
        
        public static LookupItem GetOrderStatusLookupByCodeValue(string codeValue)
        {
            return m_orderStatusMessages.Values.SingleOrDefault(b => b.CodeValue == codeValue);
        }

        public static LookupItem GetOrderSidesLookupByCodeValue(string codeValue)
        {
            return m_orderSides.Values.SingleOrDefault(b => b.CodeValue == codeValue);
        }

        public static LookupItem GetTimeInForceLookup(string fixValue)
        {
            return m_timeInForceTypes[fixValue];
        }

        public static LookupItem GetTimeInForceLookupByCode(string codeValue)
        {
            return m_timeInForceTypes.Values.SingleOrDefault(b => b.CodeValue == codeValue);
        }

        public static LookupItem GetOrderCancelRejectReasonsLookup(string fixValue)
        {
            return m_orderCancelRejectReasons[fixValue];
        }

        public static LookupItem GetRejectionReasonLookup(string fixValue)
        {
            if (!m_rejectionMessages.ContainsKey(fixValue))
                return null;
            return m_rejectionMessages[fixValue];
        }

        internal static LookupItem GetExecTypeLookupByCodeValue(string codeValue)
        {
            return m_orderExecutionMessages.Values.SingleOrDefault(b => b.CodeValue == codeValue);
        }

        internal static string[] GetOrderSides()
        {
            return m_orderSides.Values.Select(b => b.CodeValue).ToArray();
        }

        internal static string[] GetOrderStatusParameters()
        {
            return m_orderStatusMessages.Values.Select(b => b.CodeValue).ToArray();
        }

        internal static string[] GetOrderTypes()
        {
            return m_orderTypes.Values.Select(b => b.CodeValue).ToArray();
        }

        internal static string[] GetTimeInForceTypes()
        {
            return m_timeInForceTypes.Values.Select(b => b.CodeValue).ToArray();
        }

        internal static LookupItem GetTimeInForceLookupByCodeValue(string timeInForce)
        {
            return m_timeInForceTypes.Values.SingleOrDefault(b => b.CodeValue == timeInForce);
        }

        internal static LookupItem GetOrderTypeLookupByCodeValue(string orderType)
        {
            return m_orderTypes.Values.SingleOrDefault(b=>b.CodeValue == orderType);
        }

        internal static string[] GetCurrencyTypes()
        {
            return m_currencyTypes.Values.Select(b => b.CodeValue).ToArray();
        }

        internal static string[] GetExchangeDestinationsIDs()
        {
            return m_exDestinationTypes.Values.Select(b => b.CodeValue).ToArray();
        }

        public static LookupItem GetCurrencyLookupByCurrencyCode(string currCode)
        {
            return m_currencyTypes.Values.SingleOrDefault(b=> b.CodeValue == currCode);
        }
      

        public static LookupItem GetExchangeDestinationByExchangeID(string exchangeID)
        {
            return m_exDestinationTypes.Values.SingleOrDefault(b => b.CodeValue == exchangeID);
        }

    }


   

}
