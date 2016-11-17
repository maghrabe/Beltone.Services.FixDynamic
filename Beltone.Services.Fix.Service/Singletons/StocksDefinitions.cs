using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.DataLayer;
using System.Data;

namespace Beltone.Services.Fix.Service.Singletons
{
    public static class StocksDefinitions
    {
        private static Dictionary<string, Stock> m_stocks = null;
        static DataTable m_dtStocks;

        public static void Initialize()
        {
            DatabaseMethods db = new DatabaseMethods();
            m_stocks = new Dictionary<string, Stock>();
            m_dtStocks = db.GetStocksDetails();
            m_dtStocks.PrimaryKey = new DataColumn[] { m_dtStocks.Columns["Code"] };

            foreach (DataRow row in m_dtStocks.Rows)
            {
                m_stocks.Add(row["code"].ToString(), new Stock()
                {
                    Code = row["code"].ToString().Trim(),
                    CurrencyCode = Currencies.GetCurrencyByID((int)row["CurrencyID"]).Code,
                    ExchangeID = row["ExchangeID"].ToString().Trim(),
                    GroupID = row["GroupID"].ToString().Trim(),
                    MarketID = row["MarketID"].ToString().Trim(),
                    ID = (int)row["ID"],
                    NameAr = row["NameAr"].ToString().Trim(),
                    NameEn = row["NameEn"].ToString().Trim(),
                    Reuter = row["Reuter"].ToString().Trim()
                }
                );
            }
        }

        public static Stock GetStockByCode(string code)
        {
            if (!m_stocks.ContainsKey(code))
                return null;
            return m_stocks[code];
        }

        public static Dictionary<string, Stock> GetStocks()
        {
            return m_stocks;
        }


        //public static string GetStockGroup(string stockCode)
        //{
        //    return m_dtStocks.Rows.Find(stockCode)["GroupID"].ToString();
        //}
        //public static string GetStockMarket(string stockCode)
        //{
        //    return m_dtStocks.Rows.Find(stockCode)["MarketID"].ToString();
        //}
        //public static string GetStockExchange(string stockCode)
        //{
        //    return m_dtStocks.Rows.Find(stockCode)["ExchangeID"].ToString();
        //}

    }
}
