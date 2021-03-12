/**************************************************************************
 *
 * Filename: PS5000ABlockForm.cs
 * 
 * Description:
 *   This is a GUI-based program that demonstrates how to use the
 *   PicoScope 5000 Series (ps5000a) driver API functions using .NET
 *   in order to collect a block of data.
 *
 * Supported PicoScope models:
 *
 *		PicoScope 5242A/B & 5442A/B
 *		PicoScope 5243A/B & 5443A/B
 *		PicoScope 5244A/B & 5444A/B
 * 
 * Examples:
 *    Collect a block of samples immediately
 *    Collect a block of samples when a trigger event occurs
 *    Collect a block using ETS
 *    Collect a stream of data immediately
 *    Collect a stream of data when a trigger event occurs
 *    Set Signal Generator, using built in or custom signals
 *    
 * Copyright (C) 2013 - 2017 Pico Technology Ltd. See LICENSE file for terms.   
 *    
 **************************************************************************/

using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using PS5000AImports;
using PicoPinnedArray;
using PicoStatus;

namespace PS5000A
{
    public partial class PS5000ABlockForm : Form
    {
        private short _handle;
        public const int BUFFER_SIZE = 1024;
        public const int MAX_CHANNELS = 4;
        public const int QUAD_SCOPE = 4;
        public const int DUAL_SCOPE = 2;


        uint _timebase = 5;
        short _oversample = 1;
        bool _scaleVoltages = true;

        ushort[] inputRanges = { 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000 };
        bool _ready = false;
        short _trig = 0;
        uint _trigAt = 0;
        int _sampleCount = 0;
        uint _startIndex = 0;
        bool _autoStop;
        //private ChannelSettings[] _channelSettings;
        private int _channelCount;
        private Imports.Range _firstRange;
        private Imports.Range _lastRange;
        private int _digitalPorts;
        private Imports.ps5000aBlockReady _callbackDelegate;
        private string StreamFile = "stream.txt";
        private string BlockFile = "block.txt";

        public PS5000ABlockForm()
        {
            InitializeComponent();

            comboRangeA.DataSource = System.Enum.GetValues(typeof(Imports.Range));
            comboRangeB.DataSource = System.Enum.GetValues(typeof(Imports.Range));
            comboRangeC.DataSource = System.Enum.GetValues(typeof(Imports.Range));
            comboRangeD.DataSource = System.Enum.GetValues(typeof(Imports.Range));
        }

       private void BlockCallback(short handle, short status, IntPtr pVoid)
        {
            // flag to say done reading data
            if (status != (short)StatusCodes.PICO_CANCELLED)
                _ready = true;
        }

       private uint SetTrigger(Imports.TriggerChannelProperties[] channelProperties,
           short nChannelProperties,
           Imports.TriggerConditions[] triggerConditions,
           short nTriggerConditions,
           Imports.ThresholdDirection[] directions,
           uint delay,
           short auxOutputEnabled,
           int autoTriggerMs)
       {
            uint status;

            status = Imports.SetTriggerChannelProperties(_handle, channelProperties, nChannelProperties, auxOutputEnabled,
                                                   autoTriggerMs);
            if (status != StatusCodes.PICO_OK)
            {
                return status;
            }

            status = Imports.SetTriggerChannelConditions(_handle, triggerConditions, nTriggerConditions);

            if (status != StatusCodes.PICO_OK)
            {
                return status;
            }

            if (directions == null)
            {
                directions = new Imports.ThresholdDirection[] { Imports.ThresholdDirection.None,
                                Imports.ThresholdDirection.None, Imports.ThresholdDirection.None, Imports.ThresholdDirection.None,
                                Imports.ThresholdDirection.None, Imports.ThresholdDirection.None};
            }

            status = Imports.SetTriggerChannelDirections(_handle,
                                                               directions[(int)Imports.Channel.ChannelA],
                                                               directions[(int)Imports.Channel.ChannelB],
                                                               directions[(int)Imports.Channel.ChannelC],
                                                               directions[(int)Imports.Channel.ChannelD],
                                                               directions[(int)Imports.Channel.External],
                                                               directions[(int)Imports.Channel.Aux]);
            if (status != StatusCodes.PICO_OK)
            {
                return status;
            }

            status = Imports.SetTriggerDelay(_handle, delay);

            if (status != StatusCodes.PICO_OK)
            {
                return status;
            }

            return status;
       }



