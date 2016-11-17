//using Beltone.Services.MCDR.Contract.Entities.FromAdmin;
//using Beltone.Services.MCDR.Contract.Entities.ReqMsgs;
//using Beltone.Services.MCDR.Contract.Entities.ResMsgs;
//using Beltone.Services.MCDR.Contract.Entities.Shared;
//using Beltone.Services.MCDR.Contract.Entities.ToAdmin;
//using Beltone.Services.MCDR.Contract.Interfaces;
//using Beltone.Services.MCDR.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Data.SqlClient;
//using System.Drawing;
//using System.Linq;
//using System.Messaging;
//using System.ServiceModel;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace Beltone.Services.MCDR.TestWinClient
//{
//    public partial class Form1 : Form
//    {

//        CancellationTokenSource _cancelToken = new CancellationTokenSource();
//        bool _isConnectedToAdmin = false;
//        internal ServiceHost _host = null;
//        Guid _clientKey;
//        static bool _isSubscribed = false;
//        IMcdrAdmin _client;
//        Color _repColor = Color.Blue;

//        public Form1()
//        {
//            InitializeComponent();
//        }

//        private void Form1_Load(object sender, EventArgs e)
//        {
//            try
//            {
//                DataTable dt = new DataTable();
//                SqlDataAdapter da = new SqlDataAdapter("select * from stocks", SystemConfigurations.GetAppSetting("BasicDataDBConnectionString"));
//                da.Fill(dt);

//                foreach (DataRow row in dt.Rows)
//                    comboStocks.Items.Add(string.Format("{0}|{1}", row["NameEn"].ToString(), row["code"].ToString()));

//                this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
//                comboStocks.SelectedIndexChanged += comboStocks_SelectedIndexChanged;
//                comboStocks.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
//                comboStocks.AutoCompleteSource = AutoCompleteSource.ListItems;

//                new Thread(new ThreadStart(StartRecievingMsgs)).Start();
//            }
//            catch (Exception Ex)
//            {
//                MessageBox.Show(this, "Kalem Ramy we 2olo Error: " + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }

//        }

//        void SetReport(string msg, Color? color = null)
//        {
//            if (!color.HasValue)
//            {
//                if (_repColor == Color.Blue)
//                    _repColor = Color.Green;
//                else
//                    _repColor = Color.Blue;
//                color = _repColor;
//            }
//            tbReport.AppendText(msg, color.Value);
//            tbReport.AppendText(Environment.NewLine);
//            tbReport.AppendText(Environment.NewLine);
//            tbReport.SelectionStart = tbReport.Text.Length;
//            tbReport.ScrollToCaret();
//        }

//        public void StartRecievingMsgs()
//        {
//            MessageQueue m_msgSenderQueue = new MessageQueue(string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", SystemConfigurations.GetAppSetting("ResponseQueueIP"), SystemConfigurations.GetAppSetting("ResponseQueueName")));
//            XmlMessageFormatter formatter = new XmlMessageFormatter(new Type[] { typeof(Contract.Entities.FromAdmin.McdrTestReq), typeof(Contract.Entities.ResMsgs.AllocRes) });
//            //if (!m_msgSenderQueue.CanRead)
//            //{
//            //    // warning  
//            //}
//            while (true)
//            {
//                try
//                {
//                    //MessageQueueTransaction mqt = new MessageQueueTransaction();
//                    //mqt.Begin();
//                    System.Messaging.Message msg = (System.Messaging.Message)m_msgSenderQueue.Receive();
//                    if (msg == null)
//                    {
//                        continue;
//                    }

//                    object recievedMsg = formatter.Read(msg);

//                    this.BeginInvoke(new Action(() => SetReport(string.Format("Recieved {0}", recievedMsg.GetType().ToString()), Color.DarkGray)));

