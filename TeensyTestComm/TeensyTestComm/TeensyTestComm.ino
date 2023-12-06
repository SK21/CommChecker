
#include <NativeEthernet.h>
#include <NativeEthernetUdp.h>
#include <EEPROM.h> 

// Test Comm
# define InoDescription "TeensyTestComm :  05-Dec-2023"
const uint16_t InoID = 5123;	// change to send defaults to eeprom, ddmmy, no leading 0

# define MaxReadBuffer 100	// bytes
# define MaxProductCount 2

struct ModuleConfig
{
	uint8_t ID = 0;
	uint8_t SensorCount = 2;        // up to 2 sensors, if 0 rate control will be disabled
	uint8_t RelayOnSignal = 0;	    // value that turns on relays
	uint8_t FlowOnDirection = 0;	// sets on value for flow valve or sets motor direction
	uint8_t IP0 = 192;
	uint8_t IP1 = 168;
	uint8_t IP2 = 1;
	uint8_t IP3 = 60;
	uint8_t RelayControl = 5;		// 0 - no relays, 1 - RS485, 2 - PCA9555 8 relays, 3 - PCA9555 16 relays, 4 - MCP23017, 5 - Teensy GPIO
	uint8_t ESPserialPort = 1;		// serial port to connect to wifi module
	uint8_t RelayPins[16] = { 8,9,10,11,12,25,26,27,0,0,0,0,0,0,0,0 };		// pin numbers when GPIOs are used for relay control (5), default RC11
};

ModuleConfig MDL;

struct SensorConfig
{
	uint8_t FlowPin;
	uint8_t DirPin;
	uint8_t PWMPin;
	bool FlowEnabled;
	double UPM;				// sent as upm X 1000
	double PWM;
	uint32_t CommTime;
	byte ControlType;		// 0 standard, 1 combo close, 2 motor, 3 motor/weight, 4 fan, 5 timed combo
	uint32_t TotalPulses;
	double TargetUPM;
	double MeterCal;
	double ManualAdjust;
	double KP;
	double KI;
	double KD;
	byte MinPWM;
	byte MaxPWM;
	bool UseMultiPulses;	// 0 - time for one pulse, 1 - average time for multiple pulses
	uint8_t Debounce;
};

SensorConfig Sensor[2];

// ethernet
EthernetUDP UDPcomm;
uint16_t ListeningPort = 28888;
uint16_t DestinationPort = 29999;
IPAddress DestinationIP(MDL.IP0, MDL.IP1, MDL.IP2, 255);

const uint16_t LoopTime = 50;      //in msec = 20hz
uint32_t LoopLast = LoopTime;
const uint16_t SendTime = 200;
uint32_t SendLast = SendTime;

void setup()
{
	Serial.begin(38400);
	delay(5000);
	Serial.println("");
	Serial.println("");
	Serial.println("");
	Serial.println(InoDescription);
	Serial.println("");

	// eeprom
	int16_t StoredID;
	EEPROM.get(50, StoredID);
	if (StoredID == InoID)
	{
		// load stored data
		Serial.println("Loading stored settings.");
		EEPROM.get(110, MDL);

		for (int i = 0; i < MaxProductCount; i++)
		{
			EEPROM.get(200 + i * 80, Sensor[i]);
		}
	}
	else
	{
		// update stored data
		Serial.println("Updating stored data.");
		EEPROM.put(50, InoID);
		EEPROM.put(110, MDL);

		for (int i = 0; i < MaxProductCount; i++)
		{
			EEPROM.put(200 + i * 80, Sensor[i]);
		}
	}

	if (MDL.SensorCount > MaxProductCount) MDL.SensorCount = MaxProductCount;

	// ethernet 
	Serial.println("Starting Ethernet ...");
	MDL.IP3 = MDL.ID + 60;
	IPAddress LocalIP(MDL.IP0, MDL.IP1, MDL.IP2, MDL.IP3);
	static uint8_t LocalMac[] = { 0x0A,0x0B,0x42,0x0C,0x0D,MDL.IP3 };

	Ethernet.begin(LocalMac, 0);
	Ethernet.setLocalIP(LocalIP);

	delay(1500);
	if (Ethernet.linkStatus() == LinkON)
	{
		Serial.println("Ethernet Connected.");
	}
	else
	{
		Serial.println("Ethernet Not Connected.");
	}
	Serial.print("IP Address: ");
	Serial.println(Ethernet.localIP());
	DestinationIP = IPAddress(MDL.IP0, MDL.IP1, MDL.IP2, 255);	// update from saved data
	Serial.println("");

	// UDP
	UDPcomm.begin(ListeningPort);

	Serial.println("");
	Serial.println("Finished setup.");
	Serial.println("");
}

void loop()
{
	if (millis() - SendLast > SendTime)
	{
		SendLast = millis();
		SendUDPwired();
	}

	ReceiveUDPwired();
	Blink();
}

byte ParseModID(byte ID)
{
	// top 4 bits
	return ID >> 4;
}

byte ParseSenID(byte ID)
{
	// bottom 4 bits
	return (ID & 0b00001111);
}

byte BuildModSenID(byte Mod_ID, byte Sen_ID)
{
	return ((Mod_ID << 4) | (Sen_ID & 0b00001111));
}

bool GoodCRC(byte Data[], byte Length)
{
	byte ck = CRC(Data, Length - 1, 0);
	bool Result = (ck == Data[Length - 1]);
	return Result;
}

byte CRC(byte Chk[], byte Length, byte Start)
{
	byte Result = 0;
	int CK = 0;
	for (int i = Start; i < Length; i++)
	{
		CK += Chk[i];
	}
	Result = (byte)CK;
	return Result;
}

bool State = false;
elapsedMillis BlinkTmr;
elapsedMicros LoopTmr;
byte ReadReset;
uint32_t MaxLoopTime;

void Blink()
{
	if (BlinkTmr > 1000)
	{
		BlinkTmr = 0;
		State = !State;
		digitalWrite(LED_BUILTIN, State);
		Serial.println(".");	// needed to allow PCBsetup to connect

		Serial.print(" Micros: ");
		Serial.print(MaxLoopTime);

		Serial.print(", Temp: ");
		Serial.print(tempmonGetTemp());

		Serial.println("");

		if (ReadReset++ > 10)
		{
			ReadReset = 0;
			MaxLoopTime = 0;
		}
	}
	if (LoopTmr > MaxLoopTime) MaxLoopTime = LoopTmr;
	LoopTmr = 0;
}
