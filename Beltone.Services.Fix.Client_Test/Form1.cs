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

namespace Beltone.Services.Fix.Client_Test
{

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

    public partial class Form1 : Form
    {
        Guid clientKey;
        bool isSubscribed = false;
        Color repColor = Color.Blue;

        public Form1()
        {
            //InitializeService();
            InitializeComponent();

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

                new Thread(new ThreadStart(StartRecievingMsgs)).Start();
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
                if(price <= 0 )
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

        }

        void rbMarket_CheckedChanged(object sender, EventArgs e)
        {
            panelPrice.Visible = rbLimit.Checked;
        }

        void comboStocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboStocks.SelectedIndex<0)
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
            MessageQueue m_msgSenderQueue = new MessageQueue(@".\private$\client");
            XmlMessageFormatter formatter =  new XmlMessageFormatter(new Type[] { typeof(Fix_OrderAcceptedResponse), typeof(SubscriberInitializationInfo), typeof(AreYouAlive), typeof(SubscriptionStatus), typeof(Fix_OrderRejectionResponse), typeof(Fix_ExecutionReport), typeof(LogOutResponse), typeof(LogOutResponse), typeof(Fix_BusinessMessageReject), typeof(Fix_OrderReplaceCancelReject) });
            if (!m_msgSenderQueue.CanRead)
            {
                // warning  
            }
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
                    if (recievedMsg.GetType() == typeof(SubscriptionStatus))
                    {
                        SubscriptionStatus status = (SubscriptionStatus)recievedMsg;
                        if (status.IsSubscribed)
                        {
                            clientKey = status.ClientKey;
                            this.BeginInvoke(new Action(() =>
                            { 
                                lblSub.ForeColor = Color.Green; 
                                lblSub.Text = "Subscribed";
                                btnSub.BackColor = Color.Red;
                                btnSub.Text = "Unsubscribe";
                                isSubscribed = true;
                                btnSub.Enabled = true;
                            }
                            ));
                        }
                        else
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                lblSub.ForeColor = Color.Red;
                                lblSub.Text = "Unsubscribed";
                                btnSub.BackColor = Color.Green;
                                btnSub.Text = "Subscribe";
                                isSubscribed = false;
                                btnSub.Enabled = true;
                            }
                            ));
                            //using (OrdersProxy proxy = new OrdersProxy())
                            //{
                            //    string path = string.Format(@"Formatname:DIRECT=TCP:{0}", SystemConfigurations.GetAppSetting("ResponseQueue"));
                            //    proxy.SubscribeSession(path);
                            //}
                        }
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_OrderAcceptedResponse))
                    { 
                        Fix_OrderAcceptedResponse resp = (Fix_OrderAcceptedResponse)recievedMsg;
                        this.BeginInvoke(new Action(()=> SetReport(string.Format("order : {0} accepted", resp.RequesterOrderID), Color.Green)));
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_ExecutionReport))
                    {
                        Fix_ExecutionReport resp = (Fix_ExecutionReport)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} TotalExecuted: {1}, TradeExecuted: {2}, Remaining {3}, Status : {4}", resp.RequesterOrderID, resp.TotalExecutedQuantity, resp.TradeExecutedQuantity, resp.RemainingQuantity, resp.OrderStatus),Color.Blue)));
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
                    else if (recievedMsg.GetType() == typeof(AreYouAlive))
                    {
                        AreYouAlive resp = (AreYouAlive)recievedMsg;
                        //m_msgSenderQueue.Send(new AmAlive() { ClientKey = clientKey });
                        using (OrdersProxy proxy = new OrdersProxy())
                        {
                            proxy.HandleRequest(new AmAlive() { ClientKey = clientKey });
                        }
                    }
                    else if (recievedMsg.GetType() == typeof(Fix_OrderReplacedResponse))
                    {
                        Fix_OrderReplacedResponse replace = (Fix_OrderReplacedResponse)recievedMsg;
                        this.BeginInvoke(new Action(() => SetReport(string.Format("order : {0} replaced : Quantity {1} price {2}", replace.RequesterOrderID, replace.Quantity, replace.Price), Color.DarkOrange)));
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error Sending Msg: " + ex.ToString());
                    Console.ResetColor();
                }
            }
        }

        internal  ServiceHost myServiceHost = null;

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
                        p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = Guid.NewGuid(), ClientID = int.Parse(tbClient.Text), CustodyID = tbCust.Text, OrderType = rbLimit.Checked ? "Limit" : "Market", Price = rbLimit.Checked ? double.Parse(tbPrice.Text) : 0, Quantity = int.Parse(tbQty.Text), SecurityID = lblCode.Text.Trim(), OrderSide = "Buy", TimeInForce = tbTIF.Text, DateTime = DateTime.Now, ExchangeID = tbMarket.Text, HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention });
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
                        p.HandleRequest(new NewSingleOrder() { ClientKey = clientKey, RequesterOrderID = Guid.NewGuid(), ClientID = int.Parse(tbClient.Text), CustodyID = tbCust.Text, OrderType = rbLimit.Checked ? "Limit" : "Market", Price = rbLimit.Checked ? double.Parse(tbPrice.Text) : 0, Quantity = int.Parse(tbQty.Text), SecurityID = lblCode.Text.Trim(), OrderSide = "Sell", TimeInForce = tbTIF.Text, DateTime = DateTime.Now, ExchangeID = tbMarket.Text, HandleInst = Contract.Enums.HandleInstruction.No_Broker_Invention });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                //using (OrdersProxy p = new OrdersProxy())
                //{
                //    p.HandleRequest(new ModifyCancelOrder() { ClientKey = clientKey, RequesterOrderID = buyRequesterOrderID, OrderType = "Limit", Price = 20.1, Quantity = 200, TimeInForce = "Day" });
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                //using (OrdersProxy p = new OrdersProxy())
                //{
                //    p.HandleRequest(new ModifyCancelOrder() { ClientKey = clientKey, RequesterOrderID = sellRequesterOrderID, OrderType = "Limit", Price = 20.1, Quantity = 300, TimeInForce = "Day" });
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSub_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show(this, "Subscribe to FIX orders service ?", "FIX Service", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                using (OrdersProxy proxy = new OrdersProxy())
                {
                    //string path = string.Format(@"{0}\private$\client", Environment.MachineName);
                    //string path = @"10.30.60.11\private$\client";
                    //string ip = SystemConfigurations.GetMachineIP();
                    
                    btnSub.Enabled = false;

                    if (!isSubscribed)
                    {
                        string path = string.Format(@"Formatname:DIRECT=TCP:{0}", SystemConfigurations.GetAppSetting("ResponseQueue"));
                        proxy.SubscribeSession(path);
                    }
                    else
                    {
                        proxy.UnsubscribeSession(clientKey);
                    }
                    //66A92E99-721E-47C2-B866-FA853E0AE17F
                    //proxy.ResubscribeMe(new Guid("57D42C89-2EF5-4AEA-9C67-8CE0F0A8088C"), @".\private$\client");
                }
            }
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