        private void buttonOpen_Click(object sender, EventArgs e)
        {
            StringBuilder UnitInfo = new StringBuilder(80);

            short handle;

            string[] description = {
                           "Driver Version    ",
                           "USB Version       ",
                           "Hardware Version  ",
                           "Variant Info      ",
                           "Serial            ",
                           "Cal Date          ",
                           "Kernel Ver        ",
                           "Digital Hardware  ",
                           "Analogue Hardware "
                         };

            Imports.DeviceResolution resolution = Imports.DeviceResolution.PS5000A_DR_16BIT;
            //Imports.DeviceResolution resolution = Imports.DeviceResolution.PS5000A_DR_8BIT;


            if (_handle > 0)
            {
                Imports.CloseUnit(_handle);
                textBoxUnitInfo.Text = "";
                _handle = 0;
                buttonOpen.Text = "Open";
            }
            else
            {
                uint status = Imports.OpenUnit(out handle, null, resolution);

                if (handle > 0) {
                    _handle = handle;

                    if (status == StatusCodes.PICO_POWER_SUPPLY_NOT_CONNECTED || status == StatusCodes.PICO_USB3_0_DEVICE_NON_USB3_0_PORT)
                    {
                        status = Imports.ChangePowerSource(_handle, status);
                    }
                    else if (status != StatusCodes.PICO_OK)
                    {
                        MessageBox.Show("Cannot open device error code: " + status.ToString(), "Error Opening Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                    }
                    else
                    {
                        // Do nothing - power supply connected
                    }

                    textBoxUnitInfo.Text = "Handle            " + _handle.ToString() + "\r\n";

                    for (int i = 0; i < 9; i++)
                    {
                        short requiredSize;
                        Imports.GetUnitInfo(_handle, UnitInfo, 80, out requiredSize, (uint) i);
                        textBoxUnitInfo.AppendText(description[i] + UnitInfo + "\r\n");
                    }
                    buttonOpen.Text = "Close";
                }
            }  
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            uint status;
            status = Imports.SetChannel(_handle, Imports.Channel.ChannelA, 1, Imports.Coupling.PS5000A_DC, Imports.Range.Range_2V, 0);
            status = Imports.SetChannel(_handle, Imports.Channel.ChannelB, 0, Imports.Coupling.PS5000A_DC, Imports.Range.Range_2V, 0);
            status = Imports.SetChannel(_handle, Imports.Channel.ChannelC, 0, Imports.Coupling.PS5000A_DC, Imports.Range.Range_2V, 0);
            status = Imports.SetChannel(_handle, Imports.Channel.ChannelD, 0, Imports.Coupling.PS5000A_DC, Imports.Range.Range_2V, 0);

            short enable = 0;
            uint delay = 0;
            short threshold = 25000;
            short auto = 0;
            Imports.tPS5000ATriggerChannelPropertiesV2[] properties = new Imports.tPS5000ATriggerChannelPropertiesV2[1];

//            status = Imports.SetSimpleTrigger(_handle, enable, Imports.Channel.ChannelA, threshold, Imports.ThresholdDirection.Rising, delay, auto);

            properties[0].thresholdLower = 25000;
            properties[0].thresholdUpper = 25000;
            properties[0].hysteresisLower = 100;
            properties[0].hysteresisUpper = 100;
            properties[0].Channel = Imports.Channel.ChannelA;

            Imports.tPS5000ACondition[] conditions = new Imports.tPS5000ACondition[1];
            conditions[0].source = Imports.Channel.ChannelA;
            conditions[0].condition = Imports.TriggerState.True;

            Imports.tPS5000ADirection[] directions = new Imports.tPS5000ADirection[1];
            directions[0].channel = Imports.Channel.ChannelA;
            directions[0].direction = Imports.ThresholdDirection.Above;
            directions[0].mode = Imports.ThresholdMode.Level;

            status = Imports.SetTriggerChannelConditionsV2(_handle, conditions, 1, Imports.PS5000A_CONDITIONS_INFO.PS5000A_CLEAR);

            status = Imports.SetTriggerChannelPropertiesV2(_handle, properties, 1, 1);

            status = Imports.SetTriggerChannelConditionsV2(_handle, conditions, 1, Imports.PS5000A_CONDITIONS_INFO.PS5000A_ADD);

            status = Imports.SetTriggerChannelDirectionsV2(_handle, directions, 1);

            status = Imports.SetAutoTriggerMicroSeconds(_handle, 0);

            _ready = false;
            _callbackDelegate = BlockCallback;
            _channelCount = 4;
            string data;
            int x;

            textMessage.Clear();
            textData.Clear();

            bool retry;
            uint sampleCount = 1000;
            PinnedArray<short>[] minPinned = new PinnedArray<short>[_channelCount];
            PinnedArray<short>[] maxPinned = new PinnedArray<short>[_channelCount];

            int timeIndisposed;
            short[] databuffer = new short[sampleCount];
            //short[] minBuffers = new short[sampleCount];
            //short[] maxBuffers = new short[sampleCount];
            //minPinned[0] = new PinnedArray<short>(minBuffers);
            //maxPinned[0] = new PinnedArray<short>(maxBuffers);
            //status = Imports.SetDataBuffers(_handle, Imports.Channel.ChannelA, maxBuffers, minBuffers, (int)sampleCount, 0, Imports.RatioMode.None);
            status = Imports.SetDataBuffer(_handle, Imports.Channel.ChannelA, databuffer, (int)sampleCount, 0, Imports.RatioMode.None);
            textMessage.AppendText("BlockData\n");

            /*Find the maximum number of samples and the time interval(in nanoseconds).
            * If the function returns PICO_OK, the timebase will be used.
            */
            int timeInterval;
            int maxSamples;
            while (Imports.GetTimebase(_handle, _timebase, (int)sampleCount, out timeInterval, out maxSamples, 0) != 0)
            {
                textMessage.AppendText("Timebase selection\n");
                _timebase++;

            }
            textMessage.AppendText("Timebase Set\n");

            /* Start it collecting, then wait for completion*/
            _ready = false;
            _callbackDelegate = BlockCallback;

            do
            {
                retry = false;
                status = Imports.RunBlock(_handle, 0, (int)sampleCount, _timebase, out timeIndisposed, 0, _callbackDelegate, IntPtr.Zero);
                if (status == (short)StatusCodes.PICO_POWER_SUPPLY_CONNECTED || status == (short)StatusCodes.PICO_POWER_SUPPLY_NOT_CONNECTED || status == (short)StatusCodes.PICO_POWER_SUPPLY_UNDERVOLTAGE)
                {
                    status = Imports.ChangePowerSource(_handle, status);
                    retry = true;
                }
                else
                {
                    textMessage.AppendText("Run Block Called\n");
                }
            }
            while (retry);

            textMessage.AppendText("Waiting for Data\n");

            while (!_ready)
            {
                Thread.Sleep(100);
            }

            Imports.Stop(_handle);

            if (_ready)
            {
                short overflow;
                status = Imports.GetValues(_handle, 0, ref sampleCount, 1, Imports.RatioMode.None, 0, out overflow);

                if (status == (short)StatusCodes.PICO_OK)
                {
                    textMessage.AppendText("Have Data\n");
                    chartData.Series[0].Points.Clear();

                    for (int I = 0; I < 1000; I++) {
                    chartData.Series[0].Points.AddXY(I, databuffer[I]);
                }
//                    for (x = 0; x < sampleCount; x++)

//                    {
//                       data = maxBuffers[x].ToString();
//                       textData.AppendText(data);
//                       textData.AppendText("\n");
//                    }


                }
                else
                {
                    textMessage.AppendText("No Data\n");

                }
            }
            else
            {
                textMessage.AppendText("data collection aborted\n");
            }

            Imports.Stop(_handle);

                foreach (PinnedArray<short> p in minPinned)
                {
                    if (p != null)
                        p.Dispose();
                }
                foreach (PinnedArray<short> p in maxPinned)
                {
                    if (p != null)
                        p.Dispose();
                }
        }

    }
}

/*    PICO_STATUS setTrigger(unitType* UNIT,
        PS5000A_TRIGGER_CHANNEL_PROPERTIES_V2* channelProperties,
        int16_t nChannelProperties,
        PS5000A_CONDITION* triggerConditions,
        int16_t nTriggerConditions,
        PS5000A_DIRECTION* directions,
        uint16_t nDirections,

struct tPwq * pwq,
uint32_t delay,
uint64_t autoTriggerUs)
{
PICO_STATUS status;
    PS5000A_CONDITIONS_INFO info = PS5000A_CLEAR;
    PS5000A_CONDITIONS_INFO pwqInfo = PS5000A_CLEAR;

    int16_t auxOutputEnabled = 0; // Not used by function call

    status = ps5000aSetTriggerChannelPropertiesV2(UNIT.handle, channelProperties, nChannelProperties, auxOutputEnabled);

if (status != PICO_OK) 
{
    printf("setTrigger:ps5000aSetTriggerChannelPropertiesV2 ------ Ox%08lx \n", status);
    return status;
}

if (nTriggerConditions != 0)
{
    info = (PS5000A_CONDITIONS_INFO) (PS5000A_CLEAR | PS5000A_ADD); // Clear and add trigger condition specified unless no trigger conditions have been specified
}

status = ps5000aSetTriggerChannelConditionsV2(unit->handle, triggerConditions, nTriggerConditions, info);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetTriggerChannelConditionsV2 ------ 0x%08lx \n", status);
return status;
}

status = ps5000aSetTriggerChannelDirectionsV2(unit->handle, directions, nDirections);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetTriggerChannelDirectionsV2 ------ 0x%08lx \n", status);
return status;
}

status = ps5000aSetAutoTriggerMicroSeconds(unit->handle, autoTriggerUs);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetAutoTriggerMicroSeconds ------ 0x%08lx \n", status);
return status;
}

status = ps5000aSetTriggerDelay(unit->handle, delay);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetTriggerDelay ------ 0x%08lx \n", status);
return status;
}

// Clear and add pulse width qualifier condition, clear if no pulse width qualifier has been specified
if (pwq->nPwqConditions != 0)
{
pwqInfo = (PS5000A_CONDITIONS_INFO)(PS5000A_CLEAR | PS5000A_ADD);
}

status = ps5000aSetPulseWidthQualifierConditions(unit->handle, pwq->pwqConditions, pwq->nPwqConditions, pwqInfo);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetPulseWidthQualifierConditions ------ 0x%08lx \n", status);
return status;
}

status = ps5000aSetPulseWidthQualifierDirections(unit->handle, pwq->pwqDirections, pwq->nPwqDirections);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetPulseWidthQualifierDirections ------ 0x%08lx \n", status);
return status;
}

status = ps5000aSetPulseWidthQualifierProperties(unit->handle, pwq->lower, pwq->upper, pwq->type);

if (status != PICO_OK)
{
printf("setTrigger:ps5000aSetPulseWidthQualifierProperties ------ Ox%08lx \n", status);
return status;
}

return status; 
}*/