using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Beltone.Services.Fix.Service.Handlers.RequestMessagesHandlers
{
    public class RequestMessagesHandlers: ConfigurationSection
    {
        [ConfigurationProperty("RequestMessagesHandlersList", IsDefaultCollection = false),
        ConfigurationCollection(typeof(RequestMessageHandler), AddItemName = "RequestMessageHandler")]
        public RequestMessagesHandlersList RequestMessagesHandlersList
        {
            get { return this["RequestMessagesHandlersList"] as RequestMessagesHandlersList; }
            set { this["RequestMessagesHandlersList"] = value; }
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



    public class RequestMessagesHandlersList : ConfigurationElementCollection
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

        public void Add(RequestMessageHandler handler)
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

        public RequestMessageHandler this[int index]
        {
            get { return (RequestMessageHandler)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public RequestMessageHandler this[string key]
        {
            get { return (RequestMessageHandler)BaseGet(key); }
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
            return new RequestMessageHandler();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RequestMessageHandler)element).Name;
        }
    }



    public class RequestMessageHandler : ConfigurationElement
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
