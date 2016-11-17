using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Entities.Entities;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class Currencies
    {

        static Dictionary<int, Dictionary<int, decimal>> m_rates;
        static Dictionary<int, CurrencyItem> m_CurrID_Curr;
        static Dictionary<string, CurrencyItem> m_CurrCode_Curr;

        public static void Initialize()
        {
            DatabaseMethods db = new DatabaseMethods();
            //db.UpdateCurrenciesConversionRates();
            m_rates = db.GetFX_Rate();
            m_CurrID_Curr = db.GetCurrencies();
            m_CurrCode_Curr = new Dictionary<string, CurrencyItem>();
            foreach (KeyValuePair<int, CurrencyItem> kvp in m_CurrID_Curr)
            {
                m_CurrCode_Curr.Add(kvp.Value.Code, kvp.Value);
            }
            db = null;
        }

        public static decimal GetRate(int currencyID_From, int CurrencyID_To)
        {
            return m_rates[currencyID_From][CurrencyID_To];
        }

        public static CurrencyItem GetCurrencyByID(int currencyID)
        {
            return m_CurrID_Curr[currencyID];
        }

        internal static CurrencyItem GetCurrencyByCode(string currCode)
        {
            return m_CurrCode_Curr[currCode];
        }
    }
}
