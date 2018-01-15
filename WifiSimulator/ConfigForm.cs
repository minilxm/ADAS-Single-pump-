using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WifiSimulator
{
    public partial class ConfigForm : Form
    {
        public string IP
        {
            get { return this.tbIPAddress.Text;}
        }

        public int Port
        {
            get { return Convert.ToInt32(this.tbPort.Text); }
        }

        public int SerialNo
        {
            get { return Convert.ToInt32(this.tbSerialNo.Text); }
        }

        public ConfigForm()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
