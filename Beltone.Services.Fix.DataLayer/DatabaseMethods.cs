using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beltone.Services.Fix.Utilities;
using System.Threading;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Contract.Enums;
using System.Data;
using Beltone.Services.Fix.Contract.Entities;
using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Entities.Constants;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data.Common;

namespace Beltone.Services.Fix.DataLayer
{
    public class DatabaseMethods : IDisposable
    {
        public long AddNewSingleOrder(Guid callbackID, string username, Dictionary<string, object> data, Dictionary<string, object> details)
        {
            try
            {
                using (CmdOnPoolGenerator gen = new CmdOnPoolGenerator(ConStrongKeys.FixDbCon))
                {
                    DbCommand cmdOrder = gen.CreateCommand(new CustomizedCommand() { ColumnsData = data, FilteredColumnsData = null, CommandType = CustomizedCommandType.New, ConnectionString = SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon), DataProvider = DataProvider.SqlServer, TableName = "Orders" }, true);
                    using (System.Transactions.TransactionScope tran = new System.Transactions.TransactionScope())
                    {
                        long orderID = Convert.ToInt64(gen.DatabaseInstance.ExecuteScalar(cmdOrder));
                        details.Add(SingleOrdDetailsProps.OrderID, orderID);
                        DbCommand cmdOrdersDetail = gen.CreateCommand(new CustomizedCommand() { ColumnsData = details, FilteredColumnsData = null, CommandType = CustomizedCommandType.New, ConnectionString = SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon), DataProvider = DataProvider.SqlServer, TableName = "Orders_Details" }, false);
                        gen.DatabaseInstance.ExecuteNonQuery(cmdOrdersDetail);
                        DbCommand cmdSub = gen.DatabaseInstance.GetSqlStringCommand(string.Format("INSERT INTO [SessionOrders] (SessionID,Username, ReqOrdID, OrdID, SubscriptionDateTime) Values (@SubscriberID, @username,@RequesterOrderID, @OrderID, @SubscriptionDateTime)"));
                        DbParameter para1 = cmdSub.CreateParameter();
                        para1.ParameterName = "@SubscriberID";
                        para1.Value = callbackID;
                        cmdSub.Parameters.Add(para1);
                        DbParameter para2 = cmdSub.CreateParameter();
                        para2.ParameterName = "@RequesterOrderID";
                        para2.Value = (Guid)data[SingleOrderProperties.RequesterOrderID];
                        cmdSub.Parameters.Add(para2);
                        DbParameter para3 = cmdSub.CreateParameter();
                        para3.ParameterName = "@OrderID";
                        para3.Value = orderID;
                        cmdSub.Parameters.Add(para3);
                        DbParameter para4 = cmdSub.CreateParameter();
                        para4.ParameterName = "@SubscriptionDateTime";
                        para4.Value = data[SingleOrderProperties.PlacementDateTime];
                        cmdSub.Parameters.Add(para4);
                        DbParameter para5 = cmdSub.CreateParameter();
                        para5.ParameterName = "@Username";
                        para5.Value = username;
                        cmdSub.Parameters.Add(para5);
                        gen.DatabaseInstance.ExecuteNonQuery(cmdSub);
                        tran.Complete();
                        return orderID;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void UpdateOrderDetails(Dictionary<string, object> updates, Dictionary<string, object> keys, Dictionary<string, object> ordersDetails_columns_values)
        {
            using (CmdOnPoolGenerator gen = new CmdOnPoolGenerator(ConStrongKeys.FixDbCon))
            {
                DbCommand cmdOrders = gen.CreateCommand(new CustomizedCommand() { ColumnsData = updates, FilteredColumnsData = keys, CommandType = CustomizedCommandType.Update, ConnectionString = SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon), DataProvider = DataProvider.SqlServer, TableName = "Orders" }, true);
                DbCommand cmdOrdersDetails = gen.CreateCommand(new CustomizedCommand() { ColumnsData = ordersDetails_columns_values, FilteredColumnsData = null, CommandType = CustomizedCommandType.New, ConnectionString = SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon), DataProvider = DataProvider.SqlServer, TableName = "Orders_Details" }, false);
                using (System.Transactions.TransactionScope tran = new System.Transactions.TransactionScope())
                {
                    gen.DatabaseInstance.ExecuteNonQuery(cmdOrders);
                    gen.DatabaseInstance.ExecuteNonQuery(cmdOrdersDetails);
                    tran.Complete();
                }
            }
        }