//                    if (recievedMsg.GetType() == typeof(McdrTestReq))
//                    {
//                        try
//                        {
//                            IMcdrToAdmin msgTo = new McdrTestRes() { TestKey = ((McdrTestReq)recievedMsg).TestKey };
//                            _client.HandleMsg(msgTo);
//                        }
//                        catch (Exception ex)
//                        {
//                            this.BeginInvoke(new Action(() => MessageBox.Show(ex.Message)));
//                        }
//                        this.BeginInvoke(new Action(() => SetReport("sent FixAdmin_TestResponse", Color.DarkGray)));
//                    }
//                    else if (recievedMsg.GetType() == typeof(AllocRes))
//                    {
//                        AllocRes resp = (AllocRes)recievedMsg;
//                        this.BeginInvoke(new Action(() => SetReport(string.Format("Alloc Resp [{0}]", resp.ResType), Color.DarkOrange)));
//                        if (resp.Fields != null)
//                            foreach (var f in resp.Fields)
//                                this.BeginInvoke(new Action(() => SetReport(string.Format("Operand [{0}]  Value [{1}]", f.Operand, f.Value), Color.DarkOrange)));

//                    }
//                }
//                catch (Exception ex)
//                {
//                    this.BeginInvoke(new Action(() => SetReport(string.Format("Error : {0}", ex.Message), Color.Red)));
//                    Thread.Sleep(2000);
//                }
//            }
//        }

//        private void OnIncomingMsg(IMcdrFromAdmin[] msgs)
//        {
//            foreach (var msg in msgs)
//            {
//                if (msg is McdrSessionUp)
//                {
//                    try
//                    {
//                        _clientKey = ((McdrSessionUp)msg).SessionKey;
//                        this.BeginInvoke(new Action(() =>
//                        {
//                            //MessageBox.Show(this, "Session up Key {0} " + clientKey, "Session up", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                            SetReport("Session Key : " + _clientKey.ToString(), Color.DarkGreen);
//                            SetSubscribed(true);
//                        }));

//                        //_client.UnsubscribeSession();  
//                    }
//                    catch (Exception ex)
//                    {
//                        this.BeginInvoke(new Action(() =>
//                        {
//                            SetReport(ex.Message, Color.Red);
//                        }));
//                    }
//                }
//            }
//        }


//        private void SetSubscribed(bool subDone)
//        {
//            if (subDone)
//            {
//                _isSubscribed = true;
//                this.BeginInvoke(new Action(() =>
//                {
//                    lblSub.ForeColor = Color.Green;
//                    lblSub.Text = "Subscribed";
//                    btnSub.BackColor = Color.Red;
//                    btnSub.Text = "Unsubscribe";
//                    btnSub.Enabled = true;
//                }));
//            }
//            else
//            {
//                _isSubscribed = false;
//                this.BeginInvoke(new Action(() =>
//                {
//                    lblSub.ForeColor = Color.Red;
//                    lblSub.Text = "Unsubscribed";
//                    btnSub.BackColor = Color.Green;
//                    btnSub.Text = "Subscribe";
//                    btnSub.Enabled = true;
//                }));
//            }
//        }

//        void Form1_FormClosing(object sender, FormClosingEventArgs e)
//        {
//            Environment.Exit(1);
//        }

//        void comboStocks_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            try
//            {
//                if (comboStocks.SelectedIndex < 0)
//                    return;
//                string value = comboStocks.SelectedItem.ToString();
//                string code = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[1];
//                lblCode.Text = code;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(ex.Message);
//            }
//        }


//        private void btnSub_Click(object sender, EventArgs e)
//        {
//            {
//                btnSub.Enabled = false;

