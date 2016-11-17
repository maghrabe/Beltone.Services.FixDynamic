using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Beltone.Services.Fix.DataLayer
{

    public enum CustomizedCommandType { New, Update, Delete }

    public class CustomizedCommand
    {
        public string TableName { get; set; }
        public string ConnectionString { get; set; }
        public DataProvider DataProvider { get; set; }
        public CustomizedCommandType CommandType { get; set; } 
        /// <summary>
        /// Column Name, Value Data and Type
        /// </summary>
        public Dictionary<string, object> ColumnsData { get; set; }
        public Dictionary<string, object> FilteredColumnsData { get; set; }
    }

    public class Value
    {
        public object Data { get; set; }
        public Type DataType { get; set; }
    }

    public class CommandsGenerator
    {
        /// <summary>
        /// returns number of affected rows if no identity is required
        /// </summary>
        /// <param name="cmds"></param>
        /// <param name="transactional"></param>
        /// <param name="needIdentity"></param>
        /// <returns></returns>
        //public IDbCommand CreateCommand(CustomizedCommand cmd, bool needIdentity)
        //{
        //    if (cmd.ColumnsData == null || cmd.ColumnsData.Count == 0) { throw new Exception("No Defined Columns"); }
        //    if (string.IsNullOrEmpty(cmd.ConnectionString)) { throw new Exception("Missing Connection String"); }
        //    if (string.IsNullOrEmpty(cmd.TableName)) { throw new Exception("Missing TableName"); }
        //    return CreateCommandInternal(cmd, needIdentity);
        //}

        public DBManager CreateCommand(CustomizedCommand cmd, bool needIdentity)
        {
            if (cmd.ColumnsData == null || cmd.ColumnsData.Count == 0) { throw new Exception("No Defined Columns"); }
            if (string.IsNullOrEmpty(cmd.ConnectionString)) { throw new Exception("Missing Connection String"); }
            if (string.IsNullOrEmpty(cmd.TableName)) { throw new Exception("Missing TableName"); }
            return CreateCommandInternal(cmd, needIdentity);
        }

        //private IDbCommand CreateCommandInternal(CustomizedCommand cmd, bool needIdentity)
        //{
        //    // check if column has identity or auto incremental
        //    //DBManager db = new DBManager(cmd.DataProvider, cmd.ConnectionString);
        //    SqlCommand sqlCmd = new SqlCommand();
        //    sqlCmd.Connection = new SqlConnection(cmd.ConnectionString);
        //    sqlCmd.CommandType = CommandType.Text;


        //    if (cmd.CommandType == CustomizedCommandType.New)
        //    {
        //        string columns = string.Empty;
        //        string values = string.Empty;
        //        //string filters = string.Empty;


        //        // create parameters (no filters in insert query)
        //        //db.CreateParameters(cmd.ColumnsData.Count + (cmd.FilteredColumnsData == null ? 0 : cmd.FilteredColumnsData.Count));
        //        //db.CreateParameters(cmd.ColumnsData.Count);
        //        //byte index = 0;       
        //        foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
        //        {
        //            sqlCmd.Parameters.AddWithValue(string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
        //            //index++;
        //        }
        //        //foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
        //        //{
        //        //    if (!cmd.ColumnsData.ContainsKey(kvp.Key))
        //        //    {
        //        //        db.AddParameters(index, string.Format("@{0}", kvp.Key), kvp.Value);
        //        //        index++;
        //        //    }
        //        //}


        //        // create statement
        //        foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
        //        {
        //            columns += string.Format("{0},", kvp.Key);
        //            values += string.Format("@{0},", kvp.Key);
        //        }
        //        //foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
        //        //{
        //        //    filters += string.Format("@{0} AND", kvp.Key);
        //        //}

        //        columns = columns.Remove(columns.Length - 1, 1);
        //        //filters = filters.Remove(filters.Length - 4, 4);
        //        values = values.Remove(values.Length - 1, 1);


        //        string statement = string.Format("Insert into {0} ({1}) values ({2}) {3} ", cmd.TableName, columns, values, needIdentity ? "Select SCOPE_IDENTITY()" : string.Empty);
        //        //string statement = string.Format("Insert into {0} ({1}) values ({2}) {3} {4}", cmd.TableName, columns, values, filters != string.Empty ? " Where " + filters : string.Empty, needIdentity ? "Select SCOPE_IDENTITY()" : string.Empty);
        //        sqlCmd.CommandText = statement;
        //    }



        //    else if (cmd.CommandType == CustomizedCommandType.Update)
        //    {
        //        string columns = string.Empty;
        //        string filters = string.Empty;


        //        // create parameters
        //        //db.CreateParameters(cmd.ColumnsData.Count + (cmd.FilteredColumnsData == null ? 0 : cmd.FilteredColumnsData.Count));
        //        //byte index = 0;
        //        foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
        //        {
        //            sqlCmd.Parameters.AddWithValue(string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
        //            //index++;
        //        }
        //        foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
        //        {
        //            if (!cmd.ColumnsData.ContainsKey(kvp.Key))
        //            {
        //                sqlCmd.Parameters.AddWithValue(string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
        //            }
        //        }


        //        // create statement
        //        foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
        //        {
        //            columns += string.Format("{0} = @{0},", kvp.Key);
        //        }
        //        foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
        //        {
        //            filters += string.Format("@{0} AND ", kvp.Key);
        //        }

        //        columns = columns.Remove(columns.Length - 1, 1);


        //        string statement = string.Format("Update {0} set {1} {2}", cmd.TableName, columns, filters != string.Empty ? " Where " + filters : string.Empty);
        //        sqlCmd.CommandText = statement;
        //    }
            
        //    else if (cmd.CommandType == CustomizedCommandType.Delete) 
        //    {
        //        throw new Exception("CommandGenerator : Delete Command not implemented yet.");
        //    }
            
        //    else
        //    {
        //        throw new Exception("Command Type Not Defined : " + cmd.CommandType.ToString());
        //    }

        //    return sqlCmd;
        //}



        private DBManager CreateCommandInternal(CustomizedCommand cmd, bool needIdentity)
        {
            // check if column has identity or auto incremental
            DBManager db = new DBManager(cmd.DataProvider, cmd.ConnectionString);

            if (cmd.CommandType == CustomizedCommandType.New)
            {
                string columns = string.Empty;
                string values = string.Empty;
                // create parameters (no filters in insert query)
                db.CreateParameters(cmd.ColumnsData.Count);
                byte index = 0;
                foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
                {
                    db.AddParameters(index, string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
                    index++;
                }
                // create statement
                foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
                {
                    columns += string.Format("{0},", kvp.Key);
                    values += string.Format("@{0},", kvp.Key);
                }
                columns = columns.Remove(columns.Length - 1, 1);
                values = values.Remove(values.Length - 1, 1);
                string statement = string.Format("Insert into {0} ({1}) values ({2}) {3} ", cmd.TableName, columns, values, needIdentity ? "Select SCOPE_IDENTITY()" : string.Empty);
                db.GeneralCommandText = statement;
            }

            else if (cmd.CommandType == CustomizedCommandType.Update)
            {
                string columns = string.Empty;
                string filters = string.Empty;
                // create parameters
                db.CreateParameters(cmd.ColumnsData.Count + (cmd.FilteredColumnsData == null ? 0 : cmd.FilteredColumnsData.Count));
                byte index = 0;
                foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
                {
                    db.AddParameters(index, string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
                    index++;
                }
                foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
                {
                    if (!cmd.ColumnsData.ContainsKey(kvp.Key))
                    {
                        db.AddParameters(index, string.Format("@{0}", kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
                        index++;
                    }
                }
                // create statement
                foreach (KeyValuePair<string, object> kvp in cmd.ColumnsData)
                {
                    columns += string.Format("{0} = @{0},", kvp.Key);
                }
                foreach (KeyValuePair<string, object> kvp in cmd.FilteredColumnsData)
                {
                    filters += string.Format("{0} = @{0}  AND ", kvp.Key);
                }
                columns = columns.Remove(columns.Length - 1, 1);
                filters = filters.Remove(filters.Length - 4, 4);
                string statement = string.Format("Update {0} set {1} {2}", cmd.TableName, columns, filters != string.Empty ? " Where " + filters : string.Empty);
                db.GeneralCommandText = statement;
            }

            else if (cmd.CommandType == CustomizedCommandType.Delete)
            {
                throw new Exception("CommandGenerator : Delete Command not implemented yet.");
            }

            else
            {
                throw new Exception("Command Type Not Defined : " + cmd.CommandType.ToString());
            }

            return db;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Affected Rows Number</returns>
        public void CreateCommandAsync()
        {
            //invoke CreateAndExecuteCommand()
        }
    }
}
