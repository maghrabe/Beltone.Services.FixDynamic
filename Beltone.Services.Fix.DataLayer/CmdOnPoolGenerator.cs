using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Beltone.Services.Fix.DataLayer
{
    public class CmdOnPoolGenerator : IDisposable
    {
        Database _DatabaseInstance = null;
        public Database DatabaseInstance { get { return _DatabaseInstance; } }
        static Dictionary<string, Database> _dbs = null;
        static object _lockObj = null;
        public void Dispose()
        {
           
        }

        //public CmdOnPoolGenerator(string dbName)
        //{
        //    if (_dbs == null)
        //        _dbs = new Dictionary<string, Database>();
        //    if (!_dbs.ContainsKey(dbName))
        //        _dbs.Add(dbName, DatabaseFactory.CreateDatabase(dbName));
        //    _DatabaseInstance = _dbs[dbName];
        //}

        public CmdOnPoolGenerator(string dbName)
        {
            if (_dbs == null)
            {
                Initialize();
            }
            lock (_lockObj)
            {
                if (!_dbs.ContainsKey(dbName))
                    _dbs.Add(dbName, DatabaseFactory.CreateDatabase(dbName));
            }
            _DatabaseInstance = _dbs[dbName];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void Initialize()
        {
            // to make sure that second thread won't initialize
            if (_dbs == null) 
            {
                _dbs = new Dictionary<string, Database>();
                _lockObj = new object();
            }
        }

        public DbCommand CreateCommand(CustomizedCommand customCmd, bool needIdentity)
        {
            if (customCmd.ColumnsData == null || customCmd.ColumnsData.Count == 0) { throw new Exception("No Defined Columns"); }
            if (string.IsNullOrEmpty(customCmd.ConnectionString)) { throw new Exception("Missing Connection String"); }
            if (string.IsNullOrEmpty(customCmd.TableName)) { throw new Exception("Missing TableName"); }
            DbCommand cmd = _DatabaseInstance.GetSqlStringCommand("none");
            // check if column has identity or auto incremental
            if (customCmd.CommandType == CustomizedCommandType.New)
            {
                string columns = string.Empty;
                string values = string.Empty;
                // create parameters (no filters in insert query)
                foreach (KeyValuePair<string, object> kvp in customCmd.ColumnsData)
                {
                    DbParameter para = cmd.CreateParameter();
                    para.ParameterName = string.Format("@{0}", kvp.Key);
                    para.Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                    cmd.Parameters.Add(para);
                }
                // create statement
                foreach (KeyValuePair<string, object> kvp in customCmd.ColumnsData)
                {
                    columns += string.Format("{0},", kvp.Key);
                    values += string.Format("@{0},", kvp.Key);
                }
                columns = columns.Remove(columns.Length - 1, 1);
                values = values.Remove(values.Length - 1, 1);
                string statement = string.Format("Insert into {0} ({1}) values ({2}) {3} ", customCmd.TableName, columns, values, needIdentity ? "Select SCOPE_IDENTITY()" : string.Empty);
                cmd.CommandText = statement;
            }
            else if (customCmd.CommandType == CustomizedCommandType.Update)
            {
                string columns = string.Empty;
                string filters = string.Empty;
                // create parameters
                foreach (KeyValuePair<string, object> kvp in customCmd.ColumnsData)
                {
                    DbParameter para = cmd.CreateParameter();
                    para.ParameterName = string.Format("@{0}", kvp.Key);
                    para.Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                    cmd.Parameters.Add(para);
                }
                foreach (KeyValuePair<string, object> kvp in customCmd.FilteredColumnsData)
                {
                    if (!customCmd.ColumnsData.ContainsKey(kvp.Key))
                    {
                        DbParameter para = cmd.CreateParameter();
                        para.ParameterName = string.Format("@{0}", kvp.Key);
                        para.Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                        cmd.Parameters.Add(para);
                    }
                }
                // create statement
                foreach (KeyValuePair<string, object> kvp in customCmd.ColumnsData)
                {
                    columns += string.Format("{0} = @{0},", kvp.Key);
                }
                foreach (KeyValuePair<string, object> kvp in customCmd.FilteredColumnsData)
                {
                    filters += string.Format("{0} = @{0}  AND ", kvp.Key);
                }
                columns = columns.Remove(columns.Length - 1, 1);
                filters = filters.Remove(filters.Length - 4, 4);
                string statement = string.Format("Update {0} set {1} {2}", customCmd.TableName, columns, filters != string.Empty ? " Where " + filters : string.Empty);
                cmd.CommandText = statement;
            }

            else if (customCmd.CommandType == CustomizedCommandType.Delete)
            {
                throw new Exception("CommandGenerator : Delete Command not implemented yet.");
            }

            else
            {
                throw new Exception("Command Type Not Defined : " + customCmd.CommandType.ToString());
            }

            return cmd;
        }

    }
}
