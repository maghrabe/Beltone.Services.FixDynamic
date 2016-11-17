using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Beltone.Services.Fix.Contract.Interfaces;
using System.ServiceModel;
using Beltone.Services.Fix.Contract.Entities.RequestMessages;
using Beltone.Services.Fix.Contract.Entities.ResponseMessages;
using Beltone.Services.Fix.Proxy;
using System.Messaging;
using System.Threading;
using Beltone.Services.Fix.DataLayer;
using Beltone.Services.Fix.Utilities;
using System.Data.SqlClient;
using Beltone.Services.Fix.Contract;
using Beltone.Services.Fix.Contract.Entities.FromAdminMsgs;
using Beltone.Services.Fix.Contract.Enums;
using Beltone.Services.Fix.Contract.Entities.ToAdminMsgs;
using System.Threading.Tasks;
using Beltone.Services.Fix.Contract.Constants;

namespace Beltone.Services.Fix.Client_Test
{

    public partial class frmTest : Form
    {
        Guid clientKey;
        static bool isSubscribed = false;
        IFixAdmin _client;
        Color repColor = Color.Blue;
        Guid requestGuid;

        public frmTest()
        {
            //InitializeService();
            InitializeComponent();
            requestGuid = new Guid("2fedef97-bb85-473b-9df0-0d74d8d63e71");
            tbRequestGuid.Text = requestGuid.ToString();

            try
            {
                DatabaseMethods db = new DatabaseMethods();
                DataTable dt = db.GetStocksDetails();


                foreach (DataRow row in dt.Rows)
                {
                    comboStocks.Items.Add(string.Format("{0}  |  {1}", row["NameEn"].ToString(), row["code"].ToString()));
                }

                this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
                comboStocks.SelectedIndexChanged += comboStocks_SelectedIndexChanged;
                rbMarket.CheckedChanged += rbMarket_CheckedChanged;
                comboStocks.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                comboStocks.AutoCompleteSource = AutoCompleteSource.ListItems;
                comboOrders.SelectedIndexChanged += comboOrders_SelectedIndexChanged;

                new Thread(new ThreadStart(StartRecievingMsgs)).Start();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, "Kalem Ramy we 2olo Error: " + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void comboOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                dataGridView1.DataSource = null;
                if (comboOrders.SelectedIndex == 0)
                {
                    DatabaseMethods db = new DatabaseMethods();
                    dataGridView1.DataSource = db.test_GetTodayOrders();
                    dataGridView1.Columns["IsActive"].Visible = false;
                    dataGridView1.Columns["IsExecuted"].Visible = false;
                    dataGridView1.Columns["IsCompleted"].Visible = false;
                }
                else if (comboOrders.SelectedIndex == 1)
                {
                    DatabaseMethods db = new DatabaseMethods();
                    dataGridView1.DataSource = db.test_GetTodayExecOrders();
                    dataGridView1.Columns["IsActive"].Visible = false;
                    dataGridView1.Columns["IsExecuted"].Visible = false;
                    dataGridView1.Columns["IsCompleted"].Visible = false;
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(this, "Kalem Ramy we 2olo Error: " + Ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        string IsValid()
        {
            if (rbLimit.Checked)
            {
                if (string.IsNullOrEmpty(tbPrice.Text))
                    return "Invalid Price";
                double price;
                if (!double.TryParse(tbPrice.Text, out price))
                    return "Invalid Price";
                if (price <= 0)
                    return "Invalid Price";
            }


            if (string.IsNullOrEmpty(tbQty.Text))
                return "Invalid Qty";
            int qty;
            if (!int.TryParse(tbQty.Text, out qty))
                return "Invalid Qty";
            if (qty <= 0)
                return "Invalid Qty";

            if (string.IsNullOrEmpty(tbClient.Text))
                return "Invalid Client ID";
            int client;
            if (!int.TryParse(tbClient.Text, out client))
                return "Invalid Client ID";
            if (client <= 0)
                return "Invalid Client ID";

            if (string.IsNullOrEmpty(tbCust.Text))
                return "Invalid Custody ID";

            if (string.IsNullOrEmpty(lblCode.Text) || lblCode.Text == "No ISIN Code")
                return "Invalid ISIN";

            return "valid";
        }

        void SetReport(string msg, Color? color = null)
        {
            if (!color.HasValue)
            {
                if (repColor == Color.Blue)
                    repColor = Color.Green;
                else
                    repColor = Color.Blue;
                color = repColor;
            }
            tbReport.AppendText(msg, color.Value);
            tbReport.AppendText(Environment.NewLine);
            tbReport.AppendText(Environment.NewLine);
            tbReport.SelectionStart = tbReport.Text.Length;
            tbReport.ScrollToCaret();
        }

        void rbMarket_CheckedChanged(object sender, EventArgs e)
        {
            panelPrice.Visible = rbLimit.Checked;
        }

        void comboStocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboStocks.SelectedIndex < 0)
                return;
            string value = comboStocks.SelectedItem.ToString();
            string code = value.Split('|')[1];
            lblCode.Text = code;
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(1);
        }

        public void StartRecievingMsgs()
        {
            // MessageQueue m_msgSenderQueue = new MessageQueue(string.Format(@"{0}\private$\{1}", SystemConfigurations.GetAppSetting("ResponseQueueIP"), SystemConfigurations.GetAppSetting("ResponseQueueName")));
            MessageQueue m_msgSenderQueue = new MessageQueue(string.Format(@"Formatname:DIRECT=TCP:{0}\private$\{1}", SystemConfigurations.GetAppSetting("ResponseQueueIP"), SystemConfigurations.GetAppSetting("ResponseQueueName")));
            //List<Type> types = System.Reflection.Assembly.Load("Beltone.Services.Fix.Contract").GetTypes().Where(x=>x.IsClass).ToList();
            //types.Add(typeof(Dictionary<string, object>));
            //XmlMessageFormatter formatter = new XmlMessageFormatter(types.ToArray());

            XmlMessageFormatter formatter = new XmlMessageFormatter
                (new Type[] {typeof(Fix_OrderReplaceRefusedByService), typeof(FixAdminMsg), typeof(FixAdmin_TestRequest),
                    typeof(Fix_OrderAcceptedResponse), typeof(Fix_OrderRejectionResponse), typeof(Fix_ExecutionReport),
                    typeof(LogOutResponse), typeof(LogOutResponse), typeof(Fix_BusinessMessageReject), typeof(Fix_OrderReplaceCancelReject) ,
                    typeof(Fix_OrderReplacedResponse),
                    typeof(Fix_PendingReplaceResponse) ,typeof(Fix_OrderRefusedByService),typeof(Fix_PendingNewResponse),typeof(Fix_PendingCancelResponse),
                    typeof(Fix_OrderCanceledResponse)
                    
                
                
                });

            //if (!m_msgSenderQueue.CanRead)
            //{
            //    // warning  
            //}
            while (true)
            {
                try
                {
                    //MessageQueueTransaction mqt = new MessageQueueTransaction();
                    //mqt.Begin();
                    System.Messaging.Message msg = (System.Messaging.Message)m_msgSenderQueue.Receive();
                    if (msg == null)
                    {
                        continue;
                    }

                    object recievedMsg = formatter.Read(msg);

                    this.BeginInvoke(new Action(() => SetReport(string.Format("Recieved {0}", recievedMsg.GetType().ToString()), Color.DarkGray)));

                    if (recievedMsg.GetType() == typeof(FixAdmin_TestRequest))
                    {
                        try
                        {
                            IToAdminMsg msgTo = new FixAdmin_TestResponse() { TestKey = ((FixAdmin_TestRequest)recievedMsg).TestKey };
                            _client.HandleMsg(msgTo);
                            //_client.HandleRequest(new FixAdmin_TestResponse() { TestKey = "" });
                        }
                        catch (Exception ex)
                        {
                            this.BeginInvoke(new Action(() => MessageBox.Show(ex.Message)));
                        }
                        this.BeginInvoke(new Action(() => SetReport("sent FixAdmin_TestResponse", Color.DarkGray)));
                    }
                    if (recievedMsg.GetType() == typeof(Fix_OrderAcceptedResponse))
                    {
                        Fix_OrderAcceptedResponse resp = (Fix_OrderAcceptedResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} accepted", resp.ReqOrdID), Color.Green)));
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_ExecutionReport))
                    {
                        Fix_ExecutionReport resp = (Fix_ExecutionReport)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} TotalExecuted: {1}, TradeExecuted: {2}, Remaining {3}, Status : {4}", resp.RequesterOrderID, resp.TotalExecutedQuantity, resp.TradeExecutedQuantity, resp.RemainingQuantity, resp.OrderStatus), Color.Blue)));
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_OrderSuspensionResponse))
                    {
                        Fix_OrderSuspensionResponse resp = (Fix_OrderSuspensionResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} suspended : {1}", resp.RequesterOrderID, resp.Message), Color.Red)));
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_OrderRejectionResponse))
                    {
                        Fix_OrderRejectionResponse resp = (Fix_OrderRejectionResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} rejected : {1}", resp.RequesterOrderID, resp.RejectionReason), Color.Red)));
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_OrderReplacedResponse))
                    {
                        Fix_OrderReplacedResponse replace = (Fix_OrderReplacedResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} replaced : Quantity {1} price {2}", replace.ReqOrdID, replace.Qty, replace.Prc), Color.DarkGreen)));
                    }

