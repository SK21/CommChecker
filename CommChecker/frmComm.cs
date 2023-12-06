using CommChecker;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RateController
{
    public partial class frmComm : Form
    {
        private frmModule mf;

        public frmComm(frmModule CallingForm)
        {
            mf = CallingForm;
            InitializeComponent();
        }

        private void bntOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnConnect1_Click(object sender, EventArgs e)
        {
            if (btnConnect1.Text == "Connect")
            {
                mf.SER[0].OpenRCport();
            }
            else
            {
                mf.SER[0].CloseRCport();
            }
            SetPortButtons1();
        }

        private void btnConnect2_Click(object sender, EventArgs e)
        {
            if (btnConnect2.Text == "Connect")
            {
                mf.SER[1].OpenRCport();
            }
            else
            {
                mf.SER[1].CloseRCport();
            }
            SetPortButtons2();
        }

        private void btnConnect3_Click(object sender, EventArgs e)
        {
            if (btnConnect3.Text == "Connect")
            {
                mf.SER[2].OpenRCport();
            }
            else
            {
                mf.SER[2].CloseRCport();
            }
            SetPortButtons3();
        }

        private void btnRescan_Click(object sender, EventArgs e)
        {
            LoadRCbox();
        }

        private void cboBaud1_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[0].ArduinoPort.BaudRate = Convert.ToInt32(cboBaud1.Text);
            mf.SER[0].RCportBaud = Convert.ToInt32(cboBaud1.Text);
        }

        private void cboBaud2_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[1].ArduinoPort.BaudRate = Convert.ToInt32(cboBaud2.Text);
            mf.SER[1].RCportBaud = Convert.ToInt32(cboBaud2.Text);
        }

        private void cboBaud3_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[2].ArduinoPort.BaudRate = Convert.ToInt32(cboBaud3.Text);
            mf.SER[2].RCportBaud = Convert.ToInt32(cboBaud3.Text);
        }

        private void cboPort1_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[0].RCportName = cboPort1.Text;
        }

        private void cboPort2_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[1].RCportName = cboPort2.Text;
        }

        private void cboPort3_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.SER[2].RCportName = cboPort3.Text;
        }

        private void frmComm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                mf.Tls.SaveFormData(this);
            }
        }

        private void frmComm_Load(object sender, EventArgs e)
        {
            mf.Tls.LoadFormData(this);
            UpdateForm();
            LoadRCbox();
        }

        private void groupBox3_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            mf.Tls.DrawGroupBox(box, e.Graphics, this.BackColor, Color.Black, Color.Blue);
        }

        private void LoadRCbox()
        {
            cboPort1.Items.Clear();
            foreach (String s in System.IO.Ports.SerialPort.GetPortNames())
            {
                cboPort1.Items.Add(s);
                cboPort2.Items.Add(s);
                cboPort3.Items.Add(s);
            }
            SetPortButtons1();
            SetPortButtons2();
            SetPortButtons3();
        }

        private void SetPortButtons1()
        {
            cboPort1.SelectedIndex = cboPort1.FindStringExact(mf.SER[0].RCportName);
            cboBaud1.SelectedIndex = cboBaud1.FindStringExact(mf.SER[0].RCportBaud.ToString());

            if (mf.SER[0].ArduinoPort.IsOpen)
            {
                cboBaud1.Enabled = false;
                cboPort1.Enabled = false;
                btnConnect1.Text = "Disconnect";
                PortIndicator1.Image = CommChecker.Properties.Resources.On;
            }
            else
            {
                cboBaud1.Enabled = true;
                cboPort1.Enabled = true;
                btnConnect1.Text = "Connect";
                PortIndicator1.Image = CommChecker.Properties.Resources.Off;
            }
        }

        private void SetPortButtons2()
        {
            cboPort2.SelectedIndex = cboPort2.FindStringExact(mf.SER[1].RCportName);
            cboBaud2.SelectedIndex = cboBaud2.FindStringExact(mf.SER[1].RCportBaud.ToString());

            if (mf.SER[1].ArduinoPort.IsOpen)
            {
                cboBaud2.Enabled = false;
                cboPort2.Enabled = false;
                btnConnect2.Text = "Disconnect";
                PortIndicator2.Image = CommChecker.Properties.Resources.On;
            }
            else
            {
                cboBaud2.Enabled = true;
                cboPort2.Enabled = true;
                btnConnect2.Text = "Connect";
                PortIndicator2.Image = CommChecker.Properties.Resources.Off;
            }
        }

        private void SetPortButtons3()
        {
            cboPort3.SelectedIndex = cboPort3.FindStringExact(mf.SER[2].RCportName);
            cboBaud3.SelectedIndex = cboBaud3.FindStringExact(mf.SER[2].RCportBaud.ToString());

            if (mf.SER[2].ArduinoPort.IsOpen)
            {
                cboBaud3.Enabled = false;
                cboPort3.Enabled = false;
                btnConnect3.Text = "Disconnect";
                PortIndicator3.Image = CommChecker.Properties.Resources.On;
            }
            else
            {
                cboBaud3.Enabled = true;
                cboPort3.Enabled = true;
                btnConnect3.Text = "Connect";
                PortIndicator3.Image = CommChecker.Properties.Resources.Off;
            }
        }

        private void UpdateForm()
        {
            PortIndicator1.BackColor = CommChecker.Properties.Settings.Default.DayColour;
            PortIndicator2.BackColor = CommChecker.Properties.Settings.Default.DayColour;
            PortIndicator3.BackColor = CommChecker.Properties.Settings.Default.DayColour;

            this.BackColor = CommChecker.Properties.Settings.Default.DayColour;

            foreach (Control c in this.Controls)
            {
                c.ForeColor = Color.Black;
            }
        }
    }
}