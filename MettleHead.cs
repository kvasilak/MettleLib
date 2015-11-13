using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;
using System.Globalization;

namespace MettleLib
{
    public class MettleHead
    {

        //Sends an event with the current whole line of data
        public event TagLIneHandeler TagLine;
        public delegate void TagLIneHandeler(string s);

        //Sends an event the the current tag values
        public event TagHandeler TagEvents;
        public delegate void TagHandeler(TagEvent e);

        //Sends an error message
        public event ErrorHandeler TagErrorEvent;
        public delegate void ErrorHandeler(string s);

        private String RXBuffer = string.Empty; 

        private SerialPort TheSerialPort = new SerialPort();

        //recursive function that Walks through the current form and finds all Mettle custom controls
        //then add them to our event handeler
        //this allows us to automatically update them all when a tag is recieved
        private void AddControl(Control ctl)
        {
            //determine if the control is one of our custom ones,
            //our custom controls all implement ITagInterface
            if (ctl is MettleLib.ITagInterface)
            {
                Trace.WriteLine("found, " + ctl.GetType().ToString());

                TagEvents += new TagHandeler(((MettleLib.ITagInterface)ctl).UpdateEvent);
                ((MettleLib.ITagInterface)ctl).Initialize();

            }

            if (ctl is MettleLib.ITagErrorInterface)
            {
                TagErrorEvent += new ErrorHandeler(((MettleLib.ITagErrorInterface)ctl).UpdateEvent);
                ((MettleLib.ITagErrorInterface)ctl).Initialize();
            }

            //recursive call into children
            foreach (Control child in ctl.Controls)
            {
                AddControl(child);
            }
        }

        //Walk through each tab and find all our custom controls
        //then register them for the tag recieved event
        public void FindControlls(Form frmMain)
        {
            foreach (Control c in frmMain.Controls)
            {
                AddControl(c);

                foreach (Control ctl in c.Controls)
                {
                    AddControl(ctl);
                }
                
            }
        }

        public void Close()
        {
            SafeSerialClose();
        }

        //Recursively walk through the controls and reset them
        private void ResetControl(Control ctl)
        {
            //determine if the control is one of our custom ones,
            //our custom controls all implement ITagInterface
            if (ctl is MettleLib.ITagInterface)
            {
                Trace.WriteLine("found, " + ctl.GetType().ToString());
                ((MettleLib.ITagInterface)ctl).Reset();

            }

            if (ctl is MettleLib.ITagErrorInterface)
            {
                ((MettleLib.ITagErrorInterface)ctl).Reset();
            }

            //recursive call into children
            foreach (Control child in ctl.Controls)
            {
                ResetControl(child);
            }
        }

        //clear or reset all Mettle controls
        public void Reset(Form frmMain)
        {
            foreach (Control c in frmMain.Controls)
            {
                ResetControl(c);

                foreach (Control ctl in c.Controls)
                {
                    ResetControl(ctl);
                }
            }
        }

        public bool Open(string port, int baud)
        {
            bool retval = false;

            if (!TheSerialPort.IsOpen)
            {
                try
                {
                    TheSerialPort.PortName = port; // "COM3";  
                    TheSerialPort.BaudRate = baud; // 9600;  
                    TheSerialPort.DataBits = 8;
                    TheSerialPort.Parity = System.IO.Ports.Parity.None;
                    TheSerialPort.StopBits = System.IO.Ports.StopBits.One;
                    //serialPort1.RtsEnable = true;
                    TheSerialPort.Encoding = Encoding.GetEncoding(28591); //So I can read all 8 bits from the stupid serial port
                    TheSerialPort.Open();
                    TheSerialPort.DiscardInBuffer();

                    TheSerialPort.DataReceived += new SerialDataReceivedEventHandler(serialDataReceived);

                    retval = true;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Serial port; " + ex.Message, "Error!");
                }
            }

            return retval;
        }

        // close the serial port in a seperate thread to prevent
        //A GUI deadlock
        private void SafeSerialClose()
        {
            Thread myThread = new System.Threading.Thread(delegate()
            {
                if (TheSerialPort.IsOpen)
                {
                    TheSerialPort.Close();
                }
            });
            myThread.Start();
        }

        //we have recieved serial data, get a line of it and send it to the parser
        private void serialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string temp;
            string Rx;
            int position = 0;

