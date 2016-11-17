using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMessagesHandlers
{
    public class ResponseMessagesHandlers : ConfigurationSection
    {
        [ConfigurationProperty("ResponseMessagesHandlersList", IsDefaultCollection = false),
        ConfigurationCollection(typeof(ResponseMessageHandler), AddItemName = "ResponseMessageHandler")]
        public ResponseMessagesHandlersList ResponseMessagesHandlersList
        {
            get { return this["ResponseMessagesHandlersList"] as ResponseMessagesHandlersList; }
            set { this["ResponseMessagesHandlersList"] = value; }
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        //protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        //{
        //    return base.SerializeElement(writer, serializeCollectionKey);
        //}
    }



    public class ResponseMessagesHandlersList : ConfigurationElementCollection
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

        public void Add(ResponseMessageHandler handler)
        {
            BaseAdd(handler);
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

        public ResponseMessageHandler this[int index]
        {
            get { return (ResponseMessageHandler)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public ResponseMessageHandler this[string key]
        {
            get { return (ResponseMessageHandler)BaseGet(key); }
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
            return new ResponseMessageHandler();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ResponseMessageHandler)element).Name;
        }
    }



    public class ResponseMessageHandler : ConfigurationElement
    {

        [ConfigurationProperty("Name")]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { base["Name"] = value; }
        }

        [ConfigurationProperty("Type")]
        public string Type
        {
            get { return (string)base["Type"]; }
            set { base["Type"] = value; }
        }

        [ConfigurationProperty("MsgTypeValue")]
        public string MsgTypeValue
        {
            get { return (string)base["MsgTypeValue"]; }
            set { base["MsgTypeValue"] = value; }
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
