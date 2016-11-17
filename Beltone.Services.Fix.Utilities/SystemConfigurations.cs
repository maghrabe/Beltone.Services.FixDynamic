using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Configuration;
using System.Net;
using System.Collections.Specialized;

namespace Beltone.Services.Fix.Utilities
{
    public static class SystemConfigurations
    {
        private static NameValueCollection m_singletonAppSettings = null;
        private static ConnectionStringSettingsCollection m_singletonConnectionStrings = null;

        public static void Initialize()
        {
            if (m_singletonAppSettings == null)
            {
                m_singletonAppSettings = ConfigurationManager.AppSettings;
            }
            if (m_singletonConnectionStrings == null)
            {
                m_singletonConnectionStrings = ConfigurationManager.ConnectionStrings;
            }
        }

        public static string GetConnectionString(string connectionKey)
        {
            return m_singletonConnectionStrings[connectionKey].ConnectionString;
        }

        public static string GetAppSetting(string key)
        {
            return m_singletonAppSettings[key];
        }

        public static NameValueCollection GetAppSettings()
        {
            return m_singletonAppSettings;
        }

        public static void ReloadConfigurations()
        {
            try
            {
                lock (m_singletonAppSettings)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(Environment.CurrentDirectory + "\\General.StartUp.exe.config");
                    XmlNode appSettingsNode = xmlDoc.SelectSingleNode("configuration/appSettings");
                    XmlNode conStringsNode = xmlDoc.SelectSingleNode("configuration/connectionStrings");
                    // Attempt to locate the requested setting.
                    lock (m_singletonAppSettings)
                    {
                        foreach (XmlNode childNode in appSettingsNode)
                        {
                            m_singletonAppSettings[childNode.Attributes["key"].Value] = childNode.Attributes["value"].Value;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("{0} = ", childNode.Attributes["key"].Value);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(childNode.Attributes["value"].Value);
                        }
                    }
                }
                //EventsLogger.LogEvent("Configurations have been reloaded ");
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine("Configurations have been reloaded ");
                //EventsLogger.LogEvent("Configurations have been reloaded ");
                //Console.ResetColor();
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Configurations Reload Error: " + ex.Message);
                //EventsLogger.LogEvent("Configurations Reload Error: " + ex.ToString());
            }
            //lock (m_singletonConnectionStrings)
            //{
            //    foreach (XmlNode childNode in conStringsNode)
            //    {
            //        //m_singletonConnectionStrings[childNode.Attributes["name"].Value] = childNode.Attributes["connectionString"].Value;
            //    }
            //}

            //lock (m_singletonAppSettings)
            //{
            //    Configuration conf = ConfigurationManager.OpenExeConfiguration(@"E:\Development Applications\MarketBeat\Conditional-SmartOrdersEngine_MultiBinding\General.StartUp\bin\Debug\General.StartUp.exe.config");
            //    foreach (KeyValueConfigurationElement k in conf.AppSettings.Settings)
            //    {
            //        m_singletonAppSettings[k.Key] = k.Value;
            //    }

            //    //m_singletonAppSettings = ConfigurationManager.AppSettings;
            //}
            //lock (m_singletonConnectionStrings)
            //{
            //    m_singletonConnectionStrings = ConfigurationManager.ConnectionStrings;
            //}
        }

        public static void SetAppSetting(string key, string value)
        {
            lock (m_singletonAppSettings)
            {
                try
                {
                    m_singletonAppSettings.Set(key, value);
                    //EventsLogger.LogEvent(string.Format("Configurations value changed Key: {0}, Old Value: {1}, New Value {2}", key, m_singletonAppSettings[key], value));
                }
                catch (Exception ex)
                {
                    //Console.ForegroundColor = ConsoleColor.Red;
                    //Console.WriteLine("Error: SetAppSetting, Error: " + ex.Message);
                    //Console.ResetColor();
                    //EventsLogger.LogError("Error: SetAppSetting, Error: " + ex.ToString());
                }
            }
        }

        public static string GetMachineIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Environment.MachineName);
            string ip = string.Empty;
            if (addresses.Length > 1)
            {
                foreach (IPAddress ipAdd in addresses)
                {
                    ip = ip + string.Format(" [{0}] ", ipAdd.ToString());
                }
            }
            else
            {
                ip = Dns.GetHostAddresses(Environment.MachineName)[0].ToString();
            }
            return ip;
        }
    }
}
