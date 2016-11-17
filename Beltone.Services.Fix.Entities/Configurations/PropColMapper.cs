using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Beltone.Services.Fix.Entities.Configurations
{
    public static class PropColMapper
    {

        static Dictionary<string, List<PropertiesColumnsSchemaItem>> m_table_schemaItems;
        static Dictionary<string, Dictionary<string, PropertiesColumnsSchemaItem>> m_table_col_prop;
        static Dictionary<string, Dictionary<string, PropertiesColumnsSchemaItem>> m_table_prop_col;

        public static void Initialize()
        {
            PropertiesColumnsSchema schema = (PropertiesColumnsSchema)ConfigurationManager.GetSection("PropertiesColumnsSchema");
            if (schema == null || schema.PropertiesColumnsSchemaList.Count == 0)
            {
                throw new ArgumentNullException("PropertiesColumnsSchema Not Found");
            }
            m_table_schemaItems = new Dictionary<string, List<PropertiesColumnsSchemaItem>>();
            m_table_col_prop = new Dictionary<string, Dictionary<string, PropertiesColumnsSchemaItem>>();
            m_table_prop_col = new Dictionary<string, Dictionary<string, PropertiesColumnsSchemaItem>>();

            foreach (PropertiesColumnsSchemaItem item in schema.PropertiesColumnsSchemaList)
            {
                if (!m_table_schemaItems.ContainsKey(item.TableName))
                {
                    m_table_schemaItems.Add(item.TableName, new List<PropertiesColumnsSchemaItem>());
                }
                m_table_schemaItems[item.TableName].Add(item);
                if (!m_table_col_prop.ContainsKey(item.TableName))
                {
                    m_table_col_prop.Add(item.TableName, new Dictionary<string, PropertiesColumnsSchemaItem>());
                }
                if (!m_table_col_prop.ContainsKey(item.ColumnName))
                {
                    m_table_col_prop[item.TableName].Add(item.ColumnName, item);
                }
                if (!m_table_prop_col.ContainsKey(item.TableName))
                {
                    m_table_prop_col.Add(item.TableName, new Dictionary<string, PropertiesColumnsSchemaItem>());
                }
                if (!m_table_prop_col.ContainsKey(item.PropertyName))
                {
                    m_table_prop_col[item.TableName].Add(item.PropertyName, item);
                }
            }
        }


        public static PropertiesColumnsSchemaItem[]  GetTableSchema(string tableName)
        {
            return m_table_schemaItems[tableName].ToArray();
        }

        public static PropertiesColumnsSchemaItem GetColumnByProperty(string tableName,  string propertyName)
        {
            return m_table_prop_col[tableName][propertyName];
        }

        public static PropertiesColumnsSchemaItem GetPropertyByColumn(string tableName, string columnName)
        {
            return m_table_col_prop[tableName][columnName];
        }



    }
}
