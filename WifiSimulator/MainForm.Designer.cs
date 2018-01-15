namespace WifiSimulator
{
    partial class MainForm
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
            this.btnHighAlarm = new System.Windows.Forms.Button();
            this.btnBitAlarm = new System.Windows.Forms.Button();
            this.btnLowAlarm = new System.Windows.Forms.Button();
            this.btnLowVolC6 = new System.Windows.Forms.Button();
            this.btnDelepeC6 = new System.Windows.Forms.Button();
            this.btnNoAlarm = new System.Windows.Forms.Button();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbIPAddress = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.tbCount = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbServerIP = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbLocalport = new System.Windows.Forms.TextBox();
            this.btnCreateSingle = new System.Windows.Forms.Button();
            this.btnCloseSingle = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbPumpCount = new System.Windows.Forms.ComboBox();
            this.chSelectAll = new System.Windows.Forms.CheckBox();
            this.pnlCheckBoxs = new System.Windows.Forms.Panel();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.tbLocalIP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.btnDelepeC6Single = new System.Windows.Forms.Button();
            this.btnLowAlarmSingle = new System.Windows.Forms.Button();
            this.btnLowVolC6Single = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnNoAlarmC6Single = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnPartPumpType = new System.Windows.Forms.Button();
            this.btnResponsePumpType = new System.Windows.Forms.Button();
            this.btnC8LowAlarmSingle = new System.Windows.Forms.Button();
            this.btnC8LowVolC6Single = new System.Windows.Forms.Button();
            this.btnC8NoAlarmC6Single = new System.Windows.Forms.Button();
            this.btnC8DelepeC6Single = new System.Windows.Forms.Button();
            this.btnStopAll = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.pnlCheckBoxs.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnHighAlarm
            // 
            this.btnHighAlarm.Location = new System.Drawing.Point(366, 177);
            this.btnHighAlarm.Name = "btnHighAlarm";
            this.btnHighAlarm.Size = new System.Drawing.Size(108, 45);
            this.btnHighAlarm.TabIndex = 1;
            this.btnHighAlarm.Text = "发送高字节报警";
            this.btnHighAlarm.UseVisualStyleBackColor = true;
            this.btnHighAlarm.Click += new System.EventHandler(this.btnHighAlarm_Click);
            // 
            // btnBitAlarm
            // 
            this.btnBitAlarm.Location = new System.Drawing.Point(366, 70);
            this.btnBitAlarm.Name = "btnBitAlarm";
            this.btnBitAlarm.Size = new System.Drawing.Size(108, 48);
            this.btnBitAlarm.TabIndex = 1;
            this.btnBitAlarm.Text = "按位发送报警";
            this.btnBitAlarm.UseVisualStyleBackColor = true;
            this.btnBitAlarm.Click += new System.EventHandler(this.btnBitAlarm_Click);
            // 
            // btnLowAlarm
            // 
            this.btnLowAlarm.Location = new System.Drawing.Point(366, 124);
            this.btnLowAlarm.Name = "btnLowAlarm";
            this.btnLowAlarm.Size = new System.Drawing.Size(108, 47);
            this.btnLowAlarm.TabIndex = 1;
            this.btnLowAlarm.Text = "发送低字节报警";
            this.btnLowAlarm.UseVisualStyleBackColor = true;
            this.btnLowAlarm.Click += new System.EventHandler(this.btnLowAlarm_Click);
            // 
            // btnLowVolC6
            // 
            this.btnLowVolC6.Location = new System.Drawing.Point(366, 228);
            this.btnLowVolC6.Name = "btnLowVolC6";
            this.btnLowVolC6.Size = new System.Drawing.Size(108, 48);
            this.btnLowVolC6.TabIndex = 1;
            this.btnLowVolC6.Text = "C6低电报警";
            this.btnLowVolC6.UseVisualStyleBackColor = true;
            this.btnLowVolC6.Click += new System.EventHandler(this.btnLowVolC6_Click);
            // 
            // btnDelepeC6
            // 
            this.btnDelepeC6.Location = new System.Drawing.Point(366, 282);
            this.btnDelepeC6.Name = "btnDelepeC6";
            this.btnDelepeC6.Size = new System.Drawing.Size(108, 48);
            this.btnDelepeC6.TabIndex = 1;
            this.btnDelepeC6.Text = "C6耗尽电报警";
            this.btnDelepeC6.UseVisualStyleBackColor = true;
            this.btnDelepeC6.Click += new System.EventHandler(this.btnDelepeC6_Click);
            // 
            // btnNoAlarm
            // 
            this.btnNoAlarm.Location = new System.Drawing.Point(366, 20);
            this.btnNoAlarm.Name = "btnNoAlarm";
            this.btnNoAlarm.Size = new System.Drawing.Size(108, 48);
            this.btnNoAlarm.TabIndex = 1;
            this.btnNoAlarm.Text = "C6无报警";
            this.btnNoAlarm.UseVisualStyleBackColor = true;
            this.btnNoAlarm.Click += new System.EventHandler(this.btnNoAlarm_Click);
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(256, 12);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(100, 21);
            this.tbPort.TabIndex = 4;
            this.tbPort.Text = "20160";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(222, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "端口：";
            // 
            // tbIPAddress
            // 
            this.tbIPAddress.Location = new System.Drawing.Point(65, 12);
            this.tbIPAddress.Name = "tbIPAddress";
            this.tbIPAddress.Size = new System.Drawing.Size(100, 21);
            this.tbIPAddress.TabIndex = 5;
            this.tbIPAddress.Text = "127.0.0.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "目标IP：";
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(171, 8);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(45, 29);
            this.btnCreate.TabIndex = 6;
            this.btnCreate.Text = "创建";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(359, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(47, 28);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(423, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "客户端数量：";
            // 
            // tbCount
            // 
            this.tbCount.Location = new System.Drawing.Point(496, 12);
            this.tbCount.Name = "tbCount";
            this.tbCount.Size = new System.Drawing.Size(70, 21);
            this.tbCount.TabIndex = 4;
            this.tbCount.Text = "50";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "目标IP：";
            // 
            // tbServerIP
            // 
            this.tbServerIP.Location = new System.Drawing.Point(71, 25);
            this.tbServerIP.Name = "tbServerIP";
            this.tbServerIP.Size = new System.Drawing.Size(100, 21);
            this.tbServerIP.TabIndex = 5;
            this.tbServerIP.Text = "127.0.0.1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(-2, 76);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 12);
            this.label5.TabIndex = 2;
            this.label5.Text = "本地端口：";
            // 
            // tbLocalport
            // 
            this.tbLocalport.Location = new System.Drawing.Point(71, 73);
            this.tbLocalport.Name = "tbLocalport";
            this.tbLocalport.Size = new System.Drawing.Size(100, 21);
            this.tbLocalport.TabIndex = 4;
            this.tbLocalport.Text = "30000";
            // 
            // btnCreateSingle
            // 
            this.btnCreateSingle.Location = new System.Drawing.Point(177, 23);
            this.btnCreateSingle.Name = "btnCreateSingle";
            this.btnCreateSingle.Size = new System.Drawing.Size(53, 23);
            this.btnCreateSingle.TabIndex = 6;
            this.btnCreateSingle.Text = "创建";
            this.btnCreateSingle.UseVisualStyleBackColor = true;
            this.btnCreateSingle.Click += new System.EventHandler(this.btnCreateSingle_Click);
            // 
            // btnCloseSingle
            // 
            this.btnCloseSingle.Location = new System.Drawing.Point(180, 69);
            this.btnCloseSingle.Name = "btnCloseSingle";
            this.btnCloseSingle.Size = new System.Drawing.Size(50, 25);
            this.btnCloseSingle.TabIndex = 6;
            this.btnCloseSingle.Text = "关闭";
            this.btnCloseSingle.UseVisualStyleBackColor = true;
            this.btnCloseSingle.Click += new System.EventHandler(this.btnCloseSingle_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbPumpCount);
            this.groupBox1.Controls.Add(this.chSelectAll);
            this.groupBox1.Controls.Add(this.pnlCheckBoxs);
            this.groupBox1.Controls.Add(this.tbLocalIP);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.tbServerIP);
            this.groupBox1.Controls.Add(this.btnCloseSingle);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnCreateSingle);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tbLocalport);
            this.groupBox1.Location = new System.Drawing.Point(18, 43);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(653, 102);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "创建单个客户端";
            // 
            // cbPumpCount
            // 
            this.cbPumpCount.FormattingEnabled = true;
            this.cbPumpCount.Items.AddRange(new object[] {
            "6",
            "5",
            "4",
            "3",
            "2",
            "1"});
            this.cbPumpCount.Location = new System.Drawing.Point(441, 28);
            this.cbPumpCount.Name = "cbPumpCount";
            this.cbPumpCount.Size = new System.Drawing.Size(121, 20);
            this.cbPumpCount.TabIndex = 12;
            // 
            // chSelectAll
            // 
            this.chSelectAll.AutoSize = true;
            this.chSelectAll.Location = new System.Drawing.Point(388, 8);
            this.chSelectAll.Name = "chSelectAll";
            this.chSelectAll.Size = new System.Drawing.Size(48, 16);
            this.chSelectAll.TabIndex = 11;
            this.chSelectAll.Text = "全选";
            this.chSelectAll.UseVisualStyleBackColor = true;
            this.chSelectAll.CheckedChanged += new System.EventHandler(this.chSelectAll_CheckedChanged);
            // 
            // pnlCheckBoxs
            // 
            this.pnlCheckBoxs.Controls.Add(this.checkBox5);
            this.pnlCheckBoxs.Controls.Add(this.checkBox6);
            this.pnlCheckBoxs.Controls.Add(this.checkBox1);
            this.pnlCheckBoxs.Controls.Add(this.checkBox2);
            this.pnlCheckBoxs.Controls.Add(this.checkBox3);
            this.pnlCheckBoxs.Controls.Add(this.checkBox4);
            this.pnlCheckBoxs.Location = new System.Drawing.Point(257, 30);
            this.pnlCheckBoxs.Name = "pnlCheckBoxs";
            this.pnlCheckBoxs.Size = new System.Drawing.Size(167, 64);
            this.pnlCheckBoxs.TabIndex = 10;
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(68, 42);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(30, 16);
            this.checkBox5.TabIndex = 9;
            this.checkBox5.Text = "5";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox6
            // 
            this.checkBox6.AutoSize = true;
            this.checkBox6.Location = new System.Drawing.Point(123, 42);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(30, 16);
            this.checkBox6.TabIndex = 9;
            this.checkBox6.Text = "6";
            this.checkBox6.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(11, 13);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(30, 16);
            this.checkBox1.TabIndex = 9;
            this.checkBox1.Text = "1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(68, 13);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(30, 16);
            this.checkBox2.TabIndex = 9;
            this.checkBox2.Text = "2";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(123, 13);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(30, 16);
            this.checkBox3.TabIndex = 9;
            this.checkBox3.Text = "3";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(11, 42);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(30, 16);
            this.checkBox4.TabIndex = 9;
            this.checkBox4.Text = "4";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // tbLocalIP
            // 
            this.tbLocalIP.Location = new System.Drawing.Point(71, 49);
            this.tbLocalIP.Name = "tbLocalIP";
            this.tbLocalIP.Size = new System.Drawing.Size(100, 21);
            this.tbLocalIP.TabIndex = 8;
            this.tbLocalIP.Text = "127.0.0.30";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 52);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(53, 12);
            this.label6.TabIndex = 7;
            this.label6.Text = "本地IP：";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(254, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(137, 12);
            this.label7.TabIndex = 2;
            this.label7.Text = "发生耗尽报警的泵数量：";
            // 
            // btnDelepeC6Single
            // 
            this.btnDelepeC6Single.Location = new System.Drawing.Point(143, 128);
            this.btnDelepeC6Single.Name = "btnDelepeC6Single";
            this.btnDelepeC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnDelepeC6Single.TabIndex = 1;
            this.btnDelepeC6Single.Text = "C6耗尽电报警";
            this.btnDelepeC6Single.UseVisualStyleBackColor = true;
            this.btnDelepeC6Single.Click += new System.EventHandler(this.btnDelepeC6Single_Click);
            // 
            // btnLowAlarmSingle
            // 
            this.btnLowAlarmSingle.Location = new System.Drawing.Point(143, 21);
            this.btnLowAlarmSingle.Name = "btnLowAlarmSingle";
            this.btnLowAlarmSingle.Size = new System.Drawing.Size(108, 47);
            this.btnLowAlarmSingle.TabIndex = 1;
            this.btnLowAlarmSingle.Text = "发送低字节报警";
            this.btnLowAlarmSingle.UseVisualStyleBackColor = true;
            this.btnLowAlarmSingle.Click += new System.EventHandler(this.btnLowAlarmSingle_Click);
            // 
            // btnLowVolC6Single
            // 
            this.btnLowVolC6Single.Location = new System.Drawing.Point(143, 74);
            this.btnLowVolC6Single.Name = "btnLowVolC6Single";
            this.btnLowVolC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnLowVolC6Single.TabIndex = 1;
            this.btnLowVolC6Single.Text = "C6低电报警";
            this.btnLowVolC6Single.UseVisualStyleBackColor = true;
            this.btnLowVolC6Single.Click += new System.EventHandler(this.btnLowVolC6Single_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(8, 151);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(673, 359);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnDelepeC6);
            this.tabPage1.Controls.Add(this.btnHighAlarm);
            this.tabPage1.Controls.Add(this.btnLowAlarm);
            this.tabPage1.Controls.Add(this.btnBitAlarm);
            this.tabPage1.Controls.Add(this.btnLowVolC6);
            this.tabPage1.Controls.Add(this.btnNoAlarm);
            this.tabPage1.Controls.Add(this.btnLowAlarmSingle);
            this.tabPage1.Controls.Add(this.btnLowVolC6Single);
            this.tabPage1.Controls.Add(this.btnNoAlarmC6Single);
            this.tabPage1.Controls.Add(this.btnDelepeC6Single);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(665, 333);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "C6";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnNoAlarmC6Single
            // 
            this.btnNoAlarmC6Single.Location = new System.Drawing.Point(143, 182);
            this.btnNoAlarmC6Single.Name = "btnNoAlarmC6Single";
            this.btnNoAlarmC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnNoAlarmC6Single.TabIndex = 1;
            this.btnNoAlarmC6Single.Text = "C6无报警";
            this.btnNoAlarmC6Single.UseVisualStyleBackColor = true;
            this.btnNoAlarmC6Single.Click += new System.EventHandler(this.btnNoAlarmC6Single_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnPartPumpType);
            this.tabPage2.Controls.Add(this.btnStopAll);
            this.tabPage2.Controls.Add(this.btnResponsePumpType);
            this.tabPage2.Controls.Add(this.btnC8LowAlarmSingle);
            this.tabPage2.Controls.Add(this.btnC8LowVolC6Single);
            this.tabPage2.Controls.Add(this.btnC8NoAlarmC6Single);
            this.tabPage2.Controls.Add(this.btnC8DelepeC6Single);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(665, 333);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "C8";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnPartPumpType
            // 
            this.btnPartPumpType.Location = new System.Drawing.Point(355, 48);
            this.btnPartPumpType.Name = "btnPartPumpType";
            this.btnPartPumpType.Size = new System.Drawing.Size(75, 23);
            this.btnPartPumpType.TabIndex = 2;
            this.btnPartPumpType.Text = "部分启动";
            this.btnPartPumpType.UseVisualStyleBackColor = true;
            this.btnPartPumpType.Click += new System.EventHandler(this.btnPartPumpType_Click);
            // 
            // btnResponsePumpType
            // 
            this.btnResponsePumpType.Location = new System.Drawing.Point(355, 19);
            this.btnResponsePumpType.Name = "btnResponsePumpType";
            this.btnResponsePumpType.Size = new System.Drawing.Size(75, 23);
            this.btnResponsePumpType.TabIndex = 2;
            this.btnResponsePumpType.Text = "全部启动";
            this.btnResponsePumpType.UseVisualStyleBackColor = true;
            this.btnResponsePumpType.Click += new System.EventHandler(this.btnResponsePumpType_Click_1);
            // 
            // btnC8LowAlarmSingle
            // 
            this.btnC8LowAlarmSingle.Location = new System.Drawing.Point(183, 7);
            this.btnC8LowAlarmSingle.Name = "btnC8LowAlarmSingle";
            this.btnC8LowAlarmSingle.Size = new System.Drawing.Size(108, 47);
            this.btnC8LowAlarmSingle.TabIndex = 1;
            this.btnC8LowAlarmSingle.Text = "发送低字节报警";
            this.btnC8LowAlarmSingle.UseVisualStyleBackColor = true;
            this.btnC8LowAlarmSingle.Click += new System.EventHandler(this.btnC8LowAlarmSingle_Click);
            // 
            // btnC8LowVolC6Single
            // 
            this.btnC8LowVolC6Single.Location = new System.Drawing.Point(183, 60);
            this.btnC8LowVolC6Single.Name = "btnC8LowVolC6Single";
            this.btnC8LowVolC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnC8LowVolC6Single.TabIndex = 1;
            this.btnC8LowVolC6Single.Text = "C8低电报警";
            this.btnC8LowVolC6Single.UseVisualStyleBackColor = true;
            this.btnC8LowVolC6Single.Click += new System.EventHandler(this.btnLowVolC8Single_Click);
            // 
            // btnC8NoAlarmC6Single
            // 
            this.btnC8NoAlarmC6Single.Location = new System.Drawing.Point(183, 168);
            this.btnC8NoAlarmC6Single.Name = "btnC8NoAlarmC6Single";
            this.btnC8NoAlarmC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnC8NoAlarmC6Single.TabIndex = 1;
            this.btnC8NoAlarmC6Single.Text = "C8无报警";
            this.btnC8NoAlarmC6Single.UseVisualStyleBackColor = true;
            this.btnC8NoAlarmC6Single.Click += new System.EventHandler(this.btnNoAlarmC8Single_Click);
            // 
            // btnC8DelepeC6Single
            // 
            this.btnC8DelepeC6Single.Location = new System.Drawing.Point(183, 114);
            this.btnC8DelepeC6Single.Name = "btnC8DelepeC6Single";
            this.btnC8DelepeC6Single.Size = new System.Drawing.Size(108, 48);
            this.btnC8DelepeC6Single.TabIndex = 1;
            this.btnC8DelepeC6Single.Text = "C8耗尽电报警";
            this.btnC8DelepeC6Single.UseVisualStyleBackColor = true;
            this.btnC8DelepeC6Single.Click += new System.EventHandler(this.btnDelepeC8Single_Click);
            // 
            // btnStopAll
            // 
            this.btnStopAll.Location = new System.Drawing.Point(355, 75);
            this.btnStopAll.Name = "btnStopAll";
            this.btnStopAll.Size = new System.Drawing.Size(75, 23);
            this.btnStopAll.TabIndex = 2;
            this.btnStopAll.Text = "全部停止";
            this.btnStopAll.UseVisualStyleBackColor = true;
            this.btnStopAll.Click += new System.EventHandler(this.btnStopAll_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(683, 522);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.tbCount);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbIPAddress);
            this.Controls.Add(this.label1);
            this.Name = "MainForm";
            this.Text = "模拟器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.pnlCheckBoxs.ResumeLayout(false);
            this.pnlCheckBoxs.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnHighAlarm;
        private System.Windows.Forms.Button btnBitAlarm;
        private System.Windows.Forms.Button btnLowAlarm;
        private System.Windows.Forms.Button btnLowVolC6;
        private System.Windows.Forms.Button btnDelepeC6;
        private System.Windows.Forms.Button btnNoAlarm;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbIPAddress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbCount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbServerIP;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbLocalport;
        private System.Windows.Forms.Button btnCreateSingle;
        private System.Windows.Forms.Button btnCloseSingle;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbLocalIP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnDelepeC6Single;
        private System.Windows.Forms.Button btnLowAlarmSingle;
        private System.Windows.Forms.Button btnLowVolC6Single;
        private System.Windows.Forms.CheckBox checkBox6;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Panel pnlCheckBoxs;
        private System.Windows.Forms.CheckBox chSelectAll;
        private System.Windows.Forms.ComboBox cbPumpCount;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnC8DelepeC6Single;
        private System.Windows.Forms.Button btnC8LowAlarmSingle;
        private System.Windows.Forms.Button btnC8LowVolC6Single;
        private System.Windows.Forms.Button btnNoAlarmC6Single;
        private System.Windows.Forms.Button btnC8NoAlarmC6Single;
        private System.Windows.Forms.Button btnPartPumpType;
        private System.Windows.Forms.Button btnResponsePumpType;
        private System.Windows.Forms.Button btnStopAll;
    }
}

