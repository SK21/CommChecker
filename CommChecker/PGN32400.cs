using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommChecker
{
    public class PGN32400
    {
        //PGN32400, Rate info from module to RC
        //0     HeaderLo    144
        //1     HeaderHi    126
        //2     Mod/Sen ID          0-15/0-15
        //3	    rate applied Lo 	1000 X actual
        //4     rate applied Mid
        //5	    rate applied Hi
        //6	    acc.Quantity Lo		10 X actual
        //7	    acc.Quantity Mid
        //8     acc.Quantity Hi
        //9     PWM Lo
        //10    PWM Hi
        //11    Status
        //      bit 0 - sensor 0 connected
        //      bit 1 - sensor 1 connected
        //      bit 2   - wifi rssi < -80
        //      bit 3	- wifi rssi < -70
        //      bit 4	- wifi rssi < -65
        //12    CRC

        private const byte cByteCount = 13;
        private const byte HeaderHi = 126;
        private const byte HeaderLo = 144;
        private readonly frmModule mf;
        private int cElapsedTime;
        private DateTime cLastTime = DateTime.Now;
        private bool cModuleIsReceivingData;
        private double cPWMsetting;
        private double cQuantity;
        private double cUPM;
        private bool LastModuleReceiving;
        private bool LastModuleSending;
        private DateTime ReceiveTime;

        public PGN32400(frmModule CalledFrom)
        {
            mf = CalledFrom;
        }

        public double AccumulatedQuantity()
        {
            return cQuantity;
        }

        public void CheckModuleComm()
        {
            if (LastModuleSending != ModuleSending())
            {
                LastModuleSending = ModuleSending();
                mf.Tls.WriteActivityLog("Module: 0  Sensor: 0  Sending: " + ModuleSending().ToString(), true);
            }

            if (LastModuleReceiving != ModuleReceiving())
            {
                LastModuleReceiving = ModuleReceiving();
                mf.Tls.WriteActivityLog("Module: 0  Sensor: 0  Receiving: " + ModuleReceiving().ToString(), true);
            }
        }

        public bool Connected()
        {
            return ModuleReceiving() && ModuleSending();
        }

        public int ElapsedTime()
        {
            int Result = 4000;
            if ((DateTime.Now - cLastTime).TotalMilliseconds < 4000) Result = cElapsedTime;

            CheckModuleComm();
            return Result;
        }

        public bool ModuleReceiving()
        {
            return cModuleIsReceivingData;
        }

        public bool ModuleSending()
        {
            return ((DateTime.Now - ReceiveTime).TotalSeconds < 4);
        }

        public bool ParseByteData(byte[] Data)
        {
            bool Result = false;
            byte cWifiStrength;

            if (Data[1] == HeaderHi && Data[0] == HeaderLo &&
                Data.Length >= cByteCount && mf.Tls.GoodCRC(Data))
            {
                int tmp = mf.Tls.ParseModID(Data[2]);
                if (0 == tmp)
                {
                    tmp = mf.Tls.ParseSenID(Data[2]);
                    if (0 == tmp)
                    {
                        cElapsedTime = (int)(DateTime.Now - cLastTime).TotalMilliseconds;
                        cLastTime = DateTime.Now;

                        cUPM = (Data[5] << 16 | Data[4] << 8 | Data[3]) / 1000.0;
                        cQuantity = (Data[8] << 16 | Data[7] << 8 | Data[6]) / 10.0;
                        cPWMsetting = (Int16)(Data[10] << 8 | Data[9]);  // need to cast to 16 bit integer to preserve the sign bit

                        // status
                        if (tmp == 0)
                        {
                            // sensor 0
                            cModuleIsReceivingData = ((Data[11] & 0b00000001) == 0b00000001);
                        }
                        else
                        {
                            // sensor 1
                            cModuleIsReceivingData = ((Data[11] & 0b00000010) == 0b00000010);
                        }

                        // wifi strength
                        cWifiStrength = 0;
                        if ((Data[11] & 0b00000100) == 0b00000100) cWifiStrength = 1;
                        if ((Data[11] & 0b00001000) == 0b00001000) cWifiStrength = 2;
                        if ((Data[11] & 0b00010000) == 0b00010000) cWifiStrength = 3;
                        mf.WifiStrength = cWifiStrength;

                        ReceiveTime = DateTime.Now;
                        Result = true;
                    }
                }
            }
            CheckModuleComm();
            return Result;
        }

        public bool ParseStringData(string[] Data)
        {
            bool Result = false;
            byte[] BD;
            if (Data.Length < 100)
            {
                BD = new byte[Data.Length];
                for (int i = 0; i < Data.Length; i++)
                {
                    byte.TryParse(Data[i], out BD[i]);
                }
                Result = ParseByteData(BD);
            }
            return Result;
        }

        public double PWMsetting()
        {
            return cPWMsetting;
        }

        public double UPM()
        {
            double Result = cUPM;
            return Result;
        }
    }
}