            if (TheSerialPort.IsOpen)
            {
                int cr;
                try
                {
                    //get rx chars
                    RXBuffer += TheSerialPort.ReadExisting();

                    //is there a \n?
                    cr = RXBuffer.IndexOf("\n");

                    //there HAS to be at least 1 character to be at all valid;
                    while (cr >= 0)
                    {
                        //copy all data up to \n
                        //as long as there IS data
                        if (cr > 1)
                        {
                            Rx = RXBuffer.Substring(0, cr);

                            //send message to UI
                            if (null != TagLine)
                                TagLine.Invoke(Rx);

                            //Trace.WriteLine(Rx + "\n");

                            //Process the message
                            try
                            {
                                do //may have multiple tags per line
                                {
                                    position = ParseTags(Rx, position);
                                }
                                while ((position > 0) && (position < Rx.Length));
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine("serial0, " + ex.Message + "\n");
                                Type ep = ex.GetType();
                                return;
                            }
                        }

                        //Copy everything after \n back into rx buffer, removing string just sent
                        int len = RXBuffer.Length - (cr + 1);
                        temp = string.Empty;

                        //Anything left to copy?
                        if (len > 0)
                        {
                            temp = RXBuffer.Substring(cr + 1, len);
                        }

                        RXBuffer = temp;

                        //any more \n?
                        cr = RXBuffer.IndexOf("\n");

                        position = 0;

                    }

                    //stripError.Text = "No Errors";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("serial, " + ex.Message + "\n");
                    Type ep = ex.GetType();
                    return;
                }
            }
        }

        //find the next tag and data, set an event to all listeners
        private int ParseTags(string instr, int offset)
        {
            int start;
            int end = instr.Length;
            int comma;
            int comma2;
            int d;

            try
            {
                //Tag format is >string,string,string<
                start = instr.IndexOf(">", offset);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("serialx, " + ex.Message + "\n");
                Type ep = ex.GetType();
                return end;
            }

            //how to determine we missed a '>'
            if (start >= 0)//May be first character
            {
                try
                {
                    end = instr.IndexOf("<", start + 1);

                    if (end > start + 5)//>a,b,c< absloute minimum valid tag
                    {
                        //found start and end, find the comma
                        comma = instr.IndexOf(",", start + 1);

                        //comma not found look for spaces instead
                        if(comma < 0)
                        {
                            comma = instr.IndexOf(" ", start + 1);
                        }

                        if (comma > 0)
                        {
                            //find the second comma
                            comma2 = instr.IndexOf(",", comma + 1);

                            //second comma not found, look for second space
                            if (comma2 < 0)
                            {
                                comma2 = instr.IndexOf(" ", comma + 1);
                            }

                            //find the second command AND need at last one char between second comma and <
                            if ((comma2 > 0) && ((comma2 + 1) < end))
                            {
                                //set the tag recieved event
                                TagEvent t = new TagEvent();

                                //split the tag and cleanup any whitespace
                                t.ModuleName = instr.Substring(start + 1, comma - (start + 1)).Trim(); //module name
                                t.Name = instr.Substring(comma + 1, comma2 - (comma + 1)).Trim(); //tag name
                                t.Data = instr.Substring(comma2 + 1, end - (comma2 + 1)).Trim(); //data

                                //see if there is a number in the data
                                if (t.Data.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    string valstr = t.Data.Substring(2);
                                    UInt16 val;

                                    //Hex numbers are considered unsigned
                                    if(UInt16.TryParse(valstr, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out val))
                                    {
                                        t.Value = val;
                                        t.ValueValid = true;
                                    }
                                }
                                else
                                {
                                    if (int.TryParse(t.Data, out d))
                                    {
                                        t.Value = d;
                                        t.ValueValid = true;

                                    }
                                }

                                //Trace.WriteLine("Module, " + t.ModuleName + ", ");
                                //Trace.WriteLine("Name, " + t.Name + ", ");
                                //Trace.WriteLine("Data, " + t.Data + "\n");

                                //send the tag to all regestered handelers
                                if (null != TagEvents)
                                    TagEvents.Invoke(t);

                            }
                            else
                            {
                                if (null != TagErrorEvent)
                                    TagErrorEvent(instr.Substring(offset));
                            }
                        }
                        else
                        {
                            //Trace.WriteLine("error1, " + instr.Substring(offset) + "\n");

                            if (null != TagErrorEvent)
                                TagErrorEvent(instr.Substring(offset));
                        }
                    }
                    else
                    {
                        //Trace.WriteLine("error2, " + instr.Substring(offset) + "\n");

                        if (null != TagErrorEvent)
                            TagErrorEvent(instr.Substring(offset));
                    }
                }
                catch (Exception ex)
                {
                    //Trace.WriteLine("tag formatting, " + ex.Message + "\n");
                    if (null != TagErrorEvent)
                        TagErrorEvent("tag format, " + ex.Message);
                }
            }

            return end;
        }

        
    }

    
}