        //public bool UpdateBourseOrderID(long orderID, string bourseOrderID)
        //{
        //    DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
        //    db.Open();
        //    db.CreateParameters(2);
        //    db.AddParameters(0, "@OrderID", orderID);
        //    db.AddParameters(1, "@BourseOrderID", bourseOrderID);
        //    int affectedRows = db.ExecuteNonQuery(CommandType.Text, "Update Orders Set BourseOrderID = @BourseOrderID Where OrderID = @OrderID");
        //    db.Close();
        //    db = null;
        //    return affectedRows > 0;
        //}

        public Dictionary<string, Dictionary<string, LookupItem>> GetLookups()
        {
            Dictionary<string,Dictionary<string,LookupItem>> lookups = new  Dictionary<string,Dictionary<string,LookupItem>>();
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataSet ds ;
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixExecutionTypes");
            lookups.Add("Lookup_ExecutionTypeMessages", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_ExecutionTypeMessages"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                     CodeValue = row["CodeValue"].ToString().Trim(),
                     ConstName = row["Status"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["MessageAr"].ToString().Trim(),
                     MessageEn = row["MessageEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixOrderStatusTypes");
            lookups.Add("Lookup_OrderStatusTypeMessages", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_OrderStatusTypeMessages"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                     CodeValue = row["CodeValue"].ToString().Trim(),
                     ConstName = row["Status"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["MessageAr"].ToString().Trim(),
                     MessageEn = row["MessageEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixOrderTypes");
             lookups.Add("Lookup_OrderTypes", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_OrderTypes"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["OrderTypeNameAr"].ToString().Trim(),
                     MessageEn = row["OrderTypeNameEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixRejectionReasonMessages");
            lookups.Add("Lookup_RejectionReasonMessages", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_RejectionReasonMessages"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                     CodeValue = row["CodeValue"].ToString().Trim(),
                     ConstName = row["Status"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["MessageAr"].ToString().Trim(),
                     MessageEn = row["MessageEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixOrderSides");
             lookups.Add("Lookup_OrderSides", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_OrderSides"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["NameAr"].ToString().Trim(),
                     MessageEn = row["NameEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixOrderTimeInForceTypes");
             lookups.Add("Lookup_OrderTimeInForceType", new Dictionary<string,LookupItem>());
            foreach(DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_OrderTimeInForceType"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                     ConstName = row["CodeValue"].ToString().Trim(),
                     FixValue = row["FixValue"].ToString().Trim(),
                     MessageAr = row["NameAr"].ToString().Trim(),
                     MessageEn = row["NameEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixCurrencyTypes");
            lookups.Add("Lookup_CurrencyTypes", new Dictionary<string, LookupItem>());
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_CurrencyTypes"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                    FixValue = row["FixValue"].ToString().Trim(),
                    MessageAr = row["NameAr"].ToString().Trim(),
                    MessageEn = row["NameEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixExchangeDestinationTypes");
            lookups.Add("Lookup_ExchangeDestinationTypes", new Dictionary<string, LookupItem>());
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_ExchangeDestinationTypes"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                    FixValue = row["FixValue"].ToString().Trim(),
                    MessageAr = row["NameAr"].ToString().Trim(),
                    MessageEn = row["NameEn"].ToString().Trim()
                });
            }

            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixSessionRejectReason");
            lookups.Add("Lookup_SessionRejectReason", new Dictionary<string, LookupItem>());
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_SessionRejectReason"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                    FixValue = row["FixValue"].ToString().Trim(),
                    MessageAr = row["MessageAr"].ToString().Trim(),
                    MessageEn = row["MessageEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixOrderCancelRejectReasons");
            lookups.Add("Lookup_FixOrderCancelRejectReasons", new Dictionary<string, LookupItem>());
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_FixOrderCancelRejectReasons"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                    FixValue = row["FixValue"].ToString().Trim(),
                    MessageAr = row["MessageAr"].ToString().Trim(),
                    MessageEn = row["MessageEn"].ToString().Trim()
                });
            }
            ds = db.ExecuteDataSet(System.Data.CommandType.Text, "select * from Lookup_FixHandleInstTypes");
            lookups.Add("Lookup_OrderHandleInstTypes", new Dictionary<string, LookupItem>());
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                lookups["Lookup_OrderHandleInstTypes"].Add(row["FixValue"].ToString().Trim(), new LookupItem()
                {
                    CodeValue = row["CodeValue"].ToString().Trim(),
                    ConstName = row["CodeValue"].ToString().Trim(),
                    FixValue = row["FixValue"].ToString().Trim(),
                    MessageAr = row["NameAr"].ToString().Trim(),
                    MessageEn = row["NameEn"].ToString().Trim()
                });
            }

            return lookups;
        }
       
        public DataTable FillOrdersData()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();

            string qeury = "SELECT Orders.*, SessionOrders.*  FROM  Orders INNER JOIN SessionOrders ON Orders.OrderID = SessionOrders.OrdID where orders.IsActive = 1 or (orders.isactive = 0  and orders.ispending = 1)";
            DataTable dtOrders = db.ExecuteDataSet(CommandType.Text, qeury).Tables[0];
            db.Close();
            db = null;
            return dtOrders;
        }

        public DataTable FillOrdersRequestIDs()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dtOrdersRequests = db.ExecuteDataSet(CommandType.Text, "SELECT RequesterOrderID FROM  Orders").Tables[0];
            db.Close();
            db = null;
            return dtOrdersRequests;
        }