//                IMcdrAdminCallback m_callbackHandler;
//                InstanceContext m_InstanceContext;
//                DuplexChannelFactory<IMcdrAdmin> m_factory;
//                ICallbackHandler.IncomingMessageDelegate m_IncomingMessageDelegate = new ICallbackHandler.IncomingMessageDelegate(OnIncomingMsg);
//                m_callbackHandler = new ICallbackHandler(m_IncomingMessageDelegate);
//                m_InstanceContext = new InstanceContext(m_callbackHandler);
//                m_factory = new DuplexChannelFactory<IMcdrAdmin>(m_InstanceContext, "netTcpBinding_IFixAdmin");
//                _client = m_factory.CreateChannel();
//                ((ICommunicationObject)_client).Closed += TcpClient_Closed;
//                ((ICommunicationObject)_client).Faulted += tcpClient_Faulted;
//                //for (int i = 0; i < 3; i++)
//                //{
//                try
//                {
//                    //FixAdminMsg returnMsg = _client.SubscribeSession(
//                    //   new LoginReq()
//                    //   {
//                    //       Username = "simpletrader",
//                    //       Password = "simplepassword",
//                    //       QueueName = SystemConfigurations.GetAppSetting("ResponseQueueName"),
//                    //       QueueIP = SystemConfigurations.GetAppSetting("ResponseQueueIP"),
//                    //       FlushUpdatesOffline = true,
//                    //   });

//                    McdrAdminMsg returnMsg = _client.Subscribe(
//                       new subReq()
//                       {
//                           QueueName = SystemConfigurations.GetAppSetting("ResponseQueueName"),
//                           QueueIP = SystemConfigurations.GetAppSetting("ResponseQueueIP"),
//                           Username = "Fix",
//                           Password = "Fix",
//                           FlushUpdatesOffline = true,
//                       });



//                    _isConnectedToAdmin = true;
//                    SetReport(returnMsg.Code + " " + returnMsg.Note ?? "");
//                    Task.Factory.StartNew(() => { while (_isConnectedToAdmin) { _client.Ping(); Thread.Sleep(3000); } }, _cancelToken.Token);
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show(ex.Message);
//                }
//                //}
//            }
//        }

//        void tcpClient_Faulted(object sender, EventArgs e)
//        {
//            _cancelToken.Cancel();
//            _isConnectedToAdmin = false;
//            this.BeginInvoke(new Action(() =>
//                MessageBox.Show(this, "Session Faulted", "Session Down", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
//        }

//        void TcpClient_Closed(object sender, EventArgs e)
//        {
//            _cancelToken.Cancel();
//            _isConnectedToAdmin = false;
//            this.BeginInvoke(new Action(() =>
//            MessageBox.Show(this, "Session Closed", "Session Down", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
//        }

//        public class ICallbackHandler : IMcdrAdminCallback
//        {
//            private IncomingMessageDelegate m_IncomingMessageDelegate;
//            public delegate void IncomingMessageDelegate(IMcdrFromAdmin[] msg);

//            public ICallbackHandler(IncomingMessageDelegate del)
//            {
//                m_IncomingMessageDelegate = del;
//            }

//            public void PushAdminMsg(IMcdrFromAdmin[] msgs)
//            {
//                if (m_IncomingMessageDelegate != null)
//                {
//                    m_IncomingMessageDelegate(msgs);
//                }
//            }
//        }

//        private void btnAllocate_Click(object sender, EventArgs e)
//        {
//            try
//            {
//                using (Proxy.McdrProxy p = new Proxy.McdrProxy())
//                {
//                    Guid reqID = Guid.NewGuid();
//                    List<OpVal> ops = new List<OpVal>();
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.ALLOC_QTY, Value = int.Parse(tbQty.Text) });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.EX_CODE, Value = tbMarket.Text });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.CUST_CODE, Value = tbCust.Text });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.BROKER_CODE, Value = tbBrokerCode.Text });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.UNI_CODE, Value = tbUniCode.Text });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.SEC_CODE, Value = lblCode.Text });
//                    ops.Add(new OpVal() { Operand = Contract.Constants.ALLOC_REQ_FIELDS.OP_TYP, Value = Contract.Constants.ALLOC_SIDE.SELL });
//                    p.Handle(new NewAllocReq() { SessionKey = _clientKey, ReqID = reqID, Fields = ops.ToArray() });
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(ex.Message);
//            }
//        }
//    }



//    public static class RichTextBoxExtensions
//    {
//        public static void AppendText(this RichTextBox box, string text, Color color)
//        {
//            box.SelectionStart = box.TextLength;
//            box.SelectionLength = 0;

//            box.SelectionColor = color;
//            box.AppendText(text);
//            box.SelectionColor = box.ForeColor;
//        }
//    }
//}


