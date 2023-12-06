using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommChecker
{
    public class PGN32500
    {
        //PGN32500, Rate settings from RC to module
        //0	    HeaderLo		    244
        //1	    HeaderHi		    126
        //2     Mod/Sen ID          0-15/0-15
        //3	    rate set Lo		    1000 X actual
        //4     rate set Mid
        //5	    rate set Hi
        //6	    Flow Cal Lo	        1000 X actual
        //7     Flow Cal Mid
        //8     Flow Cal Hi
        //9	    Command
        //	        - bit 0		    reset acc.Quantity
        //	        - bit 1,2,3		control type 0-4
        //	        - bit 4		    MasterOn
        //          - bit 5         0 - time for one pulse, 1 - average time for multiple pulses
        //          - bit 6         AutoOn
        //          - bit 7         -
        //10    manual pwm Lo
        //11    manual pwm Hi
        //12    -
        //13    CRC

        private const byte cByteCount = 14;
        private byte[] cData = new byte[cByteCount];
        private DateTime cSendTime;
        private frmModule mf;

        public PGN32500(frmModule CalledFrom)
        {
            mf = CalledFrom;
        }

        public DateTime SendTime
        { get { return cSendTime; } }

        public void Send()
        {
            double Tmp = 0;
            double RateSet;
            Array.Clear(cData, 0, cByteCount);
            cData[0] = 244;
            cData[1] = 126;

            cData[2] = mf.Tls.BuildModSenID(0, 0);

            // rate set
            RateSet = 100;

            if (mf.Enabled)
            {
                cData[3] = (byte)RateSet;
                cData[4] = (byte)((int)RateSet >> 8);
                cData[5] = (byte)((int)RateSet >> 16);
            }

            // flow cal
            Tmp = 56 * 1000.0;
            cData[6] = (byte)Tmp;
            cData[7] = (byte)((int)Tmp >> 8);
            cData[8] = (byte)((int)Tmp >> 16);

            // command byte

            // standard valve
            cData[9] &= 0b11110001; // clear bit 1, 2, 3

            // CRC
            cData[cByteCount - 1] = mf.Tls.CRC(cData, cByteCount - 1);

            // send
            mf.UDPmodules.SendUDPMessage(cData);

            cSendTime = DateTime.Now;
        }
    }
}