        public void AddReplicationSessionSubscriber(Guid sessionKey, string msgTypes, string subscriberQueuePath, DateTime dateTime)
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            db.CreateParameters(5);
            db.AddParameters(0, "@SubscriberID", sessionKey);
            db.AddParameters(1, "@SubscriberQueuePath", subscriberQueuePath);
            db.AddParameters(2, "@SubscriptionDateTime", DateTime.Now);
            db.AddParameters(3, "@SubscribedFixMsgsTypes", msgTypes);
            db.AddParameters(4, "@IsExpiredSession", false);
            db.ExecuteNonQuery(CommandType.Text, "Insert into ReplicationSessionSubscribers (SubscriberID, SubscriberQueuePath, SubscriptionDateTime, SubscribedFixMsgsTypes, IsExpiredSession) values(@SubscriberID, @SubscriberQueuePath, @SubscriptionDateTime, @SubscribedFixMsgsTypes, @IsExpiredSession)");
            db.Close();
            db = null;
        }

        public Beltone.Services.Fix.Entities.Entities.RepSession[] GetReplicationSessionsSubscribers()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            db.ExecuteNonQuery(CommandType.Text, string.Format("update ReplicationSessionSubscribers Set IsExpiredSession = 1 where  Convert(varchar,subscriptiondatetime,101) < Convert(varchar,Getdate(),101)"));
            DataTable dtSubscribers = db.ExecuteDataSet(CommandType.Text, string.Format("select * from ReplicationSessionSubscribers where IsExpiredSession = 0  and Convert(varchar,subscriptiondatetime,101) = Convert(varchar,Getdate(),101) AND IsExpiredSession = 0")).Tables[0];
            List<RepSession> subscribers = new List<RepSession>();
            foreach (DataRow row in dtSubscribers.Rows)
            {
                subscribers.Add(new RepSession() { SubscriberKey = (Guid)row["SubscriberID"], SubscribedFixMsgsTypes = row["SubscriberQueuePath"].ToString().Split(','), QueuePath = row["SubscriberQueuePath"].ToString(), SubscriptionDateTime = (DateTime)row["SubscriptionDateTime"] });
            }
            return subscribers.ToArray();
        }

        public bool RemoveReplicationSessionSubscriber(Guid subscriberKey)
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            db.CreateParameters(1);
            db.AddParameters(0, "@subscriberid", subscriberKey);
            int affectedRows = db.ExecuteNonQuery(CommandType.Text, "update ReplicationSessionSubscribers set IsExpiredSession = 1 where subscriberid = @subscriberid");
            db.Close();
            db = null;
            return affectedRows > 0;
        }

        public object AddRecord(string tableName, Dictionary<string, object> col_val, bool needIdentity, string connKey)
        {
            try
            {
                using (CmdOnPoolGenerator gen = new CmdOnPoolGenerator(connKey))
                {
                    DbCommand cmd = gen.CreateCommand(new CustomizedCommand() { ColumnsData = col_val, FilteredColumnsData = null, CommandType = CustomizedCommandType.New, ConnectionString = connKey, DataProvider = DataProvider.SqlServer, TableName = tableName }, needIdentity);
                    if (needIdentity)
                        return gen.DatabaseInstance.ExecuteScalar(cmd);
                    else
                    {
                        gen.DatabaseInstance.ExecuteNonQuery(cmd);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateRecord(string tableName, Dictionary<string, object> col_val, Dictionary<string, object> keys, string conString)
        {
            try
            {
                using (CmdOnPoolGenerator gen = new CmdOnPoolGenerator(conString))
                {
                    DbCommand cmd = gen.CreateCommand(new CustomizedCommand() { ColumnsData = col_val, FilteredColumnsData = keys, CommandType = CustomizedCommandType.Update, ConnectionString = SystemConfigurations.GetConnectionString(conString), DataProvider = DataProvider.SqlServer, TableName = tableName }, false);
                    return gen.DatabaseInstance.ExecuteNonQuery(cmd);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable GetTable(string tableName)
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dtOrders = db.ExecuteDataSet(CommandType.Text, string.Format("SELECT * from {0}", tableName)).Tables[0];
            db.Close();
            db = null;
            return dtOrders;
        }

        public void UpdateTodaySequenceReset()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            db.ExecuteNonQuery(CommandType.Text, "Delete from DaySequence");
            db.CreateParameters(2);
            db.AddParameters(0, "@DateTime", DateTime.Now);
            db.AddParameters(1, "@IsReset", true);
            db.ExecuteNonQuery(CommandType.Text, "Insert into DaySequence (DateTime, IsReset) values(@DateTime, @IsReset)");
            db.Close();
            db = null;
        }

        public bool IsTodaySequenceReset()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dt = db.ExecuteDataSet(CommandType.Text, "select * from DaySequence").Tables[0];
            db.Close();
            db = null;
            if (dt.Rows.Count == 0) { return false; }
            else
            {
                if (((DateTime)dt.Rows[0]["DateTime"]).Date == DateTime.Today.Date && (bool)dt.Rows[0]["IsReset"])
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public IEnumerable<string> GetExecutionIDs()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dt = db.ExecuteDataSet(CommandType.Text, string.Format("Select executionid from orders_details  where Convert(varchar,ExecutionDate,101) = Convert(varchar,Getdate(),101) AND executionid <> null")).Tables[0];
            //List<Guid> subscribersIDs = new List<Guid>();
            foreach (DataRow row in dt.Rows)
            {
                if (!row.IsNull("executionid"))
                {
                    yield return row["executionid"].ToString();
                }
            }
        }

        public bool AddExecution(long orderID, string clOrderID, string origClOrderID, string bourseOrderID, Guid requesterOrderID,int clientID, string custodyID, 
            int originalQuantity, int currentQuantity, int executedQuantity, int currentExecutedQuantity, int remainingQuantity, 
            double originalPrice, double currentPrice, string securityCode, string orderType, string orderStatus, 
            DateTime placementDateTime, DateTime modifiedDateTime, string timeInForce, string Note)
        {
            throw new NotImplementedException();
        }

        public DataTable GetStocksDetails()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataTable dt = db.ExecuteDataSet(CommandType.Text, string.Format("Select * from stocks")).Tables[0];
            //List<Guid> subscribersIDs = new List<Guid>();
            db.Close();
            return dt;
        }

        public FixExchange[] GetFixExchangeInfo()
        {
            List<FixExchange> exchanges = new List<FixExchange>();
            string query = "select * from exchanges";
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataSet ds = db.ExecuteDataSet(System.Data.CommandType.Text, query);
            //string today = DateTime.Today.DayOfWeek.ToString();
            foreach (DataRow rowExchange in ds.Tables[0].Rows)
            {
                FixExchange exchange = new FixExchange();
                exchange.ExchangeID = rowExchange["ExchangeID"].ToString();
                exchange.NameAr = rowExchange["NameAr"].ToString();
                exchange.NameEn = rowExchange["NameEn"].ToString();
                List<ExchangeMarket> markets = new List<ExchangeMarket>();
                string queryMarkets = string.Format("select * from Exchanges_Markets where exchangeid = '{0}'", exchange.ExchangeID);
                DataTable dtMarkets = db.ExecuteDataSet(System.Data.CommandType.Text, queryMarkets).Tables[0];
                foreach (DataRow rowMarket in dtMarkets.Rows)
                {
                    ExchangeMarket market = new ExchangeMarket();
                    market.ExchangeID = exchange.ExchangeID;
                    market.MarketID = rowMarket["MarketID"].ToString();
                    market.NameAr = rowMarket["NameAr"].ToString();
                    market.NameEn = rowMarket["NameEn"].ToString();

                    string queryGroups = string.Format("select * from Markets_Groups where exchangeid = '{0}' and marketid = '{1}';", exchange.ExchangeID, market.MarketID);
                    DataTable dtGroups = db.ExecuteDataSet(System.Data.CommandType.Text, queryGroups).Tables[0];
                    List<MarketGroup> groups = new List<MarketGroup>();
                    foreach (DataRow rowGroup in dtGroups.Rows)
                    {
                        MarketGroup group = new MarketGroup();
                        group.ExchangeID = exchange.ExchangeID;
                        group.MarketID = market.MarketID;
                        group.GroupID = rowGroup["GroupID"].ToString();

                        List<GroupSession> sessions = new List<GroupSession>();
                        string querySessions = string.Format("select * from Markets_Groups_Sessions where exchangeid = '{0}' and marketid = '{1}' and groupid = '{2}';", exchange.ExchangeID, market.MarketID, group.GroupID);
                        DataTable dtSessions = db.ExecuteDataSet(System.Data.CommandType.Text, querySessions).Tables[0];
                        foreach (DataRow rowSessions in dtSessions.Rows)
                        {
                            GroupSession session = new GroupSession();
                            session.ExchangeID = exchange.ExchangeID;
                            session.GroupID = group.GroupID;
                            session.MarketID = market.MarketID;
                            session.SessionStartTime = (DateTime)rowSessions["SessionStartTime"];
                            session.SessionEndTime = (DateTime)rowSessions["SessionEndTime"];
                            session.WorkingDays = rowSessions["WorkingDays"].ToString();
                            sessions.Add(session);
                        }
                        group.Sessions = sessions.ToArray();
                        groups.Add(group);
                    }
                    market.Groups = groups.ToArray();
                    markets.Add(market);
                }
                exchange.Markets = markets.ToArray();
                exchanges.Add(exchange);
            }
            db.Close();
            db = null;
            return exchanges.ToArray();
        }

        public GroupSession[] GetAllGroupsSessions()
        {
            List<GroupSession> marketsGroups = new List<GroupSession>();
            string query = "select * from Markets_Groups_Sessions";
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataSet ds = db.ExecuteDataSet(System.Data.CommandType.Text, query);
            db.Close();
            db = null;
            string today = DateTime.Today.DayOfWeek.ToString();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row["WorkingDays"].ToString().Contains(today))
                {
                    marketsGroups.Add(new GroupSession()
                    {
                        MarketID = row["MarketID"].ToString(),
                        GroupID = row["GroupID"].ToString(),
                        ExchangeID = row["ExchangeID"].ToString(),
                        SessionStartTime = (DateTime)row["SessionStartTime"],
                        SessionEndTime = (DateTime)row["SessionEndTime"],
                        WorkingDays = row["WorkingDays"].ToString()
                    });
                }
            }
            return marketsGroups.ToArray();
        }

        public Dictionary<int, Dictionary<int, decimal>> GetFX_Rate()
        {
            Dictionary<int, Dictionary<int, decimal>> rates = new Dictionary<int, Dictionary<int, decimal>>();
            string query = "select * from CurrenciesConversions";
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataSet ds = db.ExecuteDataSet(System.Data.CommandType.Text, query);
            db.Close();
            db = null;
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                int from = (int)row["CurrencyID_From"];
                int to = (int)row["CurrencyID_To"];
                decimal rate = (decimal)row["ConversionRate"];
                if (!rates.ContainsKey(from))
                {
                    rates.Add(from, new Dictionary<int, decimal>());
                }
                if (!rates[from].ContainsKey(to))
                {
                    rates[from].Add(to, rate);
                }
            }
            return rates;
        }

        public Dictionary<int, CurrencyItem> GetCurrencies()
        {
            Dictionary<int, CurrencyItem> currencies = new Dictionary<int, CurrencyItem>();
            string query = "select * from Currencies";
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataSet ds = db.ExecuteDataSet(System.Data.CommandType.Text, query);
            db.Close();
            db = null;
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                currencies.Add((int)row["CurrencyID"], new CurrencyItem() { ID = (int)row["CurrencyID"], Code = row["CurrencyCode"].ToString(), NameAr = row["NameAr"].ToString(), NameEn = row["NameEn"].ToString() });
            }
            return currencies;
        }

        public DataTable test_GetTodayOrders()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dtOrders = db.ExecuteDataSet(CommandType.Text, "SELECT  SecurityCode,OrderStatus, OrderSide, CurrentQuantity, RemainingQuantity, ExecutedQuantity, LastExecQuantity, CurrentPrice, AvgPrice, IsActive, IsCompleted, LastExecPrice, PlacementDateTime, IsExecuted from orders where  Convert(varchar,orders.placementdatetime,101) = Convert(varchar,Getdate(),101)").Tables[0];
            db.Close();
            return dtOrders;
        }

        public DataTable test_GetTodayExecOrders()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            DataTable dtOrders = db.ExecuteDataSet(CommandType.Text, "SELECT  SecurityCode,OrderStatus, OrderSide, CurrentQuantity, RemainingQuantity, ExecutedQuantity, LastExecQuantity, CurrentPrice, AvgPrice, IsActive, IsCompleted, LastExecPrice, PlacementDateTime, IsExecuted from orders where  Convert(varchar,orders.placementdatetime,101) = Convert(varchar,Getdate(),101) and isExecuted = 1").Tables[0];
            db.Close();
            return dtOrders;
        }

        public void Dispose()
        {
        }

        public DayOff[] GetAllDaysOff()
        {
            List<DayOff> daysOff = new List<DayOff>();
            string query = "select * from sysdaysoff where convert(nvarchar,getdate(),101) >= convert(nvarchar,datefrom,101) and convert(nvarchar,getdate(),101) <= convert(nvarchar,dateto,101) ";
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString("BasicDataDBConnectionString"));
            db.Open();
            DataSet ds = db.ExecuteDataSet(System.Data.CommandType.Text, query);
            db.Close();
            db = null;
            string today = DateTime.Today.DayOfWeek.ToString();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                daysOff.Add(new DayOff()
                {
                    OccasionAr = row["Occasion_Ar"].ToString(),
                    OccasionEn = row["Occasion_En"].ToString(),
                    ExchangeID = row["ExchangeID"].ToString(),
                    DateFrom = (DateTime)row["DateFrom"],
                    DateTo = (DateTime)row["DateTo"]
                });
            }
            return daysOff.ToArray();
        
        }

        public void InitSessions()
        {
            DBManager db = new DBManager(DataProvider.SqlServer, SystemConfigurations.GetConnectionString(ConStrongKeys.FixDbCon));
            db.Open();
            db.ExecuteNonQuery(CommandType.Text, "truncate table [Beltone.FixService].[dbo].[Sessions]");
            db.Close();
            
        }

    }
}
