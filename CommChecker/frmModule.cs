using PCBsetup.Forms;
using RateController;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CommChecker
{
    public partial class frmModule : Form
    {
        public readonly int MaxModules = 8;
        public PGN32401 AnalogData;
        public PGN32400 ArduinoModule;
        public PGN32500 ModuleRateSettings;
        public SerialComm[] SER = new SerialComm[3];
        public clsTools Tls;
        public UDPcomm UDPmodules;
        private int CommPort = 0;// 0-2
        private byte cWifiStrength;
        private bool FreezeUpdate;
        private DateTime[] ModuleTime;

        public frmModule()
        {
            InitializeComponent();
            Tls = new clsTools(this);
            for (int i = 0; i < 3; i++)
            {
                SER[i] = new SerialComm(this, i);
            }
            UDPmodules = new UDPcomm(this, 29999, 28888, 1688);    // arduino
            AnalogData = new PGN32401(this);
            ArduinoModule = new PGN32400(this);
            ModuleTime = new DateTime[MaxModules];
            ModuleRateSettings = new PGN32500(this);
        }

        public byte WifiStrength
        {
            get { return cWifiStrength; }
            set
            {
                cWifiStrength = value;
            }
        }

        public void CloseComms()
        {
            for (int i = 0; i < 3; i++)
            {
                SER[i].CloseRCport();
            }
        }

        public void StartSerial()
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    String ID = "_" + i.ToString() + "_";
                    SER[i].RCportName = Tls.LoadProperty("RCportName" + ID + i.ToString());

                    int tmp;
                    if (int.TryParse(Tls.LoadProperty("RCportBaud" + ID + i.ToString()), out tmp))
                    {
                        SER[i].RCportBaud = tmp;
                    }
                    else
                    {
                        SER[i].RCportBaud = 38400;
                    }

                    bool tmp2;
                    bool.TryParse(Tls.LoadProperty("RCportSuccessful" + ID + i.ToString()), out tmp2);
                    if (tmp2) SER[i].OpenRCport();
                }
            }
            catch (Exception ex)
            {
                Tls.WriteErrorLog("FormRateControl/StartSerial: " + ex.Message);
                Tls.ShowHelp(ex.Message, this.Text, 3000, true);
            }
        }

        public void UpdateModuleConnected(int ModuleID)
        {
            if (ModuleID > -1 && ModuleID < MaxModules) ModuleTime[ModuleID] = DateTime.Now;
        }

        private void bntOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (FreezeUpdate)
            {
                FreezeUpdate = false;
                btnStart.Image = Properties.Resources.Stop;
            }
            else
            {
                FreezeUpdate = true;
                btnStart.Image = Properties.Resources.Start;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form fs = Application.OpenForms["frmComm"];

            if (fs == null)
            {
                Form frm = new frmComm(this);
                frm.Show();
            }
            else
            {
                fs.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form fs = Application.OpenForms["frmWifi"];

            if (fs == null)
            {
                Form frm = new frmWifi(this);
                frm.Show();
            }
            else
            {
                fs.Focus();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form fs = Application.OpenForms["frmFirmware"];

            if (fs == null)
            {
                Form frm = new frmFirmware(this);
                frm.Show();
            }
            else
            {
                fs.Focus();
            }
        }

        private void cboPort1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommPort = Convert.ToByte(cboPort1.SelectedIndex);
            PortName.Text = "(" + SER[CommPort].RCportName + ")";
        }

        private void frmModule_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                Tls.SaveFormData(this);
            }
            timer1.Enabled = false;
        }

        private void frmModule_Load(object sender, EventArgs e)
        {
            Tls.LoadFormData(this);
            lbIP.Text = UDPmodules.EthernetIP();
            lbWifi.Text = UDPmodules.WifiIP();
            lbAppVersion.Text = Tls.AppVersion();
            lbDate.Text = Tls.VersionDate();
            timer1.Enabled = true;

            this.BackColor = Properties.Settings.Default.DayColour;
            foreach (Control c in this.Controls)
            {
                c.ForeColor = Color.Black;
            }

            tabControl1.TabPages[0].BackColor = this.BackColor;
            tabControl1.TabPages[1].BackColor = this.BackColor;
            tbEthernet.BackColor = this.BackColor;
            tbSerial.BackColor = this.BackColor;

            cboPort1.SelectedIndex = 0;
            UpdateLogs();

            UDPmodules.StartUDPServer();
            if (!UDPmodules.IsUDPSendConnected)
            {
                Tls.ShowHelp("UDPnetwork failed to start.", "", 3000, true, true);
            }
            UDPmodules.EthernetEP = Tls.LoadProperty("EthernetEP");
            StartSerial();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbInoID.Text = AnalogData.InoID.ToString();
            lbModID.Text = AnalogData.ModuleID.ToString();
            int Elapsed = ArduinoModule.ElapsedTime();
            if (Elapsed < 4000)
            {
                lbTime.Text = (Elapsed / 1000.0).ToString("N3");
            }
            else
            {
                lbTime.Text = "--";
            }

            if (!FreezeUpdate)
            {
                tbSerial.Text = SER[CommPort].Log();
                tbSerial.Select(tbSerial.Text.Length, 0);
                tbSerial.ScrollToCaret();

                tbEthernet.Text = UDPmodules.Log();
                tbEthernet.Select(tbEthernet.Text.Length, 0);
                tbEthernet.ScrollToCaret();

                UpdateLogs();
            }

            ModuleRateSettings.Send();

            if(ArduinoModule.ModuleReceiving())
            {
                lbReceive.BackColor = Color.LightGreen;
            }
            else
            {
                lbReceive.BackColor = Color.Red;
            }

            if(ArduinoModule.ModuleSending())
            {
                lbSend.BackColor = Color.LightGreen;
            }
            else
            {
                lbSend.BackColor= Color.Red;
            }
        }

        private void UpdateLogs()
        {
            tbActivity.Text = Tls.ReadTextFile("Activity Log.txt");
            tbActivity.Select(tbActivity.Text.Length, 0);
            tbActivity.ScrollToCaret();

            tbErrors.Text = Tls.ReadTextFile("Error Log.txt");
            tbErrors.Select(tbErrors.Text.Length, 0);
            tbErrors.ScrollToCaret();
        }

        private void button2_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            string Message = "Set subnet.";

            Tls.ShowHelp(Message, "Subnet");
            hlpevent.Handled = true;
        }

        private void button3_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            string Message = "Upload firmware.";

            Tls.ShowHelp(Message, "Upload");
            hlpevent.Handled = true;
        }
    }
}