namespace Beltone.Services.Fix.Client_Test
{
    partial class frmTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnBuy = new System.Windows.Forms.Button();
            this.btnSell = new System.Windows.Forms.Button();
            this.btnModifyBuy = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.comboStocks = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rbLimit = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rbMarket = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.panelPrice = new System.Windows.Forms.Panel();
            this.tbPrice = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lblSub = new System.Windows.Forms.Label();
            this.tbClient = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbCust = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbMarket = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.lblCode = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.nudRepeat = new System.Windows.Forms.NumericUpDown();
            this.btnSub = new System.Windows.Forms.Button();
            this.tbReport = new System.Windows.Forms.RichTextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.comboOrders = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbQty = new System.Windows.Forms.TextBox();
            this.tbRequestGuid = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnCancelSellOrder = new System.Windows.Forms.Button();
            this.txtUnifiedCode = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.comboBoxAllocationType = new System.Windows.Forms.ComboBox();
            this.btnCancelBuy = new System.Windows.Forms.Button();
            this.combTimeInforce = new System.Windows.Forms.ComboBox();
            this.panel1.SuspendLayout();
            this.panelPrice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRepeat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnBuy
            // 
            this.btnBuy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(231)))), ((int)(((byte)(251)))));
            this.btnBuy.Location = new System.Drawing.Point(14, 236);
            this.btnBuy.Name = "btnBuy";
            this.btnBuy.Size = new System.Drawing.Size(75, 23);
            this.btnBuy.TabIndex = 0;
            this.btnBuy.Text = "Buy";
            this.btnBuy.UseVisualStyleBackColor = false;
            this.btnBuy.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnSell
            // 
            this.btnSell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.btnSell.Location = new System.Drawing.Point(191, 236);
            this.btnSell.Name = "btnSell";
            this.btnSell.Size = new System.Drawing.Size(75, 23);
            this.btnSell.TabIndex = 1;
            this.btnSell.Text = "Sell";
            this.btnSell.UseVisualStyleBackColor = false;
            this.btnSell.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnModifyBuy
            // 
            this.btnModifyBuy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(231)))), ((int)(((byte)(251)))));
            this.btnModifyBuy.Location = new System.Drawing.Point(14, 265);
            this.btnModifyBuy.Name = "btnModifyBuy";
            this.btnModifyBuy.Size = new System.Drawing.Size(75, 23);
            this.btnModifyBuy.TabIndex = 0;
            this.btnModifyBuy.Text = "Modify";
            this.btnModifyBuy.UseVisualStyleBackColor = true;
            this.btnModifyBuy.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.button4.Location = new System.Drawing.Point(191, 265);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 1;
            this.button4.Text = "Modify";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // comboStocks
            // 
            this.comboStocks.FormattingEnabled = true;
            this.comboStocks.Location = new System.Drawing.Point(12, 39);
            this.comboStocks.Name = "comboStocks";
            this.comboStocks.Size = new System.Drawing.Size(182, 21);
            this.comboStocks.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(151, 101);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Client";
            // 
            // rbLimit
            // 
            this.rbLimit.AutoSize = true;
            this.rbLimit.Checked = true;
            this.rbLimit.Location = new System.Drawing.Point(62, 4);
            this.rbLimit.Name = "rbLimit";
            this.rbLimit.Size = new System.Drawing.Size(46, 17);
            this.rbLimit.TabIndex = 4;
            this.rbLimit.TabStop = true;
            this.rbLimit.Text = "Limit";
            this.rbLimit.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rbMarket);
            this.panel1.Controls.Add(this.rbLimit);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(12, 66);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(182, 26);
            this.panel1.TabIndex = 5;
            // 
            // rbMarket
            // 
            this.rbMarket.AutoSize = true;
            this.rbMarket.Location = new System.Drawing.Point(114, 6);
            this.rbMarket.Name = "rbMarket";
            this.rbMarket.Size = new System.Drawing.Size(58, 17);
            this.rbMarket.TabIndex = 4;
            this.rbMarket.Text = "Market";
            this.rbMarket.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Type";
            // 
            // panelPrice
            // 
            this.panelPrice.Controls.Add(this.tbPrice);
            this.panelPrice.Controls.Add(this.label3);
            this.panelPrice.Location = new System.Drawing.Point(5, 130);
            this.panelPrice.Name = "panelPrice";
            this.panelPrice.Size = new System.Drawing.Size(106, 26);
            this.panelPrice.TabIndex = 5;
            // 
            // tbPrice
            // 
            this.tbPrice.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.tbPrice.Location = new System.Drawing.Point(33, 3);
            this.tbPrice.Name = "tbPrice";
            this.tbPrice.Size = new System.Drawing.Size(68, 20);
            this.tbPrice.TabIndex = 6;
            this.tbPrice.Text = "10.5";
            this.tbPrice.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Price";
            // 
            // lblSub
            // 
            this.lblSub.AutoSize = true;
            this.lblSub.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSub.ForeColor = System.Drawing.Color.Red;
            this.lblSub.Location = new System.Drawing.Point(9, 12);
            this.lblSub.Name = "lblSub";
            this.lblSub.Size = new System.Drawing.Size(91, 14);
            this.lblSub.TabIndex = 3;
            this.lblSub.Text = "Not Subscribed";
            // 
            // tbClient
            // 
            this.tbClient.Location = new System.Drawing.Point(198, 98);
            this.tbClient.Name = "tbClient";
            this.tbClient.Size = new System.Drawing.Size(68, 20);
            this.tbClient.TabIndex = 6;
            this.tbClient.Text = "3001";
            this.tbClient.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(151, 160);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(45, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Custody";
            // 
            // tbCust
            // 
            this.tbCust.Location = new System.Drawing.Point(198, 158);
            this.tbCust.Name = "tbCust";
            this.tbCust.Size = new System.Drawing.Size(68, 20);
            this.tbCust.TabIndex = 6;
            this.tbCust.Text = "4508";
            this.tbCust.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 187);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(23, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "MK";
            // 
            // tbMarket
            // 
            this.tbMarket.Location = new System.Drawing.Point(56, 184);
            this.tbMarket.Name = "tbMarket";
            this.tbMarket.ReadOnly = true;
            this.tbMarket.Size = new System.Drawing.Size(68, 20);
            this.tbMarket.TabIndex = 6;
            this.tbMarket.Text = "CA";
            this.tbMarket.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 213);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(23, 13);
            this.label7.TabIndex = 3;
            this.label7.Text = "TIF";
            // 
            // lblCode
            // 
            this.lblCode.AutoSize = true;
            this.lblCode.ForeColor = System.Drawing.Color.Blue;
            this.lblCode.Location = new System.Drawing.Point(194, 42);
            this.lblCode.Name = "lblCode";
            this.lblCode.Size = new System.Drawing.Size(73, 13);
            this.lblCode.TabIndex = 3;
            this.lblCode.Text = "No ISIN Code";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 161);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Repeat";
            // 
            // nudRepeat
            // 
            this.nudRepeat.Location = new System.Drawing.Point(56, 160);
            this.nudRepeat.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudRepeat.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudRepeat.Name = "nudRepeat";
            this.nudRepeat.Size = new System.Drawing.Size(45, 20);
            this.nudRepeat.TabIndex = 7;
            this.nudRepeat.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.nudRepeat.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnSub
            // 
            this.btnSub.BackColor = System.Drawing.Color.Green;
            this.btnSub.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSub.ForeColor = System.Drawing.Color.White;
            this.btnSub.Location = new System.Drawing.Point(198, 7);
            this.btnSub.Name = "btnSub";
            this.btnSub.Size = new System.Drawing.Size(75, 23);
            this.btnSub.TabIndex = 0;
            this.btnSub.Text = "Subscribe";
            this.btnSub.UseVisualStyleBackColor = false;
            this.btnSub.Click += new System.EventHandler(this.btnSub_Click);
            // 
            // tbReport
            // 
            this.tbReport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbReport.Location = new System.Drawing.Point(322, 7);
            this.tbReport.Name = "tbReport";
            this.tbReport.Size = new System.Drawing.Size(414, 289);
            this.tbReport.TabIndex = 8;
            this.tbReport.Text = "";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 354);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(75, 13);
            this.label9.TabIndex = 3;
            this.label9.Text = "Display Orders";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(15, 387);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(720, 173);
            this.dataGridView1.TabIndex = 9;
            // 
            // comboOrders
            // 
            this.comboOrders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboOrders.FormattingEnabled = true;
            this.comboOrders.Items.AddRange(new object[] {
            "Today Orders",
            "Exec Orders"});
            this.comboOrders.Location = new System.Drawing.Point(91, 352);
            this.comboOrders.Name = "comboOrders";
            this.comboOrders.Size = new System.Drawing.Size(182, 21);
            this.comboOrders.TabIndex = 2;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 114);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(23, 13);
            this.label8.TabIndex = 3;
            this.label8.Text = "Qty";
            // 
            // tbQty
            // 
            this.tbQty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.tbQty.Location = new System.Drawing.Point(38, 107);
            this.tbQty.Name = "tbQty";
            this.tbQty.Size = new System.Drawing.Size(68, 20);
            this.tbQty.TabIndex = 6;
            this.tbQty.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // tbRequestGuid
            // 
            this.tbRequestGuid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.tbRequestGuid.Location = new System.Drawing.Point(399, 318);
            this.tbRequestGuid.Name = "tbRequestGuid";
            this.tbRequestGuid.Size = new System.Drawing.Size(337, 20);
            this.tbRequestGuid.TabIndex = 11;
            this.tbRequestGuid.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbRequestGuid.TextChanged += new System.EventHandler(this.tbRequestGuid_TextChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(322, 318);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(72, 13);
            this.label10.TabIndex = 10;
            this.label10.Text = "Request Guid";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // btnCancelSellOrder
            // 
            this.btnCancelSellOrder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.btnCancelSellOrder.Location = new System.Drawing.Point(191, 295);
            this.btnCancelSellOrder.Name = "btnCancelSellOrder";
            this.btnCancelSellOrder.Size = new System.Drawing.Size(75, 23);
            this.btnCancelSellOrder.TabIndex = 12;
            this.btnCancelSellOrder.Text = "Cancel";
            this.btnCancelSellOrder.UseVisualStyleBackColor = false;
            this.btnCancelSellOrder.Click += new System.EventHandler(this.btnCancelSellOrder_Click);
            // 
            // txtUnifiedCode
            // 
            this.txtUnifiedCode.Location = new System.Drawing.Point(198, 130);
            this.txtUnifiedCode.Name = "txtUnifiedCode";
            this.txtUnifiedCode.Size = new System.Drawing.Size(68, 20);
            this.txtUnifiedCode.TabIndex = 14;
            this.txtUnifiedCode.Text = "2079333";
            this.txtUnifiedCode.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(131, 132);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(65, 13);
            this.label11.TabIndex = 13;
            this.label11.Text = "UnifiedCode";
            // 
            // comboBoxAllocationType
            // 
            this.comboBoxAllocationType.FormattingEnabled = true;
            this.comboBoxAllocationType.Items.AddRange(new object[] {
            "Regular",
            "SameDay",
            "SameDay_Plus"});
            this.comboBoxAllocationType.Location = new System.Drawing.Point(134, 184);
            this.comboBoxAllocationType.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxAllocationType.Name = "comboBoxAllocationType";
            this.comboBoxAllocationType.Size = new System.Drawing.Size(139, 21);
            this.comboBoxAllocationType.TabIndex = 15;
            // 
            // btnCancelBuy
            // 
            this.btnCancelBuy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(231)))), ((int)(((byte)(251)))));
            this.btnCancelBuy.Location = new System.Drawing.Point(15, 294);
            this.btnCancelBuy.Name = "btnCancelBuy";
            this.btnCancelBuy.Size = new System.Drawing.Size(75, 23);
            this.btnCancelBuy.TabIndex = 16;
            this.btnCancelBuy.Text = "Cancel";
            this.btnCancelBuy.UseVisualStyleBackColor = true;
            this.btnCancelBuy.Click += new System.EventHandler(this.btnCancelBuy_Click);
            // 
            // combTimeInforce
            // 
            this.combTimeInforce.AutoCompleteCustomSource.AddRange(new string[] {
            "Day",
            "GoodTillDate",
            "GoodTillCancel",
            "Immediate"});
            this.combTimeInforce.FormattingEnabled = true;
            this.combTimeInforce.Items.AddRange(new object[] {
            "Day",
            "GoodTillDate",
            "GoodTillCancel"});
            this.combTimeInforce.Location = new System.Drawing.Point(37, 213);
            this.combTimeInforce.Margin = new System.Windows.Forms.Padding(2);
            this.combTimeInforce.Name = "combTimeInforce";
            this.combTimeInforce.Size = new System.Drawing.Size(139, 21);
            this.combTimeInforce.TabIndex = 17;
            // 
            // frmTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(750, 572);
            this.Controls.Add(this.combTimeInforce);
            this.Controls.Add(this.btnCancelBuy);
            this.Controls.Add(this.comboBoxAllocationType);
            this.Controls.Add(this.txtUnifiedCode);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.btnCancelSellOrder);
            this.Controls.Add(this.tbRequestGuid);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.tbReport);
            this.Controls.Add(this.nudRepeat);
            this.Controls.Add(this.tbQty);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tbMarket);
            this.Controls.Add(this.tbCust);
            this.Controls.Add(this.tbClient);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.panelPrice);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblSub);
            this.Controls.Add(this.lblCode);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboOrders);
            this.Controls.Add(this.comboStocks);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.btnSell);
            this.Controls.Add(this.btnModifyBuy);
            this.Controls.Add(this.btnSub);
            this.Controls.Add(this.btnBuy);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "frmTest";
            this.Text = "Simple Trader";
            this.Load += new System.EventHandler(this.frmTest_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panelPrice.ResumeLayout(false);
            this.panelPrice.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRepeat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnBuy;
        private System.Windows.Forms.Button btnSell;
        private System.Windows.Forms.Button btnModifyBuy;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ComboBox comboStocks;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbLimit;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton rbMarket;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panelPrice;
        private System.Windows.Forms.TextBox tbPrice;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblSub;
        private System.Windows.Forms.TextBox tbClient;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbCust;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbMarket;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblCode;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nudRepeat;
        private System.Windows.Forms.Button btnSub;
        private System.Windows.Forms.RichTextBox tbReport;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox comboOrders;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbQty;
        private System.Windows.Forms.TextBox tbRequestGuid;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btnCancelSellOrder;
        private System.Windows.Forms.TextBox txtUnifiedCode;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBoxAllocationType;
        private System.Windows.Forms.Button btnCancelBuy;
        private System.Windows.Forms.ComboBox combTimeInforce;
    }
}

