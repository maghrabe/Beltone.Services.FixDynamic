using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beltone.Services.Fix.Service.Handlers.ResponseMcsdMessagesHandlers
    
{
    public class McsdResponseMessagesHandlers : ConfigurationSection
    {
        [ConfigurationProperty("McsdResponseMessagesHandlersList", IsDefaultCollection = false),
        ConfigurationCollection(typeof(McsdResponseMessageHandler), AddItemName = "McsdResponseMessageHandler")]
        public McsdResponseMessagesHandlersList McsdResponseMessagesHandlersList
        {
            get { return this["McsdResponseMessagesHandlersList"] as McsdResponseMessagesHandlersList; }
            set { this["McsdResponseMessagesHandlersList"] = value; }
        }

        public override bool IsReadOnly()
        {
            return false;
        }
    }


    public class McsdResponseMessagesHandlersList : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }


        public void Add(McsdResponseMessageHandler handler)
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

        public McsdResponseMessageHandler this[int index]
        {
            get { return (McsdResponseMessageHandler)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public McsdResponseMessageHandler this[string key]
        {
            get { return (McsdResponseMessageHandler)BaseGet(key); }
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

     

        protected override ConfigurationElement CreateNewElement()
        {
            return new McsdResponseMessageHandler();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((McsdResponseMessageHandler)element).Name;
        }
    }



    public class McsdResponseMessageHandler : ConfigurationElement
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