                    else if (recievedMsg.GetType() == typeof(Fix_OrderRefusedByService))
                    {
                        Fix_OrderRefusedByService refuseMsg = (Fix_OrderRefusedByService)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} refused by service : msessage : {1} ", refuseMsg.RequesterOrderID, refuseMsg.RefuseMessage), Color.DarkOrange)));
                    }

                    else if (recievedMsg.GetType() == typeof(Fix_OrderCanceledResponse))
                    {
                        Fix_OrderCanceledResponse msgCanceled = (Fix_OrderCanceledResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} canceld successfully ! ", msgCanceled.RequesterOrderID), Color.DarkOrange)));
                    }


                    else if (recievedMsg.GetType() == typeof(Fix_PendingNewResponse) || recievedMsg.GetType() == typeof(Fix_PendingReplaceResponse) || recievedMsg.GetType() == typeof(Fix_PendingCancelResponse))
                    {

                        this.BeginInvoke(new Action(() => SetReport(string.Format("order pending"), Color.DarkOrange)));
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() => SetReport(string.Format("Error : {0}", ex.Message), Color.Red)));
                    Thread.Sleep(2000);
                }
            }
        }

        private void SetSubscribed(bool subDone)
        {
            if (subDone)
            {
                isSubscribed = true;
                this.BeginInvoke(new Action(() =>
                {
                    lblSub.ForeColor = Color.Green;
                    lblSub.Text = "Subscribed";
                    btnSub.BackColor = Color.Red;
                    btnSub.Text = "Unsubscribe";
                    btnSub.Enabled = true;
                }));
            }
            else
            {
                isSubscribed = false;
                this.BeginInvoke(new Action(() =>
                {
                    lblSub.ForeColor = Color.Red;
                    lblSub.Text = "Unsubscribed";
                    btnSub.BackColor = Color.Green;
                    btnSub.Text = "Subscribe";
                    btnSub.Enabled = true;
                }));
            }
        }

        internal ServiceHost myServiceHost = null;

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isSubscribed)
            {
                MessageBox.Show(this, "ya 3am subscribe first!", "Subscription Required", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            try
            {
                string result = IsValid();
                if (result != "valid")
                {
                    MessageBox.Show(this, result, "Invalid Order", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                if (MessageBox.Show(this, "Send Order?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
                    return;

                using (OrdersProxy p = new OrdersProxy())
                {//EGMFC003PX11
                    //p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = buyRequesterOrderID, ClientID = 1001, CustodyID = "4508", OrderType = "Limit", Price = 18, Quantity = 100, SecurityID = "EGS48031C016", OrderSide = "Buy", TimeInForce = "Day", CurrencyCode = "EGP", DateTime = DateTime.Now, ExchangeID = "CA" });
                    //p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = buyRequesterOrderID, ClientID = 3001, CustodyID = "4999", OrderType = "Limit", Price = 20, Quantity = 10, SecurityID = "EGS48031C016", OrderSide = "Buy", TimeInForce = "Day", DateTime = DateTime.Now, ExchangeID = "CA", HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention });
                    for (int i = 0; i < nudRepeat.Value; i++)
                    {

                        Dictionary<string, object> optionalParams = new Dictionary<string, object>();
                        //optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                        //optionalParams.Add("UnifiedCode", "2079333");
                        Guid reqGuid = Guid.NewGuid();
                        tbRequestGuid.Text = reqGuid.ToString();
                        p.Handle(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = reqGuid, ClientID = int.Parse(tbClient.Text), CustodyID = tbCust.Text, OrderType = rbLimit.Checked ? "Limit" : "Market", Price = rbLimit.Checked ? double.Parse(tbPrice.Text) : 0, Quantity = int.Parse(tbQty.Text), SecurityID = lblCode.Text.Trim(), OrderSide = "Buy", TimeInForce = combTimeInforce.Text.Trim() , DateTime = DateTime.Now, ExchangeID = tbMarket.Text, HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention,ExpirationDateTime = DateTime.Now.AddDays(3), OptionalParam = optionalParams });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //public class IOrdersCallbackHandler : IOrdersCallback
        //{

        //    #region IOrdersCallback Members

        //    public void PushUpdates(object msg)
        //    {
        //        if (msg.GetType() == typeof(Rejection))
        //        {
        //            MessageBox.Show(string.Format("Order: {0} Rejected: {1}", ((Rejection)msg).RequesterOrderID, ((Rejection)msg).RejectionReason));
        //        }
        //        if (msg.GetType() == typeof(OrderStatusResponse))
        //        {
        //            OrderStatusResponse status = (OrderStatusResponse)msg;
        //            MessageBox.Show(string.Format("Order: {0} Status: {1} Message: {2}",status.RequesterOrderID, status.OrderStatus.ToString(), status.Message));
        //        }
        //        else if (msg.GetType() == typeof(string))
        //        {
        //            MessageBox.Show((msg.ToString()));
        //        }
        //    }

        //    #endregion
        //}

        private void button2_Click(object sender, EventArgs e)
        {

            if (!isSubscribed)
            {
                MessageBox.Show(this, "ya 3am subscribe first!", "Subscription Required", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                string result = IsValid();
                if (result != "valid")
                {
                    MessageBox.Show(this, result, "Invalid Order", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }


                if (MessageBox.Show(this, "Send Order?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Cancel)
                    return;


                using (OrdersProxy p = new OrdersProxy())
                {                                                                                                                                   //2183732
                    //p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = sellRequesterOrderID, OrderType = "Limit", ClientID = 9001, CustodyID = "4508", Price = 35, Quantity = 100, SecurityID = "EGMFC003PX11", OrderSide = "Sell", TimeInForce = "Day" });
                    //p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = sellRequesterOrderID, OrderType = "Limit", ClientID = 3001, CustodyID = "4999", Price = 20, Quantity = 10, SecurityID = "EGS48031C016", OrderSide = "Sell", TimeInForce = "Day", DateTime = DateTime.Now, ExchangeID = "CA" });
                    for (int i = 0; i < nudRepeat.Value; i++)
                    {
                        Guid reqGuid = Guid.NewGuid();
                        tbRequestGuid.Text = reqGuid.ToString();


                        Dictionary<string, object> optionalParams = new Dictionary<string, object>();



                        string allocType = comboBoxAllocationType.SelectedItem.ToString();

                        switch (allocType)
                        {
                            case "Regular":
                                optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                                break;

                            case "SameDay":
                                optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.SAMEDAY);
                                break;
                            case "SameDay_Plus":
                                optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.SAMEDAYPLUS);
                                break;
                            default:
                                break;
                        }
                        
                        optionalParams.Add("UnifiedCode", txtUnifiedCode.Text.Trim());

                        p.Handle(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = reqGuid, ClientID = int.Parse(tbClient.Text), CustodyID = tbCust.Text, OrderType = rbLimit.Checked ? "Limit" : "Market", Price = rbLimit.Checked ? double.Parse(tbPrice.Text) : 0, Quantity = int.Parse(tbQty.Text), SecurityID = lblCode.Text.Trim(), OrderSide = "Sell", TimeInForce = combTimeInforce.Text.Trim(), DateTime = DateTime.Now, ExchangeID = tbMarket.Text, HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention, OptionalParam = optionalParams });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void PlaceSellOrderWithAllocation()
        {

            using (OrdersProxy p = new OrdersProxy())
            {


                Guid reqGuid = Guid.NewGuid();  // request orderid created by client.

                Dictionary<string, object> optionalParams = new Dictionary<string, object>();

                string allocType = comboBoxAllocationType.SelectedItem.ToString();

                optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR); // to allocate t+2   
                // or   // optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.SAMEDAY); // to allocate t+0
                // or //  optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.SAMEDAYPLUS); // to allocate same day plus.
                // client UnifiedCode is mandatory.                 
                optionalParams.Add("UnifiedCode", txtUnifiedCode.Text.Trim());

                p.Handle(new NewSingleOrder()
                {
                    ClientKey = clientKey, // session Guid key
                    RequesterOrderID = reqGuid,  // unique orderid GUID  
                    ClientID = int.Parse(tbClient.Text), // Bimsi ClientID 
                    CustodyID = "4603",
                    OrderType = "Limit", // or "Market" 
                    Price = 212.5,
                    Quantity = 100,
                    SecurityID = "EGB48011G026",
                    OrderSide = "Sell",
                    TimeInForce = "Day",
                    DateTime = DateTime.Now,
                    ExchangeID = "CA",
                    HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention,
                    ExpirationDateTime = DateTime.Now,  // order expiry dayte
                    OptionalParam = optionalParams
                });

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Sell Modfiy
            try
            {
                using (OrdersProxy p = new OrdersProxy())
                {
                    //Dictionary<string, object> optionalParams = new Dictionary<string, object>();
                    //optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                    //optionalParams.Add("UnifiedCode", "2079333");

                    Guid reqGuid = new Guid(tbRequestGuid.Text);
                   

                    p.Handle(new ModifyCancelOrder() { ClientKey = clientKey, OrderType = "Limit", Price = double.Parse(tbPrice.Text), Quantity = int.Parse(tbQty.Text), RequesterOrderID = reqGuid, TimeInForce = "Day", OptionalParam = null });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Sell Modfiy
            try
            {
                using (OrdersProxy p = new OrdersProxy())
                {
                    //Dictionary<string, object> optionalParams = new Dictionary<string, object>();
                    //optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                    //optionalParams.Add("UnifiedCode", "2079333");

                    Guid reqGuid = new Guid(tbRequestGuid.Text);

                    p.Handle(new ModifyCancelOrder() { ClientKey = clientKey, OrderType = "Limit", Price = double.Parse(tbPrice.Text), Quantity = int.Parse(tbQty.Text), RequesterOrderID = reqGuid, TimeInForce = "Day", OptionalParam = null });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnCancelSellOrder_Click(object sender, EventArgs e)
        {
            try
            {
                using (OrdersProxy p = new OrdersProxy())
                {
                    Dictionary<string, object> optionalParams = new Dictionary<string, object>();
                    //optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                    //optionalParams.Add("UnifiedCode", "2079333");

                    Guid reqGuid = new Guid(tbRequestGuid.Text);

                    p.Handle(new CancelSingleOrder() { ClientKey = clientKey, OptionalParam = optionalParams, RequesterOrderID = reqGuid });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnIncomingMsg(IFromAdminMsg[] msgs)
        {
            foreach (IFromAdminMsg msg in msgs)
            {
                if (msg is FixAdmin_MarketStatus)
                {
                    FixAdmin_MarketStatus status = (FixAdmin_MarketStatus)msg;
                    this.BeginInvoke(new Action(() =>
                    {
                        SetReport(string.Format("Market: {0}", status.IsConnected ? "Connected" : "Disconnected"), status.IsConnected ? Color.Green : Color.Red);
                    }));
                }
                else if (msg is FixAdmin_SessionUp)
                {
                    clientKey = ((FixAdmin_SessionUp)msg).SessionKey;
                    this.BeginInvoke(new Action(() =>
                    {
                        //MessageBox.Show(this, "Session up Key {0} " + clientKey, "Session up", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetSubscribed(true);
                    }));
                    try
                    {
                        //_client.UnsubscribeSession();  
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            SetReport(ex.Message, Color.Red);
                        }));
                    }
                }
            }
        }

        CancellationTokenSource tokenSource = new CancellationTokenSource();
        bool isConnectedToAdmin = false;
        private void btnSub_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show(this, "Subscribe to FIX orders service ?", "FIX Service", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                using (OrdersProxy proxy = new OrdersProxy())
                {
                    btnSub.Enabled = false;

                    ICallbackHandler m_callbackHandler;
                    InstanceContext m_InstanceContext;
                    DuplexChannelFactory<IFixAdmin> m_factory;
                    ICallbackHandler.IncomingMessageDelegate m_IncomingMessageDelegate = new ICallbackHandler.IncomingMessageDelegate(OnIncomingMsg);
                    m_callbackHandler = new ICallbackHandler(m_IncomingMessageDelegate);
                    m_InstanceContext = new InstanceContext(m_callbackHandler);
                    m_factory = new DuplexChannelFactory<IFixAdmin>(m_InstanceContext, "netTcpBinding_IFixAdmin");
                    _client = m_factory.CreateChannel();
                    ((ICommunicationObject)_client).Closed += TcpClient_Closed;
                    ((ICommunicationObject)_client).Faulted += tcpClient_Faulted;
                    //for (int i = 0; i < 3; i++)
                    //{
                    try
                    {
                        FixAdminMsg returnMsg = _client.Subscribe(
                           new subReq()
                           {
                               Username = "maghrabi",
                               Password = "123",
                               QueueName = SystemConfigurations.GetAppSetting("ResponseQueueName"),
                               QueueIP = SystemConfigurations.GetAppSetting("ResponseQueueIP"),
                               FlushUpdatesOffline = true,
                           });

                        //FixAdminMsg returnMsg = _client.Resubscribe(
                        //   new ResupReq()
                        //   {
                        //       SessionKey = new Guid("6D0AC300-CC63-4F34-81B6-B4E6DB358DC6"),
                        //       NewQueue = true,
                        //       QueueName = SystemConfigurations.GetAppSetting("ResponseQueueName"),
                        //       QueueIP = SystemConfigurations.GetAppSetting("ResponseQueueIP"),
                        //       Username = "sessionplayer",
                        //       Password = "fixpassword",
                        //       FlushUpdatesOffline = true,
                        //   });

                        isConnectedToAdmin = true;
                        SetReport(returnMsg.Code + " " + returnMsg.Note ?? "");
                        //   Task.Factory.StartNew(() => { while (isConnectedToAdmin) { _client.Ping(); Thread.Sleep(3000); } }, tokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    //}
                }
            }
        }

        void tcpClient_Faulted(object sender, EventArgs e)
        {
            tokenSource.Cancel();
            isConnectedToAdmin = false;
            this.BeginInvoke(new Action(() =>
                MessageBox.Show(this, "Session Faulted", "Session Down", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        }

        void TcpClient_Closed(object sender, EventArgs e)
        {
            tokenSource.Cancel();
            isConnectedToAdmin = false;
            this.BeginInvoke(new Action(() =>
            MessageBox.Show(this, "Session Closed", "Session Down", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        }

        private void frmTest_Load(object sender, EventArgs e)
        {
            comboBoxAllocationType.SelectedIndex = 0;
            //if(ChkBoxInitilizeSessions.Checked)
            //{
            //    DatabaseMethods db = new DatabaseMethods();
            //  db.InitSessions();
            //}
        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void tbRequestGuid_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnCancelBuy_Click(object sender, EventArgs e)
        {
            try
            {
                using (OrdersProxy p = new OrdersProxy())
                {
                    Dictionary<string, object> optionalParams = new Dictionary<string, object>();
                    //optionalParams.Add("ALLOC_TYPE", ALLOC_TYPE.REGULAR);
                    //optionalParams.Add("UnifiedCode", "2079333");

                    Guid reqGuid = new Guid(tbRequestGuid.Text);

                    p.Handle(new CancelSingleOrder() { ClientKey = clientKey, OptionalParam = optionalParams, RequesterOrderID = reqGuid });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }

    public class ICallbackHandler : IFixAdminCallback
    {
        private IncomingMessageDelegate m_IncomingMessageDelegate;
        public delegate void IncomingMessageDelegate(IFromAdminMsg[] msg);

        public ICallbackHandler(IncomingMessageDelegate del)
        {
            m_IncomingMessageDelegate = del;
        }

        public void PushAdminMsg(IFromAdminMsg[] msgs)
        {
            if (m_IncomingMessageDelegate != null)
            {
                m_IncomingMessageDelegate(msgs);
            }
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }

    //[ServiceContract(SessionMode = SessionMode.Required)]
    //public interface IFixMessageAcceptor
    //{
    //    [OperationContract(IsOneWay = true)]
    //    void RecieveMessage(IResponseMessage msg);
    //}


    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    //public class MyService : IFixMessageAcceptor
    //{

    //    #region IFixMessageAcceptor Members

    //    public void RecieveMessage(IResponseMessage msg)
    //    {
    //         if (msg.GetType() == typeof(Rejection))
    //         {
    //             MessageBox.Show(string.Format("Order: {0} Rejected: {1}", ((Rejection)msg).RequesterOrderID, ((Rejection)msg).RejectionReason));
    //         }
    //         if (msg.GetType() == typeof(OrderStatusResponse))
    //         {
    //             OrderStatusResponse status = (OrderStatusResponse)msg;
    //             MessageBox.Show(string.Format("Order: {0} Status: {1} Message: {2}", status.RequesterOrderID, status.OrderStatus.ToString(), status.Message));
    //         }
    //         else if (msg.GetType() == typeof(string))
    //         {
    //             MessageBox.Show((msg.ToString()));
    //         }
    //         else if (msg.GetType() == typeof(SubscriptionStatus))
    //         {
 
    //         }
    //    }

    //    #endregion
    //}

    //public class MyProxy : ClientBase<IFixMessageAcceptor>, IFixMessageAcceptor
    //{
    //    #region IFixMessageAcceptor Members

    //    public void RecieveMessage(IResponseMessage msg)
    //    {
    //        this.Channel.RecieveMessage(msg);
    //    }

    //    #endregion
    //}


}