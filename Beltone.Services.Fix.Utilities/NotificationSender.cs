using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Configuration;
using System.IO;
using System.Diagnostics;


namespace Beltone.Services.Fix.Utilities
{
    public static class NotificationSender
    {

        #region Declaration

        static private SmtpClient m_smtpClient;
        //static private NotifierConfigurations m_conf;

        #endregion

        #region Constructors

        /// <summary>
        /// Default SMTP server
        /// </summary>
        public static void Initialize()
        {
            //m_conf = LoadConfigurations();
            m_smtpClient = new SmtpClient(ConfigurationManager.AppSettings.Get("SMTPHostName"));
            int timeout = 1000;
            if (!int.TryParse(ConfigurationManager.AppSettings.Get("SMTPTimeout"), out timeout))
            {
                timeout = 1000;
            }
            m_smtpClient.Timeout = timeout;
            m_smtpClient.SendCompleted += new SendCompletedEventHandler(m_smtpClient_SendCompleted);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendAllOnce">false to send mail for each recipients</param>
        /// <param name="resendInFailure">try sending if failure occured</param>
        /// <param name="async">commit async process</param>
        /// <param name="from">sender mail</param>
        /// <param name="to">recipient mail</param>
        /// <param name="cc"></param>
        /// <param name="subject"></param>
        /// <param name="displayName"></param>
        /// <param name="messageBody">message text</param>
        /// <param name="attachedFile">stream that includes attached file contents</param>
        public static bool Send(bool sendAllOnce, bool resendInFailure, bool async, string from, string to, string[] cc, string subject, string displayName, string messageBody, Stream attachedFile)
        {
            //bool isFirstTime = false;
            try
            {
                MailMessage msg = new MailMessage();
                msg.Sender = new MailAddress(from);
                //msg.Priority = (MailPriority)Enum.Parse(typeof(MailPriority),ConfigurationManager.AppSettings.Get("SMTPPriority"));
                msg.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
                msg.From = new MailAddress(from);
                msg.Subject = subject;
                msg.Body = messageBody;
                if (attachedFile != null)
                {
                    //msg.Attachments.Add(new Attachment(attachedFile,));

                    msg.Attachments.Add(new Attachment(attachedFile, "ErrorScreenShot.bmp"));
                }
                if (sendAllOnce)
                {
                    if (cc != null && cc.Length > 0)
                    {
                        foreach (string rec in cc)
                        {
                            if (rec == string.Empty) { continue; }
                            msg.CC.Add(rec);
                        }
                    }
                    msg.To.Add(to);
                    if (async)
                    {
                        m_smtpClient.SendAsync(msg, null);
                    }
                    else
                    {
                        m_smtpClient.Send(msg);
                    }
                }
                else
                {
                    MailAddressCollection mac = new MailAddressCollection();
                    if (cc != null && cc.Length != 0)
                    {
                        foreach (string rec in cc)
                        {
                            mac.Add(rec);
                        }
                    }
                    mac.Add(to);
                    SendOneByOne(sendAllOnce, msg, mac);
                }
                return true;
            }
            catch(Exception ex)
            {
                SystemLogger.WriteOnConsoleAsync(true, string.Format("Error sending mail ({0}) : {1}", subject, ex.Message), ConsoleColor.Red, ConsoleColor.Black, true);
                StackFrame sf = new StackFrame(true);
                if (resendInFailure)
                {
                    //if (!isFirstTime)
                    //{
                    //    isFirstTime = true;
                    //    //Send(sendAllOnce, resendInFailure, async, from, to, cc, subject, displayName, messageBody, attachedFile);
                    //}
                    //else
                    //{
                    //    logError(sf, "error sending message");
                    //    return false;
                    //}
                }
                else
                {
                    logError(sf, "error sending message");
                }
                return false;
            }
        }

        #endregion

        #region Private Methods

        static private void SendOneByOne(bool async, MailMessage msg, MailAddressCollection mac)
        {
            msg.CC.Clear();
            foreach (MailAddress address in mac)
            {
                msg.To.Clear();
                msg.To.Add(address.Address);
                if (async)
                {
                    m_smtpClient.SendAsync(msg, null);
                }
                else
                {
                    m_smtpClient.Send(msg);
                }
            }
        }

        static private void m_smtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            // log
            if (e.Cancelled)
            {

            }
            else if (e.Error != null)
            {
                StackFrame sf = new StackFrame(true);
                // log
                sf = null;
            }
            else
            {

            }
        }

        //static private void LoadConfigurations()
        //{
        //    string filePath = (System.Windows.Forms.Application.StartupPath + @"\NotifierConf.xml");
        //    NotifierConfigurations conf = null; ;
        //    TextReader reader = null;
        //    TextWriter writer = null;
        //    XmlSerializer serializer = null;
        //    try
        //    {
        //        if (File.Exists(filePath))
        //        {
        //            reader = new StreamReader(filePath);
        //            serializer = new XmlSerializer(typeof(NotifierConfigurations));
        //            conf = (NotifierConfigurations)serializer.Deserialize(reader);
        //            reader.Close();
        //            reader = null;
        //            serializer = null;
        //        }
        //        else
        //        {
        //            writer = new StreamWriter(filePath);
        //            serializer = new XmlSerializer(typeof(NotifierConfigurations));
        //            conf = new NotifierConfigurations()
        //            {
        //                Body = "Notifier configuration of exceptions service needs data",
        //                CC = new string[] { "brokeragesupport1@efg-hermes.com", "relzawahry@efg-hermes.com" },
        //                DisplayName = "Brokerage Support Team",
        //                HostServer = "mail2003.efg-hermes.local",
        //                Recipient = "brokeragesupport@efg-hermes.com",
        //                Sender = "brokeragesupport1@efg-hermes.com",
        //                ServerType = "SMTP",
        //                Subject = "Service Error",
        //                Priority = MailPriority.Normal,
        //                Timeout = 30
        //            };
        //            serializer.Serialize(writer, conf);
        //            writer.Close();
        //            writer = null;
        //            serializer = null;
        //        }
        //    }
        //    catch
        //    {
        //        if (reader != null) { reader = null; }
        //        if (writer != null) { writer = null; }
        //        if (serializer != null) { serializer = null; }
        //    }
        //    return conf;
        //}

        static private void logError(StackFrame sf, string note)
        {

        }

        #endregion Private Methods

        #region IDisposable Members

        public static void Dispose()
        {
            m_smtpClient.SendCompleted -= new SendCompletedEventHandler(m_smtpClient_SendCompleted);
        }

        #endregion
    }
}
