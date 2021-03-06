﻿/******************************************************************************
 * 
 *  Filename: USBDRDAQForm.cs
 *
 *  Description:
 *      Class for application graphical user interface.              
 *       
 *  Copyright © 2012-2017 Pico Technology Ltd. See LICENSE file for terms.
 *
 ******************************************************************************/
 
using System;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace DrDAQRemote
{
    public partial class USBDRDAQForm : Form
    {
        public short handleDAQ1, handleDAQ2, handleDAQ3;

        public USBDRDAQForm()
        {
            InitializeComponent();            
        }

        private void refreshrate_Tick(object sender, EventArgs e)
        {
            StringBuilder xmitStringData = new StringBuilder();
            XmlDocument xmlDAQ = new XmlDocument();
            XmlDocumentType doctype;
            
            doctype = xmlDAQ.CreateDocumentType("DrDaq", null, null, "<!ELEMENT DrDaq ANY>");
            xmlDAQ.AppendChild(doctype);
            //Create the root element and 
            //add it to the document.

            if (runDevice1.Checked)
            {
                deviceXmlBuilder(handleDAQ1, nameDevice1, settingsDevice1, xmitStringData);
                logBox.AppendText(DateTime.Now.ToString() + ": " + xmitStringData.ToString() + "\r\n");
                if (serverActive.Checked)
                {
                    xmlDAQ.LoadXml(xmitStringData.ToString());
                    USBDRDAQXml.PostXMLTransaction("http://" + serverAddr.Text, xmlDAQ);
                    logBox.AppendText("Sent to " + serverAddress.Text + "\r\n");
                }
            }

            xmitStringData.Clear();

            if (runDevice2.Checked)
            {
                deviceXmlBuilder(handleDAQ2, nameDevice2, settingsDevice2, xmitStringData);
                logBox.AppendText(DateTime.Now.ToString() + ": " + xmitStringData.ToString() + "\r\n");
                if (serverActive.Checked)
                {
                    xmlDAQ.LoadXml(xmitStringData.ToString());
                    USBDRDAQXml.PostXMLTransaction("http://" + serverAddr.Text, xmlDAQ);
                    logBox.AppendText("Sent to " + serverAddress.Text + "\r\n");
                }
            }

            xmitStringData.Clear();

            if (runDevice3.Checked)
            {
                deviceXmlBuilder(handleDAQ3, nameDevice3, settingsDevice3, xmitStringData);
                logBox.AppendText(DateTime.Now.ToString() + ": " + xmitStringData.ToString() + "\r\n" );
                if (serverActive.Checked)
                {
                    xmlDAQ.LoadXml(xmitStringData.ToString());
                    USBDRDAQXml.PostXMLTransaction("http://" + serverAddr.Text, xmlDAQ);
                    logBox.AppendText("Sent to " + serverAddress.Text + "\r\n");
                }
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            refreshrate.Interval = 1000 * Convert.ToInt32(numericUpDown1.Value);
        }

        private void openDrDAQ()
        {
            short handleDAQ;
            System.Text.StringBuilder line = new System.Text.StringBuilder(80);
            short requiredSize;

            while (Imports.OpenUnit(out handleDAQ) == 0)
            {
                Imports.GetUnitInfo(handleDAQ, line, 80, out requiredSize, Imports.Info.USBDrDAQ_BATCH_AND_SERIAL);

                if (handleDAQ1 == 0)
                {
                    device1Serial.Text = line.ToString();
                    handleDAQ1 = handleDAQ;
                    runDevice1.Enabled = true;
                    settingsDevice1.Enabled = true;
                    nameDevice1.Enabled = true;
                    logBox.AppendText(nameDevice1.Text + " " + device1Serial.Text + "\r\n");
                }
                else if (handleDAQ2 == 0)
                {
                    device2Serial.Text = line.ToString();
                    handleDAQ2 = handleDAQ;
                    runDevice2.Enabled = true;
                    settingsDevice2.Enabled = true;
                    nameDevice2.Enabled = true;
                    logBox.AppendText(nameDevice2.Text + " " + device2Serial.Text + "\r\n");
                }
                else if (handleDAQ3 == 0)
                {
                    device3Serial.Text = line.ToString();
                    handleDAQ3 = handleDAQ;
                    runDevice3.Enabled = true;
                    settingsDevice3.Enabled = true;
                    nameDevice3.Enabled = true;
                    logBox.AppendText(nameDevice3.Text + " " + device3Serial.Text + "\r\n");
                }

                //if (line.ToString().Equals(device1Serial.Text))
                //{
                //    handleDAQ1 = handleDAQ;
                //}
                //else if (line.ToString().Equals(device2Serial.Text))
                //{
                //    handleDAQ2 = handleDAQ;
                //}
                //else if (line.ToString().Equals(device3Serial.Text))
                //{
                //    handleDAQ3 = handleDAQ;
                //}
            }        
        }

        private void closeDrDAQ()
        {
            Imports.CloseUnit(handleDAQ1);
            Imports.CloseUnit(handleDAQ2);
            Imports.CloseUnit(handleDAQ3);
            handleDAQ1 = 0;
            handleDAQ2 = 0;
            handleDAQ3 = 0;
        }

        private void deviceXmlBuilder(short handleDAQ, TextBox nameDevice, CheckedListBox settingsDevice, StringBuilder xmitData)
        {
            short level = 0;
            ushort overflow = 0;

            uint totalSamples = 200;
            short[] data = new short[totalSamples];
            
            uint triggerIndex = 0;
            
            short isReady = 0;
            uint us_for_block = 100000;
            
            Imports.Inputs[] channels = { Imports.Inputs.USB_DRDAQ_CHANNEL_LIGHT };
            short numChannels = (short) channels.Length;
            uint numSamplesPerChannel = totalSamples / (uint) numChannels;
            uint numSamplesCollected = numSamplesPerChannel; // If collecting data in a loop, reset this value each time as it could be modified in the call to GetValues()

            Imports.SetTrigger(handleDAQ, 0, 0, 0, 0, 0, 0, 0, 0);

            Imports.SetInterval(handleDAQ, ref us_for_block, numSamplesPerChannel, ref channels[0], numChannels);
                       
            Imports.Run(handleDAQ, totalSamples, Imports._BLOCK_METHOD.BM_STREAM);

            while (isReady == 0)
            {
                Imports.Ready(handleDAQ, out isReady);
            }

            Imports.GetValues(handleDAQ, out data[0], ref numSamplesCollected, out overflow, out triggerIndex);

            xmitData.Append("<" + nameDevice.Text + ">");
            if (settingsDevice.GetItemChecked(0))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_LIGHT, out level, out overflow);
                deviceXMLComponent(xmitData, "lightLevel", level);
            }
            if (settingsDevice.GetItemChecked(1))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_TEMP, out level, out overflow);
                if (level < -100)
                    level = -102;
                deviceXMLComponent(xmitData, "tempIn", level);
            }
            if (settingsDevice.GetItemChecked(2))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_MIC_LEVEL, out level, out overflow);
                deviceXMLComponent(xmitData, "soundLevel", level);
            }
            if (settingsDevice.GetItemChecked(3))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_PH, out level, out overflow);
                deviceXMLComponent(xmitData, "phLevel", level);
            }
            if (settingsDevice.GetItemChecked(4))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_SCOPE, out level, out overflow);
                deviceXMLComponent(xmitData, "scopeLevel", level);
            }
            if (settingsDevice.GetItemChecked(5))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_EXT1, out level, out overflow);
                if (level < -100)
                    level = -102;
                deviceXMLComponent(xmitData, "tempExt1", level);
            }
            if (settingsDevice.GetItemChecked(6))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_EXT2, out level, out overflow);
                if (level < -100)
                    level = -102;
                deviceXMLComponent(xmitData, "tempExt2", level);
            }
            if (settingsDevice.GetItemChecked(7))
            {
                Imports.GetSingle(handleDAQ, Imports.Inputs.USB_DRDAQ_CHANNEL_EXT3, out level, out overflow);
                if (level < -100)
                    level = -102;
                deviceXMLComponent(xmitData, "tempExt3", level);
            }
            xmitData.Append("</" + nameDevice.Text + ">");
        }

        private void deviceXMLComponent(StringBuilder xmitDataBuilder, String component, short level)
        {
            xmitDataBuilder.Append("<" + component + ">");
            xmitDataBuilder.Append(level.ToString());
            xmitDataBuilder.Append("</" + component + ">");
        }

        private void USBDRDAQForm_Load(object sender, EventArgs e)
        {

        }

        private void checkBoxRunning_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRunning.Checked)
            {
                openDrDAQ();
            }
            else
            {
                closeDrDAQ();
            }
        }

        private void settingsDevice1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
