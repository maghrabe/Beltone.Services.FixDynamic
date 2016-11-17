using Beltone.Services.Fix.Entities.Entities;
using Beltone.Services.Fix.Service.Entities;
using Beltone.Services.Fix.Service.Singletons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Beltone.Services.Fix.Service.GUI
{
    public partial class frmMonitor : Form
    {

        delegate void GUIInvoker(string value);
        delegate void GUIControlValueInvoker(Control btn, string property, object value);

        bool m_LoadDone = false;
        public frmMonitor()
        {
            InitializeComponent();


            Dictionary<string, Stock> stks = StocksDefinitions.GetStocks();

            comboStocksQueryOrders.DataSource = stks.Values.ToList();
            comboStocksQueryOrders.DisplayMember = "NameEn";
            comboStocksQueryOrders.ValueMember = "Code";
            comboStocksQueryOrders.SelectedIndex = -1;

            m_LoadDone = true;
            gridQueryOrders.DataError += gridQueryOrders_DataError;
            gridSearchOrder.DataError += gridSearchOrder_DataError;
            gridSessions.DataError += gridSessions_DataError;
            this.FormClosing += frmMonitor_FormClosing;
            this.WindowState = FormWindowState.Minimized;
        }

        void frmMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
        }

        void gridSessions_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        void gridSearchOrder_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        void gridQueryOrders_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            if (chkFilter.Checked)
            {
                tbFilter.Visible = true;
            }
            else
            {
                tbFilter.Visible = false;
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                listBox1.Items.Clear();
                ChangeControlValue(btnBrowse, "Enabled", false);
                ChangeControlValue(btnLoad, "Enabled", false);
                Thread t = new Thread(new ThreadStart(StartReading)); t.IsBackground = true; t.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void StartReading()
        {
            try
            {
                //string filePath = @"D:\Documents and Settings\relzawahry.EFG-HERMES\Desktop\logger.txt";
                string filePath = string.Empty;
                long totalLength = 0;
                int linesCount = 0;
                ChangeControlValue(lblLinesCount, "Text", linesCount.ToString());
                if (rbBrowseLog.Checked)
                {
                    filePath = lblFilePath.Text;
                    FileInfo f = new FileInfo(filePath);
                    totalLength = f.Length;
                }
                else
                {
                    string currentLogsPath = Application.StartupPath + @"\logDirectory";
                    DirectoryInfo di = new DirectoryInfo(currentLogsPath);
                    FileInfo[] files = di.GetFiles("*.txt");
                    foreach (FileInfo fi in files)
                    {
                        if (fi.Name.Contains(DateTime.Now.ToString("yyyy-MM-dd")))
                        {
                            filePath = fi.FullName;
                            totalLength = fi.Length;
                            ChangeControlValue(lblFileLength, "Text", totalLength.ToString());
                            ChangeControlValue(lblFilePath, "Text", fi.FullName);
                            break;
                        }
                    }
                }

                if (filePath == string.Empty)
                {
                    MessageBox.Show("File not found !");
                    return;
                }
                int totalSteps = int.Parse((totalLength / (1000)).ToString());
                ChangeControlValue(pBLog, "Maximum", totalSteps);
                ChangeControlValue(pBLog, "Value", 0);


                StreamReader file = new StreamReader(filePath, System.Text.Encoding.GetEncoding(1256));
                string line = string.Empty;
                long readed = 0;
                int count = 0;
                if (chkFilter.Checked)
                {
                    string[] filters = tbFilter.Text.ToLower().Split(',');
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line == null || line == string.Empty) { continue; }
                        string lineToRead = line.ToLower();
                        foreach (string filter in filters)
                            if (lineToRead.Contains(filter.ToLower()))
                            {
                                linesCount++;
                                AddToList(line);
                                readed += line.Length;
                                if (readed >= 1024)
                                {
                                    if (count <= totalSteps)
                                    {
                                        ChangeControlValue(pBLog, "Value", count++);
                                    }
                                    readed = 0;
                                }
                                break;
                            }
                    }
                }
                else
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line == null || line == string.Empty) { continue; }
                        linesCount++;
                        AddToList(line);
                        readed += line.Length;
                        if (readed >= 1024)
                        {
                            if (count <= totalSteps)
                            {
                                ChangeControlValue(pBLog, "Value", count++);
                            }
                            readed = 0;
                        }
                    }
                }
                ChangeControlValue(btnBrowse, "Enabled", true);
                ChangeControlValue(btnLoad, "Enabled", true);
                ChangeControlValue(pBLog, "Value", totalSteps);
                ChangeControlValue(lblLinesCount, "Text", linesCount.ToString());
                MessageBox.Show("Done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ChangeControlValue(btnBrowse, "Enabled", true);
                ChangeControlValue(btnLoad, "Enabled", true);
            }
        }

        private void AddToList(string line)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new GUIInvoker(InvokeToList), line);
                }
                else
                {
                    listBox1.Items.Add(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InvokeToList(string line)
        {
            listBox1.Items.Add(line);
        }

        private void ChangeControlValue(Control control, string property, object value)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new GUIControlValueInvoker(InvokeControlPropertyValue), control, property, value);
                }
                else
                {
                    InvokeControlPropertyValue(control, property, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InvokeControlPropertyValue(Control control, string property, object value)
        {
            try
            {
                PropertyInfo propInfo = control.GetType().GetProperty(property);
                propInfo.SetValue(control, value, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                openFileDialog1.InitialDirectory = Application.StartupPath + @"\logDirectory";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog1.FileName != string.Empty)
                    {
                        lblFilePath.Text = openFileDialog1.FileName;
                        FileInfo f = new FileInfo(openFileDialog1.FileName);
                        lblFileLength.Text = f.Length.ToString();
                        listBox1.Items.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void rbBrowseLog_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                listBox1.Items.Clear();
                if (!rbBrowseLog.Checked)
                {
                    label1.Visible = false;
                    btnBrowse.Visible = false;
                    lblFilePath.Visible = false;
                    lblFileLength.Visible = false;
                }
                else
                {
                    label1.Visible = true;
                    btnBrowse.Visible = true;
                    lblFilePath.Visible = true;
                    lblFileLength.Visible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnRefreshTHreads_Click(object sender, EventArgs e)
        {
            try
            {
                lbProcesses.Items.Clear();
                Process p;
                if (cbCurrProc.Checked)
                    p = Process.GetCurrentProcess();
                else
                    p = Process.GetProcessById(int.Parse(tbProcID.Text));
                tbProcDefinition.Text = string.Format("{0}{1}PagedMemorySize64:{2}{1}NonpagedSystemMemorySize64:{3}{1}Threads Count:{4}{1}VirtualMemorySize64:{5}{1}",
                p.ProcessName, Environment.NewLine, p.PagedMemorySize64, p.NonpagedSystemMemorySize64, p.Threads.Count, p.VirtualMemorySize64);
                foreach (ProcessThread pt in p.Threads)
                {
                    lbProcesses.Items.Add(string.Format("ID:{1}{0}TotalProcessorTime:{2}{0}UserProcessorTime:{2}{0}PrivilegedProcessorTime:{3}",
                        "  |  ", pt.Id, pt.TotalProcessorTime, pt.UserProcessorTime, pt.PrivilegedProcessorTime));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString());
            }
        }

        private void btnSearchOrder_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                gridSearchOrder.DataSource = null;
                SingleOrder someOrder = null;
                if (rbSearchOrderByOrderID.Checked)
                {
                    someOrder = OrdersManager.GetOrder(long.Parse(tbSearchOrder.Text));
                }
                else if (rbSearchOrderByReqOrderID.Checked)
                {
                    someOrder = OrdersManager.GetOrder(new Guid(tbSearchOrder.Text));
                }
                else if (rbSearchByBourseOrdID.Checked)
                {
                    someOrder = OrdersManager.GetOrder(tbSearchOrder.Text);
                }
                else if (rbSearchByClOrdID.Checked)
                {
                    someOrder = OrdersManager.GetOrder(tbSearchOrder.Text, false);
                }


                if (someOrder != null)
                {
                    lock (someOrder)
                    {
                        DataTable dt = new DataTable();
                        foreach (string probName in someOrder.Data.Keys)
                        {
                            dt.Columns.Add(probName);
                        }
                        DataRow row = dt.NewRow();
                        foreach (KeyValuePair<string, object> kvp in someOrder.Data)
                        {
                            row[kvp.Key] = kvp.Value;
                        }
                        dt.Rows.Add(row);
                        gridSearchOrder.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboStocksQueryOrders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                gridQueryOrders.DataSource = null;
                if (comboStocksQueryOrders.SelectedIndex > -1)
                {
                    //gridOrders.DataSource = comboMBO.SelectedItem;
                    string code = comboStocksQueryOrders.SelectedValue.ToString();
                    List<SingleOrder> orders = null;

                    if (rbBuys.Checked)
                    {
                        orders = OrdersManager.monitor_GetOrders("Buy", code);
                    }
                    else
                    {
                        orders = OrdersManager.monitor_GetOrders("Sell",code);
                    }

                    if (orders != null && orders.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        foreach (string probName in orders[0].Data.Keys)
                        {
                            dt.Columns.Add(probName);
                        }

                        foreach (SingleOrder order in orders)
                        {
                            DataRow row = dt.NewRow();
                            foreach (KeyValuePair<string, object> kvp in order.Data)
                            {
                                row[kvp.Key] = kvp.Value;
                            }
                            dt.Rows.Add(row);
                        }
                        gridQueryOrders.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private void rbBuys_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void rbSells_CheckedChanged(object sender, EventArgs e)
        {
            comboStocksQueryOrders_SelectedIndexChanged(null, null);
        }

        private void btnRefreshSessions_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                gridSessions.DataSource = null;
                List<SessionInfo> sessions = Sessions.monitor_GetSession(rbOnlineSessions.Checked);
                gridSessions.DataSource = sessions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnSetInactiveSession_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                if (gridSessions.SelectedRows.Count == 0)
                {
                    MessageBox.Show(this, "Select Session!", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                if (MessageBox.Show(this, "deactive selected Session?", "Session Deactivation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    Guid key = (Guid)gridSessions.SelectedRows[0].Cells["SessionKey"].Value;
                    Sessions.monitor_SetSessionInActive(key);
                    MessageBox.Show(this, "Session has been deactivated!", "Session Deativation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUnsubSession_Click(object sender, EventArgs e)
        {
            if (!m_LoadDone) { return; }
            try
            {
                if (gridSessions.SelectedRows.Count == 0)
                {
                    MessageBox.Show(this, "Select Session!", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                if (MessageBox.Show(this, "Unsubscribe selected Session?", "Session Unsubscription", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    Guid key = (Guid)gridSessions.SelectedRows[0].Cells["SessionKey"].Value;
                    Sessions.monitor_UnsubSession(key);
                    MessageBox.Show(this, "Session has been unsubscribed!", "Session Unsubscription", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
