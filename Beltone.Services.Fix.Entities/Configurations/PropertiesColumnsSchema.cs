using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Beltone.Services.Fix.Entities.Configurations
{
    public class PropertiesColumnsSchema : ConfigurationSection
    {
        [ConfigurationProperty("PropertiesColumnsSchemaList", IsDefaultCollection = false),
        ConfigurationCollection(typeof(PropertiesColumnsSchemaItem), AddItemName = "PropertiesColumnsSchemaItem")]
        public PropertiesColumnsSchemaList PropertiesColumnsSchemaList
        {
            get { return this["PropertiesColumnsSchemaList"] as PropertiesColumnsSchemaList; }
            set { this["PropertiesColumnsSchemaList"] = value; }
        }

        public override bool IsReadOnly()
        {
            return false;
        }
    }

    public class PropertiesColumnsSchemaList : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }


        //protected override void BaseAdd(ConfigurationElement element)
        //{
        //    base.BaseAdd(element,true);
        //}

        public void Add(PropertiesColumnsSchemaItem item)
        {
            BaseAdd(item);
        }

        public void Remove(object keyQueuePath)
        {
            BaseRemove(keyQueuePath);
        }

        protected override bool IsElementRemovable(ConfigurationElement element)
        {
            return true;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        public PropertiesColumnsSchemaItem this[int index]
        {
            get { return (PropertiesColumnsSchemaItem)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public PropertiesColumnsSchemaItem this[string key]
        {
            get { return (PropertiesColumnsSchemaItem)BaseGet(key); }
            set
            {
                if (BaseGet(key) != null)
                    BaseRemove(key);
                BaseAdd(value);
            }
        }

        protected override bool SerializeElement(System.Xml.XmlWriter writer, bool serializeCollectionKey)
        {
            bool ret = base.SerializeElement(writer,
                serializeCollectionKey);
            // You can enter your custom processing code here.
            return ret;
        }

        //protected override bool SerializeToXmlElement(XmlWriter writer, string elementName)
        //{
        //    return base.SerializeToXmlElement(writer, elementName);
        //}

        protected override ConfigurationElement CreateNewElement()
        {
            return new PropertiesColumnsSchemaItem();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PropertiesColumnsSchemaItem)element).TableName + ((PropertiesColumnsSchemaItem)element).PropertyName;
        }
    }

    public class PropertiesColumnsSchemaItem : ConfigurationElement
    {

        [ConfigurationProperty("TableName")]
        public string TableName
        {
            get { return (string)base["TableName"]; }
            set { base["TableName"] = value; }
        }

        [ConfigurationProperty("PropertyName")]
        public string PropertyName
        {
            get { return (string)base["PropertyName"]; }
            set { base["PropertyName"] = value; }
        }

        [ConfigurationProperty("ColumnName")]
        public string ColumnName
        {
            get { return (string)base["ColumnName"]; }
            set { base["ColumnName"] = value; }
        }

        [ConfigurationProperty("PropertyType")]
        public string PropertyType
        {
            get { return (string)base["PropertyType"]; }
            set { base["PropertyType"] = value; }
        }

        [ConfigurationProperty("ColumnDBType", IsRequired = false)]
        public string ColumnDBType
        {
            get { return (string)base["ColumnDBType"]; }
            set { base["ColumnDBType"] = value; }
        }

        [ConfigurationProperty("DefaultValue", IsRequired = false)]
        public string DefaultValue
        {
            get { return (string)base["DefaultValue"]; }
            set { base["DefaultValue"] = value; }
        }

        [ConfigurationProperty("ColumnDBSize", IsRequired = false, DefaultValue=0)]
        public int ColumnDBSize
        {
            get { return (int)base["ColumnDBSize"]; }
            set { base["ColumnDBSize"] = value; }
        }

        [ConfigurationProperty("IsActive", IsRequired = true, DefaultValue=true)]
        public bool IsActive
        {
            get { return (bool)base["IsActive"]; }
            set { base["IsActive"] = value; }
        }



        public override bool IsReadOnly()
        {
            return false;
        }



        protected override bool SerializeElement(System.Xml.XmlWriter writer, bool serializeCollectionKey)
        {
            bool ret = base.SerializeElement(writer, serializeCollectionKey);
            // You can enter your custom processing code here.
            return ret;
        }

    }
}
